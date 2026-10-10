using System;

namespace CSharpDemo
{
    internal sealed class OperationSnapshot
    {
        public int[] Status;
        public int[] Planned;
        public int[] Encoder;
    }

    internal interface IOperationSafetyCard
    {
        void Open();
        OperationSnapshot ReadSnapshot();
        short ReadSlaveCount();
        short ReadInitStep();
        void InitializeBus();
        void EnableAxis(short axis);
        void StopAll();
    }

    internal enum OperationInitializationPhase { NotStarted, Waiting, Ready, Failed }

    // Session permission is separate from the drive's persistent enable bit.
    // This class never moves, clears controller faults or disables drive torque.
    internal sealed class OperationSafety
    {
        public const int HazardBits = 0x17F;
        public const int RunningBits = 0x1400;
        public const int EnabledBit = 0x200;
        public const long FeedbackMaxAgeMs = 500;
        public const long InitializationTimeoutMs = 15000;
        public const long InitializationMinimumWaitMs = 1000;
        public const long EnableFeedbackTimeoutMs = 1000;
        private readonly IOperationSafetyCard card;
        private readonly Func<long> monotonicClock;
        private OperationSnapshot latest;
        private long observedAtMs, initializationStartedAtMs, authorizationStartedAtMs;
        private long sampleSequence, authorizationSampleSequence;

        public int AuthorizedAxis { get; private set; }
        public bool AwaitingEnableFeedback { get; private set; }
        public bool EmergencyLatched { get; private set; }
        public bool StopRequestSucceeded { get; private set; }
        public bool CardOpened { get; private set; }
        public bool BusReady { get { return Phase == OperationInitializationPhase.Ready; } }
        public OperationInitializationPhase Phase { get; private set; }
        public string Error { get; private set; }

        public OperationSafety(IOperationSafetyCard card) : this(card, null) { }

        public OperationSafety(IOperationSafetyCard card, Func<long> monotonicClock)
        {
            if (card == null) throw new ArgumentNullException("card");
            this.card = card;
            this.monotonicClock = monotonicClock;
            Phase = OperationInitializationPhase.NotStarted;
            Error = String.Empty;
        }

        public void BeginInitialization(long nowMs)
        {
            if (Phase != OperationInitializationPhase.NotStarted)
                throw new InvalidOperationException("本次启动已尝试初始化，不会重复打开控制卡或初始化总线。");
            // Advance before the first adapter call so even a failed Open cannot retry.
            Phase = OperationInitializationPhase.Waiting;
            initializationStartedAtMs = nowMs;
            RevokeAuthorization();
            try
            {
                card.Open();
                CardOpened = true;
                short count = card.ReadSlaveCount();
                short step = card.ReadInitStep();
                Observe(card.ReadSnapshot(), ReadNow(nowMs));
                EnsureIdle(latest, false);
                if (count == 4 && step == 0)
                {
                    Phase = OperationInitializationPhase.Ready;
                    Error = String.Empty;
                    return;
                }
                if (count != 0)
                    throw new InvalidOperationException("当前总线从站数为 " + count + "、初始化站号为 " + step + "；拒绝重复初始化或覆盖异常总线。");
                EnsureIdle(latest, true);
                card.InitializeBus();
                initializationStartedAtMs = ReadNow(nowMs);
                Error = String.Empty;
            }
            catch (Exception error) { Fail(error); }
        }

        public void PollInitialization(long nowMs)
        {
            if (Phase != OperationInitializationPhase.Waiting) return;
            try
            {
                long age = ReadNow(nowMs) - initializationStartedAtMs;
                if (age < 0 || age > InitializationTimeoutMs)
                    throw new InvalidOperationException("总线初始化未在15秒内完成，运动保持锁定。");
                short count = card.ReadSlaveCount();
                short step = card.ReadInitStep();
                Observe(card.ReadSnapshot(), ReadNow(nowMs));
                age = ReadNow(nowMs) - initializationStartedAtMs;
                if (age < 0 || age > InitializationTimeoutMs)
                    throw new InvalidOperationException("总线初始化读取超时，运动保持锁定。");
                EnsureIdle(latest, true);
                if (count < 0 || count > 4)
                    throw new InvalidOperationException("初始化期间从站数量异常：" + count + "。");
                if (count == 4 && step == 0 && age >= InitializationMinimumWaitMs)
                {
                    Phase = OperationInitializationPhase.Ready;
                    Error = String.Empty;
                }
            }
            catch (Exception error) { Fail(error); }
        }

        public void Observe(OperationSnapshot snapshot, long nowMs)
        {
            if (!Complete(snapshot))
            {
                Invalidate();
                throw new InvalidOperationException("四轴反馈数据不完整，运动保险已撤销。");
            }
            // Copy all arrays: adapter buffers and callers can be reused or mutated.
            latest = new OperationSnapshot { Status = (int[])snapshot.Status.Clone(),
                Planned = (int[])snapshot.Planned.Clone(), Encoder = (int[])snapshot.Encoder.Clone() };
            observedAtMs = nowMs;
            sampleSequence++;
            if (AuthorizedAxis == 0) return;
            for (int index = 0; index < 4; index++)
                if ((latest.Status[index] & HazardBits) != 0) { RevokeAuthorization(); return; }
            bool enabled = (latest.Status[AuthorizedAxis - 1] & EnabledBit) != 0;
            if (AwaitingEnableFeedback)
            {
                long elapsed = nowMs - authorizationStartedAtMs;
                if (elapsed < 0 || elapsed > EnableFeedbackTimeoutMs) { RevokeAuthorization(); return; }
                if (enabled && sampleSequence > authorizationSampleSequence) AwaitingEnableFeedback = false;
            }
            else if (!enabled) RevokeAuthorization();
        }

        public void Invalidate()
        {
            latest = null;
            RevokeAuthorization();
        }

        public void AuthorizeAxis(int axis, long nowMs)
        {
            CheckAxis(axis);
            if (AwaitingEnableFeedback && nowMs - authorizationStartedAtMs <= EnableFeedbackTimeoutMs)
                throw new InvalidOperationException("使能请求已发送，正在等待新的使能反馈。");
            RevokeAuthorization();
            try
            {
                CheckReadyAndIdle(axis, nowMs);
                if (EmergencyLatched)
                {
                    if (!StopRequestSucceeded)
                        throw new InvalidOperationException("急停请求尚未成功，请再次按急停后核对反馈。");
                }
                // Every manual authorization revalidates the bus, including emergency recovery.
                // Background samples and successful StopAll never clear the emergency latch.
                short count = card.ReadSlaveCount();
                short step = card.ReadInitStep();
                if (count != 4 || step != 0)
                    throw new InvalidOperationException("使能前核验未通过：总线不是4从站且初始化完成。");
                // Snapshot is the last read and is timestamped after the SDK returns.
                Observe(card.ReadSnapshot(), ReadNow(nowMs));
                CheckReadyAndIdle(axis, ReadNow(nowMs));
                card.EnableAxis((short)axis);
                AuthorizedAxis = axis;
                authorizationStartedAtMs = ReadNow(nowMs);
                authorizationSampleSequence = sampleSequence;
                AwaitingEnableFeedback = true;
                EmergencyLatched = false;
                Error = String.Empty;
            }
            catch (Exception error)
            {
                RevokeAuthorization();
                Error = error.Message;
                throw;
            }
        }

        public void AssertCanMove(int axis, long nowMs)
        {
            CheckAxis(axis);
            if (EmergencyLatched) throw new InvalidOperationException("急停已锁定，请核对现场后重新点击使能保险。");
            if (AwaitingEnableFeedback && (nowMs < authorizationStartedAtMs || nowMs - authorizationStartedAtMs > EnableFeedbackTimeoutMs))
                RevokeAuthorization();
            if (AuthorizedAxis != axis)
                throw new InvalidOperationException("请先点击当前轴的使能保险。");
            CheckReadyAndIdle(axis, nowMs);
            if (AwaitingEnableFeedback || (latest.Status[axis - 1] & EnabledBit) == 0)
                throw new InvalidOperationException("正在等待当前轴新的使能反馈，暂不能运动。");
        }

        public void EmergencyStop()
        {
            // The lock and permission cancellation must happen before SDK access.
            EmergencyLatched = true;
            StopRequestSucceeded = false;
            RevokeAuthorization();
            try { card.StopAll(); StopRequestSucceeded = true; Error = String.Empty; }
            catch (Exception error) { Error = "急停请求失败，运动保持锁定：" + error.Message; }
            // A successful command is only a request; it does not prove physical stopping.
        }

        private void CheckReadyAndIdle(int axis, long nowMs)
        {
            if (!BusReady || !CardOpened) throw new InvalidOperationException("系统初始化尚未就绪，运动保持锁定。");
            if (latest == null || nowMs < observedAtMs || nowMs - observedAtMs > FeedbackMaxAgeMs)
                throw new InvalidOperationException("反馈无效或超过500毫秒未更新，运动保持锁定。");
            EnsureIdle(latest, false);
            long difference = (long)latest.Planned[axis - 1] - latest.Encoder[axis - 1];
            if (Math.Abs(difference) > 100)
                throw new InvalidOperationException("当前轴静止规划与编码器反馈相差 " + difference + " 计数（上限100），请先检查跟随状态。");
        }

        private static bool Complete(OperationSnapshot snapshot)
        {
            return snapshot != null && snapshot.Status != null && snapshot.Status.Length >= 4
                && snapshot.Planned != null && snapshot.Planned.Length >= 4
                && snapshot.Encoder != null && snapshot.Encoder.Length >= 4;
        }

        private static void EnsureIdle(OperationSnapshot snapshot, bool requireDisabled)
        {
            int mask = HazardBits | RunningBits | (requireDisabled ? EnabledBit : 0);
            for (int index = 0; index < 4; index++)
                if ((snapshot.Status[index] & mask) != 0)
                    throw new InvalidOperationException("轴" + (index + 1) + "有运动、报警/限位" + (requireDisabled ? "或使能" : "") + "状态（0x" + snapshot.Status[index].ToString("X8") + "），运动保持锁定。");
        }

        private static void CheckAxis(int axis)
        {
            if (axis < 1 || axis > 4) throw new InvalidOperationException("请选择轴1至轴4。");
        }

        private void RevokeAuthorization()
        {
            AuthorizedAxis = 0;
            AwaitingEnableFeedback = false;
        }

        private long ReadNow(long fallback) { return monotonicClock == null ? fallback : monotonicClock(); }

        private void Fail(Exception error)
        {
            Phase = OperationInitializationPhase.Failed;
            Error = error.Message;
            RevokeAuthorization();
        }
    }
}
