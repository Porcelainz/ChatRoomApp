using Npgsql;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Test.Server
{
	internal class PostGreSqlHelper
	{
		public static ConcurrentDictionary<string, string> LoadUsersFromDatabase(string postgresConnectionString)
		{
			var users = new ConcurrentDictionary<string, string>();
			using (var conn = new NpgsqlConnection(postgresConnectionString))
			{
				conn.Open();
				string query = "SELECT account, password_hash FROM users";
				using (var cmd = new NpgsqlCommand(query, conn))
				{
					using (var reader = cmd.ExecuteReader())
					{
						while (reader.Read())
						{
							var account = reader.GetString(0);
							var passwordHash = reader.GetString(1);
							users[account] = passwordHash;
						}
					}
				}
			}
			return users;
		}

		public static async Task StoreMessagesToPostgresAsync(List<string> messages, NpgsqlDataSource _dataSource)
		{
			try
			{
				using (var conn = await _dataSource.OpenConnectionAsync())
				using (var transaction = conn.BeginTransaction())
				{
					using (var writer = conn.BeginTextImport("COPY chatroom_message (message) FROM STDIN"))
					{
						foreach (var message in messages)
						{
							await writer.WriteLineAsync(message);
						}
					}

					await transaction.CommitAsync();
					Console.WriteLine($"Stored {messages.Count} messages to Postgres using bulk insert");
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Error storing messages to Postgres: {ex.Message}");
				throw;
			}
		}
	}
}
