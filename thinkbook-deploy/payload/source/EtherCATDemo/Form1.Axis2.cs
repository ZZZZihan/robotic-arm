using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Globalization;
using System.Windows.Forms;

namespace CSharpDemo
{
    public partial class Form1
    {
        private NumericUpDown axis2Turns, axis2Rpm;
        private Label feedbackLabel, axis2Info, resultLabel;
        private Button axis2Stop;
        private Axis2Motion axis2Motion;
        private readonly Stopwatch enabledStable = new Stopwatch();
        private bool stoppingOnError;
        private bool feedbackWasLost, axis2BaselineValid;
        private string lastResultMessage;
        private string logPath;
        private long lastLogMs;
        private readonly Stopwatch sessionClock = Stopwatch.StartNew();

        private static bool TryReadNumber(NumericUpDown input, out decimal value)
        {
            // Reading NumericUpDown.Value validates and rewrites an unfinished edit.
            // Read draft text without moving the caret or committing it during refresh.
            return Decimal.TryParse(input.Text, NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingWhite | NumberStyles.AllowTrailingWhite,
                CultureInfo.CurrentCulture, out value) && value >= input.Minimum && value <= input.Maximum
                && Decimal.Round(value, input.DecimalPlaces) == value;
        }

        private sealed class Axis2NumberInput : NumericUpDown
        {
            protected override void ValidateEditText()
            {
                decimal value;
                // Preserve invalid text for correction instead of silently clamping a
                // requested travel (for example 20 turns) into a different move.
                if (TryReadNumber(this, out value)) base.ValidateEditText();
            }
            public override void UpButton()
            {
                decimal value;
                if (TryReadNumber(this, out value)) base.UpButton();
            }
            public override void DownButton()
            {
                decimal value;
                if (TryReadNumber(this, out value)) base.DownButton();
            }
            protected override void OnKeyDown(KeyEventArgs e)
            {
                if (e.Control && e.KeyCode == Keys.A)
                {
                    Select(0, Text.Length); e.Handled = true; e.SuppressKeyPress = true; return;
                }
                if (e.KeyCode == Keys.Enter)
                {
                    ValidateEditText(); e.Handled = true; e.SuppressKeyPress = true; return;
                }
                base.OnKeyDown(e);
            }
        }

        private bool TryReadAxis2Inputs(out decimal turns, out decimal rpm, out string error)
        {
            bool turnsValid = TryReadNumber(axis2Turns, out turns);
            bool rpmValid = TryReadNumber(axis2Rpm, out rpm);
            error = !turnsValid ? "圈数请输入 0～10 的数字，最多两位小数。"
                : !rpmValid ? "转速请输入 0.01～120 转/分钟，最多两位小数。" : null;
            return error == null;
        }

        private sealed class Axis2Card : IAxis2Card
        {
            private readonly Form1 owner;
            public Axis2Card(Form1 owner) { this.owner = owner; }
            public Axis2Snapshot ReadSnapshot()
            {
                MultiCardCS.MultiCardCS.TAllSysStatusDataSX status = owner.CreateStatusBuffer();
                Require("GA_GetAllSysStatusSX", owner.MultiCardCS_1.GA_GetAllSysStatusSX(ref status));
                owner.Log(String.Format("PREFLIGHT axis2 planned={0} encoder={1} statuses={2:X8},{3:X8},{4:X8},{5:X8}",
                    status.lAxisPrfPos[1], status.lAxisEncPos[1], status.lAxisStatus[0], status.lAxisStatus[1], status.lAxisStatus[2], status.lAxisStatus[3]));
                return Snapshot(status);
            }
            public int ReadObject(short index, short sub, short length)
            {
                int value = 0;
                short debugFlag = 0;
                int code = owner.MultiCardCS_1.GA_ECatGetSdoValue(2, index, sub, ref value, ref debugFlag, length, 0);
                Require(String.Format("SDO {0:X4}:{1:X2}", index, sub), code);
                if (Axis2Motion.IsCoeAbort(value))
                    throw new InvalidOperationException(String.Format("SDO {0:X4}:{1:X2} 返回 CoE Abort 0x{2:X8}", index, sub, value));
                return value;
            }
            public void CheckScale()
            {
                short count = 0;
                Require("GA_ECatGetSlaveCount", owner.MultiCardCS_1.GA_ECatGetSlaveCount(ref count));
                if (count != 4) throw new InvalidOperationException("当前从站数不是4，拒绝轴2运动。");
                long original = 0, configured = 0;
                Require("GA_ECatGetPlusePerCircle", owner.MultiCardCS_1.GA_ECatGetPlusePerCircle(2, ref original, ref configured));
                if (original != 1 || configured != 1) throw new InvalidOperationException("卡端脉冲比例不是已核对的1:1。");
            }
            public int Move(int target, double velocity)
            {
                if (!owner.ContainsFocus)
                    throw new InvalidOperationException("窗口已失去焦点，未启动轴2运动。");
                owner.Log("COMMAND axis=2 target=" + target + " velocityPulsePerMs=" + velocity.ToString("R", System.Globalization.CultureInfo.InvariantCulture));
                return owner.MultiCardCS_1.GA_SetTrapPosAndUpdate(2, target, velocity, 1, 1, 0, 0, 0);
            }
            public int Stop()
            {
                owner.Log("STOP axis=2 requested; active=" + owner.axis2Motion.Active);
                int code = owner.MultiCardCS_1.GA_Stop(Axis2Motion.Mask, Axis2Motion.Mask);
                owner.Log("STOP axis=2 returned=" + code);
                return code;
            }
        }

        private static void Require(string api, int code)
        {
            if (code != 0) throw new InvalidOperationException(api + " 返回 " + code +
                (code == -1 ? "（通信失败）" : code == -7 ? "（控制器无响应）" : ""));
        }

        private static Axis2Snapshot Snapshot(MultiCardCS.MultiCardCS.TAllSysStatusDataSX status)
        {
            return new Axis2Snapshot { Planned = status.lAxisPrfPos[1], Encoder = status.lAxisEncPos[1], Status = status.lAxisStatus };
        }

        private void InitializeAxis2Controls()
        {
            comboBoxAxisSel.DropDownStyle = ComboBoxStyle.DropDownList;
            comboBoxAxisSel.SelectedIndex = 1;
            axis2Motion = new Axis2Motion(new Axis2Card(this));
            axis2Turns = new Axis2NumberInput { Name = "axis2Turns", DecimalPlaces = 2, Increment = 0.1M, Minimum = 0, Maximum = 10, Value = 0 };
            axis2Rpm = new Axis2NumberInput { Name = "axis2Rpm", DecimalPlaces = 2, Increment = 1, Minimum = 0.01M, Maximum = 120, Value = 120 };
            axis2Info = new Label { Name = "axis2Info" };
            axis2Stop = new Button { Name = "axis2Stop" };
            feedbackLabel = new Label { Name = "feedbackLabel", Text = "尚未读取反馈" };
            resultLabel = new Label { Name = "resultLabel" };
            logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs", "axis2-" + DateTime.Now.ToString("yyyyMMdd-HHmmss") + ".log");
            BuildDashboard();
            axis2Stop.Click += delegate { StopFromDashboard(); };
            axis2Turns.ValueChanged += delegate { RefreshAxis2Controls(); };
            axis2Rpm.ValueChanged += delegate { RefreshAxis2Controls(); };
            axis2Turns.TextChanged += delegate { RefreshAxis2Controls(); };
            axis2Rpm.TextChanged += delegate { RefreshAxis2Controls(); };
            comboBoxAxisSel.SelectedIndexChanged += delegate { RefreshAxis2Controls(); };
            FormClosing += Axis2FormClosing;
            Deactivate += delegate { if (axis2Motion.Active) StopAxis2("窗口失去焦点，停止轴2"); };
            RefreshAxis2Controls();
        }

        private void RefreshAxis2Controls()
        {
            if (axis2Motion == null) return;
            bool axis2 = comboBoxAxisSel.SelectedIndex == 1;
            bool idle = !axis2Motion.Active;
            decimal turns, rpm; string inputError;
            bool inputsValid = TryReadAxis2Inputs(out turns, out rpm, out inputError);
            comboBoxAxisSel.Enabled = idle;
            axis2Turns.Enabled = idle && axis2;
            axis2Rpm.Enabled = idle && axis2;
            axis2Stop.Enabled = cardOpened;
            buttonJogN.Enabled = buttonJogP.Enabled = idle && cardOpened && !axis2;
            buttonJogN.Text = "按住反向";
            buttonJogP.Text = "按住正向";
            button4.Text = axis2 ? "↑  上升  ·  正向" : "移动到100000";
            button5.Text = axis2 ? "↓  下降  ·  反向" : "移动到-100000";
            button4.Enabled = button5.Enabled = idle && cardOpened && (!axis2 || (inputsValid && turns > 0));
            buttonAxisOn.Enabled = button1.Enabled = buttonOpenY0.Enabled = buttonCloseY0.Enabled = idle && cardOpened;
            button2.Enabled = button3.Enabled = idle && cardOpened;
            buttonOpenCard.Enabled = idle && !cardOpened;
            axis2Info.Text = !inputsValid ? inputError : turns > 0
                ? String.Format("每次 {0:0.##} 电机圈  ·  目标 {1:0.##} 圈/秒  ·  预计约 {2:0.00} 秒",
                    turns, rpm / 60, Axis2Motion.MakePlan(0, 0, turns, rpm, 1).EstimatedSeconds)
                : String.Format("目标 {0:0.##} 圈/秒  ·  填写圈数后，可预览本次运动时间", rpm / 60);
            RefreshDashboard();
        }

        private void Log(string message)
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(logPath));
                File.AppendAllText(logPath, DateTime.UtcNow.ToString("o") + " " + message + Environment.NewLine);
            }
            catch (Exception error) { resultLabel.Text = "日志写入失败：" + error.Message; }
        }

        private void ShowResult(string message, bool error)
        {
            resultLabel.ForeColor = error ? Color.DarkRed : Color.DarkBlue;
            resultLabel.Text = message + "\r\n日志：" + logPath;
            if (message != lastResultMessage) Log(message);
            lastResultMessage = message;
            DashboardNotice(message, error);
        }

        private void StartAxis2(int direction)
        {
            if (!cardOpened || axis2Motion.Active) return;
            try
            {
                decimal turns, rpm; string inputError;
                if (!TryReadAxis2Inputs(out turns, out rpm, out inputError)) throw new InvalidOperationException(inputError);
                Axis2Plan plan = axis2Motion.Start(turns, rpm, direction, enabledStable.ElapsedMilliseconds);
                axis2BaselineValid = true;
                ShowResult(String.Format("轴2{4}指令已下发：{0:+0.####;-0.####} 电机圈，目标位置 {1}；速度上限 {2} 转/分钟。\r\n含加减速预计约 {3:0.00} 秒；实际升降请看现场。",
                    (double)plan.Delta / Axis2Motion.CountsPerRevolution, plan.Target, rpm, plan.EstimatedSeconds,
                    direction > 0 ? "上升（正向）" : "下降（反向）"), false);
            }
            catch (Exception error) { ShowResult(error.Message, true); }
            RefreshAxis2Controls();
        }

        private void StopAxis2(string reason)
        {
            if (!cardOpened) return;
            try { axis2Motion.Stop(); ShowResult(reason + "：已发送停止，等待反馈确认。", false); }
            catch (Exception error) { ShowResult(reason + "：" + error.Message, true); }
            RefreshAxis2Controls();
        }

        private bool HandleAxis2Sample(int code, MultiCardCS.MultiCardCS.TAllSysStatusDataSX status)
        {
            try
            {
                Require("GA_GetAllSysStatusSX", code);
                Axis2Snapshot snapshot = Snapshot(status);
                if (feedbackWasLost)
                {
                    feedbackWasLost = false;
                    ShowResult("状态读取已恢复；请检查当前使能与总线状态。", false);
                }
                UpdateDashboardSample(status);
                if ((snapshot.Status[1] & Axis2Motion.EnabledBit) != 0)
                {
                    if (!enabledStable.IsRunning) enabledStable.Start();
                }
                else enabledStable.Reset();
                int index = comboBoxAxisSel.SelectedIndex;
                feedbackLabel.Text = String.Format("软件轴{0}\r\n规划位置：{1}\r\n实际反馈：{2}\r\n规划−反馈：{3}\r\n状态：0x{4:X8}", index + 1,
                    status.lAxisPrfPos[index], status.lAxisEncPos[index], (long)status.lAxisPrfPos[index] - status.lAxisEncPos[index], status.lAxisStatus[index]);
                if (index == 1 && axis2BaselineValid && axis2Motion.Plan != null)
                    feedbackLabel.Text += String.Format("\r\n本次实际增量：{0:+0.0000;-0.0000;0} 电机圈", ((long)snapshot.Encoder - axis2Motion.Plan.StartEncoder) / (double)Axis2Motion.CountsPerRevolution);
                if (axis2Motion.Active && sessionClock.ElapsedMilliseconds - lastLogMs >= 250)
                {
                    lastLogMs = sessionClock.ElapsedMilliseconds;
                    Log("SAMPLE planned=" + snapshot.Planned + " encoder=" + snapshot.Encoder + " status=0x" + snapshot.Status[1].ToString("X8"));
                }
                if (axis2Motion.Observe(snapshot))
                {
                    ShowResult(String.Format("轴2规划已停止。目标 {0}，规划 {1}，实际反馈 {2}，状态 0x{3:X8}。{4}\r\n升降是否正常请以现场观察为准。",
                        axis2Motion.Plan.Target, snapshot.Planned, snapshot.Encoder, snapshot.Status[1],
                        axis2BaselineValid ? String.Format("本次实际增量 {0:+0.0000;-0.0000;0} 电机圈。", ((long)snapshot.Encoder - axis2Motion.Plan.StartEncoder) / (double)Axis2Motion.CountsPerRevolution) : "反馈曾中断，本次圈数不再累计。"), false);
                    RefreshAxis2Controls();
                }
                return true;
            }
            catch (Exception error)
            {
                enabledStable.Reset();
                feedbackWasLost = true;
                axis2BaselineValid = false;
                feedbackLabel.Text = "反馈无效：" + error.Message;
                InvalidateDashboardSample();
                if (axis2Motion.Active && !stoppingOnError)
                {
                    stoppingOnError = true;
                    try { StopAxis2("反馈或运动异常"); }
                    finally { stoppingOnError = false; }
                }
                ShowResult(error.Message + (axis2Motion.Active ? "；尚未确认轴2停止。" : ""), true);
                return false;
            }
        }

        private void Axis2FormClosing(object sender, FormClosingEventArgs e)
        {
            if (axis2Motion.Active)
            {
                StopAxis2("关闭窗口前停止轴2");
                e.Cancel = true;
                return;
            }
            if (cardOpened)
            {
                try { Require("GA_Close", MultiCardCS_1.GA_Close()); cardOpened = false; }
                catch (Exception error) { ShowResult(error.Message, true); e.Cancel = true; }
            }
        }
    }
}
