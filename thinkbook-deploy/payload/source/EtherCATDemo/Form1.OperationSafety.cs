using System;
using System.Windows.Forms;

namespace CSharpDemo
{
    public partial class Form1
    {
        private OperationSafety operationSafety;
        private IOperationSafetyCard operationCard;
        private Button emergencyStop;
        private OperationInitializationPhase lastInitializationPhase;
        private bool legacyJogActive;
        private Button heldJogButton;

        private sealed class OperationCard : IOperationSafetyCard
        {
            private readonly Form1 owner;
            public OperationCard(Form1 owner) { this.owner = owner; }
            public void Open()
            {
                if (owner.cardOpened) return;
                foreach (System.Net.IPEndPoint endpoint in System.Net.NetworkInformation.IPGlobalProperties.GetIPGlobalProperties().GetActiveUdpListeners())
                    if (endpoint.Port == 60000) throw new InvalidOperationException("控制端口被占用，请先正常关闭其他控制程序。");
                Require("GA_Open", owner.MultiCardCS_1.GA_Open(1, "192.168.0.200", 60000, "192.168.0.1", 60000));
                owner.cardOpened = true;
            }
            public OperationSnapshot ReadSnapshot()
            {
                MultiCardCS.MultiCardCS.TAllSysStatusDataSX sample = owner.CreateStatusBuffer();
                Require("GA_GetAllSysStatusSX", owner.MultiCardCS_1.GA_GetAllSysStatusSX(ref sample));
                return ToOperationSnapshot(sample);
            }
            public short ReadSlaveCount()
            {
                short count = 0;
                Require("GA_ECatGetSlaveCount", owner.MultiCardCS_1.GA_ECatGetSlaveCount(ref count));
                owner.label_SlaveCount.Text = "从站数量：" + count;
                return count;
            }
            public short ReadInitStep()
            {
                short station = 0, mode = 0, step = 0;
                Require("GA_ECatGetInitStep", owner.MultiCardCS_1.GA_ECatGetInitStep(ref station, ref mode, ref step));
                return station;
            }
            public void InitializeBus()
            {
                owner.Log("INITIALIZE bus requested; no axis enable or motion");
                Require("GA_ECatInit", owner.MultiCardCS_1.GA_ECatInit());
            }
            public void EnableAxis(short axis)
            {
                owner.Log("INSURANCE axis=" + axis + " enable requested by operator");
                Require("GA_AxisOn", owner.MultiCardCS_1.GA_AxisOn(axis));
            }
            public void StopAll()
            {
                if (!owner.cardOpened) throw new InvalidOperationException("控制卡尚未连接，无法发送软件急停。");
                owner.Log("STOP all motion requested");
                // Stop planning urgently, retaining servo holding torque for the lift.
                Require("GA_Stop", owner.MultiCardCS_1.GA_Stop(0xFFFFF, 0xFFFFF));
                owner.Log("STOP all motion request returned 0; awaiting stationary feedback");
            }
        }

        private static OperationSnapshot ToOperationSnapshot(MultiCardCS.MultiCardCS.TAllSysStatusDataSX sample)
        {
            return new OperationSnapshot { Status = sample.lAxisStatus, Planned = sample.lAxisPrfPos, Encoder = sample.lAxisEncPos };
        }

        private void InitializeOperationSafety(bool autoInitialize)
        {
            operationCard = new OperationCard(this);
            operationSafety = new OperationSafety(operationCard, delegate { return sessionClock.ElapsedMilliseconds; });
            if (autoInitialize) Shown += delegate { BeginOperationInitialization(); };
            buttonJogN.MouseCaptureChanged += delegate { StopJogIfCaptureLost(buttonJogN); };
            buttonJogP.MouseCaptureChanged += delegate { StopJogIfCaptureLost(buttonJogP); };
            Deactivate += delegate { if (legacyJogActive) StopLegacyJog(); };
            RefreshAxis2Controls();
        }

        private void BeginOperationInitialization()
        {
            ShowResult("正在连接控制卡并检查总线；运动保险保持关闭。", false);
            operationSafety.BeginInitialization(sessionClock.ElapsedMilliseconds);
            ReportInitialization();
        }

        private void PollOperationInitialization()
        {
            if (operationSafety == null || operationSafety.Phase != OperationInitializationPhase.Waiting) return;
            operationSafety.PollInitialization(sessionClock.ElapsedMilliseconds);
            ReportInitialization();
        }

        private void ReportInitialization()
        {
            OperationInitializationPhase phase = operationSafety.Phase;
            if (phase == OperationInitializationPhase.Ready)
                label_InitStatus.Text = "总线就绪 · 运动前请点击使能保险";
            else if (phase == OperationInitializationPhase.Waiting)
                label_InitStatus.Text = "总线初始化中 · 运动保险关闭";
            else if (phase == OperationInitializationPhase.Failed)
                label_InitStatus.Text = "初始化未完成：" + operationSafety.Error;
            if (phase != lastInitializationPhase)
            {
                lastInitializationPhase = phase;
                ShowResult(label_InitStatus.Text, phase == OperationInitializationPhase.Failed);
            }
            RefreshAxis2Controls();
        }

        private bool InsuranceAllowsMotion()
        {
            if (operationSafety == null) return false;
            try { operationSafety.AssertCanMove(comboBoxAxisSel.SelectedIndex + 1, sessionClock.ElapsedMilliseconds); return true; }
            catch (InvalidOperationException) { return false; }
        }

        private bool PrepareMotion()
        {
            try
            {
                if (operationSafety == null) throw new InvalidOperationException("系统尚未初始化。");
                operationSafety.AssertCanMove(comboBoxAxisSel.SelectedIndex + 1, sessionClock.ElapsedMilliseconds);
                if (operationCard.ReadSlaveCount() != 4 || operationCard.ReadInitStep() != 0)
                {
                    operationSafety.Invalidate();
                    throw new InvalidOperationException("当前总线未就绪，运动保险已关闭。");
                }
                operationSafety.Observe(operationCard.ReadSnapshot(), sessionClock.ElapsedMilliseconds);
                operationSafety.AssertCanMove(comboBoxAxisSel.SelectedIndex + 1, sessionClock.ElapsedMilliseconds);
                return true;
            }
            catch (Exception error) { operationSafety.Invalidate(); ShowResult(error.Message, true); RefreshAxis2Controls(); return false; }
        }

        private void StartLegacyJog(int direction)
        {
            if (!cardOpened || axis2Motion.Active || comboBoxAxisSel.SelectedIndex == 1 || !PrepareMotion()) return;
            short axis = (short)(comboBoxAxisSel.SelectedIndex + 1);
            try
            {
                MultiCardCS.MultiCardCS.TJogPrm parameters;
                parameters.dAcc = 1; parameters.dDec = 1; parameters.dSmooth = 0;
                Require("GA_PrfJog", MultiCardCS_1.GA_PrfJog(axis));
                Require("GA_SetJogPrm", MultiCardCS_1.GA_SetJogPrm(axis, ref parameters));
                Require("GA_SetVel", MultiCardCS_1.GA_SetVel(axis, direction * 20));
                Require("GA_Update", MultiCardCS_1.GA_Update(1 << (axis - 1)));
                legacyJogActive = true;
                heldJogButton = direction < 0 ? buttonJogN : buttonJogP;
            }
            catch (Exception error) { EmergencyStopFromDashboard(); ShowResult(error.Message + "；运动已锁定，请确认现场停止。", true); }
        }

        private void StopJogIfCaptureLost(Button button)
        {
            if (legacyJogActive && heldJogButton == button && !button.Capture) StopLegacyJog();
        }

        private void StopLegacyJog()
        {
            if (!cardOpened) return;
            legacyJogActive = false;
            heldJogButton = null;
            try
            {
                operationCard.StopAll();
                ShowResult("点动停止请求已发送，请查看反馈。", false);
            }
            catch (Exception error) { EmergencyStopFromDashboard(); ShowResult("点动停止未确认：" + error.Message + "；请使用现场物理急停。", true); }
            RefreshAxis2Controls();
        }

        private void EmergencyStopFromDashboard()
        {
            // No dialog or feedback prerequisite may delay the stop request.
            operationSafety.EmergencyStop();
            axis2Motion.RecordExternalStopRequest();
            legacyJogActive = false;
            heldJogButton = null;
            enabledStable.Reset();
            ShowResult(operationSafety.StopRequestSucceeded
                ? "急停请求已发送至全部轴；运动保险已锁定，请确认机械臂停止。"
                : "急停未确认送达：" + operationSafety.Error + "。运动已锁定，请立即使用现场物理急停。",
                !operationSafety.StopRequestSucceeded);
            RefreshAxis2Controls();
        }
    }
}
