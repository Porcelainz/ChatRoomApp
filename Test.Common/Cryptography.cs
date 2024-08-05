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
			using (var sha256 = SHA256.Create())
			{
				// Combine the password and fixed salt
				byte[] combinedBytes = new byte[FixedSalt.Length + Encoding.UTF8.GetBytes(password).Length];
				Array.Copy(FixedSalt, 0, combinedBytes, 0, FixedSalt.Length);
				Array.Copy(Encoding.UTF8.GetBytes(password), 0, combinedBytes, FixedSalt.Length, Encoding.UTF8.GetBytes(password).Length);

				// Compute the hash
				byte[] hashBytes = sha256.ComputeHash(combinedBytes);

				// Convert to Base64
				string base64Hash = Convert.ToBase64String(hashBytes);
				return base64Hash;
			}
		}
	}
}