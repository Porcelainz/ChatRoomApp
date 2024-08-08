using System;
using System.IO;
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
		private async Task<string> ReadMessageAsync(Stream stream, byte[] buffer)
		{
			int bytesRead = await stream.ReadAsync(buffer, 0, 4);
			if (bytesRead == 0) return null;

			var messageLength = BitConverter.ToInt32(buffer, 0);
			using (var memoryStream = new MemoryStream())
			{
				int remainingBytes = messageLength;
				while (remainingBytes > 0)
				{
					int bytesToRead = Math.Min(remainingBytes, buffer.Length);
					bytesRead = await stream.ReadAsync(buffer, 0, bytesToRead);
					if (bytesRead == 0) break;
					await memoryStream.WriteAsync(buffer, 0, bytesRead);
					remainingBytes -= bytesRead;
				}

				var messageBytes = memoryStream.ToArray();
				var message = Encoding.UTF8.GetString(messageBytes);
				return message;
			}
		}
		private async Task ReceiveMessagesAsync()
		{
			const int BufferSize = 1024;
			byte[] messageBuffer = new byte[BufferSize];
			try
			{
				while (true)
				{
					var message = await ReadMessageAsync(_stream, messageBuffer);
					if (message == null) break;

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
