using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using StackExchange.Redis;

namespace Test.Server
{
	internal class RedisHelper
	{
		public static async Task StoreMessagesToRedisAsync(List<string> messages, IDatabase database)
		{
			try
			{
				var batch = database.CreateBatch();
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
