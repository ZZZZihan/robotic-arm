using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace CSharpDemo
{
    public partial class Form1 : Form
    {
        private bool cardOpened = false;
        //声明卡对象，如果有多个卡，可以声明多个对象
        MultiCardCS.MultiCardCS MultiCardCS_1 = new MultiCardCS.MultiCardCS();
        //MultiCardCS.MultiCardCS MultiCardCS_2 = new MultiCardCS.MultiCardCS();
        //MultiCardCS.MultiCardCS MultiCardCS_3 = new MultiCardCS.MultiCardCS();

        public Form1() : this(true) { }

        internal Form1(bool autoInitialize)
        {
            InitializeComponent();

            for (int axis = 1; axis <= 4; axis++) comboBoxAxisSel.Items.Add("轴" + axis);
            comboBoxAxisSel.SelectedIndex = 0;
            InitializeAxis2Controls();
            InitializeOperationSafety(autoInitialize);

            Timer timer1 = new Timer();
            timer1.Enabled = autoInitialize;
            timer1.Interval = 100;
            timer1.Tick += new System.EventHandler(this.timer1_Tick);
        }

        private MultiCardCS.MultiCardCS.TAllSysStatusDataSX CreateStatusBuffer()
        {
            MultiCardCS.MultiCardCS.TAllSysStatusDataSX m_AllSysStatusData;

            m_AllSysStatusData.lAxisEncPos = new int[16];
            m_AllSysStatusData.lAxisPrfPos = new int[16];
            m_AllSysStatusData.lAxisStatus = new int[16];

            m_AllSysStatusData.nADCValue = new short[2];
            m_AllSysStatusData.lUserSegNum = new int[2];
            m_AllSysStatusData.lRemainderSegNum = new short[2];
            m_AllSysStatusData.nCrdRunStatus = new short[2];
            m_AllSysStatusData.lCrdSpace = new short[2];
            m_AllSysStatusData.dCrdVel = new float[2];

            m_AllSysStatusData.lCrdPos = new int[2][];
            m_AllSysStatusData.lCrdPos[0] = new int[5];
            m_AllSysStatusData.lCrdPos[1] = new int[5];

            m_AllSysStatusData.lLimitPosRaw = 0;
            m_AllSysStatusData.lLimitNegRaw = 0;
            m_AllSysStatusData.lAlarmRaw = 0;
            m_AllSysStatusData.lHomeRaw = 0;
            m_AllSysStatusData.lMPG = 0;
            m_AllSysStatusData.lGpiRaw = new int[8];
            m_AllSysStatusData.lGpoRaw = new int[8];

            m_AllSysStatusData.lMPGEncPos = 0;

            return m_AllSysStatusData;
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            PollOperationInitialization();
            if (!cardOpened)
            {
                label_InitStatus.Text = "未连接控制卡";
                labelPrfPos.Text = "--";
                label_SlaveCount.Text = "--";
                InvalidateDashboardSample();
                return;
            }
            MultiCardCS.MultiCardCS.TAllSysStatusDataSX status = CreateStatusBuffer();
            int iRes;
            try { iRes = MultiCardCS_1.GA_GetAllSysStatusSX(ref status); }
            catch (Exception error)
            {
                HandleAxis2Sample(-1, status);
                ShowResult(error.Message, true);
                return;
            }
            if (!HandleAxis2Sample(iRes, status)) { labelPrfPos.Text = "反馈无效"; return; }
            labelPrfPos.Text = Convert.ToString(status.lAxisPrfPos[comboBoxAxisSel.SelectedIndex]);
            if (axis2Motion.Active) return;

            short pCutInitSlaveNum = 0;
            short pMode = 0; ;
            short pModeStep = 0;
            string strText;

            int initCode;
            try { initCode = MultiCardCS_1.GA_ECatGetInitStep(ref pCutInitSlaveNum, ref pMode, ref pModeStep); }
            catch (Exception error) { operationSafety.Invalidate(); RefreshAxis2Controls(); label_InitStatus.Text = "总线状态读取失败：" + error.Message; return; }
            if (initCode != 0) { operationSafety.Invalidate(); RefreshAxis2Controls(); label_InitStatus.Text = "总线状态读取失败：" + initCode; return; }

            strText = pCutInitSlaveNum < 0 ? "总线未就绪（站号 " + pCutInitSlaveNum + "）" : string.Format("正在初始化第{0:G}个站点,当前模式{1:G}，子步{2:G}", pCutInitSlaveNum, pMode, pModeStep);

            if (0 == pCutInitSlaveNum)
            {
                 strText = operationSafety.BusReady ? "总线就绪 · 运动前请点击使能保险" : "等待系统初始化确认";
            }
            else { operationSafety.Invalidate(); RefreshAxis2Controls(); }
            if (operationSafety.Phase == OperationInitializationPhase.Failed) strText = "初始化未完成：" + operationSafety.Error;
            label_InitStatus.Text = strText;
        }

        private void buttonOpenCard_Click(object sender, EventArgs e)
        {
            int iRes = 0;
            if (cardOpened) return;
            foreach (System.Net.IPEndPoint endpoint in System.Net.NetworkInformation.IPGlobalProperties.GetIPGlobalProperties().GetActiveUdpListeners())
            {
                if (endpoint.Port == 60000) { ShowResult("UDP 60000正被占用，请先正常关闭其他控制程序。", true); return; }
            }

            //GA_Open的4个参数依次是卡号、PC端IP地址、PC端端口号、板卡端IP地址、板卡端端口号
            //如注释部分，同时打开3个板卡代码如下
            //注意板卡端端口号必须和PC端端口号保持一致
            iRes = MultiCardCS_1.GA_Open(1, "192.168.0.200", 60000, "192.168.0.1", 60000);
            //iRes = MultiCardCS_2.GA_Open(2, "192.168.0.200", 60001, "192.168.0.2", 60001);
            //iRes = MultiCardCS_3.GA_Open(3, "192.168.0.200", 60002, "192.168.0.3", 60002);

            cardOpened = (iRes == 0);
            RefreshAxis2Controls();
            if (iRes == 0)
            {
                ShowResult("控制卡连接已打开，等待实时反馈。", false);
            }
            else
            {
                ShowResult("GA_Open 返回 " + iRes, true);
            }
        }

        private void buttonOpenY0_Click(object sender, EventArgs e)
        {
            MultiCardCS_1.GA_SetExtDoBit(0, 0, 1);
        }

        private void buttonCloseY0_Click(object sender, EventArgs e)
        {
            MultiCardCS_1.GA_SetExtDoBit(0, 0, 0);
        }

        private void button1_Click(object sender, EventArgs e)
        {
            //MultiCardCS_1.GA_ECatLoadPDOConfig(0);
            try { Require("GA_ECatInit", MultiCardCS_1.GA_ECatInit()); ShowResult("总线初始化请求已发送，请等待状态显示初始化完成。", false); }
            catch (Exception error) { ShowResult(error.Message, true); }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            short nCount = 0;
            string strText;

            MultiCardCS_1.GA_ECatGetSlaveCount(ref nCount);

            strText = string.Format("扫描到{0:G}个从站", nCount);
            label_SlaveCount.Text = strText;
        }

        private void button2_Click(object sender, EventArgs e)
        {
            short pCutInitSlaveNum = 0;
            short pMode = 0;;
            short pModeStep = 0;
            string strText;

            MultiCardCS_1.GA_ECatGetInitStep(ref pCutInitSlaveNum, ref pMode, ref pModeStep);

            strText = string.Format("正在初始化第{0:G}个站点,当前模式{1:G}，子步{2:G}", pCutInitSlaveNum, pMode, pModeStep);
            label_InitStatus.Text = strText;
        }

        private void buttonAxisOn_Click(object sender, EventArgs e)
        {
            if (operationSafety == null || axis2Motion.Active) return;
            try
            {
                short axis = (short)(comboBoxAxisSel.SelectedIndex + 1);
                operationSafety.AuthorizeAxis(axis, sessionClock.ElapsedMilliseconds);
                if (axis == 2) enabledStable.Reset();
                ShowResult("轴" + axis + "使能保险已确认；等待新的使能反馈后可操作。", false);
            }
            catch (Exception error) { ShowResult(error.Message, true); }
            RefreshAxis2Controls();
        }

        private void buttonJogN_MouseDown(object sender, MouseEventArgs e)
        {
            StartLegacyJog(-1);
        }

        private void buttonJogN_MouseUp(object sender, MouseEventArgs e)
        {
            StopLegacyJog();
        }

        private void buttonJogP_MouseDown(object sender, MouseEventArgs e)
        {
            StartLegacyJog(1);
        }

        private void buttonJogP_MouseUp(object sender, MouseEventArgs e)
        {
            StopLegacyJog();
        }

        private void button4_Click(object sender, EventArgs e)
        {
            int iRes = 0;
            int lValue = 0;
        }

        private void button6_Click(object sender, EventArgs e)
        {
            int iRes = 0;

            byte[] Temp = new byte[128];
            short nLength = 0;

            iRes = MultiCardCS_1.GA_ECatGetAllPDOData(1, ref Temp[0], ref nLength);
        }

        private void button7_Click(object sender, EventArgs e)
        {

        }

        private void button5_Click(object sender, EventArgs e)
        {
            int iRes = 0;
            iRes = MultiCardCS_1.GA_ECatSetSdoValue(1, 0X60FE, 0, 123,2);
        }

        private void button7_Click_1(object sender, EventArgs e)
        {
            int iRes = 0;

            iRes = MultiCardCS_1.GA_EStopSetIO(0, 15, 0, 10);
            iRes = MultiCardCS_1.GA_EStopOnOff(1);
        }

        private void button8_Click(object sender, EventArgs e)
        {
            MultiCardCS_1.GA_EStopClrSts();
        }

        private void button5_Click_1(object sender, EventArgs e)
        {
            if (!cardOpened || axis2Motion.Active) return;
            if (!PrepareMotion()) return;
            if (comboBoxAxisSel.SelectedIndex == 1) { StartAxis2(-1); return; }
            short nAxisNum = (short)(comboBoxAxisSel.SelectedIndex + 1);

            try { Require("GA_SetTrapPosAndUpdate", MultiCardCS_1.GA_SetTrapPosAndUpdate(nAxisNum, -100000, 20, 1, 1, 0, 0, 0)); }
            catch (Exception error) { EmergencyStopFromDashboard(); ShowResult(error.Message + "；运动已锁定，请确认现场停止。", true); }
        }

        private void button4_Click_1(object sender, EventArgs e)
        {
            if (!cardOpened || axis2Motion.Active) return;
            if (!PrepareMotion()) return;
            if (comboBoxAxisSel.SelectedIndex == 1) { StartAxis2(1); return; }
            short nAxisNum = (short)(comboBoxAxisSel.SelectedIndex + 1);

            try { Require("GA_SetTrapPosAndUpdate", MultiCardCS_1.GA_SetTrapPosAndUpdate(nAxisNum, 100000, 20, 1, 1, 0, 0, 0)); }
            catch (Exception error) { EmergencyStopFromDashboard(); ShowResult(error.Message + "；运动已锁定，请确认现场停止。", true); }
        }
    }
}
