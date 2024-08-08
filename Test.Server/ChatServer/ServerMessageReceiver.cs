using System;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Test.Common;

namespace Test.Server.ChatServer
{
	partial class Server
	{
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
					var message = await MessageReader.ReadMessageAsync(stream, buffer);
					if (message == null) break;

					string formattedMessage = $"{username}: {message}";
					await _sub.PublishAsync("chatroom:messages_pubSub", formattedMessage);
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

		
	}
}
