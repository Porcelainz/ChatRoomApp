using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Test.Common;

namespace Test.Client.ChatClient
{
	partial class Client
	{
		public async Task SendMessageAsync(string message)
		{
			try
			{
				var messageBytes = Encoding.UTF8.GetBytes(message);
				var messageForSen = LengthPrefixAdder.AddLengthPrefix(messageBytes);
				await _stream.WriteAsync(messageForSen, 0, messageForSen.Length);
			}
			catch (Exception ex)
			{
				_form.DisplayMessage("異常：" + ex.Message);
			}
		}
		private async Task ProcessMessagesAsync(CancellationToken cancellationToken)
		{
			while (!cancellationToken.IsCancellationRequested)
			{
				await _messageSemaphore.WaitAsync(cancellationToken);
				if (_messageQueue.Count >= 1)
				{
					var messageBatch = _messageQueue.ToList();
					await WriteLogBatchAsync(messageBatch);
					_messageQueue = null;
					messageBatch.Clear();
				}
			}
		}
		private async Task WriteLogBatchAsync(List<string> messages)
		{
			try
			{
				var batchContent = string.Join(Environment.NewLine, messages);
				await _logWriter.WriteAsync(batchContent + Environment.NewLine);
				_logWriter.Close();
			}
			catch (Exception ex)
			{
				_form.DisplayMessage($"Log Exception: {ex.Message}");
			}

		}
	}
}
