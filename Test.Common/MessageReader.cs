using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Test.Common
{
	public class MessageReader
	{
		public static async Task<string> ReadMessageAsync(Stream stream, byte[] buffer)
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
	}
}
