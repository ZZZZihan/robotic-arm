using System;
using CSharpDemo;

internal static class Axis2MotionTests
{
    private sealed class Fake : IAxis2Card
    {
        public int Reads, Moves, Stops, MoveCode, StopCode, Drift;
        public bool ScaleBad;
        public int DriveState = 0x1637, Motor = 14101;
        public Axis2Snapshot Data = new Axis2Snapshot { Planned = 29978, Encoder = 29978, Status = new int[] { 0xA00, 0xA00, 0xA00, 0xA00 } };
        public int LastTarget;
        public double LastVelocity;
        public Axis2Snapshot ReadSnapshot()
        {
            Reads++;
            return new Axis2Snapshot { Planned = Data.Planned + (Reads > 1 ? Drift : 0), Encoder = Data.Encoder, Status = (int[])Data.Status.Clone() };
        }
        public int ReadObject(short index, short sub, short length)
        {
            if (index == 0x2000) return Motor;
            if (index == 0x6091) return 1;
            if (index == 0x6061) return 8;
            if (index == 0x603F) return 0;
            if (index == 0x6041) return DriveState;
            if (index == 0x2002) return 100;
            if (index == 0x6064) return Data.Encoder;
            throw new Exception("unexpected object");
        }
        public void CheckScale() { if (ScaleBad) throw new Exception("scale mismatch"); }
        public int Move(int target, double velocity) { Moves++; LastTarget = target; LastVelocity = velocity; return MoveCode; }
        public int Stop() { Stops++; return StopCode; }
    }

    private static void Assert(bool value, string message) { if (!value) throw new Exception(message); }
    private static void Throws(Action action)
    {
        bool thrown = false;
        try { action(); } catch { thrown = true; }
        Assert(thrown, "expected refusal");
    }
    private static int passed;
    private static void Test(string name, Action test)
    {
        try { test(); passed++; Console.WriteLine("PASS " + name); }
        catch (Exception error) { Console.Error.WriteLine("FAIL " + name + ": " + error.Message); Environment.Exit(1); }
    }

    private static int Main()
    {
        Test("120rpm equals 2 motor rev/s; bounded absolute target", delegate {
            Fake f = new Fake(); Axis2Motion m = new Axis2Motion(f);
            Axis2Plan p = m.Start(2M, 120M, 1, 1000);
            Assert(f.Moves == 1 && f.LastTarget == 554266, "wrong target or write count");
            Assert(Math.Abs(f.LastVelocity - 524.288) < 1e-10, "wrong pulse/ms conversion");
            Assert(p.EstimatedSeconds > 1, "acceleration omitted");
        });
        Test("negative relative step and next move use fresh position", delegate {
            Fake f = new Fake(); Axis2Motion m = new Axis2Motion(f);
            Axis2Plan p = m.Start(1M, 120M, -1, 1000);
            f.Data.Planned = f.Data.Encoder = p.Target;
            Assert(m.Observe(f.Data), "first planner did not end");
            int first = p.Target;
            p = m.Start(1M, 120M, -1, 1000);
            Assert(p.Target == first - 262144, "repeated fixed absolute target");
        });
        Test("zero/negative/excessive turns and rpm rejected before reads", delegate {
            Fake f = new Fake(); Axis2Motion m = new Axis2Motion(f);
            Throws(delegate { m.Start(0M, 120M, 1, 1000); });
            Throws(delegate { m.Start(-1M, 120M, 1, 1000); });
            Throws(delegate { m.Start(11M, 120M, 1, 1000); });
            Throws(delegate { m.Start(1M, 121M, 1, 1000); });
            Throws(delegate { m.Start(1M, 0M, 1, 1000); });
            Throws(delegate { m.Start(1M, 120M, 0, 1000); });
            Assert(f.Reads == 0 && f.Moves == 0, "invalid input touched card");
        });
        Test("repeat click during active move cannot queue another command", delegate {
            Fake f = new Fake(); Axis2Motion m = new Axis2Motion(f); m.Start(1M, 120M, 1, 1000);
            Throws(delegate { m.Start(1M, 120M, 1, 1000); });
            Assert(f.Moves == 1, "duplicate write");
        });
        Test("axis disabled refuses without auto-enable", delegate {
            Fake f = new Fake(); f.Data.Status[1] = 0; Axis2Motion m = new Axis2Motion(f);
            Throws(delegate { m.Start(1M, 120M, 1, 1000); }); Assert(f.Moves == 0, "disabled write");
        });
        Test("every limit/alarm/estop/running/homing bit refuses", delegate {
            foreach (int bit in new int[] { 1,2,4,8,16,32,64,256,1024,4096 })
            {
                Fake f = new Fake(); f.Data.Status[1] |= bit; Axis2Motion m = new Axis2Motion(f);
                Throws(delegate { m.Start(1M, 120M, 1, 1000); }); Assert(f.Moves == 0, "hazard write");
            }
        });
        Test("other axes running refuses", delegate {
            Fake f = new Fake(); f.Data.Status[0] |= 1024; Axis2Motion m = new Axis2Motion(f);
            Throws(delegate { m.Start(1M, 120M, 1, 1000); }); Assert(f.Moves == 0, "other axis active");
        });
        Test("drive not operation-enabled refuses", delegate {
            Fake f = new Fake(); f.DriveState = 0x1633; Axis2Motion m = new Axis2Motion(f);
            Throws(delegate { m.Start(1M, 120M, 1, 1000); }); Assert(f.Moves == 0, "drive disabled write");
        });
        Test("scaling or motor change refuses", delegate {
            Fake f = new Fake(); f.ScaleBad = true; Axis2Motion m = new Axis2Motion(f);
            Throws(delegate { m.Start(1M, 120M, 1, 1000); }); Assert(f.Moves == 0, "scale write");
            f.ScaleBad = false; f.Motor = 999;
            Throws(delegate { m.Start(1M, 120M, 1, 1000); }); Assert(f.Moves == 0, "encoder write");
        });
        Test("brake settling and preflight drift refuse", delegate {
            Fake f = new Fake(); Axis2Motion m = new Axis2Motion(f);
            Throws(delegate { m.Start(1M, 120M, 1, 20); }); Assert(f.Moves == 0, "brake wait skipped");
            f.Reads = 0; f.Drift = 20;
            Throws(delegate { m.Start(1M, 120M, 1, 1000); }); Assert(f.Moves == 0, "drift write");
        });
        Test("32bit target overflow rejected at both boundaries", delegate {
            Throws(delegate { Axis2Motion.MakePlan(Int32.MaxValue, Int32.MaxValue, 1M, 120M, 1); });
            Throws(delegate { Axis2Motion.MakePlan(Int32.MinValue, Int32.MinValue, 1M, 120M, -1); });
        });
        Test("failed motion command attempts stop and retains active until stopped", delegate {
            Fake f = new Fake(); f.MoveCode = -7; Axis2Motion m = new Axis2Motion(f);
            Throws(delegate { m.Start(1M, 120M, 1, 1000); });
            Assert(f.Stops == 1 && m.Active && m.StopRequested, "uncertain write was cleared");
            Assert(m.Observe(f.Data) && !m.Active, "stopped feedback did not release lock");
        });
        Test("failed stop cannot falsely report stopped", delegate {
            Fake f = new Fake(); Axis2Motion m = new Axis2Motion(f); m.Start(1M, 120M, 1, 1000);
            f.StopCode = -7; f.Data.Status[1] |= 1024;
            Throws(delegate { m.Stop(); });
            Assert(m.Active && !m.Observe(f.Data), "failed stop released lock");
        });
        Test("manual stop releases only on stopped feedback; next step uses stopped position", delegate {
            Fake f = new Fake(); Axis2Motion m = new Axis2Motion(f); m.Start(4M, 60M, 1, 1000);
            f.Data.Status[1] = 0x600; f.Data.Planned = 90000; f.Data.Encoder = 87000;
            m.Stop();
            Assert(m.Active && !m.Observe(f.Data), "stop return was mistaken for stopped feedback");
            Throws(delegate { m.Start(1M, 60M, 1, 1000); });
            f.Data.Status[1] = 0xA00; f.Data.Planned = f.Data.Encoder = 95000;
            Assert(m.Observe(f.Data) && !m.Active, "manual stop left permanent lock");
            Axis2Plan next = m.Start(1M, 60M, -1, 1000);
            Assert(f.Stops == 1 && f.Moves == 2 && next.Target == 95000 - 262144 && !m.StopRequested,
                "next command reused interrupted target or sticky stop state");
        });
        Test("external all-axis stop waits for feedback and releases interrupted target", delegate {
            Fake f = new Fake(); Axis2Motion m = new Axis2Motion(f); m.Start(4M, 60M, 1, 1000);
            f.Data.Status[1] = 0x600; f.Data.Planned = f.Data.Encoder = 95000;
            m.RecordExternalStopRequest();
            Assert(m.Active && m.StopRequested && f.Stops == 0 && !m.Observe(f.Data), "external stop falsely cleared active or issued another SDK stop");
            f.Data.Status[1] = 0xA00;
            Assert(m.Observe(f.Data) && !m.Active, "external stop retained interrupted target lock");
        });
        Test("stop when idle does not block a later explicit move", delegate {
            Fake f = new Fake(); Axis2Motion m = new Axis2Motion(f); m.Stop();
            Assert(!m.Active && f.Moves == 0, "idle stop starts or locks motion");
            m.Start(1M, 60M, 1, 1000);
            Assert(f.Moves == 1 && !m.StopRequested, "idle stop blocked restart");
        });
        Test("stop does not bypass disabled or following-error checks on restart", delegate {
            Fake f = new Fake(); Axis2Motion m = new Axis2Motion(f); m.Start(1M, 60M, 1, 1000); m.Stop();
            f.Data.Status[1] = 0x800; Assert(m.Observe(f.Data), "stopped planner not released");
            Throws(delegate { m.Start(1M, 60M, 1, 1000); });
            f.Data.Status[1] = 0xA00; f.Data.Encoder -= 101;
            Throws(delegate { m.Start(1M, 60M, 1, 1000); });
            Assert(f.Moves == 1, "restart bypassed feedback or enable gate");
        });
        Test("idle but wrong target is not completion", delegate {
            Fake f = new Fake(); Axis2Motion m = new Axis2Motion(f); m.Start(1M, 120M, 1, 1000);
            Assert(!m.Observe(f.Data) && m.Active, "accepted stale idle sample");
        });
        Test("runtime fault and bad feedback are surfaced", delegate {
            Fake f = new Fake(); Axis2Motion m = new Axis2Motion(f); m.Start(1M, 120M, 1, 1000);
            f.Data.Status[1] |= 2; Throws(delegate { m.Observe(f.Data); });
            Throws(delegate { m.Observe(null); }); Assert(m.Active, "fault lost pending move");
        });
        Test("CoE abort is invalid even when SDK returns zero", delegate {
            Assert(Axis2Motion.IsCoeAbort(0x06090011), "missed CoE abort");
            Assert(!Axis2Motion.IsCoeAbort(-42520) && !Axis2Motion.IsCoeAbort(29978), "normal position rejected");
        });
        Console.WriteLine("PASS " + passed + " behavior tests; fake adapter only, no SDK loaded.");
        return 0;
    }
}
