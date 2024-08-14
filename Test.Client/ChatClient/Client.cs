using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Test.Client.ChatClient
{
	partial class Client

	{
		private TcpClient _client;
		private NetworkStream _stream;
		private ChatForm _form;
		private string _logFilePath;
		private StreamWriter _logWriter;
		private ConcurrentQueue<string> _messageQueue;
		private CancellationTokenSource _cancellationTokenSource;
		private SemaphoreSlim _messageSemaphore;
		private string _username;
		private Stopwatch _receiveTimer = new Stopwatch();
		private int _messageCount;
		private const int _targetMessageCount = 10000;
		private string _clientLogFolderPath;
		

		public Client(string ipAddress, int port, ChatForm form, string username)
		{
			_client = new TcpClient();
			_client.Connect(ipAddress, port);
			_stream = _client.GetStream();
			_form = form;
			_username = username;
			_messageQueue = new ConcurrentQueue<string>();
			_cancellationTokenSource = new CancellationTokenSource();
			_messageSemaphore = new SemaphoreSlim(0);
			_clientLogFolderPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Client_log");
			_logFilePath = Path.Combine(_clientLogFolderPath, $"chatLog_{username}.txt");

			if (_username.Contains("test"))
			{
				string _concurrent_log_file_path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "concurrent_log");
				Directory.CreateDirectory(_concurrent_log_file_path);
				_logFilePath = Path.Combine(_concurrent_log_file_path,
					$"chatLog_{username}{new Random().Next(100)}.txt");
			}

			_logWriter = new StreamWriter(_logFilePath, true) { AutoFlush = true };
		}
		public async void Start()
		{
			var receiveTask = ReceiveMessagesAsync();
			var processTask = ProcessMessagesAsync(_cancellationTokenSource.Token);
			
			
			await Task.WhenAll(receiveTask, processTask);
		}
		

		
		public void Dispose()
		{
			_logWriter?.Dispose();
			_cancellationTokenSource.Cancel();
		}
		

		
	}
}
