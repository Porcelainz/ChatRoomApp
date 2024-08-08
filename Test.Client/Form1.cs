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
using Npgsql;
using StackExchange.Redis;
using Test.Common;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.StartPanel;

namespace Test.Client
{
	public partial class ChatForm : Form
	{
		//private ChatClient _client;
		private List<ChatClient.Client> _clients = new List<ChatClient.Client>();
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

				var client = new ChatClient.Client("127.0.0.1", port, this, username + i);
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
		private async Task SendClientMessagesAsync(ChatClient.Client client)
		{
			for (int i = 0; i < MessageCount; i++)
			{
				await client.SendMessageAsync(i + MessageToSend);
			}
		}
		private async Task LoginClientAsync(ChatClient.Client client, int userNumber)
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
			var client = new ChatClient.Client("127.0.0.1", 9000, this, userName);
			await LoginClientAsync(client, 101);
		}
		private async void button2_Click(object sender, EventArgs e)
		{
			var userName = txtUsername.Text;
			var password = txtPassword.Text;
			var client = new ChatClient.Client("127.0.0.1", 9001, this, userName);
			await LoginClientAsync(client, 101);
		}

		private async void ExportDataFromPG_Click(object sender, EventArgs e)
		{
			// 連接字符串應根據你的 PostgreSQL 配置進行修改
			string connectionString = "Host=localhost;Username=op;Password=Op@1234;Database=mydb";

			// SQL 查詢語句
			string query = @"
			SELECT message
				FROM (
				SELECT t.*
				FROM public.chatroom_message t
				ORDER BY t.created_at DESC
				LIMIT 10000
			) AS subquery
			ORDER BY subquery.id ASC;";

			// 匯出的 TXT 文件路徑
			string filePath = "exported_data.txt";

			try
			{
				using (var connection = new NpgsqlConnection(connectionString))
				{
					await connection.OpenAsync();

					using (var command = new NpgsqlCommand(query, connection))
					using (var reader = await command.ExecuteReaderAsync())
					using (var writer = new StreamWriter(filePath))
					{
						while (await reader.ReadAsync())
						{
							string message = reader.GetString(0);
							await writer.WriteLineAsync(message);
						}
					}
				}

				MessageBox.Show("Data exported successfully.");
			}
			catch (Exception ex)
			{
				MessageBox.Show($"An error occurred: {ex.Message}");
			}
		}

		private async void ExportDataFromRedis_Click(object sender, EventArgs e)
		{
			string connectionString = "localhost:6379,password=wtredis";
			string redisKey = "chatroom:messages";
			int start = -10000; 
			int end = -1; 
			string filePath = "redis_exported_data.txt";

			try
			{
				var redis = ConnectionMultiplexer.Connect(connectionString);
				var db = redis.GetDatabase();
				var redisValues = await db.ListRangeAsync(redisKey, start, end);

				if (redisValues.Length == 0)
				{
					MessageBox.Show("No data found in Redis.");
					return;
				}

				using (var writer = new StreamWriter(filePath))
				{
					foreach (var value in redisValues)
					{
						await writer.WriteLineAsync(value);
					}
				}

				MessageBox.Show("Data exported successfully.");
			}
			catch (Exception ex)
			{
				MessageBox.Show($"An error occurred: {ex.Message}");
			}
		}
	}

}


