using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Npgsql;
using RedLockNet.SERedis;
using StackExchange.Redis;


namespace Test.Server
{
	internal class Program
	{
		static async Task Main(string[] args)
		{
			int port = Int32.Parse(args[0]);
			var server = new ChatServer("127.0.0.1", port);

			Task serverTask = server.StartAsync();
			Task messageMiddlewareTask = Task.CompletedTask;

			if (port == 9000)
			{
				// Start messageMiddleware
				var messageMiddleware = new MessageMiddleware();
				messageMiddlewareTask = messageMiddleware.StartAsync();
			}

			await Task.WhenAll(serverTask, messageMiddlewareTask);
			Console.WriteLine("Server and MessageMiddleware started successfully.");
		}
	}

	class ChatServer
	{
		const string RedisConnectionString = "localhost:6379,password=wtredis";
		const string PostgresConnectionString = "Host=localhost;Username=op;Password=Op@1234;Database=mydb";

		private TcpListener _listener;
		private ConcurrentDictionary<string, TcpClient> _clients;
		private Dictionary<string, string> _users;
		private static ConnectionMultiplexer _redis;
		private static IDatabase _db;
		private ISubscriber _sub;
		private ISubscriber _usersSub;
		private int _port;
		private NpgsqlDataSource _dataSource;
		private ConcurrentQueue<string> _messageQueue;
		private SemaphoreSlim _messageSemaphore;


		public ChatServer(string ipAddress, int port)
		{
			_listener = new TcpListener(IPAddress.Parse(ipAddress), port);
			_clients = new ConcurrentDictionary<string, TcpClient>();
			_users = UserHelper.InitUser();
			_redis = ConnectionMultiplexer.Connect(RedisConnectionString);
			_db = _redis.GetDatabase();
			_sub = _redis.GetSubscriber();
			_usersSub = _redis.GetSubscriber();
			_port = port;
			_dataSource = NpgsqlDataSource.Create(PostgresConnectionString);
			_messageQueue = new ConcurrentQueue<string>();
			_messageSemaphore = new SemaphoreSlim(0);

		}

		public async Task StartAsync()
		{
			_listener.Start();
			Console.WriteLine($"Server started on port {_port}.");
			//var messageProcessingTask = Task.Run(async () => await ProcessMessagesAsync());

			//await _sub.SubscribeAsync("chatroom:messages_pubsub", async (channel, message) =>
			//{

			//	var msg = (string)message;
			//	await BroadcastMessageAsync(msg);

			//});
			_sub.Subscribe("chatroom:messages_pubsub")
				.OnMessage(async message => await BroadcastMessageAsync((string)message.Message));


			await _usersSub.SubscribeAsync("chatroom:users", (channel, message) =>
			{
				var userInfo = ((string)message).Split(':');
				//Console.WriteLine(userName);
				if (userInfo[1] != _port.ToString())
				{
					if (_clients.ContainsKey(userInfo[0]))
					{
						var client = _clients.TryGetValue(userInfo[0], out TcpClient tcpClient);
						tcpClient.Close();
						_clients.TryRemove(userInfo[0], out _);
						Console.WriteLine($"{userInfo[0]} disconnected.");
					}
				}
			});

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
				await _usersSub.PublishAsync("chatroom:users", $"{username}:{_port}");
				await SendRecentMessagesAsync(client);
				//await BroadcastMessageAsync($"{username} joined the chat.");
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
						//Console.WriteLine(formattedMessage);
						// 發布消息到 Redis
						//await _sub.PublishAsync("chatroom:messages_pubsub", formattedMessage);
						await _db.ListRightPushAsync("chatroom:message_queue", formattedMessage);
						//await _db.ListRightPushAsync("chatroom:message_Persistence", formattedMessage);
						await _sub.PublishAsync("chatroom:message_queue_notification", formattedMessage);
						//await StoreMessagesToRedisAsync(formattedMessage);
						//await StoreMessageToPostgresAsync(formattedMessage);a

						//_messageQueue.Enqueue(formattedMessage);
						//_messageSemaphore.Release(); // 釋放Semaphore
					}
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine("Exception: " + ex.Message);
			}
			finally
			{
				_clients.TryRemove(username, out _);
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
				if (!_clients.ContainsKey(username))
				{
					_clients.TryAdd(username, client);
					var response = Encoding.UTF8.GetBytes("Login Success");
					await stream.WriteAsync(response, 0, response.Length);
					return username;
				}
				else
				{
					var response = Encoding.UTF8.GetBytes("You already loged in");
					await stream.WriteAsync(response, 0, response.Length);
					return username;
				}
			}
			else
			{
				var response = Encoding.UTF8.GetBytes("Login Failed");
				await stream.WriteAsync(response, 0, response.Length);
				return null;
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

			var tasks = new List<Task>();

			foreach (var client in _clients.Values)
			{
				tasks.Add(Task.Run(async () =>
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
				}));
			}

			await Task.WhenAll(tasks);
		}

	}
}
