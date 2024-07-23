using System;
using System.Security.Cryptography;
using System.Text;

namespace Test.Common
{
	public class Cryptography
	{
		private static readonly byte[] FixedSalt = Encoding.UTF8.GetBytes("ecstasy_");
		public static string HashPassword(string password)
		{
			// Hash the password with the fixed salt
			var pbkdf2 = new Rfc2898DeriveBytes(password, FixedSalt, 10000);
			byte[] hash = pbkdf2.GetBytes(20);

			// Combine the salt and password bytes for later use (even though the salt is fixed)
			byte[] hashBytes = new byte[FixedSalt.Length + hash.Length];
			Array.Copy(FixedSalt, 0, hashBytes, 0, FixedSalt.Length);
			Array.Copy(hash, 0, hashBytes, FixedSalt.Length, hash.Length);

			// Convert to Base64
			string base64Hash = Convert.ToBase64String(hashBytes);
			return base64Hash;
		}
	}
}
