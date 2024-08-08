using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using StackExchange.Redis;

namespace Test.Server.ChatServer
{
	partial class Server
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

		public Server(string ipAddress, int port)
		{
			_listener = new TcpListener(IPAddress.Parse(ipAddress), port);
			_clients = new ConcurrentDictionary<string, TcpClient>();
			_users = PostGreSqlHelper.LoadUsersFromDatabase(PostgresConnectionString);
			_redis = ConnectionMultiplexer.Connect(RedisConnectionString);
			_db = _redis.GetDatabase();
			_sub = _redis.GetSubscriber();
			_channel = _sub.Subscribe("chatroom:messages_pubSub");
			_usersSub = _redis.GetSubscriber();
			_port = port;

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

		

	}
}
