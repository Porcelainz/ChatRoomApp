using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Test.Common;

namespace Test.Server.ChatServer
{
	partial class Server
	{
		private async Task BroadcastMessageAsync(string message)
		{
			var messageForSend = PrepareMessageForSendToClient(message);
			var tasks = Enumerable.Select<TcpClient, Task>(_clients.Values, async client =>
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

		private async Task SendRecentMessagesAsync(TcpClient client)
		{
			var stream = client.GetStream();
			var toSend = await GetRecentMessagesFromRedisAsync();
			if (toSend == null) return;
			var messageForSend = PrepareMessageForSendToClient(toSend);
			await stream.WriteAsync(messageForSend, 0, messageForSend.Length);
		}

		private async Task<string> GetRecentMessagesFromRedisAsync(int count = 100)
		{
			var recentRedisValues = await Server._db.ListRangeAsync("chatroom:messages", -count, -1);
			if (recentRedisValues.Length == 0) return null;
			return ConvertMessagesToString(recentRedisValues.Select(x => (string)x).ToList());
		}

		private string ConvertMessagesToString(List<string> messages)
		{
			return String.Join("\n", messages);
		}

		private byte[] PrepareMessageForSendToClient(string message)
		{
			var messageBytes = Encoding.UTF8.GetBytes(message);
			return LengthPrefixAdder.AddLengthPrefix(messageBytes);
		}
	}
}
