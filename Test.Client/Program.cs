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
			int port = Int32.Parse(args[0]);
			var client = new ChatClient("127.0.0.1", port);
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
			const int BufferSize = 1024;
			byte[] lengthBuffer = new byte[4];
			byte[] messageBuffer = new byte[BufferSize];
			try
			{
				while (true)
				{
					// 讀取消息長度
					//var lengthBuffer = new byte[4];
					int bytesRead = await _stream.ReadAsync(lengthBuffer, 0, lengthBuffer.Length);
					if (bytesRead == 0) break; // 連接已關閉
					var messageLength = BitConverter.ToInt32(lengthBuffer, 0);
					Array.Clear(messageBuffer, 0, messageBuffer.Length);
					// 根據消息長度讀取消息
					//var messageBuffer = new byte[messageLength];
					bytesRead = await _stream.ReadAsync(messageBuffer, 0, messageLength);
					if (bytesRead == 0) break; // 連接已關閉
					var message = Encoding.UTF8.GetString(messageBuffer, 0, bytesRead);

					Console.WriteLine(message);
					
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Exception: {ex.Message}");
			}
		}

		private async Task SendMessagesAsync()
		{
			try
			{
				while (true)
				{
					var message = Console.ReadLine();
					if (message == null) break;
					var messageBuffer = Encoding.UTF8.GetBytes(message);
					var lengthBuffer = BitConverter.GetBytes(messageBuffer.Length);

					// 先發送消息長度，再發送消息內容
					await _stream.WriteAsync(lengthBuffer, 0, lengthBuffer.Length);
					await _stream.WriteAsync(messageBuffer, 0, messageBuffer.Length);
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine("Exception: " + ex.Message);
			}
		}
	}
}
