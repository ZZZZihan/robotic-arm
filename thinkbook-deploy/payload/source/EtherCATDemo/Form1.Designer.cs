namespace CSharpDemo
{
    partial class Form1
    {
        /// <summary>
        /// 必需的设计器变量。
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// 清理所有正在使用的资源。
        /// </summary>
        /// <param name="disposing">如果应释放托管资源，为 true；否则为 false。</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows 窗体设计器生成的代码

        /// <summary>
        /// 设计器支持所需的方法 - 不要
        /// 使用代码编辑器修改此方法的内容。
        /// </summary>
        private void InitializeComponent()
        {
            this.buttonOpenCard = new System.Windows.Forms.Button();
            this.buttonOpenY0 = new System.Windows.Forms.Button();
            this.buttonCloseY0 = new System.Windows.Forms.Button();
            this.button1 = new System.Windows.Forms.Button();
            this.button2 = new System.Windows.Forms.Button();
            this.button3 = new System.Windows.Forms.Button();
            this.label_InitStatus = new System.Windows.Forms.Label();
            this.label_SlaveCount = new System.Windows.Forms.Label();
            this.comboBoxAxisSel = new System.Windows.Forms.ComboBox();
            this.buttonJogN = new System.Windows.Forms.Button();
            this.buttonJogP = new System.Windows.Forms.Button();
            this.buttonAxisOn = new System.Windows.Forms.Button();
            this.labelPrfPos = new System.Windows.Forms.Label();
            this.button4 = new System.Windows.Forms.Button();
            this.button5 = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // buttonOpenCard
            // 
            this.buttonOpenCard.Location = new System.Drawing.Point(2, 1);
            this.buttonOpenCard.Margin = new System.Windows.Forms.Padding(4);
            this.buttonOpenCard.Name = "buttonOpenCard";
            this.buttonOpenCard.Size = new System.Drawing.Size(180, 49);
            this.buttonOpenCard.TabIndex = 0;
            this.buttonOpenCard.Text = "打开板卡";
            this.buttonOpenCard.UseVisualStyleBackColor = true;
            this.buttonOpenCard.Click += new System.EventHandler(this.buttonOpenCard_Click);
            // 
            // buttonOpenY0
            // 
            this.buttonOpenY0.Location = new System.Drawing.Point(3, 486);
            this.buttonOpenY0.Margin = new System.Windows.Forms.Padding(4);
            this.buttonOpenY0.Name = "buttonOpenY0";
            this.buttonOpenY0.Size = new System.Drawing.Size(117, 49);
            this.buttonOpenY0.TabIndex = 1;
            this.buttonOpenY0.Text = "打开Y0";
            this.buttonOpenY0.UseVisualStyleBackColor = true;
            this.buttonOpenY0.Click += new System.EventHandler(this.buttonOpenY0_Click);
            // 
            // buttonCloseY0
            // 
            this.buttonCloseY0.Location = new System.Drawing.Point(128, 486);
            this.buttonCloseY0.Margin = new System.Windows.Forms.Padding(4);
            this.buttonCloseY0.Name = "buttonCloseY0";
            this.buttonCloseY0.Size = new System.Drawing.Size(117, 49);
            this.buttonCloseY0.TabIndex = 2;
            this.buttonCloseY0.Text = "关闭Y0";
            this.buttonCloseY0.UseVisualStyleBackColor = true;
            this.buttonCloseY0.Click += new System.EventHandler(this.buttonCloseY0_Click);
            // 
            // button1
            // 
            this.button1.Location = new System.Drawing.Point(2, 55);
            this.button1.Margin = new System.Windows.Forms.Padding(4);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(180, 49);
            this.button1.TabIndex = 3;
            this.button1.Text = "初始化总线";
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Click += new System.EventHandler(this.button1_Click);
            // 
            // button2
            // 
            this.button2.Location = new System.Drawing.Point(3, 112);
            this.button2.Margin = new System.Windows.Forms.Padding(4);
            this.button2.Name = "button2";
            this.button2.Size = new System.Drawing.Size(180, 49);
            this.button2.TabIndex = 4;
            this.button2.Text = "获取初始化状态";
            this.button2.UseVisualStyleBackColor = true;
            this.button2.Click += new System.EventHandler(this.button2_Click);
            // 
            // button3
            // 
            this.button3.Location = new System.Drawing.Point(2, 169);
            this.button3.Margin = new System.Windows.Forms.Padding(4);
            this.button3.Name = "button3";
            this.button3.Size = new System.Drawing.Size(180, 49);
            this.button3.TabIndex = 5;
            this.button3.Text = "获取从站数量";
            this.button3.UseVisualStyleBackColor = true;
            this.button3.Click += new System.EventHandler(this.button3_Click);
            // 
            // label_InitStatus
            // 
            this.label_InitStatus.AutoSize = true;
            this.label_InitStatus.Location = new System.Drawing.Point(191, 129);
            this.label_InitStatus.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label_InitStatus.Name = "label_InitStatus";
            this.label_InitStatus.Size = new System.Drawing.Size(82, 15);
            this.label_InitStatus.TabIndex = 8;
            this.label_InitStatus.Text = "尚未初始化";
            // 
            // label_SlaveCount
            // 
            this.label_SlaveCount.AutoSize = true;
            this.label_SlaveCount.Location = new System.Drawing.Point(191, 186);
            this.label_SlaveCount.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label_SlaveCount.Name = "label_SlaveCount";
            this.label_SlaveCount.Size = new System.Drawing.Size(15, 15);
            this.label_SlaveCount.TabIndex = 9;
            this.label_SlaveCount.Text = "0";
            // 
            // comboBoxAxisSel
            // 
            this.comboBoxAxisSel.FormattingEnabled = true;
            this.comboBoxAxisSel.Location = new System.Drawing.Point(327, 15);
            this.comboBoxAxisSel.Margin = new System.Windows.Forms.Padding(4);
            this.comboBoxAxisSel.Name = "comboBoxAxisSel";
            this.comboBoxAxisSel.Size = new System.Drawing.Size(160, 23);
            this.comboBoxAxisSel.TabIndex = 10;
            // 
            // buttonJogN
            // 
            this.buttonJogN.Location = new System.Drawing.Point(327, 158);
            this.buttonJogN.Margin = new System.Windows.Forms.Padding(4);
            this.buttonJogN.Name = "buttonJogN";
            this.buttonJogN.Size = new System.Drawing.Size(117, 49);
            this.buttonJogN.TabIndex = 11;
            this.buttonJogN.Text = "反转";
            this.buttonJogN.UseVisualStyleBackColor = true;
            this.buttonJogN.MouseDown += new System.Windows.Forms.MouseEventHandler(this.buttonJogN_MouseDown);
            this.buttonJogN.MouseUp += new System.Windows.Forms.MouseEventHandler(this.buttonJogN_MouseUp);
            // 
            // buttonJogP
            // 
            this.buttonJogP.Location = new System.Drawing.Point(456, 158);
            this.buttonJogP.Margin = new System.Windows.Forms.Padding(4);
            this.buttonJogP.Name = "buttonJogP";
            this.buttonJogP.Size = new System.Drawing.Size(117, 49);
            this.buttonJogP.TabIndex = 12;
            this.buttonJogP.Text = "正转";
            this.buttonJogP.UseVisualStyleBackColor = true;
            this.buttonJogP.MouseDown += new System.Windows.Forms.MouseEventHandler(this.buttonJogP_MouseDown);
            this.buttonJogP.MouseUp += new System.Windows.Forms.MouseEventHandler(this.buttonJogP_MouseUp);
            // 
            // buttonAxisOn
            // 
            this.buttonAxisOn.Location = new System.Drawing.Point(327, 49);
            this.buttonAxisOn.Margin = new System.Windows.Forms.Padding(4);
            this.buttonAxisOn.Name = "buttonAxisOn";
            this.buttonAxisOn.Size = new System.Drawing.Size(117, 49);
            this.buttonAxisOn.TabIndex = 13;
            this.buttonAxisOn.Text = "使能";
            this.buttonAxisOn.UseVisualStyleBackColor = true;
            this.buttonAxisOn.Click += new System.EventHandler(this.buttonAxisOn_Click);
            // 
            // labelPrfPos
            // 
            this.labelPrfPos.AutoSize = true;
            this.labelPrfPos.Location = new System.Drawing.Point(472, 71);
            this.labelPrfPos.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.labelPrfPos.Name = "labelPrfPos";
            this.labelPrfPos.Size = new System.Drawing.Size(15, 15);
            this.labelPrfPos.TabIndex = 14;
            this.labelPrfPos.Text = "0";
            // 
            // button4
            // 
            this.button4.Location = new System.Drawing.Point(456, 225);
            this.button4.Margin = new System.Windows.Forms.Padding(4);
            this.button4.Name = "button4";
            this.button4.Size = new System.Drawing.Size(117, 49);
            this.button4.TabIndex = 16;
            this.button4.Text = "移动到100000";
            this.button4.UseVisualStyleBackColor = true;
            this.button4.Click += new System.EventHandler(this.button4_Click_1);
            // 
            // button5
            // 
            this.button5.Location = new System.Drawing.Point(330, 225);
            this.button5.Margin = new System.Windows.Forms.Padding(4);
            this.button5.Name = "button5";
            this.button5.Size = new System.Drawing.Size(117, 49);
            this.button5.TabIndex = 15;
            this.button5.Text = "移动到-100000";
            this.button5.UseVisualStyleBackColor = true;
            this.button5.Click += new System.EventHandler(this.button5_Click_1);
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(919, 538);
            this.Controls.Add(this.button4);
            this.Controls.Add(this.button5);
            this.Controls.Add(this.labelPrfPos);
            this.Controls.Add(this.buttonAxisOn);
            this.Controls.Add(this.buttonJogP);
            this.Controls.Add(this.buttonJogN);
            this.Controls.Add(this.comboBoxAxisSel);
            this.Controls.Add(this.label_SlaveCount);
            this.Controls.Add(this.label_InitStatus);
            this.Controls.Add(this.button3);
            this.Controls.Add(this.button2);
            this.Controls.Add(this.button1);
            this.Controls.Add(this.buttonCloseY0);
            this.Controls.Add(this.buttonOpenY0);
            this.Controls.Add(this.buttonOpenCard);
            this.Margin = new System.Windows.Forms.Padding(4);
            this.Name = "Form1";
            this.Text = "EtherCAT Demo - ThinkBook x64";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button buttonOpenCard;
        private System.Windows.Forms.Button buttonOpenY0;
        private System.Windows.Forms.Button buttonCloseY0;
        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.Button button2;
        private System.Windows.Forms.Button button3;
        private System.Windows.Forms.Label label_InitStatus;
        private System.Windows.Forms.Label label_SlaveCount;
        private System.Windows.Forms.ComboBox comboBoxAxisSel;
        private System.Windows.Forms.Button buttonJogN;
        private System.Windows.Forms.Button buttonJogP;
        private System.Windows.Forms.Button buttonAxisOn;
        private System.Windows.Forms.Label labelPrfPos;
        private System.Windows.Forms.Button button4;
        private System.Windows.Forms.Button button5;
    }
}

