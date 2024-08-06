using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using System.Threading;
using System.Threading.Tasks;
using Npgsql;
using StackExchange.Redis;


namespace Test.Server
{
	internal class MessageMiddleware
	{
		const string RedisConnectionString = "localhost:6379,password=wtredis";
		const string PostgresConnectionString = "Host=localhost;Username=op;Password=Op@1234;Database=mydb";

		private static ConnectionMultiplexer _redis;
		private static IDatabase _db;
		private ISubscriber _sub;
		private NpgsqlDataSource _dataSource;
		private SemaphoreSlim _messageSemaphore;
		private ConcurrentQueue<string> _messageQueue;
		public MessageMiddleware()
		{
			_redis = ConnectionMultiplexer.Connect(RedisConnectionString);
			_db = _redis.GetDatabase();
			_sub = _redis.GetSubscriber();
			_dataSource = NpgsqlDataSource.Create(PostgresConnectionString);
			_messageQueue = new ConcurrentQueue<string>();
			_messageSemaphore = new SemaphoreSlim(0);
		}


		public async Task StartAsync()
		{
			Console.WriteLine("MessageMiddleware started");
			await ProcessMessagesAsync();
		}
		private async Task ProcessMessagesAsync()
		{
			List<string> messageBatch = new List<string>();
			AutoResetEvent waitHandle = new AutoResetEvent(false);

			_sub.Subscribe("chatroom:messages_pubSub")
				.OnMessage(async message =>
				{
					messageBatch.Add(message.Message);
					_messageSemaphore.Release();
					if (messageBatch.Count >= 10000)
					{
						await ProcessMessageBatchAsync(messageBatch);
					}
				});
		}

		private async Task ProcessMessageBatchAsync(List<string> messages)
		{
			if (messages.Count > 0)
			{
				await StoreMessagesToPostgresAsync(messages);
				await StoreMessagesToRedisAsync(messages);
			}
		}
		private async Task StoreMessagesToPostgresAsync(List<string> messages)
		{
			try
			{
				using (var conn = await _dataSource.OpenConnectionAsync())
				using (var transaction = conn.BeginTransaction())
				{
					var valueStrings = new List<string>();
					var parameters = new List<NpgsqlParameter>();

					for (int i = 0; i < messages.Count; i++)
					{
						valueStrings.Add($"(@message{i})");
						parameters.Add(new NpgsqlParameter($"@message{i}", messages[i]));
					}

					var cmdText = $"INSERT INTO chatroom_message (message) VALUES {string.Join(",", valueStrings)}";

					using (var cmd = new NpgsqlCommand(cmdText, conn, transaction))
					{
						cmd.Parameters.AddRange(parameters.ToArray());
						await cmd.ExecuteNonQueryAsync();
					}

					await transaction.CommitAsync();
					Console.WriteLine($"Stored {messages.Count} messages to Postgres");
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Error storing messages to Postgres: {ex.Message}");
				throw;
			}
		}
		private async Task StoreMessagesToRedisAsync(List<string> messages)
		{
			try
			{
				var batch = _db.CreateBatch();
				var tasks = new List<Task>();

				foreach (var message in messages)
				{
					tasks.Add(batch.ListRightPushAsync("chatroom:messages", message));
					//tasks.Add(batch.PublishAsync("chatroom:messages_pubsub", message));
				}

				batch.Execute();
				await Task.WhenAll(tasks);

				Console.WriteLine($"Stored {messages.Count} messages to Redis");
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Error storing messages to Redis: {ex.Message}");
				throw;
			}
		}



	}

}

