using System;
using System.Text;
using System.Threading.Tasks;
using Test.Common;

namespace Test.Client.ChatClient
{
	partial class Client
	{
		public async Task<bool> AuthenticateAsync(string password)
		{
			try
			{
				string credentials = $"{_username}:{Cryptography.HashPassword(password)}";
				byte[] credentialsBytes = Encoding.UTF8.GetBytes(credentials);

				await _stream.WriteAsync(credentialsBytes, 0, credentialsBytes.Length).ConfigureAwait(false);

				byte[] buffer = new byte[1024];
				int bytesRead = await _stream.ReadAsync(buffer, 0, buffer.Length).ConfigureAwait(false);
				int response = BitConverter.ToInt32(buffer, 0);

				if (response == 1)
				{
					return true;
				}
				if (response == 2)
				{
					Console.WriteLine("You are already logged in on another device.");
					return true;
				}
				return false;

			}
			catch (Exception ex)
			{
				Console.WriteLine($"Authentication error: {ex.Message}");
				return false;
			}
		}
	}
}
