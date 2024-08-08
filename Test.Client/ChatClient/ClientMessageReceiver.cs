using System.Text;
using System.Threading.Tasks;
using System;
using Test.Common;

namespace Test.Client.ChatClient
{
	partial class Client
	{
		private async Task ReceiveMessagesAsync()
		{
			const int BufferSize = 1024;
			byte[] messageBuffer = new byte[BufferSize];

			try
			{
				_receiveTimer.Start();
				while (true)
				{
					var message = await MessageReader.ReadMessageAsync(_stream, messageBuffer);
					if (message == null) break;

					_messageQueue.Enqueue(message);
					_messageCount++;

					if (_messageCount >= 1)
					{
						_messageSemaphore.Release();
					}
					if (_messageCount == 1)
					{
						// _form.DisplayMessage($"{_username} received {TARGET_MESSAGE_COUNT} messages in {_receiveTimer.ElapsedMilliseconds} milliseconds.");
						break;
					}
				}
				_receiveTimer.Stop();
				_form.DisplayMessage($"{_username} received {TARGET_MESSAGE_COUNT} messages in {_receiveTimer.ElapsedMilliseconds} milliseconds.");
			}
			catch (Exception ex)
			{
				_form.DisplayMessage($"Exception: {ex.Message}");
			}
		}

		
	}
}
