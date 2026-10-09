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
        //声明卡对象，如果有多个卡，可以声明多个对象
        MultiCardCS.MultiCardCS MultiCardCS_1 = new MultiCardCS.MultiCardCS();
        //MultiCardCS.MultiCardCS MultiCardCS_2 = new MultiCardCS.MultiCardCS();
        //MultiCardCS.MultiCardCS MultiCardCS_3 = new MultiCardCS.MultiCardCS();

        public Form1()
        {
            InitializeComponent();

            comboBoxAxisSel.Items.Add("轴1");
            comboBoxAxisSel.Items.Add("轴2");
            comboBoxAxisSel.Items.Add("轴3");
            comboBoxAxisSel.Items.Add("轴4");
            comboBoxAxisSel.Items.Add("轴5");
            comboBoxAxisSel.Items.Add("轴6");
            comboBoxAxisSel.Items.Add("轴7");
            comboBoxAxisSel.Items.Add("轴8");
            comboBoxAxisSel.Items.Add("轴9");
            comboBoxAxisSel.Items.Add("轴10");
            comboBoxAxisSel.Items.Add("轴11");
            comboBoxAxisSel.Items.Add("轴12");
            comboBoxAxisSel.Items.Add("轴13");
            comboBoxAxisSel.Items.Add("轴14");
            comboBoxAxisSel.Items.Add("轴15");
            comboBoxAxisSel.Items.Add("轴16");
            comboBoxAxisSel.SelectedIndex = 0;

            Timer timer1 = new Timer();
            timer1.Enabled = true;
            timer1.Interval = 100;
            timer1.Tick += new System.EventHandler(this.timer1_Tick);
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            int iRes = 0;
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

            //8轴及以下控制卡用这个
            //iRes = MultiCardCS_1.GA_GetAllSysStatus(ref m_AllSysStatusData);

            //9~16轴控制卡用这个
            iRes = MultiCardCS_1.GA_GetAllSysStatusSX(ref m_AllSysStatusData);

            labelPrfPos.Text = Convert.ToString(m_AllSysStatusData.lAxisPrfPos[comboBoxAxisSel.SelectedIndex]);

        

            short pCutInitSlaveNum = 0;
            short pMode = 0; ;
            short pModeStep = 0;
            string strText;

            MultiCardCS_1.GA_ECatGetInitStep(ref pCutInitSlaveNum, ref pMode, ref pModeStep);

            strText = string.Format("正在初始化第{0:G}个站点,当前模式{0:G}，子步{0:G}", pCutInitSlaveNum, pMode, pModeStep);

            if (0 == pCutInitSlaveNum)
            {
                 strText = string.Format("初始化完成");
            }
            label_InitStatus.Text = strText;
        }

        private void buttonOpenCard_Click(object sender, EventArgs e)
        {
            int iRes = 0;

            //GA_Open的4个参数依次是卡号、PC端IP地址、PC端端口号、板卡端IP地址、板卡端端口号
            //如注释部分，同时打开3个板卡代码如下
            //注意板卡端端口号必须和PC端端口号保持一致
            iRes = MultiCardCS_1.GA_Open(1, "192.168.0.200", 60000, "192.168.0.1", 60000);
            //iRes = MultiCardCS_2.GA_Open(2, "192.168.0.200", 60001, "192.168.0.2", 60001);
            //iRes = MultiCardCS_3.GA_Open(3, "192.168.0.200", 60002, "192.168.0.3", 60002);

            if (iRes == 0)
            {
                MessageBox.Show("打开板卡成功！");
            }
            else
            {
                MessageBox.Show("打开板卡失败！");
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
            MultiCardCS_1.GA_ECatInit();
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

            strText = string.Format("正在初始化第{0:G}个站点,当前模式{0:G}，子步{0:G}", pCutInitSlaveNum, pMode, pModeStep);
            label_InitStatus.Text = strText;
        }

        private void buttonAxisOn_Click(object sender, EventArgs e)
        {
            MultiCardCS_1.GA_AxisOn((short)(comboBoxAxisSel.SelectedIndex + 1));
        }

        private void buttonJogN_MouseDown(object sender, MouseEventArgs e)
        {
            short nAxisNum = (short)(comboBoxAxisSel.SelectedIndex + 1);
            int iRes = 0;
            MultiCardCS.MultiCardCS.TJogPrm m_JogPrm;

            //加速度，单位：脉冲/毫秒/毫秒
            m_JogPrm.dAcc = 1;
            //减速度，单位：脉冲/毫秒/毫秒
            m_JogPrm.dDec = 1;
            //平滑时间(需要设置为0)
            m_JogPrm.dSmooth = 0;

            //使能轴（通常设置一次即可，不是每次必须）
            iRes = MultiCardCS_1.GA_AxisOn(nAxisNum);
            //设置为速度模式（通常设置一次即可，不是每次必须）
            iRes = MultiCardCS_1.GA_PrfJog(nAxisNum);

            //设置运动参数
            iRes = MultiCardCS_1.GA_SetJogPrm(nAxisNum, ref m_JogPrm);

            //设置速度
            iRes = MultiCardCS_1.GA_SetVel(nAxisNum, -20);

            //启动运动
            iRes = MultiCardCS_1.GA_Update(0X0001 << (nAxisNum - 1));
        }

        private void buttonJogN_MouseUp(object sender, MouseEventArgs e)
        {
            //停止所有轴和坐标系
            MultiCardCS_1.GA_Stop(0XFFFFF, 0XFFFFF);
        }

        private void buttonJogP_MouseDown(object sender, MouseEventArgs e)
        {
            short nAxisNum = (short)(comboBoxAxisSel.SelectedIndex + 1);
            int iRes = 0;
            MultiCardCS.MultiCardCS.TJogPrm m_JogPrm;

            //加速度，单位：脉冲/毫秒/毫秒
            m_JogPrm.dAcc = 1;
            //减速度，单位：脉冲/毫秒/毫秒
            m_JogPrm.dDec = 1;
            //平滑时间(需要设置为0)
            m_JogPrm.dSmooth = 0;

            //使能轴（通常设置一次即可，不是每次必须）
            iRes = MultiCardCS_1.GA_AxisOn(nAxisNum);
            //设置为速度模式（通常设置一次即可，不是每次必须）
            iRes = MultiCardCS_1.GA_PrfJog(nAxisNum);

            //设置运动参数
            iRes = MultiCardCS_1.GA_SetJogPrm(nAxisNum, ref m_JogPrm);

            //设置速度
            iRes = MultiCardCS_1.GA_SetVel(nAxisNum, 20);

            //启动运动
            iRes = MultiCardCS_1.GA_Update(0X0001 << (nAxisNum - 1));
        }

        private void buttonJogP_MouseUp(object sender, MouseEventArgs e)
        {
            //停止所有轴和坐标系
            MultiCardCS_1.GA_Stop(0XFFFFF, 0XFFFFF);
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
            short nAxisNum = (short)(comboBoxAxisSel.SelectedIndex + 1);

            MultiCardCS_1.GA_SetTrapPosAndUpdate(nAxisNum, -100000, 20, 1, 1, 0, 0, 0);
        }

        private void button4_Click_1(object sender, EventArgs e)
        {
            short nAxisNum = (short)(comboBoxAxisSel.SelectedIndex + 1);

            MultiCardCS_1.GA_SetTrapPosAndUpdate(nAxisNum, 100000, 20, 1, 1, 0, 0, 0);
        }
    }
}
