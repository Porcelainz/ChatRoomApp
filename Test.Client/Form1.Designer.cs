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
			this.txtMessage = new System.Windows.Forms.TextBox();
			this.label3 = new System.Windows.Forms.Label();
			this.listMessages = new System.Windows.Forms.ListBox();
			this.btnSend_Click = new System.Windows.Forms.Button();
			this.txtPort = new System.Windows.Forms.TextBox();
			this.label4 = new System.Windows.Forms.Label();
			this.SuspendLayout();
			// 
			// btnLogin_Click
			// 
			this.btnLogin_Click.Location = new System.Drawing.Point(230, 92);
			this.btnLogin_Click.Name = "btnLogin_Click";
			this.btnLogin_Click.Size = new System.Drawing.Size(75, 23);
			this.btnLogin_Click.TabIndex = 0;
			this.btnLogin_Click.Text = "登入";
			this.btnLogin_Click.UseVisualStyleBackColor = true;
			this.btnLogin_Click.Click += new System.EventHandler(this.button1_Click);
			// 
			// txtUsername
			// 
			this.txtUsername.Location = new System.Drawing.Point(104, 92);
			this.txtUsername.Name = "txtUsername";
			this.txtUsername.Size = new System.Drawing.Size(100, 22);
			this.txtUsername.TabIndex = 1;
			this.txtUsername.TextChanged += new System.EventHandler(this.textBox1_TextChanged);
			// 
			// txtPassword
			// 
			this.txtPassword.Location = new System.Drawing.Point(104, 129);
			this.txtPassword.Name = "txtPassword";
			this.txtPassword.PasswordChar = '*';
			this.txtPassword.Size = new System.Drawing.Size(100, 22);
			this.txtPassword.TabIndex = 2;
			// 
			// label1
			// 
			this.label1.AutoSize = true;
			this.label1.Location = new System.Drawing.Point(65, 95);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(29, 12);
			this.label1.TabIndex = 3;
			this.label1.Text = "帳號";
			// 
			// label2
			// 
			this.label2.AutoSize = true;
			this.label2.Location = new System.Drawing.Point(67, 138);
			this.label2.Name = "label2";
			this.label2.Size = new System.Drawing.Size(29, 12);
			this.label2.TabIndex = 4;
			this.label2.Text = "密碼";
			// 
			// txtMessage
			// 
			this.txtMessage.Location = new System.Drawing.Point(416, 237);
			this.txtMessage.Name = "txtMessage";
			this.txtMessage.Size = new System.Drawing.Size(139, 22);
			this.txtMessage.TabIndex = 5;
			this.txtMessage.TextChanged += new System.EventHandler(this.txtMessage_TextChanged);
			// 
			// label3
			// 
			this.label3.AutoSize = true;
			this.label3.Location = new System.Drawing.Point(414, 47);
			this.label3.Name = "label3";
			this.label3.Size = new System.Drawing.Size(41, 12);
			this.label3.TabIndex = 7;
			this.label3.Text = "留言板";
			// 
			// listMessages
			// 
			this.listMessages.FormattingEnabled = true;
			this.listMessages.ItemHeight = 12;
			this.listMessages.Location = new System.Drawing.Point(416, 81);
			this.listMessages.Name = "listMessages";
			this.listMessages.SelectionMode = System.Windows.Forms.SelectionMode.MultiExtended;
			this.listMessages.Size = new System.Drawing.Size(237, 148);
			this.listMessages.TabIndex = 8;
			this.listMessages.SelectedIndexChanged += new System.EventHandler(this.listBox1_SelectedIndexChanged);
			// 
			// btnSend_Click
			// 
			this.btnSend_Click.Location = new System.Drawing.Point(578, 235);
			this.btnSend_Click.Name = "btnSend_Click";
			this.btnSend_Click.Size = new System.Drawing.Size(75, 23);
			this.btnSend_Click.TabIndex = 9;
			this.btnSend_Click.Text = "Send";
			this.btnSend_Click.UseVisualStyleBackColor = true;
			this.btnSend_Click.Click += new System.EventHandler(this.btnSend_Click_Click);
			// 
			// txtPort
			// 
			this.txtPort.Location = new System.Drawing.Point(104, 47);
			this.txtPort.Name = "txtPort";
			this.txtPort.Size = new System.Drawing.Size(100, 22);
			this.txtPort.TabIndex = 10;
			// 
			// label4
			// 
			this.label4.AutoSize = true;
			this.label4.Location = new System.Drawing.Point(61, 50);
			this.label4.Name = "label4";
			this.label4.Size = new System.Drawing.Size(24, 12);
			this.label4.TabIndex = 11;
			this.label4.Text = "Port";
			this.label4.Click += new System.EventHandler(this.label4_Click);
			// 
			// ChatForm
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(800, 450);
			this.Controls.Add(this.label4);
			this.Controls.Add(this.txtPort);
			this.Controls.Add(this.btnSend_Click);
			this.Controls.Add(this.listMessages);
			this.Controls.Add(this.label3);
			this.Controls.Add(this.txtMessage);
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
		private System.Windows.Forms.TextBox txtMessage;
		private System.Windows.Forms.Label label3;
		private System.Windows.Forms.ListBox listMessages;
		private System.Windows.Forms.Button btnSend_Click;
		private System.Windows.Forms.TextBox txtPort;
		private System.Windows.Forms.Label label4;
	}
}

