using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using StackExchange.Redis;


namespace Test.Server
{
	internal class Program
	{
		static async Task Main(string[] args)
		{
			var server = new ChatServer("127.0.0.1", 9000);
			await server.StartAsync();
		}
	}

	class ChatServer
	{
		private TcpListener _listener;
		private ConcurrentDictionary<TcpClient, string> _clients;
		private Dictionary<string, string> _users;
		private static ConnectionMultiplexer _redis;
		private static IDatabase _db;

		public ChatServer(string ipAddress, int port)
		{
			_listener = new TcpListener(IPAddress.Parse(ipAddress), port);
			_clients = new ConcurrentDictionary<TcpClient, string>();
			_users = UserHelper.InitUser();
			_redis = ConnectionMultiplexer.Connect("localhost:6379,password=wtredis");
			_db = _redis.GetDatabase();

		}

		public async Task StartAsync()
		{
			_listener.Start();
			Console.WriteLine("Server started.");

			while (true)
			{
				var client = await _listener.AcceptTcpClientAsync();
				Console.WriteLine("Client connected.");
				var clientTask = HandleClientAsync(client);
			}
		}

		private async Task HandleClientAsync(TcpClient client)
		{
			var stream = client.GetStream();
			string username = null;

			try
			{
				username = await AuthenticateClientAsync(client);
				if (username == null)
				{
					Console.WriteLine("Authentication failed. Disconnecting client.");
					client.Close();
					return;
				}
				await SendRecentMessagesAsync(client);
				await BroadcastMessageAsync($"{username} joined the chat.");
				while (true)
				{
					// 讀取消息長度
					var lengthBuffer = new byte[4];
					if (await stream.ReadAsync(lengthBuffer, 0, lengthBuffer.Length) == 0) break;
					var messageLength = BitConverter.ToInt32(lengthBuffer, 0);

					// 根據消息長度讀取消息
					var messageBuffer = new byte[messageLength];
					await stream.ReadAsync(messageBuffer, 0, messageLength);
					var message = Encoding.UTF8.GetString(messageBuffer);
					string formattedMessage = $"{username}: {message}";
					Console.WriteLine(formattedMessage);
					await StoreMessageAsync(formattedMessage);
					await BroadcastMessageAsync(formattedMessage);
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine("Exception: " + ex.Message);
			}
			finally
			{
				_clients.TryRemove(client, out _);
				client.Close();
				Console.WriteLine($"{username} disconnected.");
			}
		}



		private async Task<string> AuthenticateClientAsync(TcpClient client)
		{
			var buffer = new byte[1024];
			var stream = client.GetStream();

			// 接收用戶名
			var bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
			var username = Encoding.UTF8.GetString(buffer, 0, bytesRead).Trim();

			// 接收密碼
			bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
			var password = Encoding.UTF8.GetString(buffer, 0, bytesRead);

			// 驗證用戶名和密碼
			if (_users.ContainsKey(username) && _users[username] == password)
			{
				_clients.TryAdd(client, username);
				var response = Encoding.UTF8.GetBytes("Login Success");
				await stream.WriteAsync(response, 0, response.Length);
				return username;
			}
			else
			{
				var response = Encoding.UTF8.GetBytes("Login Failed");
				await stream.WriteAsync(response, 0, response.Length);
				return null;
			}
		}
		private async Task StoreMessageAsync(string message)
		{
			var messageBytes = Encoding.UTF8.GetBytes(message);
			var lengthBytes = BitConverter.GetBytes(messageBytes.Length); // 注意這裡是messageBytes的長度
			var fullMessage = new byte[lengthBytes.Length + messageBytes.Length];
			lengthBytes.CopyTo(fullMessage, 0);
			messageBytes.CopyTo(fullMessage, lengthBytes.Length);
			await _db.ListRightPushAsync("chatroom:messages", fullMessage);
		}

		private async Task SendRecentMessagesAsync(TcpClient client)
		{
			var stream = client.GetStream();

			// Get the last 100 messages from Redis
			var messages = await _db.ListRangeAsync("chatroom:messages", -100, -1);

			foreach (var message in messages)
			{
				var buffer = Encoding.UTF8.GetBytes(message);
				await stream.WriteAsync(buffer, 0, buffer.Length);
			}
		}

		private async Task BroadcastMessageAsync(string message)
		{
			var messageBytes = Encoding.UTF8.GetBytes(message);
			var lengthBytes = BitConverter.GetBytes(messageBytes.Length); // 獲取消息長度的字節表示
			var fullMessage = new byte[lengthBytes.Length + messageBytes.Length];
			lengthBytes.CopyTo(fullMessage, 0); // 將長度前綴複製到完整消息的開頭
			messageBytes.CopyTo(fullMessage, lengthBytes.Length); // 將消息內容複製到長度後面

			foreach (var client in _clients.Keys)
			{
				try
				{
					var stream = client.GetStream();
					await stream.WriteAsync(fullMessage, 0, fullMessage.Length); // 發送包含長度前綴的完整消息
				}
				catch
				{
					// 忽略寫入失敗（客戶端可能已斷開連接）
				}
			}
		}
	}
}
