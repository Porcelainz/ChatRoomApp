using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Test.Client
{
	internal class ConsoleHelper
	{
		public static string ReadPassword()
		{
			var password = new StringBuilder();
			while (true)
			{
				var key = Console.ReadKey(true);
				if (key.Key == ConsoleKey.Enter)
				{
					Console.WriteLine();
					break;
				}
				if (key.Key == ConsoleKey.Backspace)
				{
					if (password.Length > 0)
					{
						password.Length--;
						Console.Write("\b \b");
					}
				}
				else
				{
					password.Append(key.KeyChar);
					Console.Write("*");
				}
			}
			return password.ToString();
		}
	}
}
