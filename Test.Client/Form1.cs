using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Runtime.InteropServices.ComTypes;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Test.Common;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.StartPanel;

namespace Test.Client
{
	public partial class ChatForm : Form
	{
		//private ChatClient _client;
		private List<ChatClient> _clients = new List<ChatClient>();
		private readonly Random _random = new Random();
		private const int ClientCount = 100;
		private const int MessageCount = 100;
		private const string MessageToSend = "我們的成功不僅僅體現在業績的增長上，更在於我們每一位員工的成長和進步。For example, the new training programs and development initiatives we introduced have significantly enhanced our skills and capabilities. We have seen many of our team members take on new roles and responsibilities, demonstrating their growth and commitment. 我們的團隊合作和協作精神也是我們成功的關鍵。The way everyone supports each other, shares knowledge, and works together towards common goals is truly inspiring.";
		List<Task> tasks = new List<Task>();
		List<TaskCompletionSource<bool>> taskSources = new List<TaskCompletionSource<bool>>();

		public ChatForm()
		{
			InitializeComponent();
			string username = "casey.yang";

			for (int i = 1; i <= ClientCount; i++)
			{
				int port = _random.Next(2) == 0 ? 9000 : 9001;

				var client = new ChatClient("127.0.0.1", port, this, username + i);
				_clients.Add(client);

				var tcs = new TaskCompletionSource<bool>();
				var count = i;
				taskSources.Add(tcs);
				tasks.Add(Task.Run(async () =>
				{
					await tcs.Task; // 等待信號
					await LoginClientAsync(client, count);
				}));
			}
		}

		private async void button1_Click(object sender, EventArgs e)
		{
			await StartClientsAsync();
		}

		private void textBox1_TextChanged(object sender, EventArgs e)
		{
		}

		private void listBox1_SelectedIndexChanged(object sender, EventArgs e)
		{
		}
		public void DisplayMessage(string message)
		{
			if (InvokeRequired)
			{
				BeginInvoke(new Action<string>(DisplayMessage), new object[] { message });
				return;
			}
			listMessages.Items.Add(message);
		}

		private void txtMessage_TextChanged(object sender, EventArgs e)
		{

		}

		private void label4_Click(object sender, EventArgs e)
		{

		}
		private async Task StartClientsAsync()
		{
			Stopwatch timer = new Stopwatch();
			timer.Start();
			// 同時啟動所有任務
			foreach (var tcs in taskSources)
			{
				tcs.SetResult(true);
			}
			await Task.WhenAll(tasks);
			timer.Stop();
			DisplayMessage($"所有用戶登入完成，共耗時 {timer.ElapsedMilliseconds} 毫秒。");
			MessageBox.Show($"{ClientCount} 個客戶端成功登入。");
		}
		private async Task SendMessagesAsync()
		{
			var tasks = new List<Task>();
			Stopwatch timer = new Stopwatch();
			timer.Start();
			foreach (var client in _clients)
			{
				tasks.Add(Task.Run(() => SendClientMessagesAsync(client)));
			}
			await Task.WhenAll(tasks);
			timer.Stop();
			DisplayMessage($"所有訊息已發送，共耗時 {timer.ElapsedMilliseconds} 毫秒。");
		}
		private async Task SendClientMessagesAsync(ChatClient client)
		{
			for (int i = 0; i < MessageCount; i++)
			{
				await client.SendMessageAsync(i + MessageToSend);
			}
		}
		private async Task LoginClientAsync(ChatClient client, int userNumber)
		{
			string username = $"casey.yang{userNumber}";
			string password = "Wan@1234"; // 你可能想要為每個用戶設置不同的密碼

			try
			{
				bool success = await client.AuthenticateAsync(password);
				if (success)
				{
					client.Start();
				}
			}
			catch (Exception ex)
			{
				DisplayMessage($"用戶 {username} 登入過程中發生錯誤: {ex.Message}");
			}
		}
		private void StopAllClients()
		{
			foreach (var client in _clients)
			{
				client.Dispose();
			}
		}
		private async void button1_Click_1(object sender, EventArgs e)
		{
			
			await SendMessagesAsync();
		}
		private void label2_Click(object sender, EventArgs e)
		{
		}
		private async void button1_Click_2(object sender, EventArgs e)
		{
			var userName = txtUsername.Text;
			var password = txtPassword.Text;
			var client = new ChatClient("127.0.0.1", 9000, this, userName);
			await LoginClientAsync(client, 101);
		}
		private async void button2_Click(object sender, EventArgs e)
		{
			var userName = txtUsername.Text;
			var password = txtPassword.Text;
			var client = new ChatClient("127.0.0.1", 9001, this, userName);
			await LoginClientAsync(client, 101);
		}
	}
	public class ChatClient

	{
		private TcpClient _client;
		private NetworkStream _stream;
		private ChatForm _form;
		private string _logFilePath;
		private StreamWriter _logWriter;
		private ConcurrentQueue<string> _messageQueue;
		private CancellationTokenSource _cancellationTokenSource;
		private SemaphoreSlim _messageSemaphore;
		private string _username;
		private Stopwatch _receiveTimer = new Stopwatch();
		private int _messageCount ;
		private const int TARGET_MESSAGE_COUNT = 10000;

		public ChatClient(string ipAddress, int port, ChatForm form, string username)
		{
			_client = new TcpClient();
			_client.Connect(ipAddress, port);
			_stream = _client.GetStream();
			_form = form;
			_username = username;
			_messageQueue = new ConcurrentQueue<string>();
			_cancellationTokenSource = new CancellationTokenSource();
			_messageSemaphore = new SemaphoreSlim(0);
			_logFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, $"chatLog_{username}.txt");

			if (_username.Contains("test"))
			{
				_logFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
					$"chatLog_{username}{new Random().Next(100)}.txt");
			}

			_logWriter = new StreamWriter(_logFilePath, true) { AutoFlush = true };
		}
		public async void Start()
		{
			var receiveTask = ReceiveMessagesAsync();
			var processTask = ProcessMessagesAsync(_cancellationTokenSource.Token);
			await Task.WhenAll(receiveTask, processTask);
		}
		private async Task WriteLogBatchAsync(List<string> messages)
		{
			try
			{
				var batchContent = string.Join(Environment.NewLine, messages);
				await _logWriter.WriteAsync(batchContent + Environment.NewLine);
			}
			catch (Exception ex)
			{
				_form.DisplayMessage($"Log Exception: {ex.Message}");
			}

		}

		public void Dispose()
		{
			_logWriter?.Dispose();
			_cancellationTokenSource.Cancel();
		}

		public async Task<bool> AuthenticateAsync(string password)
		{
			try
			{
				
				string credentials = $"{_username}:{Cryptography.HashPassword(password)}";
				byte[] credentialsBytes = Encoding.UTF8.GetBytes(credentials);

				await _stream.WriteAsync(credentialsBytes, 0, credentialsBytes.Length).ConfigureAwait(false);

				byte[] buffer = new byte[1024];
				int bytesRead = await _stream.ReadAsync(buffer, 0, buffer.Length).ConfigureAwait(false);
				int response = BitConverter.ToInt32(buffer, 0);

				if (response == 1)
				{
					return true;
				}
				if (response == 2)
				{
					Console.WriteLine("You are already logged in on another device.");
					return true;
				}
				return false;
				
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Authentication error: {ex.Message}");
				return false;
			}
		}



		private async Task ReceiveMessagesAsync()
		{
			const int BufferSize = 1024;
			byte[] messageBuffer = new byte[BufferSize];

			try
			{
				_receiveTimer.Start();
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
						_messageQueue.Enqueue(message);
						_messageCount++;

						if (_messageCount >= 1)
						{
							_messageSemaphore.Release();
						}
						if (_messageCount == 1)
						{
							
							//_form.DisplayMessage($"{_username} received {TARGET_MESSAGE_COUNT} messages in {_receiveTimer.ElapsedMilliseconds} milliseconds.");
							break;
						}
					}
				}
				_receiveTimer.Stop();
				_form.DisplayMessage($"{_username} received {TARGET_MESSAGE_COUNT} messages in {_receiveTimer.ElapsedMilliseconds} milliseconds.");
			}
			catch (Exception ex)
			{
				_form.DisplayMessage($"Exception: {ex.Message}");
			}
		}
		private async Task ProcessMessagesAsync(CancellationToken cancellationToken)
		{
			while (!cancellationToken.IsCancellationRequested)
			{
				await _messageSemaphore.WaitAsync(cancellationToken);
				if (_messageQueue.Count >= 1)
				{
					var messageBatch = _messageQueue.ToList();
					await WriteLogBatchAsync(messageBatch);
					_messageQueue = null;
					messageBatch.Clear();
				}
			}
		}
		
		public async Task SendMessageAsync(string message)
		{
			try
			{
				var messageBytes = Encoding.UTF8.GetBytes(message);
				var lengthBytes = BitConverter.GetBytes(messageBytes.Length);
				byte[] messageForSend = new byte[lengthBytes.Length + messageBytes.Length];
				Buffer.BlockCopy(lengthBytes, 0, messageForSend, 0, lengthBytes.Length);
				Buffer.BlockCopy(messageBytes, 0, messageForSend, lengthBytes.Length, messageBytes.Length);
				await _stream.WriteAsync(messageForSend, 0, messageForSend.Length);
				//await _stream.WriteAsync(lengthBuffer, 0, lengthBuffer.Length);
				//await _stream.WriteAsync(messageBuffer, 0, messageBuffer.Length);
			}
			catch (Exception ex)
			{
				_form.DisplayMessage("異常：" + ex.Message);
			}
		}
	}
}


