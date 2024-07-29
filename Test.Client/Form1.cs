using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Runtime.InteropServices.ComTypes;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Test.Common;

namespace Test.Client
{
	public partial class ChatForm : Form
	{
		private ChatClient _client;
		public ChatForm()
		{
			InitializeComponent();
		}

		private async void button1_Click(object sender, EventArgs e)
		{
			string ipAddress = "127.0.0.1";
			int port = 9001;//int.Parse(txtPort.Text);

			_client = new ChatClient(ipAddress, port, this);
			bool success = await _client.AuthenticateAsync(txtUsername.Text, txtPassword.Text);

			if (success)
			{
				MessageBox.Show("Login success");
				_client.Start();
			}
			else
			{
				MessageBox.Show("Authentication failed");
			}
		}

		private void textBox1_TextChanged(object sender, EventArgs e)
		{

		}



		private void btnSend_Click_Click(object sender, EventArgs e)
		{
			_client.SendMessage(txtMessage.Text);
			txtMessage.Clear();
		}

		private void listBox1_SelectedIndexChanged(object sender, EventArgs e)
		{
			
		}
		public void DisplayMessage(string message)
		{
			if (InvokeRequired)
			{
				Invoke(new Action<string>(DisplayMessage), new object[] { message });
				return;
			}

			listMessages.Items.Add(message);
			listMessages.TopIndex = listMessages.Items.Count - 1;
		}

		private void txtMessage_TextChanged(object sender, EventArgs e)
		{

		}
	}
	public class ChatClient

	{
		private TcpClient _client;
		private NetworkStream _stream;
		private ChatForm _form;

		public ChatClient(string ipAddress, int port, ChatForm form)
		{
			_client = new TcpClient();
			_client.Connect(ipAddress, port);
			_stream = _client.GetStream();
			_form = form;
		}

		public async Task<bool> AuthenticateAsync(string username, string password)
		{
			var usernameBytes = Encoding.UTF8.GetBytes(username);
			await _stream.WriteAsync(usernameBytes, 0, usernameBytes.Length);

			var passwordBytes = Encoding.UTF8.GetBytes(Cryptography.HashPassword(password));
			await _stream.WriteAsync(passwordBytes, 0, passwordBytes.Length);

			var buffer = new byte[1024];
			var bytesRead = await _stream.ReadAsync(buffer, 0, buffer.Length);
			var response = Encoding.UTF8.GetString(buffer, 0, bytesRead);

			return response == "Login Success";
		}
		public async void Start()
		{
			await Task.Run(() => ReceiveMessagesAsync());
		}

		private async Task ReceiveMessagesAsync()
		{
			const int BufferSize = 1024;
			byte[] lengthBuffer = new byte[4];
			byte[] messageBuffer = new byte[BufferSize];

			try
			{
				while (true)
				{
					int bytesRead = await _stream.ReadAsync(messageBuffer, 0, 4);
					if (bytesRead == 0) break;
					var messageLength = BitConverter.ToInt32(messageBuffer, 0);

					using (var memoryStream = new MemoryStream())
					{
						int remainingBytes = messageLength;
						while (remainingBytes > 0)
						{
							int bytesToRead = Math.Min(remainingBytes, messageBuffer.Length);
							bytesRead = await _stream.ReadAsync(messageBuffer, 0, bytesToRead);
							if (bytesRead == 0) break;
							await memoryStream.WriteAsync(messageBuffer, 0, bytesRead);
							remainingBytes -= bytesRead;
						}

						var messageBytes = memoryStream.ToArray();
						var message = Encoding.UTF8.GetString(messageBytes);
						_form.DisplayMessage(message);
					}
				}
			}
			catch (Exception ex)
			{
				_form.DisplayMessage($"Exception: {ex.Message}");
			}
		}
		public async void SendMessage(string message)
		{
			try
			{
				for (int i = 0; i < 100; i++)
				{
					var messageBuffer = Encoding.UTF8.GetBytes(message + i.ToString());
					var lengthBuffer = BitConverter.GetBytes(messageBuffer.Length);

					await _stream.WriteAsync(lengthBuffer, 0, lengthBuffer.Length);
					await _stream.WriteAsync(messageBuffer, 0, messageBuffer.Length);
				}
			}
			catch (Exception ex)
			{
				_form.DisplayMessage("Exception: " + ex.Message);
			}
		}
	}
}
