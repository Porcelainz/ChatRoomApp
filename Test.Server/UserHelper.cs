using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Test.Common;
namespace Test.Server
{
	internal class UserHelper
	{
		public static Dictionary<string, string> InitUser()
		{
			Dictionary<string, string> users = new Dictionary<string, string>();
			for (int i = 1; i < 101; i++)
			{
				string account = $"casey.yang{i}";
				string password = "Wan@1234";

				// Hash the password
				string hashedPassword = Cryptography.HashPassword(password);
				users.Add(account, hashedPassword);

			}
			return users;
		}
	}
}
