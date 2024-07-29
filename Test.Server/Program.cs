using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Npgsql;
using StackExchange.Redis;


namespace Test.Server
{
	internal class Program
	{
		static async Task Main(string[] args)
		{
			int port = Int32.Parse(args[0]);
			var server = new ChatServer("127.0.0.1", port);
			await server.StartAsync();
			//int port2 = Int32.Parse(args[1]);
			//var server2 = new ChatServer("127.0.0.1", 9001);
			//await server2.StartAsync();
		}
	}

	class ChatServer
	{
		const string RedisConnectionString = "localhost:6379,password=wtredis";
		const string PostgresConnectionString = "Host=localhost;Username=op;Password=Op@1234;Database=mydb";

		private TcpListener _listener;
		private ConcurrentDictionary<TcpClient, string> _clients;
		private Dictionary<string, string> _users;
		private static ConnectionMultiplexer _redis;
		private static IDatabase _db;
		private ISubscriber _sub;
		private ISubscriber _usersSub;
		private int _port;
		private NpgsqlDataSource _dataSource;

		public ChatServer(string ipAddress, int port)
		{
			_listener = new TcpListener(IPAddress.Parse(ipAddress), port);
			_clients = new ConcurrentDictionary<TcpClient, string>();
			_users = UserHelper.InitUser();
			_redis = ConnectionMultiplexer.Connect(RedisConnectionString);
			_db = _redis.GetDatabase();
			_sub = _redis.GetSubscriber();
			_usersSub = _redis.GetSubscriber();
			_port = port;
			_dataSource = NpgsqlDataSource.Create(PostgresConnectionString);

		}

		public async Task StartAsync()
		{
			_listener.Start();
			Console.WriteLine($"Server started on port {_port}.");

			await _sub.SubscribeAsync("chatroom:messages", (channel, message) =>
			{
				
				var msg = (string)message;
				//Console.WriteLine("Received message: " + msg);
				BroadcastMessageAsync(msg).Wait();

			});

			//await _usersSub.SubscribeAsync("chatroom:users", (channel, message) =>
			//{
			//	var userName = (string)message;
			//	if()
			//});

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
			byte[] buffer = new byte[1024];

			try
			{
				username = await AuthenticateClientAsync(client);
				if (username == null)
				{
					Console.WriteLine("Authentication failed. Disconnecting client.");
					client.Close();
					return;
				}
				await _usersSub.PublishAsync("chatroom:users", username);
				await SendRecentMessagesAsync(client);
				await BroadcastMessageAsync($"{username} joined the chat.");
				while (true)
				{
					// 讀取消息長度
					int bytesRead = await stream.ReadAsync(buffer, 0, 4);
					if (bytesRead == 0) break;
					var messageLength = BitConverter.ToInt32(buffer, 0);

					// 使用 MemoryStream 來構建完整消息
					using (var memoryStream = new MemoryStream())
					{
						int remainingBytes = messageLength;
						while (remainingBytes > 0)
						{
							int bytesToRead = Math.Min(remainingBytes, buffer.Length);
							bytesRead = await stream.ReadAsync(buffer, 0, bytesToRead);
							if (bytesRead == 0) break;
							await memoryStream.WriteAsync(buffer, 0, bytesRead);
							remainingBytes -= bytesRead;
						}

						var messageBytes = memoryStream.ToArray();
						var message = Encoding.UTF8.GetString(messageBytes);
						string formattedMessage = $"{username}: {message}";
						Console.WriteLine(formattedMessage);
						// 發布消息到 Redis
						await _sub.PublishAsync("chatroom:messages", formattedMessage);
						await StoreMessageToRedisAsync(formattedMessage);
						await StoreMessageToPostgresAsync(formattedMessage);
						
					}
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
		private async Task StoreMessageToRedisAsync(string message)
		{
			//var messageBytes = Encoding.UTF8.GetBytes(message);
			//var lengthBytes = BitConverter.GetBytes(messageBytes.Length); // 注意這裡是messageBytes的長度
			//var fullMessage = new byte[lengthBytes.Length + messageBytes.Length];
			//lengthBytes.CopyTo(fullMessage, 0);
			//messageBytes.CopyTo(fullMessage, lengthBytes.Length);
			await _db.ListRightPushAsync("chatroom:messages", message);
		}

		private async Task StoreMessageToPostgresAsync(string message)
		{
			using (var cmd = _dataSource.CreateCommand("INSERT INTO chatroom_message (message) VALUES ($1);"))
			{
				cmd.Parameters.AddWithValue(message);
				await cmd.ExecuteNonQueryAsync();
			}
		}

		private async Task SendRecentMessagesAsync(TcpClient client)
		{
			var stream = client.GetStream();
			var messages = await _db.ListRangeAsync("chatroom:messages", -100, -1);

			foreach (var message in messages)
			{
				var messageBytes = Encoding.UTF8.GetBytes(message);
				var lengthBytes = BitConverter.GetBytes(messageBytes.Length);
				await stream.WriteAsync(lengthBytes, 0, lengthBytes.Length);
				await stream.WriteAsync(messageBytes, 0, messageBytes.Length);
			}
		}

		private async Task BroadcastMessageAsync(string message)
		{
			var messageBytes = Encoding.UTF8.GetBytes(message);
			var lengthBytes = BitConverter.GetBytes(messageBytes.Length);

			foreach (var client in _clients.Keys)
			{
				try
				{
					var stream = client.GetStream();
					await stream.WriteAsync(lengthBytes, 0, lengthBytes.Length);
					await stream.WriteAsync(messageBytes, 0, messageBytes.Length);
					
				}
				catch
				{
					// 忽略寫入失敗（客戶端可能已斷開連接）
				}
			}
		}
	}
}
