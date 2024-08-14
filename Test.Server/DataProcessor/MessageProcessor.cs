using Npgsql;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Test.Server
{
	internal class MessageProcessor
	{
		const string RedisConnectionString = "localhost:6379,password=wtredis";
		const string PostgresConnectionString = "Host=localhost;Username=op;Password=Op@1234;Database=mydb";

		private static ConnectionMultiplexer _redis;
		private static IDatabase _db;
		private ISubscriber _sub;
		private NpgsqlDataSource _dataSource;
		private SemaphoreSlim _messageSemaphore;
		
		public MessageProcessor()
		{
			_redis = ConnectionMultiplexer.Connect(RedisConnectionString);
			_db = _redis.GetDatabase();
			_sub = _redis.GetSubscriber();
			_dataSource = NpgsqlDataSource.Create(PostgresConnectionString);
			_messageSemaphore = new SemaphoreSlim(0);
		}
		public async Task StartAsync()
		{
			Console.WriteLine("MessageProcessor started");
			ProcessMessagesAsync();
		}
		private void ProcessMessagesAsync()
		{
			List<string> batchMessage = new List<string>();
			AutoResetEvent waitHandle = new AutoResetEvent(false);

			_sub.Subscribe("chatroom:messages_pubSub")
				.OnMessage(async message =>
				{
					await OnMessageReceivedAsync(message.Message, batchMessage);
				});
		}
		private async Task OnMessageReceivedAsync(string message, List<string> messages)
		{
			messages.Add(message);
			_messageSemaphore.Release();
			if (messages.Count >= 10000)
			{
				await ProcessMessageBatchAsync(messages);
			}
		}
		private async Task ProcessMessageBatchAsync(List<string> messages)
		{
			if (messages.Count > 0)
			{
				
				Stopwatch stopwatch = new Stopwatch();
				stopwatch.Start();
				await PostGreSqlHelper.StoreMessagesToPostgresAsync(messages, _dataSource);
				stopwatch.Stop();
				Console.WriteLine($"Time to store {messages.Count} messages to Postgres: {stopwatch.ElapsedMilliseconds} ms");
				await RedisHelper.StoreMessagesToRedisAsync(messages, _db);
			}
		}
	}

}

