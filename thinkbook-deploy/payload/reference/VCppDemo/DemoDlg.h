
// DemoDlg.h : 头文件
//

#pragma once
#include "afxwin.h"


// CDemoDlg 对话框
class CDemoDlg : public CDialogEx
{
// 构造
public:
	CDemoDlg(CWnd* pParent = NULL);	// 标准构造函数

// 对话框数据
	enum { IDD = IDD_DEMO_DIALOG };

	protected:
	virtual void DoDataExchange(CDataExchange* pDX);	// DDX/DDV 支持


// 实现
protected:
	HICON m_hIcon;

	// 生成的消息映射函数
	virtual BOOL OnInitDialog();
	afx_msg void OnSysCommand(UINT nID, LPARAM lParam);
	afx_msg void OnPaint();
	afx_msg HCURSOR OnQueryDragIcon();
	DECLARE_MESSAGE_MAP()
public:

	int iOpenFlag;
	afx_msg void OnBnClickedButtonOpenCard();
	afx_msg void OnTimer(UINT_PTR nIDEvent);
	virtual BOOL PreTranslateMessage(MSG* pMsg);
	CComboBox m_ComboBoxAxis;
	afx_msg void OnBnClickedButton1();
	afx_msg void OnBnClickedButton2();
	afx_msg void OnBnClickedButton3();
	afx_msg void OnBnClickedButtonJogn();
	afx_msg void OnBnClickedButtonOpenCard2();
	afx_msg void OnBnClickedButtonJogn2();
	afx_msg void OnBnClickedButton4();
};
