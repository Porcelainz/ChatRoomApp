using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Test.Common;
namespace Test.Server
{
	internal class Program
	{
		static async Task Main(string[] args)
		{
			var server = new ChatServer("127.0.0.1", 9000);
			await server.StartAsync();
		}
	}

	class ChatServer
	{
		private TcpListener _listener;
		private ConcurrentDictionary<TcpClient, string> _clients;
		private Dictionary<string, string> _users;

		public ChatServer(string ipAddress, int port)
		{
			_listener = new TcpListener(IPAddress.Parse(ipAddress), port);
			_clients = new ConcurrentDictionary<TcpClient, string>();
			_users = UserHelper.InitUser();
            

		}

		public async Task StartAsync()
		{
			_listener.Start();
			Console.WriteLine("Server started.");

			while (true)
			{
				var client = await _listener.AcceptTcpClientAsync();
				Console.WriteLine("Client connected.");
				var clientTask = HandleClientAsync(client);
			}
		}

		private async Task HandleClientAsync(TcpClient client)
		{
			var buffer = new byte[1024];
			var stream = client.GetStream();
			string username = null;

			try
			{
				// 進行登入驗證
				var loginSuccess = await AuthenticateClientAsync(client);
				if (!loginSuccess.IsSuccess)
				{
					Console.WriteLine("Authentication failed. Disconnecting client.");
					client.Close();
					return;
				}
				username = loginSuccess.username;

				while (true)
				{
					var bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
					if (bytesRead == 0) break; // Client disconnected
					var message = Encoding.UTF8.GetString(buffer, 0, bytesRead);
					string formattedMessage = $"{username}: {message}";	
					Console.WriteLine(formattedMessage);
					await BroadcastMessageAsync(formattedMessage);
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
				Console.WriteLine("Client disconnected.");
			}
		}

		private async Task<(bool IsSuccess, string username)> AuthenticateClientAsync(TcpClient client)
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
				return (true, username);
			}
			else
			{
				var response = Encoding.UTF8.GetBytes("Login Failed");
				await stream.WriteAsync(response, 0, response.Length);
				return (false,null);
			}
		}

		private async Task BroadcastMessageAsync(string message)
		{
			var buffer = Encoding.UTF8.GetBytes(message);
			foreach (var client in _clients.Keys)
			{
				try
				{
					var stream = client.GetStream();
					await stream.WriteAsync(buffer, 0, buffer.Length);
				}
				catch
				{
					// Ignore write failures (client might have disconnected)
				}
			}
		}
	}
}
