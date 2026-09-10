
// DemoDlg.cpp : 实现文件
//

#include "stdafx.h"
#include "Demo.h"
#include "DemoDlg.h"
#include "afxdialogex.h"

#include "MultiCardCPP.h"
#pragma comment(lib,"..\\Debug\\MultiCard.lib")

//一个板卡就声明1个对象，多个板卡就声明多个对象，各个对象之间互不干涉
MultiCard g_MultiCard;

#ifdef _DEBUG
#define new DEBUG_NEW
#endif


// 用于应用程序“关于”菜单项的 CAboutDlg 对话框

class CAboutDlg : public CDialogEx
{
public:
	CAboutDlg();

// 对话框数据
	enum { IDD = IDD_ABOUTBOX };

	protected:
	virtual void DoDataExchange(CDataExchange* pDX);    // DDX/DDV 支持

// 实现
protected:
	DECLARE_MESSAGE_MAP()
};

CAboutDlg::CAboutDlg() : CDialogEx(CAboutDlg::IDD)
{
}

void CAboutDlg::DoDataExchange(CDataExchange* pDX)
{
	CDialogEx::DoDataExchange(pDX);
}

BEGIN_MESSAGE_MAP(CAboutDlg, CDialogEx)
END_MESSAGE_MAP()


// CDemoDlg 对话框




CDemoDlg::CDemoDlg(CWnd* pParent /*=NULL*/)
	: CDialogEx(CDemoDlg::IDD, pParent)
{
	m_hIcon = AfxGetApp()->LoadIcon(IDR_MAINFRAME);
}

void CDemoDlg::DoDataExchange(CDataExchange* pDX)
{
	CDialogEx::DoDataExchange(pDX);
	DDX_Control(pDX, IDC_COMBO_AXIS_SEL, m_ComboBoxAxis);
}

BEGIN_MESSAGE_MAP(CDemoDlg, CDialogEx)
	ON_WM_SYSCOMMAND()
	ON_WM_PAINT()
	ON_WM_QUERYDRAGICON()
	ON_BN_CLICKED(IDC_BUTTON_OPEN_CARD, &CDemoDlg::OnBnClickedButtonOpenCard)
	ON_WM_TIMER()
	ON_BN_CLICKED(IDC_BUTTON1, &CDemoDlg::OnBnClickedButton1)
	ON_BN_CLICKED(IDC_BUTTON2, &CDemoDlg::OnBnClickedButton2)
	ON_BN_CLICKED(IDC_BUTTON3, &CDemoDlg::OnBnClickedButton3)
	ON_BN_CLICKED(IDC_BUTTON_JOGN, &CDemoDlg::OnBnClickedButtonJogn)
	ON_BN_CLICKED(IDC_BUTTON_OPEN_CARD2, &CDemoDlg::OnBnClickedButtonOpenCard2)
	ON_BN_CLICKED(IDC_BUTTON_JOGN2, &CDemoDlg::OnBnClickedButtonJogn2)
	ON_BN_CLICKED(IDC_BUTTON4, &CDemoDlg::OnBnClickedButton4)
END_MESSAGE_MAP()


// CDemoDlg 消息处理程序

BOOL CDemoDlg::OnInitDialog()
{
	CDialogEx::OnInitDialog();

	// 将“关于...”菜单项添加到系统菜单中。

	// IDM_ABOUTBOX 必须在系统命令范围内。
	ASSERT((IDM_ABOUTBOX & 0xFFF0) == IDM_ABOUTBOX);
	ASSERT(IDM_ABOUTBOX < 0xF000);

	CMenu* pSysMenu = GetSystemMenu(FALSE);
	if (pSysMenu != NULL)
	{
		BOOL bNameValid;
		CString strAboutMenu;
		bNameValid = strAboutMenu.LoadString(IDS_ABOUTBOX);
		ASSERT(bNameValid);
		if (!strAboutMenu.IsEmpty())
		{
			pSysMenu->AppendMenu(MF_SEPARATOR);
			pSysMenu->AppendMenu(MF_STRING, IDM_ABOUTBOX, strAboutMenu);
		}
	}

	// 设置此对话框的图标。当应用程序主窗口不是对话框时，框架将自动
	//  执行此操作
	SetIcon(m_hIcon, TRUE);			// 设置大图标
	SetIcon(m_hIcon, FALSE);		// 设置小图标

	// TODO: 在此添加额外的初始化代码

	m_ComboBoxAxis.AddString("轴1（Axis1）");
	m_ComboBoxAxis.AddString("轴2（Axis2）");
	m_ComboBoxAxis.AddString("轴3（Axis3）");
	m_ComboBoxAxis.AddString("轴4（Axis4）");
	m_ComboBoxAxis.AddString("轴5（Axis5）");
	m_ComboBoxAxis.AddString("轴6（Axis6）");
	m_ComboBoxAxis.AddString("轴7（Axis7）");
	m_ComboBoxAxis.AddString("轴8（Axis8）");

	m_ComboBoxAxis.AddString("轴9（Axis9）");
	m_ComboBoxAxis.AddString("轴10（Axis10）");
	m_ComboBoxAxis.AddString("轴11（Axis11）");
	m_ComboBoxAxis.AddString("轴12（Axis12）");
	m_ComboBoxAxis.AddString("轴13（Axis13）");
	m_ComboBoxAxis.AddString("轴14（Axis14）");
	m_ComboBoxAxis.AddString("轴15（Axis15）");
	m_ComboBoxAxis.AddString("轴16（Axis16）");

	m_ComboBoxAxis.SetCurSel(0);

	iOpenFlag = 0;
	SetTimer(1,20,NULL);

	return TRUE;  // 除非将焦点设置到控件，否则返回 TRUE
}

void CDemoDlg::OnSysCommand(UINT nID, LPARAM lParam)
{
	if ((nID & 0xFFF0) == IDM_ABOUTBOX)
	{
		CAboutDlg dlgAbout;
		dlgAbout.DoModal();
	}
	else
	{
		CDialogEx::OnSysCommand(nID, lParam);
	}
}

// 如果向对话框添加最小化按钮，则需要下面的代码
//  来绘制该图标。对于使用文档/视图模型的 MFC 应用程序，
//  这将由框架自动完成。

void CDemoDlg::OnPaint()
{
	if (IsIconic())
	{
		CPaintDC dc(this); // 用于绘制的设备上下文

		SendMessage(WM_ICONERASEBKGND, reinterpret_cast<WPARAM>(dc.GetSafeHdc()), 0);

		// 使图标在工作区矩形中居中
		int cxIcon = GetSystemMetrics(SM_CXICON);
		int cyIcon = GetSystemMetrics(SM_CYICON);
		CRect rect;
		GetClientRect(&rect);
		int x = (rect.Width() - cxIcon + 1) / 2;
		int y = (rect.Height() - cyIcon + 1) / 2;

		// 绘制图标
		dc.DrawIcon(x, y, m_hIcon);
	}
	else
	{
		CDialogEx::OnPaint();
	}
}

//当用户拖动最小化窗口时系统调用此函数取得光标
//显示。
HCURSOR CDemoDlg::OnQueryDragIcon()
{
	return static_cast<HCURSOR>(m_hIcon);
}



void CDemoDlg::OnBnClickedButtonOpenCard()
{
	int iRes;

	iRes = g_MultiCard.MC_Open(1,"192.168.0.200",60000,"192.168.0.1",60000);

	if(iRes)
	{
		MessageBox("Open Card Fail,Please turn off wifi ,check PC IP address or connection!");
	}
	else
	{
		MessageBox("Open Card Successful!");
		g_MultiCard.MC_Reset();

		iOpenFlag = 1;
	}
}

unsigned long  ulTimerCount = 0;
void CDemoDlg::OnTimer(UINT_PTR nIDEvent)
{
	int iRes;
	CString strText;

	int iAxisIndex = m_ComboBoxAxis.GetCurSel();

	// TODO: 在此添加消息处理程序代码和/或调用默认值
	TAllSysStatusDataSX m_AllSysStatusDataSXTemp;

	//读取板卡状态数据。
	iRes = g_MultiCard.MC_GetAllSysStatusSX(&m_AllSysStatusDataSXTemp);

	if(iRes && iOpenFlag)
	{
		g_MultiCard.MC_SetExtDoBit(0,2,0);
		AfxMessageBox("读取状态失败！");
	}

	strText.Format("%d",m_AllSysStatusDataSXTemp.lAxisPrfPos[0]);
	GetDlgItem(IDC_STATIC_PRF_POS_1)->SetWindowText(strText);

	strText.Format("%d",m_AllSysStatusDataSXTemp.lAxisPrfPos[1]);
	GetDlgItem(IDC_STATIC_PRF_POS_2)->SetWindowText(strText);

	strText.Format("%d",m_AllSysStatusDataSXTemp.lAxisPrfPos[2]);
	GetDlgItem(IDC_STATIC_PRF_POS_3)->SetWindowText(strText);

	strText.Format("%d",m_AllSysStatusDataSXTemp.lAxisPrfPos[3]);
	GetDlgItem(IDC_STATIC_PRF_POS_4)->SetWindowText(strText);

	strText.Format("%d",m_AllSysStatusDataSXTemp.lAxisPrfPos[4]);
	GetDlgItem(IDC_STATIC_PRF_POS_5)->SetWindowText(strText);

	strText.Format("%d",m_AllSysStatusDataSXTemp.lAxisPrfPos[5]);
	GetDlgItem(IDC_STATIC_PRF_POS_6)->SetWindowText(strText);

	strText.Format("%d",m_AllSysStatusDataSXTemp.lAxisPrfPos[6]);
	GetDlgItem(IDC_STATIC_PRF_POS_7)->SetWindowText(strText);

	strText.Format("%d",m_AllSysStatusDataSXTemp.lAxisPrfPos[7]);
	GetDlgItem(IDC_STATIC_PRF_POS_8)->SetWindowText(strText);

	strText.Format("%d",m_AllSysStatusDataSXTemp.lAxisPrfPos[8]);
	GetDlgItem(IDC_STATIC_PRF_POS_9)->SetWindowText(strText);

	strText.Format("%d",m_AllSysStatusDataSXTemp.lAxisPrfPos[9]);
	GetDlgItem(IDC_STATIC_PRF_POS_10)->SetWindowText(strText);

	strText.Format("%d",m_AllSysStatusDataSXTemp.lAxisPrfPos[10]);
	GetDlgItem(IDC_STATIC_PRF_POS_11)->SetWindowText(strText);

	strText.Format("%d",m_AllSysStatusDataSXTemp.lAxisPrfPos[11]);
	GetDlgItem(IDC_STATIC_PRF_POS_12)->SetWindowText(strText);

	strText.Format("%d",m_AllSysStatusDataSXTemp.lAxisPrfPos[12]);
	GetDlgItem(IDC_STATIC_PRF_POS_13)->SetWindowText(strText);

	strText.Format("%d",m_AllSysStatusDataSXTemp.lAxisPrfPos[13]);
	GetDlgItem(IDC_STATIC_PRF_POS_14)->SetWindowText(strText);

	strText.Format("%d",m_AllSysStatusDataSXTemp.lAxisPrfPos[14]);
	GetDlgItem(IDC_STATIC_PRF_POS_15)->SetWindowText(strText);

	strText.Format("%d",m_AllSysStatusDataSXTemp.lAxisPrfPos[15]);
	GetDlgItem(IDC_STATIC_PRF_POS_16)->SetWindowText(strText);

	strText.Format("板卡剩余段数：%d",m_AllSysStatusDataSXTemp.lRemainderSegNum[0]);
	GetDlgItem(IDC_STATIC_REMAIN_SEG)->SetWindowText(strText);


	if(m_AllSysStatusDataSXTemp.lAxisStatus[iAxisIndex] & AXIS_STATUS_RUNNING)
	{
		((CButton*)GetDlgItem(IDC_RADIO_AXIS_STATUS_RUNNING))->SetCheck(true);
	}
	else
	{
		((CButton*)GetDlgItem(IDC_RADIO_AXIS_STATUS_RUNNING))->SetCheck(false);
	}

	short nCurSlaveNum;
	short nMode;
	short nModeStep;
	g_MultiCard.MC_ECatGetInitStep(&nCurSlaveNum,&nMode,&nModeStep);

	strText.Format("正在初始化从站%d，步骤%d，子步骤%d",nCurSlaveNum,nMode,nModeStep);
	GetDlgItem(IDC_STATIC_ECAT_INIT_STEP)->SetWindowText(strText);

	ulTimerCount++;

	if(0 == (ulTimerCount % 200))
	{
		g_MultiCard.MC_SetExtDoBit(0,2,1);
	}
	else if(0 == (ulTimerCount % 100))
	{
		g_MultiCard.MC_SetExtDoBit(0,2,0);
	}
	CDialogEx::OnTimer(nIDEvent);
}


BOOL CDemoDlg::PreTranslateMessage(MSG* pMsg)
{
	switch(pMsg->message)
	{
	case WM_LBUTTONDOWN:

		UpdateData(TRUE);
		//负向
		if(pMsg->hwnd == GetDlgItem(IDC_BUTTON_JOGN)->m_hWnd)
		{
			int iRes = 0;
			TJogPrm m_JogPrm;

			int iAxisNum = m_ComboBoxAxis.GetCurSel()+1;

			m_JogPrm.dAcc = 0.1;
			m_JogPrm.dDec = 0.1;
			m_JogPrm.dSmooth = 0;

			iRes = g_MultiCard.MC_PrfJog(iAxisNum);

			iRes += g_MultiCard.MC_SetJogPrm(iAxisNum,&m_JogPrm);

			iRes += g_MultiCard.MC_SetVel(iAxisNum,-20);

			iRes += g_MultiCard.MC_Update(0X0001 << (iAxisNum-1));

			if(0 == iRes)
			{
				TRACE("负向连续移动......\r\n");
			}
		}
		//正向
		if(pMsg->hwnd == GetDlgItem(IDC_BUTTON_JOGP)->m_hWnd)
		{
			int iRes = 0;
			TJogPrm m_JogPrm;

			int iAxisNum = m_ComboBoxAxis.GetCurSel()+1;

			m_JogPrm.dAcc = 0.1;
			m_JogPrm.dDec = 0.1;
			m_JogPrm.dSmooth = 0;

			iRes = g_MultiCard.MC_PrfJog(iAxisNum);

			iRes += g_MultiCard.MC_SetJogPrm(iAxisNum,&m_JogPrm);

			iRes += g_MultiCard.MC_SetVel(iAxisNum,20);

			iRes += g_MultiCard.MC_Update(0X0001 << (iAxisNum-1));

			if(0 == iRes)
			{
				TRACE("正向连续移动......\r\n");
			}
		}
		break;
	case WM_LBUTTONUP:
		//负向
		if(pMsg->hwnd == GetDlgItem(IDC_BUTTON_JOGN)->m_hWnd)
		{
			g_MultiCard.MC_Stop(0XFFFF,0XFFFF);
		}
		//正向
		if(pMsg->hwnd == GetDlgItem(IDC_BUTTON_JOGP)->m_hWnd)
		{
			g_MultiCard.MC_Stop(0XFFFF,0XFFFF);
		}
		break;
	}
	return CDialogEx::PreTranslateMessage(pMsg);

	return CDialogEx::PreTranslateMessage(pMsg);
}


void CDemoDlg::OnBnClickedButton1()
{
	int iRes;

	TCrdPrm CrdPrmTemp;

	CrdPrmTemp.dimension = 2;
	CrdPrmTemp.evenTime = 0;
	CrdPrmTemp.originPos[0] = 0;
	CrdPrmTemp.originPos[1] = 0;
	CrdPrmTemp.originPos[2] = 0;
	CrdPrmTemp.originPos[3] = 0;
	CrdPrmTemp.originPos[4] = 0;
	CrdPrmTemp.originPos[5] = 0;
	CrdPrmTemp.originPos[6] = 0;
	CrdPrmTemp.originPos[7] = 0;

	CrdPrmTemp.profile[0] = 1;
	CrdPrmTemp.profile[1] = 2;
	CrdPrmTemp.profile[2] = 3;
	CrdPrmTemp.profile[3] = 0;
	CrdPrmTemp.profile[4] = 0;
	CrdPrmTemp.profile[5] = 0;
	CrdPrmTemp.profile[6] = 0;
	CrdPrmTemp.profile[7] = 0;

	CrdPrmTemp.setOriginFlag = 1;
	CrdPrmTemp.synAccMax = 1;
	CrdPrmTemp.synVelMax = 1000;

	iRes = g_MultiCard.MC_SetCrdPrm(1,&CrdPrmTemp);

	if(iRes)
	{
		AfxMessageBox("建立坐标系失败！");
	}
	else
	{
		AfxMessageBox("建立坐标系成功！");
	}
}


void CDemoDlg::OnBnClickedButton2()
{
	g_MultiCard.MC_LnXY(1,10000,0,20,1,0,0,0);
	g_MultiCard.MC_LnXY(1,0,0,20,1,0,0,0);

	g_MultiCard.MC_LnXY(1,10000,0,20,1,0,0,0);
	g_MultiCard.MC_LnXY(1,0,0,20,1,0,0,0);

	g_MultiCard.MC_LnXY(1,10000,0,20,1,0,0,0);
	g_MultiCard.MC_LnXY(1,0,0,20,1,0,0,0);
}


void CDemoDlg::OnBnClickedButton3()
{
	g_MultiCard.MC_CrdStart(1,0);
}


void CDemoDlg::OnBnClickedButtonJogn()
{
	// TODO: 在此添加控件通知处理程序代码
}


void CDemoDlg::OnBnClickedButtonOpenCard2()
{
	g_MultiCard.MC_ECatInit();
}


void CDemoDlg::OnBnClickedButtonJogn2()
{
	int iAxisNum = m_ComboBoxAxis.GetCurSel()+1;

	g_MultiCard.MC_AxisOn(iAxisNum);
}


void CDemoDlg::OnBnClickedButton4()
{
	g_MultiCard.MC_Stop(0X10000,0X10000);
}
