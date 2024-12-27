// Resharper disable all

// Decompiled with JetBrains decompiler
// Type: Codice.Utils.CryptoServices
// Assembly: utils, Version=10.0.16.6443, Culture=neutral, PublicKeyToken=a107c9c6e34c8876
// MVID: 96660E94-0FFC-4B07-86FA-3DFB4789B1AC
// Assembly location: /Users/Chris/CodecksPlasticIntegration/Libraries/utils.dll

namespace Codice.Utils
{
	using System.Security.Cryptography;
	using System.Text;

	public class CryptoServices
	{
		public static string GetClearPassword(string cipherPassword, CryptoServices.EncryptionMode mode)
		{
			return CryptoServices.Decrypt(cipherPassword, "defaultUser", mode);
		}

		public static string GetDecryptedPassword(string encryptedPassword)
		{
			return encryptedPassword.StartsWith("|SoC|") && encryptedPassword.Length >= "|SoC|".Length
				? CryptoServices.GetClearPassword(encryptedPassword.Substring("|SoC|".Length),
					CryptoServices.EncryptionMode.TripleDES)
				: encryptedPassword;
		}

		private static string Decrypt(
			string cipherText,
			string Password,
			CryptoServices.EncryptionMode mode)
		{
			byte[] cipherData = Convert.FromBase64String(cipherText);
			PasswordDeriveBytes pdb = new PasswordDeriveBytes(Password, new byte[13]
			{
				(byte)73,
				(byte)118,
				(byte)97,
				(byte)110,
				(byte)32,
				(byte)77,
				(byte)101,
				(byte)100,
				(byte)118,
				(byte)101,
				(byte)100,
				(byte)101,
				(byte)118
			});
			SymmetricAlgorithm alg = CryptoServices.InstanceAlgorithm(mode, pdb);
			return Encoding.Unicode.GetString(CryptoServices.Decrypt(cipherData, alg));
		}

		private static byte[] Decrypt(byte[] cipherData, SymmetricAlgorithm alg)
		{
			MemoryStream memoryStream = new MemoryStream();
			CryptoStream cryptoStream =
				new CryptoStream((Stream)memoryStream, alg.CreateDecryptor(), CryptoStreamMode.Write);
			cryptoStream.Write(cipherData, 0, cipherData.Length);
			cryptoStream.Close();
			return memoryStream.ToArray();
		}

		private static SymmetricAlgorithm InstanceAlgorithm(
			CryptoServices.EncryptionMode mode,
			PasswordDeriveBytes pdb)
		{
			SymmetricAlgorithm symmetricAlgorithm = (SymmetricAlgorithm)null;
			if (mode == CryptoServices.EncryptionMode.TripleDES)
				goto label_2;
			label_1:
			return symmetricAlgorithm;
			label_2:
			symmetricAlgorithm = (SymmetricAlgorithm)TripleDES.Create();
			symmetricAlgorithm.Key = pdb.GetBytes(24);
			symmetricAlgorithm.IV = pdb.GetBytes(8);
			goto label_1;
		}

		public enum EncryptionMode
		{
			TripleDES,
		}
	}
}