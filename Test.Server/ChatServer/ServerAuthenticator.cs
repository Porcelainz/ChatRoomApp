using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Test.Server.ChatServer
{
	partial class Server
	{
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
					Console.WriteLine($"{username} has logged in successfully");
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
	}
}
