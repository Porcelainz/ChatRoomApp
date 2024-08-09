namespace Test.Client
{
	partial class ChatForm
	{
		/// <summary>
		/// 設計工具所需的變數。
		/// </summary>
		private System.ComponentModel.IContainer components = null;

		/// <summary>
		/// 清除任何使用中的資源。
		/// </summary>
		/// <param name="disposing">如果應該處置受控資源則為 true，否則為 false。</param>
		protected override void Dispose(bool disposing)
		{
			if (disposing && (components != null))
			{
				components.Dispose();
			}
			base.Dispose(disposing);
		}

		#region Windows Form 設計工具產生的程式碼

		/// <summary>
		/// 此為設計工具支援所需的方法 - 請勿使用程式碼編輯器修改
		/// 這個方法的內容。
		/// </summary>
		private void InitializeComponent()
		{
			this.btnLogin_Click = new System.Windows.Forms.Button();
			this.txtUsername = new System.Windows.Forms.TextBox();
			this.txtPassword = new System.Windows.Forms.TextBox();
			this.label1 = new System.Windows.Forms.Label();
			this.label2 = new System.Windows.Forms.Label();
			this.listMessages = new System.Windows.Forms.ListBox();
			this.SendMessageButton_Click = new System.Windows.Forms.Button();
			this.button1 = new System.Windows.Forms.Button();
			this.button2 = new System.Windows.Forms.Button();
			this.ExportDataFromPG = new System.Windows.Forms.Button();
			this.ExportDataFromRedis = new System.Windows.Forms.Button();
			this.SuspendLayout();
			// 
			// btnLogin_Click
			// 
			this.btnLogin_Click.Location = new System.Drawing.Point(37, 21);
			this.btnLogin_Click.Name = "btnLogin_Click";
			this.btnLogin_Click.Size = new System.Drawing.Size(110, 23);
			this.btnLogin_Click.TabIndex = 0;
			this.btnLogin_Click.Text = "登入100個使用者";
			this.btnLogin_Click.UseVisualStyleBackColor = true;
			this.btnLogin_Click.Click += new System.EventHandler(this.button1_Click);
			// 
			// txtUsername
			// 
			this.txtUsername.AccessibleRole = System.Windows.Forms.AccessibleRole.None;
			this.txtUsername.Location = new System.Drawing.Point(71, 100);
			this.txtUsername.Name = "txtUsername";
			this.txtUsername.ReadOnly = true;
			this.txtUsername.Size = new System.Drawing.Size(100, 22);
			this.txtUsername.TabIndex = 1;
			this.txtUsername.Text = "casey.yang_test";
			this.txtUsername.TextChanged += new System.EventHandler(this.textBox1_TextChanged);
			// 
			// txtPassword
			// 
			this.txtPassword.Location = new System.Drawing.Point(71, 133);
			this.txtPassword.Name = "txtPassword";
			this.txtPassword.PasswordChar = '*';
			this.txtPassword.ReadOnly = true;
			this.txtPassword.Size = new System.Drawing.Size(100, 22);
			this.txtPassword.TabIndex = 2;
			this.txtPassword.Text = "Wan@1234";
			// 
			// label1
			// 
			this.label1.AutoSize = true;
			this.label1.Location = new System.Drawing.Point(12, 103);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(53, 12);
			this.label1.TabIndex = 3;
			this.label1.Text = "搶登帳號";
			// 
			// label2
			// 
			this.label2.AutoSize = true;
			this.label2.Location = new System.Drawing.Point(12, 133);
			this.label2.Name = "label2";
			this.label2.Size = new System.Drawing.Size(53, 12);
			this.label2.TabIndex = 4;
			this.label2.Text = "搶登密碼";
			this.label2.Click += new System.EventHandler(this.label2_Click);
			// 
			// listMessages
			// 
			this.listMessages.FormattingEnabled = true;
			this.listMessages.ItemHeight = 12;
			this.listMessages.Location = new System.Drawing.Point(177, 21);
			this.listMessages.Name = "listMessages";
			this.listMessages.SelectionMode = System.Windows.Forms.SelectionMode.MultiExtended;
			this.listMessages.Size = new System.Drawing.Size(316, 136);
			this.listMessages.TabIndex = 8;
			this.listMessages.SelectedIndexChanged += new System.EventHandler(this.listBox1_SelectedIndexChanged);
			// 
			// SendMessageButton_Click
			// 
			this.SendMessageButton_Click.Location = new System.Drawing.Point(37, 50);
			this.SendMessageButton_Click.Name = "SendMessageButton_Click";
			this.SendMessageButton_Click.Size = new System.Drawing.Size(110, 23);
			this.SendMessageButton_Click.TabIndex = 12;
			this.SendMessageButton_Click.Text = "發送100筆訊息每個使用者";
			this.SendMessageButton_Click.UseVisualStyleBackColor = true;
			this.SendMessageButton_Click.Click += new System.EventHandler(this.button1_Click_1);
			// 
			// button1
			// 
			this.button1.Location = new System.Drawing.Point(12, 177);
			this.button1.Name = "button1";
			this.button1.Size = new System.Drawing.Size(110, 23);
			this.button1.TabIndex = 13;
			this.button1.Text = "搶登測試 伺服器1";
			this.button1.UseVisualStyleBackColor = true;
			this.button1.Click += new System.EventHandler(this.button1_Click_2);
			// 
			// button2
			// 
			this.button2.Location = new System.Drawing.Point(128, 177);
			this.button2.Name = "button2";
			this.button2.Size = new System.Drawing.Size(110, 23);
			this.button2.TabIndex = 14;
			this.button2.Text = "搶登測試 伺服器2";
			this.button2.UseVisualStyleBackColor = true;
			this.button2.Click += new System.EventHandler(this.button2_Click);
			// 
			// ExportDataFromPG
			// 
			this.ExportDataFromPG.Location = new System.Drawing.Point(271, 177);
			this.ExportDataFromPG.Name = "ExportDataFromPG";
			this.ExportDataFromPG.Size = new System.Drawing.Size(119, 23);
			this.ExportDataFromPG.TabIndex = 15;
			this.ExportDataFromPG.Text = "匯出PostGreSql資料";
			this.ExportDataFromPG.UseVisualStyleBackColor = true;
			this.ExportDataFromPG.Click += new System.EventHandler(this.ExportDataFromPG_Click);
			// 
			// ExportDataFromRedis
			// 
			this.ExportDataFromRedis.Location = new System.Drawing.Point(399, 177);
			this.ExportDataFromRedis.Name = "ExportDataFromRedis";
			this.ExportDataFromRedis.Size = new System.Drawing.Size(94, 23);
			this.ExportDataFromRedis.TabIndex = 16;
			this.ExportDataFromRedis.Text = "匯出Redis資料";
			this.ExportDataFromRedis.UseVisualStyleBackColor = true;
			this.ExportDataFromRedis.Click += new System.EventHandler(this.ExportDataFromRedis_Click);
			// 
			// ChatForm
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(505, 210);
			this.Controls.Add(this.ExportDataFromRedis);
			this.Controls.Add(this.ExportDataFromPG);
			this.Controls.Add(this.button2);
			this.Controls.Add(this.button1);
			this.Controls.Add(this.SendMessageButton_Click);
			this.Controls.Add(this.listMessages);
			this.Controls.Add(this.label2);
			this.Controls.Add(this.label1);
			this.Controls.Add(this.txtPassword);
			this.Controls.Add(this.txtUsername);
			this.Controls.Add(this.btnLogin_Click);
			this.Name = "ChatForm";
			this.Text = "ChatForm";
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.Button btnLogin_Click;
		private System.Windows.Forms.TextBox txtUsername;
		private System.Windows.Forms.TextBox txtPassword;
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.Label label2;
		private System.Windows.Forms.ListBox listMessages;
		private System.Windows.Forms.Button SendMessageButton_Click;
		private System.Windows.Forms.Button button1;
		private System.Windows.Forms.Button button2;
		private System.Windows.Forms.Button ExportDataFromPG;
		private System.Windows.Forms.Button ExportDataFromRedis;
	}
}

