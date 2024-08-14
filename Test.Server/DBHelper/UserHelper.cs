using System.Collections.Generic;
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
				string password = "ZWNzdGFzeV9BB+8tcKK482t6kLR3ra+Ltyic7w==";

				// Hash the password
				//string hashedPassword = Cryptography.HashPassword(password);
				users.Add(account, password);

			}
			return users;
		}
	}
}
