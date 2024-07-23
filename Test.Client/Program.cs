using System;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Test.Common;
namespace Test.Client
{
	internal class Program
	{
		static async Task Main(string[] args)
		{
			var client = new ChatClient("127.0.0.1", 9000);
			await client.StartAsync();
		}
	}

	class ChatClient
	{
		private TcpClient _client;
		private NetworkStream _stream;

		public ChatClient(string ipAddress, int port)
		{
			_client = new TcpClient();
			_client.Connect(ipAddress, port);
			_stream = _client.GetStream();
		}

		public async Task StartAsync()
		{
			if (!await AuthenticateAsync())
			{
				Console.WriteLine("Authentication failed. Exiting...");
				return;
			}
			Console.WriteLine("Login success");

			var receiveTask = ReceiveMessagesAsync();
			//var sendTask = SendMessagesAsync();

			//await Task.WhenAll(receiveTask, sendTask);
			await SendMessagesAsync();
		}

		private async Task<bool> AuthenticateAsync()
		{
			Console.Write("Enter username: ");
			var username = Console.ReadLine();
			Console.Write("Enter password: ");
			var password = ConsoleHelper.ReadPassword();

			var usernameBytes = Encoding.UTF8.GetBytes(username);
			await _stream.WriteAsync(usernameBytes, 0, usernameBytes.Length);

			var passwordBytes = Encoding.UTF8.GetBytes(Cryptography.HashPassword(password));
			await _stream.WriteAsync(passwordBytes, 0, passwordBytes.Length);

			var buffer = new byte[1024];
			var bytesRead = await _stream.ReadAsync(buffer, 0, buffer.Length);
			var response = Encoding.UTF8.GetString(buffer, 0, bytesRead);

			return response == "Login Success";
		}

		private async Task ReceiveMessagesAsync()
		{
			var buffer = new byte[1024];
			try
			{
				while (true)
				{
					var bytesRead = await _stream.ReadAsync(buffer, 0, buffer.Length);
					if (bytesRead == 0) break; // Server disconnected
					var message = Encoding.UTF8.GetString(buffer, 0, bytesRead);
					Console.WriteLine(message);
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine("Exception: " + ex.Message);
			}
		}

		private async Task SendMessagesAsync()
		{
			try
			{
				while (true)
				{
					var message = Console.ReadLine();
					if (message == null) break; // User entered EOF
					var buffer = Encoding.UTF8.GetBytes(message);
					await _stream.WriteAsync(buffer, 0, buffer.Length);
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine("Exception: " + ex.Message);
			}
		}
	}
}
