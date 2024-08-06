using StackExchange.Redis;
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


namespace Test.Server
{
	internal class Program
	{
		static async Task Main(string[] args)
		{
			var server1 = new ChatServer("127.0.0.1", 9000);
			var server2 = new ChatServer("127.0.0.1", 9001);

			Task serverTask1 = server1.StartAsync();
			Task serverTask2 = server2.StartAsync();

			// 只在 9000 端口的伺服器上啟動 MessageProcessor
			var messageMiddleware = new MessageProcessor();
			Task messageMiddlewareTask = messageMiddleware.StartAsync();

			await Task.WhenAll(serverTask1, serverTask2, messageMiddlewareTask);
		}
	}

	class ChatServer
	{
		private const string RedisConnectionString = "localhost:6379,password=wtredis";
		const string PostgresConnectionString = "Host=localhost;Username=op;Password=Op@1234;Database=mydb";
		private TcpListener _listener;
		private ConcurrentDictionary<string, TcpClient> _clients;
		private ConcurrentDictionary<string, string> _users;
		private static ConnectionMultiplexer _redis;
		private static IDatabase _db;
		private ISubscriber _sub;
		private ChannelMessageQueue _channel;
		private ISubscriber _usersSub;
		private int _port;

		public ChatServer(string ipAddress, int port)
		{
			_listener = new TcpListener(IPAddress.Parse(ipAddress), port);
			_clients = new ConcurrentDictionary<string, TcpClient>();
			_users = new ConcurrentDictionary<string, string>();
			_redis = ConnectionMultiplexer.Connect(RedisConnectionString);
			_db = _redis.GetDatabase();
			_sub = _redis.GetSubscriber();
			_channel = _sub.Subscribe("chatroom:messages_pubSub");
			_usersSub = _redis.GetSubscriber();
			_port = port;
			LoadUsersFromDatabase();
		}
		private void LoadUsersFromDatabase()
		{
			using (var conn = new NpgsqlConnection(PostgresConnectionString))
			{
				conn.Open();
				string query = "SELECT account, password_hash FROM users";
				using (var cmd = new NpgsqlCommand(query, conn))
				{
					using (var reader = cmd.ExecuteReader())
					{
						while (reader.Read())
						{
							var account = reader.GetString(0);
							var passwordHash = reader.GetString(1);
							_users[account] = passwordHash;
						}
					}
				}
			}
		}

		public async Task StartAsync()
		{
			var counter = 0;
			var messageBatch = new List<string>();
			_listener.Start();
			Console.WriteLine($"Server started on port {_port}.");

			_channel.OnMessage(async message =>
				{
					messageBatch.Add(message.Message);

					if (messageBatch.Count >= 10000)
					{
						Console.WriteLine("Message start to send!!!");
						var toSend = String.Join("\n", messageBatch);
						await BroadcastMessageAsync(toSend);
						counter = 0;
						messageBatch.Clear();
					}
				});


			await _usersSub.SubscribeAsync("chatroom:users", (channel, message) =>
			{
				var userInfo = ((string)message).Split(':');

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
				//Console.WriteLine("Client connected.");
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

				while (true)
				{
					int bytesRead = await stream.ReadAsync(buffer, 0, 4);
					if (bytesRead == 0) break;
					var messageLength = BitConverter.ToInt32(buffer, 0);

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

						await _sub.PublishAsync("chatroom:messages_pubSub", formattedMessage);
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
			int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length).ConfigureAwait(false);
			var credentials = Encoding.UTF8.GetString(buffer, 0, bytesRead).Split(':');
			var username = credentials[0];
			var password = credentials[1];

			byte[] response;
			if (_users.ContainsKey(username) && _users[username] == password)
			{
				if (!_clients.ContainsKey(username))
				{
					_clients.TryAdd(username, client);
					response = BitConverter.GetBytes(1);
					await stream.WriteAsync(response, 0, response.Length);
					return username;
				}
				else
				{
					response = BitConverter.GetBytes(2);
					await stream.WriteAsync(response, 0, response.Length);
					return username;
				}
			}
			response = BitConverter.GetBytes(0);
			await stream.WriteAsync(response, 0, response.Length);
			return null;

		}
		private async Task SendRecentMessagesAsync(TcpClient client)
		{
			var stream = client.GetStream();
			var messages = await _db.ListRangeAsync("chatroom:messages", -100, -1);
			if (messages.Length == 0) return;
			var a = messages.Select(x => (string)x).ToList();
			var toSend = String.Join("\n", a);
			await BroadcastMessageAsync(toSend);

		}
		private async Task BroadcastMessageAsync(string message)
		{
			var messageBytes = Encoding.UTF8.GetBytes(message);
			var lengthBytes = BitConverter.GetBytes(messageBytes.Length);
			byte[] messageForSend = new byte[lengthBytes.Length + messageBytes.Length];
			Buffer.BlockCopy(lengthBytes, 0, messageForSend, 0, lengthBytes.Length);
			Buffer.BlockCopy(messageBytes, 0, messageForSend, lengthBytes.Length, messageBytes.Length);

			var tasks = _clients.Values.Select(async client =>
			{
				try
				{
					var stream = client.GetStream();
					await stream.WriteAsync(messageForSend, 0, messageForSend.Length);
				}
				catch
				{
					// ignored
				}
			});
			await Task.WhenAll(tasks);
		}

	}
}
