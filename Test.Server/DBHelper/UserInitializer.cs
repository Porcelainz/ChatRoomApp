using Npgsql;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;

namespace Test.Server.DBHelper
{
	internal class UserInitializer
	{
		private static readonly byte[] _fixedSalt = Encoding.UTF8.GetBytes("ecstasy_");

		public void Init_user()
		{

			var userCount = UserCount();
			if (userCount)
			{
				Console.WriteLine("user already existed");
			}
			else
			{
				for (int i = 1; i <= 100; i++)
				{
					string account = $"casey.yang{i}";
					string password = "Wan@1234";

					string hashedPassword = HashPassword(password);

					if (!UserExists(account, hashedPassword))
					{
						InsertUser(account, hashedPassword);
					}
					else
					{
						Console.WriteLine("使用者已存在，無需插入。");
					}
				}
				string test_account = "casey.yang_test";
				string test_password = "Wan@1234";
				string test_hashedPassword = HashPassword(test_password);
				if (!UserExists(test_account, test_hashedPassword))
				{
					InsertUser(test_account, test_hashedPassword);
				}
				else
				{
					Console.WriteLine("使用者已存在，無需插入。");
				}
				Console.WriteLine("所有使用者已創建並插入到資料庫中。");
			}
			
		}

		public static string HashPassword(string password)
		{
			using (var sha256 = SHA256.Create())
			{
				// Combine the password and fixed salt
				byte[] combinedBytes = new byte[_fixedSalt.Length + Encoding.UTF8.GetBytes(password).Length];
				Array.Copy(_fixedSalt, 0, combinedBytes, 0, _fixedSalt.Length);
				Array.Copy(Encoding.UTF8.GetBytes(password), 0, combinedBytes, _fixedSalt.Length, Encoding.UTF8.GetBytes(password).Length);

				// Compute the hash
				byte[] hashBytes = sha256.ComputeHash(combinedBytes);

				// Convert to Base64
				string base64Hash = Convert.ToBase64String(hashBytes);
				return base64Hash;
			}
		}

		private static bool UserExists(string account, string hashedPassword)
		{
			string connectionString = "Host=localhost;Username=op;Password=Op@1234;Database=mydb"; // Update with your PostgreSQL connection details
			string query = "SELECT COUNT(1) FROM users WHERE account = @account AND password_hash = @password_hash";

			using (var conn = new NpgsqlConnection(connectionString))
			using (var cmd = new NpgsqlCommand(query, conn))
			{
				cmd.Parameters.AddWithValue("account", account);
				cmd.Parameters.AddWithValue("password_hash", hashedPassword);

				conn.Open();
				var userExists = (long)cmd.ExecuteScalar();
				return userExists > 0;
			}
		}

		private static bool UserCount()
		{
			string connectionString = "Host=localhost;Username=op;Password=Op@1234;Database=mydb";
			string query = "SELECT COUNT(*) FROM users";
			using (var conn = new NpgsqlConnection(connectionString))
			using (var cmd = new NpgsqlCommand(query, conn))
			{
				conn.Open();
				var userCount = (long)cmd.ExecuteScalar();
				return userCount > 0;
			}
		}



		private static void InsertUser(string account, string hashedPassword)
		{
			string connectionString = "Host=localhost;Username=op;Password=Op@1234;Database=mydb"; // Update with your PostgreSQL connection details
			string query = "INSERT INTO users (account, password_hash) VALUES (@account, @password_hash)";

			using (var conn = new NpgsqlConnection(connectionString))
			using (var cmd = new NpgsqlCommand(query, conn))
			{
				cmd.Parameters.AddWithValue("account", account);
				cmd.Parameters.AddWithValue("password_hash", hashedPassword);

				conn.Open();
				cmd.ExecuteNonQuery();
				Console.WriteLine("新使用者已創建並插入到資料庫中。");
			}
		}
	}
}
