using System;
using System.Diagnostics;

namespace CSharpDemo
{
    internal sealed class Axis2Snapshot
    {
        public int Planned;
        public int Encoder;
        public int[] Status;
    }

    internal interface IAxis2Card
    {
        Axis2Snapshot ReadSnapshot();
        int ReadObject(short index, short sub, short length);
        void CheckScale();
        int Move(int target, double velocity);
        int Stop();
    }

    internal sealed class Axis2Plan
    {
        public int StartPlanned, StartEncoder, Target, Delta;
        public double Velocity, EstimatedSeconds;
    }

    // No auto-enable, reset, parameter writes or unbounded JOG in this path.
    internal sealed class Axis2Motion
    {
        public const int CountsPerRevolution = 262144;
        public const int HazardBits = 0x17F;
        public const int RunningBits = 0x1400;
        public const int EnabledBit = 0x200;
        public const int Mask = 2;
        private readonly IAxis2Card card;
        private readonly Stopwatch elapsed = new Stopwatch();
        public bool Active { get; private set; }
        public bool StopRequested { get; private set; }
        public Axis2Plan Plan { get; private set; }

        public Axis2Motion(IAxis2Card card) { this.card = card; }

        public static Axis2Plan MakePlan(int planned, int encoder, decimal turns, decimal rpm, int direction)
        {
            if (turns <= 0 || turns > 10) throw new InvalidOperationException("请填写本次电机圈数（大于0，最多10圈），并确认该方向有足够行程。");
            if (rpm <= 0 || rpm > 120) throw new InvalidOperationException("轴2转速必须大于0且不超过120转/分钟。");
            if (direction != -1 && direction != 1) throw new InvalidOperationException("方向无效。");
            int delta = checked((int)Decimal.Round(turns * CountsPerRevolution, 0, MidpointRounding.AwayFromZero)) * direction;
            long target = (long)planned + delta;
            if (delta == 0 || target < Int32.MinValue || target > Int32.MaxValue)
                throw new InvalidOperationException("目标无有效增量或超出32位位置范围。");
            double velocity = (double)rpm * CountsPerRevolution / 60000.0;
            double distance = Math.Abs((double)delta);
            // Existing Demo acceleration/deceleration: 1 count/ms^2, start velocity 0.
            double milliseconds = distance >= velocity * velocity
                ? distance / velocity + velocity : 2 * Math.Sqrt(distance);
            return new Axis2Plan { StartPlanned = planned, StartEncoder = encoder,
                Target = (int)target, Delta = delta, Velocity = velocity,
                EstimatedSeconds = milliseconds / 1000.0 };
        }

        private static void CheckIdle(Axis2Snapshot snapshot)
        {
            if (snapshot == null || snapshot.Status == null || snapshot.Status.Length < 4)
                throw new InvalidOperationException("轴状态数据不完整。");
            for (int i = 0; i < 4; ++i)
                if ((snapshot.Status[i] & (HazardBits | RunningBits)) != 0)
                    throw new InvalidOperationException("轴" + (i + 1) + "正在运动或有报警/限位（状态 0x" + snapshot.Status[i].ToString("X8") + "），拒绝轴2新指令。");
            if ((snapshot.Status[1] & EnabledBit) == 0)
                throw new InvalidOperationException("轴2未使能。先单独使能，待状态稳定后再移动。");
            if (Math.Abs((long)snapshot.Planned - snapshot.Encoder) > 100)
                throw new InvalidOperationException("静止时轴2规划与反馈相差 " + ((long)snapshot.Planned - snapshot.Encoder) + " 计数（上限100），先检查跟随状态。");
        }

        public Axis2Plan Start(decimal turns, decimal rpm, int direction, long enabledStableMs)
        {
            if (Active) throw new InvalidOperationException("轴2上一条运动尚未结束。");
            MakePlan(0, 0, turns, rpm, direction); // Validate inputs before any card access.
            Stopwatch preflight = Stopwatch.StartNew();
            Axis2Snapshot before = card.ReadSnapshot();
            CheckIdle(before);
            card.CheckScale();
            if (card.ReadObject(0x2000, 1, 2) != 14101 ||
                card.ReadObject(0x6091, 1, 4) != 1 || card.ReadObject(0x6091, 2, 4) != 1)
                throw new InvalidOperationException("编码器或电子齿轮与262144计数/转的已核对配置不符。");
            if (card.ReadObject(0x6061, 0, 1) != 8 || card.ReadObject(0x603F, 0, 2) != 0 ||
                (card.ReadObject(0x6041, 0, 2) & 0x6F) != 0x27)
                throw new InvalidOperationException("驱动器未处于无报警的运行使能/CSP状态。");
            int brakeDelay = card.ReadObject(0x2002, 0x0A, 2);
            if (brakeDelay < 0 || brakeDelay > 5000 || enabledStableMs < brakeDelay + 200)
                throw new InvalidOperationException("使能尚未稳定或抱闸延时读数异常，稍候再检查。");
            int drivePosition = card.ReadObject(0x6064, 0, 4);
            Axis2Snapshot fresh = card.ReadSnapshot();
            CheckIdle(fresh);
            if (Math.Abs((long)fresh.Planned - before.Planned) > 10 ||
                Math.Abs((long)fresh.Encoder - before.Encoder) > 10 ||
                Math.Abs((long)drivePosition - fresh.Encoder) > 100)
                throw new InvalidOperationException("预检期间位置变化或驱动反馈不一致，未下发运动。");
            if (preflight.ElapsedMilliseconds > 3000)
                throw new InvalidOperationException("预检超时，未下发延迟运动指令。");
            Plan = MakePlan(fresh.Planned, fresh.Encoder, turns, rpm, direction);
            Active = true;
            StopRequested = false;
            elapsed.Restart();
            try
            {
                int code = card.Move(Plan.Target, Plan.Velocity);
                if (code != 0) throw new InvalidOperationException("GA_SetTrapPosAndUpdate 返回 " + code);
                return Plan;
            }
            catch (Exception error)
            {
                try { Stop(); }
                catch (Exception stopError) { throw new InvalidOperationException(error.Message + "；停止也失败：" + stopError.Message); }
                throw;
            }
        }

        public bool Observe(Axis2Snapshot snapshot)
        {
            if (!Active) return false;
            if (snapshot == null || snapshot.Status == null || snapshot.Status.Length < 4)
                throw new InvalidOperationException("运动反馈无效，需要停止。");
            if (!StopRequested)
            {
                if ((snapshot.Status[1] & HazardBits) != 0 || (snapshot.Status[1] & EnabledBit) == 0)
                    throw new InvalidOperationException("轴2运动中出现报警、限位或失去使能，需要停止。");
                for (int i = 0; i < 4; ++i)
                    if (i != 1 && (snapshot.Status[i] & RunningBits) != 0)
                        throw new InvalidOperationException("其他轴出现运动，需要停止本次轴2运动。");
            }
            bool stopped = (snapshot.Status[1] & RunningBits) == 0;
            if (stopped && (StopRequested || snapshot.Planned == Plan.Target))
            {
                Active = false;
                return true; // Planner ended; this is not physical-motion acceptance.
            }
            if (!StopRequested && elapsed.Elapsed.TotalSeconds > Plan.EstimatedSeconds + 3)
                throw new InvalidOperationException("轴2未在预计时间内完成，需停止并检查实际反馈。");
            return false;
        }

        public void Stop()
        {
            StopRequested = true;
            int code = card.Stop();
            if (code != 0) throw new InvalidOperationException("GA_Stop(轴2) 返回 " + code + "，未确认停止。");
            // Keep Active until a valid subsequent snapshot confirms planner stopped.
        }

        public void RecordExternalStopRequest()
        {
            if (Active) StopRequested = true;
            // All-axis stop was issued by the owner. Still await a valid stopped sample.
        }

        public static bool IsCoeAbort(int value)
        {
            uint family = unchecked((uint)value) & 0xFFFF0000U;
            return family == 0x05030000 || family == 0x05040000 || family == 0x06010000 ||
                family == 0x06020000 || family == 0x06040000 || family == 0x06060000 ||
                family == 0x06070000 || family == 0x06090000 || family == 0x08000000;
        }
    }
}
