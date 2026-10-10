using System;
using System.Collections.Generic;
using CSharpDemo;

internal static class OperationSafetyTests
{
    private sealed class Fake : IOperationSafetyCard
    {
        public readonly List<string> Calls = new List<string>();
        public OperationSnapshot Data = Snapshot(0x800);
        public short Count = 4, Step = 0;
        public string FailApi;
        public Action BeforeStop, BeforeInitialize, BeforeEnable;
        private void Call(string api)
        {
            Calls.Add(api);
            if (api == FailApi) throw new InvalidOperationException("fake " + api + " failed");
        }
        public void Open() { Call("Open"); }
        public OperationSnapshot ReadSnapshot() { Call("ReadSnapshot"); return Data; }
        public short ReadSlaveCount() { Call("ReadSlaveCount"); return Count; }
        public short ReadInitStep() { Call("ReadInitStep"); return Step; }
        public void InitializeBus() { if (BeforeInitialize != null) BeforeInitialize(); Call("InitializeBus"); }
        public void EnableAxis(short axis) { if (BeforeEnable != null) BeforeEnable(); Call("EnableAxis:" + axis); }
        public void StopAll() { if (BeforeStop != null) BeforeStop(); Call("StopAll"); }
        public int CountCalls(string api) { return Calls.FindAll(delegate(string value) { return value == api; }).Count; }
    }

    private static OperationSnapshot Snapshot(int status)
    {
        return new OperationSnapshot { Status = new int[] { status, status, status, status },
            Planned = new int[] { 100, 200, 300, 400 }, Encoder = new int[] { 100, 200, 300, 400 } };
    }
    private static OperationSafety Ready(Fake card)
    {
        OperationSafety safety = new OperationSafety(card); safety.BeginInitialization(0);
        Assert(safety.BusReady, "expected existing bus ready"); return safety;
    }
    private static void Enabled(OperationSafety safety, Fake card, int axis, long nowMs)
    {
        card.Data.Status[axis - 1] |= OperationSafety.EnabledBit;
        safety.Observe(card.Data, nowMs);
    }
    private static void Assert(bool value, string message) { if (!value) throw new Exception(message); }
    private static void Refuses(Action action)
    {
        try { action(); } catch (InvalidOperationException) { return; }
        throw new Exception("expected refusal");
    }
    private static int passed;
    private static void Test(string name, Action action)
    {
        try { action(); passed++; Console.WriteLine("PASS SAFETY " + name); }
        catch (Exception error) { Console.Error.WriteLine("FAIL " + name + ": " + error.Message); Environment.Exit(1); }
    }

    private static int Main()
    {
        Test("startup reuses four-slave bus without enable or stop", delegate {
            Fake f = new Fake(); OperationSafety s = Ready(f);
            Assert(f.Calls.Count == 4 && f.Calls[0] == "Open" && s.AuthorizedAxis == 0, "unexpected startup calls");
            Assert(s.CardOpened && !s.EmergencyLatched && !s.StopRequestSucceeded, "unexpected startup state");
            Refuses(delegate { s.AssertCanMove(2, 0); });
        });
        Test("zero-slave startup initializes exactly once and never enables", delegate {
            Fake f = new Fake(); f.Count = 0; f.Step = -1; OperationSafety s = new OperationSafety(f);
            s.BeginInitialization(0); s.PollInitialization(100); s.PollInitialization(500);
            Assert(s.Phase == OperationInitializationPhase.Waiting && f.CountCalls("InitializeBus") == 1, "wrong init count");
            Assert(s.AuthorizedAxis == 0 && f.CountCalls("StopAll") == 0 && !f.Calls.Contains("EnableAxis:2"), "startup actuated axis");
        });
        Test("initialization cannot finish before one second", delegate {
            Fake f = new Fake(); f.Count = 0; OperationSafety s = new OperationSafety(f); s.BeginInitialization(10);
            f.Count = 4; s.PollInitialization(1009);
            Assert(!s.BusReady, "early zero init field was trusted");
            s.PollInitialization(1010); Assert(s.BusReady && f.CountCalls("InitializeBus") == 1, "completion refused");
        });
        Test("startup cannot reopen or reinitialize after any result", delegate {
            Fake f = new Fake(); OperationSafety s = Ready(f); Refuses(delegate { s.BeginInitialization(1); });
            Assert(f.CountCalls("Open") == 1, "duplicate open");
            f = new Fake(); f.FailApi = "Open"; s = new OperationSafety(f); s.BeginInitialization(0);
            Refuses(delegate { s.BeginInitialization(1); }); Assert(f.CountCalls("Open") == 1 && !s.CardOpened, "failed open was retried");
        });
        Test("all startup adapter failures are visible and do not auto retry", delegate {
            foreach (string api in new string[] { "Open", "ReadSnapshot", "ReadSlaveCount", "ReadInitStep", "InitializeBus" })
            {
                Fake f = new Fake(); f.Count = 0; f.FailApi = api; OperationSafety s = new OperationSafety(f);
                s.BeginInitialization(0); int count = f.Calls.Count; s.PollInitialization(500);
                Assert(s.Phase == OperationInitializationPhase.Failed && s.Error.Length > 0 && f.Calls.Count == count, "failure hidden or retried: " + api);
            }
        });
        Test("partial or excessive startup topology is not overwritten", delegate {
            foreach (short count in new short[] { -1, 1, 2, 3, 5 })
            {
                Fake f = new Fake(); f.Count = count; OperationSafety s = new OperationSafety(f); s.BeginInitialization(0);
                Assert(s.Phase == OperationInitializationPhase.Failed && f.CountCalls("InitializeBus") == 0, "overwritten topology");
            }
        });
        Test("existing bus with incomplete init step is not reinitialized", delegate {
            Fake f = new Fake(); f.Step = -1; OperationSafety s = new OperationSafety(f); s.BeginInitialization(0);
            Assert(s.Phase == OperationInitializationPhase.Failed && f.CountCalls("InitializeBus") == 0, "unsafe reinit");
        });
        Test("zero-slave enabled running homing or hazardous axis blocks initialization", delegate {
            foreach (int bit in new int[] { 1, 2, 4, 8, 16, 32, 64, 256, 512, 1024, 4096 })
            {
                Fake f = new Fake(); f.Count = 0; f.Data.Status[3] |= bit; OperationSafety s = new OperationSafety(f); s.BeginInitialization(0);
                Assert(s.Phase == OperationInitializationPhase.Failed && f.CountCalls("InitializeBus") == 0, "unsafe init bit " + bit);
            }
        });
        Test("existing enabled idle bus carries no session permission", delegate {
            Fake f = new Fake(); f.Data = Snapshot(0xA00); OperationSafety s = Ready(f);
            Refuses(delegate { s.AssertCanMove(2, 0); }); Assert(f.CountCalls("InitializeBus") == 0 && s.AuthorizedAxis == 0, "inherited permission");
        });
        Test("existing bus with running or hazardous axis refuses readiness", delegate {
            foreach (int bit in new int[] { 1, 2, 1024, 4096 })
            {
                Fake f = new Fake(); f.Data.Status[0] |= bit; OperationSafety s = new OperationSafety(f); s.BeginInitialization(0);
                Assert(s.Phase == OperationInitializationPhase.Failed && f.CountCalls("InitializeBus") == 0, "unsafe ready");
            }
        });
        Test("initialization timeout holds failed and cannot retry", delegate {
            Fake f = new Fake(); f.Count = 0; OperationSafety s = new OperationSafety(f); s.BeginInitialization(0);
            s.PollInitialization(15001); int count = f.Calls.Count; s.PollInitialization(16000);
            Assert(s.Phase == OperationInitializationPhase.Failed && s.Error.Contains("15") && count == f.Calls.Count, "timeout retried");
        });
        Test("initialization completion requires fresh disabled hazard-free stopped axes", delegate {
            foreach (int bit in new int[] { 1, 2, 512, 1024, 4096 })
            {
                Fake f = new Fake(); f.Count = 0; OperationSafety s = new OperationSafety(f); s.BeginInitialization(0);
                f.Count = 4; f.Data.Status[1] |= bit; s.PollInitialization(1000);
                Assert(s.Phase == OperationInitializationPhase.Failed, "unsafe init completion");
            }
        });
        Test("partial topology can be observed during bounded initialization", delegate {
            Fake f = new Fake(); f.Count = 0; OperationSafety s = new OperationSafety(f); s.BeginInitialization(0);
            f.Count = 2; f.Step = 2; s.PollInitialization(1000); Assert(s.Phase == OperationInitializationPhase.Waiting, "partial progress misclassified");
            f.Count = 4; f.Step = 0; s.PollInitialization(2000); Assert(s.BusReady, "valid completion refused");
        });
        Test("initialization poll errors and malformed snapshots fail closed", delegate {
            foreach (string api in new string[] { "ReadSnapshot", "ReadSlaveCount", "ReadInitStep" })
            {
                Fake f = new Fake(); f.Count = 0; OperationSafety s = new OperationSafety(f); s.BeginInitialization(0);
                f.FailApi = api; s.PollInitialization(1000); Assert(s.Phase == OperationInitializationPhase.Failed, "poll failure hidden");
            }
            Fake malformed = new Fake(); malformed.Data.Encoder = new int[3]; OperationSafety bad = new OperationSafety(malformed); bad.BeginInitialization(0);
            Assert(bad.Phase == OperationInitializationPhase.Failed && !bad.BusReady, "malformed startup trusted");
        });
        Test("manual insurance enables only selected axis and waits for new feedback", delegate {
            Fake f = new Fake(); OperationSafety s = Ready(f); s.AuthorizeAxis(2, 10);
            Assert(s.AuthorizedAxis == 2 && s.AwaitingEnableFeedback && f.CountCalls("EnableAxis:2") == 1, "wrong manual enable");
            Refuses(delegate { s.AssertCanMove(2, 10); }); Enabled(s, f, 2, 11); s.AssertCanMove(2, 11);
            Refuses(delegate { s.AssertCanMove(1, 11); }); Assert(!f.Calls.Contains("EnableAxis:1"), "enabled wrong axis");
        });
        Test("pre-click enabled feedback cannot immediately open insurance", delegate {
            Fake f = new Fake(); f.Data = Snapshot(0xA00); OperationSafety s = Ready(f); s.AuthorizeAxis(3, 10);
            Refuses(delegate { s.AssertCanMove(3, 10); }); s.Observe(f.Data, 11); s.AssertCanMove(3, 11);
        });
        Test("pending enable waits without duplicate writes and expires", delegate {
            Fake f = new Fake(); OperationSafety s = Ready(f); s.AuthorizeAxis(2, 10); s.Observe(f.Data, 500);
            Refuses(delegate { s.AuthorizeAxis(2, 500); }); Refuses(delegate { s.AssertCanMove(2, 500); });
            Assert(s.AwaitingEnableFeedback && f.CountCalls("EnableAxis:2") == 1, "pending duplicate enable");
            s.Observe(f.Data, 1011); Assert(s.AuthorizedAxis == 0 && !s.AwaitingEnableFeedback, "pending never expired");
        });
        Test("stale feedback blocks manual enable and movement", delegate {
            Fake f = new Fake(); OperationSafety s = Ready(f); Refuses(delegate { s.AuthorizeAxis(2, 501); });
            Assert(f.CountCalls("EnableAxis:2") == 0, "stale enable write");
            s.AuthorizeAxis(2, 10); Enabled(s, f, 2, 11); Refuses(delegate { s.AssertCanMove(2, 512); });
        });
        Test("every hazard or running bit blocks all motion and manual enable", delegate {
            foreach (int bit in new int[] { 1, 2, 4, 8, 16, 32, 64, 256, 1024, 4096 })
            {
                Fake f = new Fake(); OperationSafety s = Ready(f); f.Data.Status[3] |= bit; s.Observe(f.Data, 10);
                Refuses(delegate { s.AuthorizeAxis(2, 10); }); Assert(f.CountCalls("EnableAxis:2") == 0, "hazard enable write");
            }
        });
        Test("following difference over 100 blocks selected axis", delegate {
            Fake f = new Fake(); OperationSafety s = Ready(f); f.Data.Encoder[1] -= 101; s.Observe(f.Data, 10);
            Refuses(delegate { s.AuthorizeAxis(2, 10); }); f.Data.Encoder[1]++; s.Observe(f.Data, 11);
            s.AuthorizeAxis(2, 11); Enabled(s, f, 2, 12); s.AssertCanMove(2, 12);
        });
        Test("lost enable or any hazardous axis revokes session permission", delegate {
            Fake f = new Fake(); OperationSafety s = Ready(f); s.AuthorizeAxis(2, 10); Enabled(s, f, 2, 11);
            f.Data.Status[1] &= ~OperationSafety.EnabledBit; s.Observe(f.Data, 12); Assert(s.AuthorizedAxis == 0, "lost enable retained permission");
            s.AuthorizeAxis(2, 13); Enabled(s, f, 2, 14); f.Data.Status[0] |= 2; s.Observe(f.Data, 15);
            Assert(s.AuthorizedAxis == 0, "other-axis alarm retained permission");
        });
        Test("running feedback retains permission but prevents another command", delegate {
            Fake f = new Fake(); OperationSafety s = Ready(f); s.AuthorizeAxis(2, 10); Enabled(s, f, 2, 11);
            f.Data.Status[1] |= 1024; s.Observe(f.Data, 12); Refuses(delegate { s.AssertCanMove(2, 12); });
            Assert(s.AuthorizedAxis == 2, "normal ongoing motion revoked permission");
        });
        Test("snapshot arrays are copied and malformed observe invalidates", delegate {
            Fake f = new Fake(); OperationSafety s = Ready(f); s.AuthorizeAxis(2, 10); Enabled(s, f, 2, 11);
            f.Data.Status[1] = 0; f.Data.Encoder[1] = Int32.MinValue; s.AssertCanMove(2, 11);
            Refuses(delegate { s.Observe(new OperationSnapshot(), 12); });
            Assert(s.AuthorizedAxis == 0, "malformed sample retained permission"); Refuses(delegate { s.AssertCanMove(2, 12); });
        });
        Test("invalid axis and unready bus never write enable", delegate {
            Fake f = new Fake(); OperationSafety s = new OperationSafety(f);
            Refuses(delegate { s.AuthorizeAxis(2, 0); }); Refuses(delegate { s.AuthorizeAxis(0, 0); }); Refuses(delegate { s.AuthorizeAxis(5, 0); });
            Assert(f.Calls.Count == 0, "unready or invalid axis touched card");
        });
        Test("enable failure cannot authorize an axis", delegate {
            Fake f = new Fake(); OperationSafety s = Ready(f); f.FailApi = "EnableAxis:2";
            Refuses(delegate { s.AuthorizeAxis(2, 10); }); Assert(s.AuthorizedAxis == 0 && s.Error.Length > 0, "failed enable authorized");
        });
        Test("emergency locks before stop adapter and repeated failures can retry", delegate {
            Fake f = new Fake(); OperationSafety s = Ready(f); s.AuthorizeAxis(2, 10); Enabled(s, f, 2, 11);
            f.BeforeStop = delegate { Assert(s.EmergencyLatched && s.AuthorizedAxis == 0 && !s.StopRequestSucceeded, "stop called before locking"); };
            f.FailApi = "StopAll"; s.EmergencyStop();
            Assert(s.EmergencyLatched && !s.StopRequestSucceeded && s.Error.Length > 0, "stop failure hidden");
            Refuses(delegate { s.AuthorizeAxis(2, 12); }); Assert(f.CountCalls("EnableAxis:2") == 1, "failed stop recovered");
            f.FailApi = null; s.EmergencyStop(); Assert(s.StopRequestSucceeded && s.EmergencyLatched && f.CountCalls("StopAll") == 2, "stop retry failed");
        });
        Test("emergency attempts stop even after stale invalid or failed initialization", delegate {
            Fake f = new Fake(); f.FailApi = "ReadSnapshot"; OperationSafety s = new OperationSafety(f); s.BeginInitialization(0); s.Invalidate(); s.EmergencyStop();
            Assert(s.EmergencyLatched && s.StopRequestSucceeded && f.CountCalls("StopAll") == 1 && !s.BusReady, "failed phase blocked stop");
        });
        Test("successful emergency is a locked request with no automatic recovery", delegate {
            Fake f = new Fake(); OperationSafety s = Ready(f); s.EmergencyStop(); s.Observe(f.Data, 100); s.PollInitialization(100);
            Assert(s.EmergencyLatched && s.AuthorizedAxis == 0 && s.StopRequestSucceeded, "background sample cleared latch");
            s.Invalidate(); Assert(s.BusReady && s.EmergencyLatched && s.AuthorizedAxis == 0, "invalidate changed ready phase or latch");
        });
        Test("emergency recovery requires stationary fresh feedback then bus revalidation", delegate {
            Fake f = new Fake(); OperationSafety s = Ready(f); s.EmergencyStop();
            f.Data.Status[1] |= 1024; s.Observe(f.Data, 10); Refuses(delegate { s.AuthorizeAxis(2, 10); });
            f.Data.Status[1] &= ~1024; s.Observe(f.Data, 11); f.Count = 3; Refuses(delegate { s.AuthorizeAxis(2, 11); });
            Assert(s.EmergencyLatched && f.CountCalls("EnableAxis:2") == 0, "unsafe emergency recovery");
            f.Count = 4; f.Step = 0; s.AuthorizeAxis(2, 12);
            Assert(!s.EmergencyLatched && s.AwaitingEnableFeedback && f.CountCalls("EnableAxis:2") == 1, "manual recovery failed");
            Enabled(s, f, 2, 13); s.AssertCanMove(2, 13);
            Assert(f.CountCalls("InitializeBus") == 0, "emergency recovery reset bus");
        });
        Test("recovery read or enable failure keeps emergency latch", delegate {
            foreach (string api in new string[] { "ReadSnapshot", "ReadSlaveCount", "ReadInitStep", "EnableAxis:2" })
            {
                Fake f = new Fake(); OperationSafety s = Ready(f); s.EmergencyStop(); f.FailApi = api;
                Refuses(delegate { s.AuthorizeAxis(2, 10); }); Assert(s.EmergencyLatched && s.AuthorizedAxis == 0, "failed recovery cleared latch");
            }
        });
        Test("insurance moves to explicitly selected axis only", delegate {
            Fake f = new Fake(); OperationSafety s = Ready(f); s.AuthorizeAxis(2, 10); Enabled(s, f, 2, 11);
            s.AuthorizeAxis(3, 12); Enabled(s, f, 3, 13); s.AssertCanMove(3, 13);
            Refuses(delegate { s.AssertCanMove(2, 13); }); Assert(s.AuthorizedAxis == 3 && f.CountCalls("EnableAxis:3") == 1, "axis permission accumulated");
        });
        Test("ordinary insurance revalidates topology and reads snapshot last", delegate {
            Fake f = new Fake(); OperationSafety s = Ready(f); f.Count = 3;
            Refuses(delegate { s.AuthorizeAxis(2, 10); }); Assert(f.CountCalls("EnableAxis:2") == 0, "stale bus readiness enabled axis");
            f.Count = 4; f.Step = 1; Refuses(delegate { s.AuthorizeAxis(2, 11); });
            f.Step = 0; s.AuthorizeAxis(2, 12);
            Assert(f.Calls[f.Calls.Count - 2] == "ReadSnapshot" && f.Calls[f.Calls.Count - 1] == "EnableAxis:2", "snapshot was not final pre-enable read");
        });
        Test("SDK duration does not consume initialization or enable settling wait", delegate {
            Fake f = new Fake(); f.Count = 0; long clock = 0;
            f.BeforeInitialize = delegate { clock = 1500; };
            OperationSafety s = new OperationSafety(f, delegate { return clock; }); s.BeginInitialization(0);
            f.Count = 4; clock = 2499; s.PollInitialization(clock); Assert(!s.BusReady, "init wait started before SDK returned");
            clock = 2500; s.PollInitialization(clock); Assert(s.BusReady, "completed init refused");
            f.BeforeEnable = delegate { clock = 2900; }; s.AuthorizeAxis(2, 2500);
            clock = 3400; s.Observe(f.Data, clock);
            Assert(s.AwaitingEnableFeedback && s.AuthorizedAxis == 2, "enable call latency consumed settling wait");
            Enabled(s, f, 2, 3401); s.AssertCanMove(2, 3401);
        });
        Console.WriteLine("PASS SAFETY " + passed + " operation safety behavior tests; fake adapter only, no SDK loaded.");
        return 0;
    }
}
