using StackExchange.Redis;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Npgsql;
using Test.Common;


namespace Test.Server
{
	internal class Program
	{
		static async Task Main(string[] args)
		{
			var server1 = new ChatServer.Server("127.0.0.1", 9000);
			var server2 = new ChatServer.Server("127.0.0.1", 9001);

			Task serverTask1 = server1.StartAsync();
			Task serverTask2 = server2.StartAsync();

			// 只在 9000 端口的伺服器上啟動 MessageProcessor
			var messageMiddleware = new MessageProcessor();
			Task messageMiddlewareTask = messageMiddleware.StartAsync();

			await Task.WhenAll(serverTask1, serverTask2, messageMiddlewareTask);
		}
	}

	
}
