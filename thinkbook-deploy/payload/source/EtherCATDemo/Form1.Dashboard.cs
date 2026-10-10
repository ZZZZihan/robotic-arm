using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace CSharpDemo
{
    public partial class Form1
    {
        private AxisStatusCard[] axisCards;
        private Label connectionBadge, operationTitle, operationState, modeHint;
        private Label actualValue, plannedValue, differenceValue, turnsValue, feedbackState, noticeLabel, logLocation;
        private Panel turnsPanel, legacyPanel;
        private TextBox historyBox;
        private TabControl detailTabs;
        private int[] dashboardStatus = new int[4];
        private int[] dashboardEncoder = new int[4];
        private int[] dashboardPlanned = new int[4];
        private bool dashboardValid;
        private bool dashboardReadFailed;
        private string lastHistoryMessage;

        private static Label UiLabel(string text, float size, bool bold)
        {
            return new Label { Text = text, Dock = DockStyle.Fill, AutoSize = false,
                ForeColor = DashboardTheme.Ink, TextAlign = ContentAlignment.MiddleLeft,
                Font = new Font("Microsoft YaHei UI", size, bold ? FontStyle.Bold : FontStyle.Regular), Margin = new Padding(0) };
        }

        private static void StyleButton(Button button, bool primary)
        {
            button.AutoSize = false;
            button.Dock = DockStyle.Fill;
            button.FlatStyle = FlatStyle.Flat;
            button.FlatAppearance.BorderSize = primary ? 0 : 1;
            button.FlatAppearance.BorderColor = DashboardTheme.Border;
            button.BackColor = primary ? DashboardTheme.Accent : Color.White;
            button.ForeColor = primary ? Color.White : DashboardTheme.Ink;
            button.Font = DashboardTheme.Strong;
            button.Cursor = Cursors.Hand;
            button.Margin = new Padding(4);
        }

        private static TableLayoutPanel Grid(int columns, int rows)
        {
            TableLayoutPanel grid = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = columns, RowCount = rows,
                Margin = new Padding(0), Padding = new Padding(0), BackColor = Color.Transparent };
            return grid;
        }

        private void BuildDashboard()
        {
            SuspendLayout();
            Controls.Clear();
            Text = "机械臂 · 四轴操作台 · 使能保险版";
            Font = DashboardTheme.Body;
            AutoScaleMode = AutoScaleMode.None;
            BackColor = DashboardTheme.Background;
            DoubleBuffered = true;
            ClientSize = new Size(1120, 780);
            MinimumSize = new Size(1000, 730);
            StartPosition = FormStartPosition.CenterScreen;

            TableLayoutPanel root = Grid(1, 5);
            root.Padding = new Padding(22, 12, 22, 10);
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 64));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 112));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 144));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 26));
            Controls.Add(root);

            TableLayoutPanel header = Grid(4, 1);
            header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            header.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 190));
            header.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150));
            header.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 132));
            header.Controls.Add(UiLabel("机械臂  /  四轴操作台", 20, true), 0, 0);
            connectionBadge = UiLabel("●  未连接控制卡", 10, true);
            connectionBadge.ForeColor = DashboardTheme.Muted;
            header.Controls.Add(connectionBadge, 1, 0);
            buttonOpenCard.Visible = false;
            StyleButton(buttonAxisOn, true);
            buttonAxisOn.Text = "使能保险";
            buttonAxisOn.Margin = new Padding(4, 12, 4, 12);
            header.Controls.Add(buttonAxisOn, 2, 0);
            emergencyStop = new Button { Name = "emergencyStop", Text = "急停", AccessibleName = "急停全部运动，锁定运动保险", CausesValidation = false };
            StyleButton(emergencyStop, true);
            emergencyStop.BackColor = DashboardTheme.Danger;
            emergencyStop.Margin = new Padding(4, 12, 0, 12);
            emergencyStop.Click += delegate { EmergencyStopFromDashboard(); };
            header.Controls.Add(emergencyStop, 3, 0);
            root.Controls.Add(header, 0, 0);

            TableLayoutPanel cards = Grid(4, 1);
            axisCards = new AxisStatusCard[4];
            for (int i = 0; i < 4; i++)
            {
                cards.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));
                int index = i;
                axisCards[i] = new AxisStatusCard(i + 1) { Dock = DockStyle.Fill, Margin = new Padding(i == 0 ? 0 : 6, 3, i == 3 ? 0 : 6, 9), Name = "axisCard" + (i + 1) };
                axisCards[i].Click += delegate { if (comboBoxAxisSel.Enabled) comboBoxAxisSel.SelectedIndex = index; };
                cards.Controls.Add(axisCards[i], i, 0);
            }
            root.Controls.Add(cards, 0, 1);
            // Keep the original one-based SDK selection path; cards only change its index.
            comboBoxAxisSel.Visible = false;
            Controls.Add(comboBoxAxisSel);

            TableLayoutPanel work = Grid(2, 1);
            work.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 62));
            work.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 38));
            DashboardPanel operations = new DashboardPanel { Dock = DockStyle.Fill, Margin = new Padding(0, 0, 7, 12) };
            DashboardPanel feedback = new DashboardPanel { Dock = DockStyle.Fill, Margin = new Padding(7, 0, 0, 12) };
            work.Controls.Add(operations, 0, 0); work.Controls.Add(feedback, 1, 0);
            root.Controls.Add(work, 0, 2);

            TableLayoutPanel command = Grid(1, 3);
            command.RowStyles.Add(new RowStyle(SizeType.Absolute, 57));
            command.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            command.RowStyles.Add(new RowStyle(SizeType.Absolute, 50));
            operations.Controls.Add(command);
            TableLayoutPanel commandHeader = Grid(2, 2);
            commandHeader.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            commandHeader.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 142));
            commandHeader.RowStyles.Add(new RowStyle(SizeType.Absolute, 31));
            commandHeader.RowStyles.Add(new RowStyle(SizeType.Absolute, 26));
            operationTitle = UiLabel("轴2 · 升降控制", 17, true);
            operationState = UiLabel("等待连接", 9, false); operationState.ForeColor = DashboardTheme.Muted;
            commandHeader.Controls.Add(operationTitle, 0, 0); commandHeader.Controls.Add(operationState, 0, 1);
            Label insuranceHint = UiLabel("当前轴单独使能", 9, false);
            insuranceHint.ForeColor = DashboardTheme.Muted;
            commandHeader.Controls.Add(insuranceHint, 1, 0); commandHeader.SetRowSpan(insuranceHint, 2);
            command.Controls.Add(commandHeader, 0, 0);

            Panel modes = new Panel { Dock = DockStyle.Fill, Margin = new Padding(0) };
            command.Controls.Add(modes, 0, 1);
            turnsPanel = new Panel { Dock = DockStyle.Fill };
            legacyPanel = new Panel { Dock = DockStyle.Fill };
            modes.Controls.Add(legacyPanel); modes.Controls.Add(turnsPanel);

            TableLayoutPanel turns = Grid(1, 4);
            turns.RowStyles.Add(new RowStyle(SizeType.Absolute, 58));
            turns.RowStyles.Add(new RowStyle(SizeType.Absolute, 29));
            turns.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            turns.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));
            turnsPanel.Controls.Add(turns);
            TableLayoutPanel inputs = Grid(2, 2);
            inputs.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50)); inputs.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            inputs.RowStyles.Add(new RowStyle(SizeType.Absolute, 24)); inputs.RowStyles.Add(new RowStyle(SizeType.Absolute, 32));
            inputs.Controls.Add(UiLabel("走多少 · 电机圈数", 9, false), 0, 0);
            inputs.Controls.Add(UiLabel("走多快 · 转/分钟", 9, false), 1, 0);
            axis2Turns.Dock = axis2Rpm.Dock = DockStyle.Fill;
            axis2Turns.Font = axis2Rpm.Font = new Font("Segoe UI", 12F);
            axis2Turns.BorderStyle = axis2Rpm.BorderStyle = BorderStyle.FixedSingle;
            axis2Turns.BackColor = axis2Rpm.BackColor = Color.FromArgb(247, 250, 252);
            axis2Turns.Margin = new Padding(0, 0, 14, 0); axis2Rpm.Margin = new Padding(0);
            inputs.Controls.Add(axis2Turns, 0, 1); inputs.Controls.Add(axis2Rpm, 1, 1);
            turns.Controls.Add(inputs, 0, 0);
            axis2Info.Dock = DockStyle.Fill; axis2Info.ForeColor = DashboardTheme.Muted; axis2Info.TextAlign = ContentAlignment.MiddleLeft;
            turns.Controls.Add(axis2Info, 0, 1);
            modeHint = UiLabel("正向向上，反向向下。每次从当前位置移动。\r\n圈数表示电机转动量；预计时间含加减速，升降高度以现场为准。", 9, false);
            modeHint.ForeColor = DashboardTheme.Muted;
            turns.Controls.Add(modeHint, 0, 2);
            TableLayoutPanel relative = Grid(2, 1);
            relative.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50)); relative.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            StyleButton(button5, true); StyleButton(button4, true);
            button4.AccessibleName = "轴2上升，正向移动本次电机圈数";
            button5.AccessibleName = "轴2下降，反向移动本次电机圈数";
            relative.Controls.Add(button5, 0, 0); relative.Controls.Add(button4, 1, 0);
            turns.Controls.Add(relative, 0, 3);

            TableLayoutPanel legacy = Grid(1, 4);
            legacy.RowStyles.Add(new RowStyle(SizeType.Absolute, 30)); legacy.RowStyles.Add(new RowStyle(SizeType.Absolute, 58));
            legacy.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); legacy.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));
            legacyPanel.Controls.Add(legacy);
            legacy.Controls.Add(UiLabel("按住点动，松开停止", 11, true), 0, 0);
            TableLayoutPanel jog = Grid(2, 1); jog.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50)); jog.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            StyleButton(buttonJogN, true); StyleButton(buttonJogP, true);
            jog.Controls.Add(buttonJogN, 0, 0); jog.Controls.Add(buttonJogP, 1, 0); legacy.Controls.Add(jog, 0, 1);
            Label legacyHint = UiLabel("沿用当前点动速度。下方为固定绝对位置定位，单位：计数。", 9, false);
            legacyHint.ForeColor = DashboardTheme.Muted; legacy.Controls.Add(legacyHint, 0, 2);
            // These controls forward to the same existing absolute-position handlers.
            TableLayoutPanel absolute = Grid(2, 1); absolute.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50)); absolute.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            legacyNegative = new Button { Name = "legacyNegative", Text = "定位到 −100000" };
            legacyPositive = new Button { Name = "legacyPositive", Text = "定位到 +100000" };
            StyleButton(legacyNegative, false); StyleButton(legacyPositive, false);
            legacyNegative.Click += button5_Click_1; legacyPositive.Click += button4_Click_1;
            absolute.Controls.Add(legacyNegative, 0, 0); absolute.Controls.Add(legacyPositive, 1, 0); legacy.Controls.Add(absolute, 0, 3);

            StyleButton(axis2Stop, false);
            axis2Stop.BackColor = DashboardTheme.SoftDanger; axis2Stop.ForeColor = DashboardTheme.Danger;
            axis2Stop.FlatAppearance.BorderColor = Color.FromArgb(242, 195, 195);
            axis2Stop.Margin = new Padding(4, 10, 4, 0);
            command.Controls.Add(axis2Stop, 0, 2);

            TableLayoutPanel readings = Grid(1, 7);
            readings.RowStyles.Add(new RowStyle(SizeType.Absolute, 34)); readings.RowStyles.Add(new RowStyle(SizeType.Absolute, 23));
            readings.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); readings.RowStyles.Add(new RowStyle(SizeType.Absolute, 43));
            readings.RowStyles.Add(new RowStyle(SizeType.Absolute, 43)); readings.RowStyles.Add(new RowStyle(SizeType.Absolute, 43));
            readings.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
            feedback.Controls.Add(readings);
            readings.Controls.Add(UiLabel("实时反馈", 15, true), 0, 0);
            Label posCaption = UiLabel("电机编码器位置 / 计数", 9, false); posCaption.ForeColor = DashboardTheme.Muted; readings.Controls.Add(posCaption, 0, 1);
            actualValue = UiLabel("—", 27, true); actualValue.Font = new Font("Segoe UI", 27, FontStyle.Bold); readings.Controls.Add(actualValue, 0, 2);
            plannedValue = AddMetric(readings, 3, "规划位置"); differenceValue = AddMetric(readings, 4, "规划 − 编码器"); turnsValue = AddMetric(readings, 5, "本次电机圈数");
            feedbackState = UiLabel("等待首次反馈", 9, false); feedbackState.ForeColor = DashboardTheme.Muted; readings.Controls.Add(feedbackState, 0, 6);

            BuildDetailTabs(root);
            noticeLabel = UiLabel("待连接 · 选择轴后，在主区域操作", 9, false);
            noticeLabel.ForeColor = DashboardTheme.Muted;
            root.Controls.Add(noticeLabel, 0, 4);
            ResumeLayout(true);
        }

        private Button legacyNegative, legacyPositive;

        private static Label AddMetric(TableLayoutPanel parent, int row, string caption)
        {
            TableLayoutPanel line = Grid(2, 1);
            line.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 52)); line.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 48));
            Label label = UiLabel(caption, 9, false); label.ForeColor = DashboardTheme.Muted;
            Label value = UiLabel("—", 11, true); value.TextAlign = ContentAlignment.MiddleRight;
            line.Controls.Add(label, 0, 0); line.Controls.Add(value, 1, 0); parent.Controls.Add(line, 0, row);
            return value;
        }

        private void BuildDetailTabs(TableLayoutPanel root)
        {
            detailTabs = new TabControl { Dock = DockStyle.Fill, Margin = new Padding(0, 0, 0, 6), Padding = new Point(20, 5) };
            TabPage events = new TabPage("操作记录") { BackColor = Color.White, Padding = new Padding(10) };
            historyBox = new TextBox { Dock = DockStyle.Fill, ReadOnly = true, Multiline = true, BorderStyle = BorderStyle.None,
                BackColor = Color.White, ForeColor = DashboardTheme.Muted, ScrollBars = ScrollBars.Vertical, Font = DashboardTheme.Body, Text = "打开程序后自动连接并检查总线。运动前请点击当前轴的使能保险。" };
            events.Controls.Add(historyBox); detailTabs.TabPages.Add(events);
            TabPage diagnostics = new TabPage("诊断与维护") { BackColor = Color.White, Padding = new Padding(10) };
            TableLayoutPanel tools = Grid(3, 1);
            tools.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40)); tools.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 24)); tools.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 36));
            TableLayoutPanel bus = Grid(1, 3);
            bus.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            bus.RowStyles.Add(new RowStyle(SizeType.Absolute, 24)); bus.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); bus.RowStyles.Add(new RowStyle(SizeType.Absolute, 24));
            foreach (Button b in new Button[] { button1, button2, button3, buttonOpenY0, buttonCloseY0 }) b.Visible = false;
            bus.Controls.Add(UiLabel("系统与总线", 10, true), 0, 0);
            label_InitStatus.Dock = DockStyle.Fill; label_InitStatus.AutoSize = false; label_InitStatus.ForeColor = DashboardTheme.Muted;
            bus.Controls.Add(label_InitStatus, 0, 1);
            label_SlaveCount.Dock = DockStyle.Fill; label_SlaveCount.AutoSize = false; label_SlaveCount.ForeColor = DashboardTheme.Muted; bus.Controls.Add(label_SlaveCount, 0, 2);
            tools.Controls.Add(bus, 0, 0);
            tools.Controls.Add(UiLabel("急停后保持保险锁定。\r\n确认停止并排除异常后，\r\n重新点击使能保险。", 9, false), 1, 0);
            feedbackLabel.Dock = DockStyle.Fill; feedbackLabel.Font = new Font("Microsoft YaHei UI", 8F); feedbackLabel.ForeColor = DashboardTheme.Muted;
            tools.Controls.Add(feedbackLabel, 2, 0); diagnostics.Controls.Add(tools); detailTabs.TabPages.Add(diagnostics);
            TabPage help = new TabPage("使用说明") { BackColor = Color.White, Padding = new Padding(10) };
            TableLayoutPanel helpLayout = Grid(1, 2); helpLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); helpLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 22));
            helpLayout.Controls.Add(UiLabel("启动自动准备总线。选择轴后点击使能保险，再操作运动；保险只允许当前轴运动。\r\n轴2：↑ 上升，↓ 下降。轴1、3、4：按住点动，松开停止。软件急停依赖通信，现场物理急停仍须可用。", 9, false), 0, 0);
            logLocation = UiLabel("日志：" + logPath, 8, false); logLocation.ForeColor = DashboardTheme.Muted; helpLayout.Controls.Add(logLocation, 0, 1);
            help.Controls.Add(helpLayout); detailTabs.TabPages.Add(help); root.Controls.Add(detailTabs, 0, 3);
            resultLabel.Visible = false;
        }

        private void RefreshDashboard()
        {
            if (axisCards == null) return;
            int selected = comboBoxAxisSel.SelectedIndex;
            bool lift = selected == 1;
            bool feedbackBusy = false;
            for (int i = 0; i < 4; i++)
                feedbackBusy |= dashboardValid && (dashboardStatus[i] & Axis2Motion.RunningBits) != 0;
            comboBoxAxisSel.Enabled = !axis2Motion.Active && !feedbackBusy;
            turnsPanel.Visible = lift; legacyPanel.Visible = !lift;
            operationTitle.Text = lift ? "轴2 · 升降控制" : "轴" + (selected + 1) + " · 运动控制";
            operationState.Text = dashboardValid ? DashboardTheme.State(dashboardStatus[selected]) : (cardOpened ? "等待反馈" : "连接后可操作");
            operationState.ForeColor = dashboardValid ? DashboardTheme.StateColor(dashboardStatus[selected]) : DashboardTheme.Muted;
            connectionBadge.Text = !cardOpened ? "●  未连接控制卡" : dashboardValid ? "●  通信正常" : dashboardReadFailed ? "●  反馈异常" : "●  等待有效反馈";
            connectionBadge.ForeColor = dashboardReadFailed ? DashboardTheme.Danger : dashboardValid ? DashboardTheme.Accent : DashboardTheme.Muted;
            bool ready = cardOpened && dashboardValid && !axis2Motion.Active && !feedbackBusy;
            bool motionAllowed = ready && InsuranceAllowsMotion();
            decimal turns, rpm; string inputError;
            bool inputsValid = TryReadAxis2Inputs(out turns, out rpm, out inputError);
            button4.Enabled = button5.Enabled = motionAllowed && (!lift || (inputsValid && turns > 0));
            buttonJogN.Enabled = (motionAllowed && !lift) || (legacyJogActive && heldJogButton == buttonJogN);
            buttonJogP.Enabled = (motionAllowed && !lift) || (legacyJogActive && heldJogButton == buttonJogP);
            legacyNegative.Enabled = legacyPositive.Enabled = motionAllowed && !lift;
                        bool insured = operationSafety != null && operationSafety.AuthorizedAxis == selected + 1;
            bool emergency = operationSafety != null && operationSafety.EmergencyLatched;
            buttonAxisOn.Text = emergency ? "重新打开保险" : insured ? (operationSafety.AwaitingEnableFeedback ? "等待使能反馈" : "保险已开启") : "使能保险";
            buttonAxisOn.Enabled = ready && operationSafety != null && operationSafety.BusReady && (!insured || emergency);
            if (emergency) { operationState.Text = "急停锁定 · 确认停稳后重新打开保险"; operationState.ForeColor = DashboardTheme.Danger; }
            else if (ready && !insured) operationState.Text = "运动保险关闭 · 请点击使能保险";
            axis2Stop.Text = lift ? "停止轴2" : "停止全部运动";
            for (int i = 0; i < 4; i++)
            {
                axisCards[i].Selected = i == selected;
                axisCards[i].Enabled = comboBoxAxisSel.Enabled;
                axisCards[i].Valid = dashboardValid;
                axisCards[i].Position = dashboardEncoder[i]; axisCards[i].Status = dashboardStatus[i];
                axisCards[i].Invalidate();
            }
            actualValue.Text = dashboardValid ? dashboardEncoder[selected].ToString("N0") : "—";
            plannedValue.Text = dashboardValid ? dashboardPlanned[selected].ToString("N0") : "—";
            differenceValue.Text = dashboardValid ? ((long)dashboardPlanned[selected] - dashboardEncoder[selected]).ToString("N0") : "—";
            turnsValue.Text = dashboardValid && lift && axis2BaselineValid && axis2Motion.Plan != null
                ? (((long)dashboardEncoder[1] - axis2Motion.Plan.StartEncoder) / (double)Axis2Motion.CountsPerRevolution).ToString("+0.0000;-0.0000;0.0000") : "—";
            feedbackState.Text = dashboardValid ? "已读取 · " + DashboardTheme.State(dashboardStatus[selected]) : "暂无有效反馈";
            feedbackState.ForeColor = dashboardValid ? DashboardTheme.StateColor(dashboardStatus[selected]) : DashboardTheme.Muted;
            foreach (Button button in new Button[] { buttonOpenCard, button4, button5, buttonJogN, buttonJogP })
                PaintEnabledButton(button, true, false);
            PaintEnabledButton(buttonAxisOn, true, false);
            foreach (Button button in new Button[] { legacyNegative, legacyPositive, button1, button2, button3, buttonOpenY0, buttonCloseY0 })
                PaintEnabledButton(button, false, false);
            PaintEnabledButton(axis2Stop, false, true);
            // The stop stays available even with invalid input, stale feedback or failed initialization.
            emergencyStop.Enabled = true;
            emergencyStop.BackColor = DashboardTheme.Danger;
            emergencyStop.ForeColor = Color.White;
        }

        private static void PaintEnabledButton(Button button, bool primary, bool stop)
        {
            button.BackColor = !button.Enabled ? Color.FromArgb(235, 239, 244) : stop ? DashboardTheme.SoftDanger : primary ? DashboardTheme.Accent : Color.White;
            button.ForeColor = !button.Enabled ? DashboardTheme.Muted : stop ? DashboardTheme.Danger : primary ? Color.White : DashboardTheme.Ink;
        }

        private void UpdateDashboardSample(MultiCardCS.MultiCardCS.TAllSysStatusDataSX status)
        {
            if (operationSafety != null) operationSafety.Observe(ToOperationSnapshot(status), sessionClock.ElapsedMilliseconds);
            Array.Copy(status.lAxisEncPos, dashboardEncoder, 4);
            Array.Copy(status.lAxisPrfPos, dashboardPlanned, 4);
            Array.Copy(status.lAxisStatus, dashboardStatus, 4);
            dashboardValid = true;
            dashboardReadFailed = false;
            RefreshDashboard();
        }

        private void InvalidateDashboardSample()
        {
            if (operationSafety != null) operationSafety.Invalidate();
            dashboardValid = false;
            dashboardReadFailed = cardOpened;
            RefreshDashboard();
        }

        private void DashboardNotice(string message, bool error)
        {
            if (noticeLabel == null) return;
            noticeLabel.Text = message.Split(new string[] { "\r\n", "\n" }, StringSplitOptions.None)[0];
            noticeLabel.ForeColor = error ? DashboardTheme.Danger : DashboardTheme.Accent;
            if (message == lastHistoryMessage) return;
            lastHistoryMessage = message;
            if (historyBox.TextLength > 20000) historyBox.Clear();
            historyBox.AppendText(Environment.NewLine + DateTime.Now.ToString("HH:mm:ss") + "  " + message.Replace("\r\n", "  ") + Environment.NewLine);
            historyBox.SelectionStart = historyBox.TextLength; historyBox.ScrollToCaret();
        }

        private void StopFromDashboard()
        {
            if (!cardOpened) return;
            if (comboBoxAxisSel.SelectedIndex == 1) { StopAxis2("手动停止"); return; }
            try
            {
                // Same stop scope as the established legacy JOG mouse-up path.
                Require("GA_Stop", MultiCardCS_1.GA_Stop(0xFFFFF, 0xFFFFF));
                ShowResult("已发送全部运动停止请求；请查看反馈确认。", false);
            }
            catch (Exception error) { ShowResult(error.Message, true); }
        }
    }
}
