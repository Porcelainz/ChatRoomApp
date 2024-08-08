using System;
using System.Collections.Generic;
using System.Text;

namespace Test.Common
{
	public class PrefixAdder
	{
		public static byte[] AddLengthPrefix(byte[] messageBytes)
		{
			var lengthBytes = BitConverter.GetBytes(messageBytes.Length);
			byte[] prefixedMessage = new byte[lengthBytes.Length + messageBytes.Length];
			Buffer.BlockCopy(lengthBytes, 0, prefixedMessage, 0, lengthBytes.Length);
			Buffer.BlockCopy(messageBytes, 0, prefixedMessage, lengthBytes.Length, messageBytes.Length);
			return prefixedMessage;
		}
	}
}
