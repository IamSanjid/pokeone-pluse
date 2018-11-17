using System;
using System.Text;

namespace Poke1Protocol
{
    public static class StringCipher
    {
        public static string EncryptOrDecrypt(string plainText, string passPhrase)
        {
            StringBuilder builder = new StringBuilder();

            for (int i = 0; i < plainText.Length; ++i)
            {
                builder.Append((char)(plainText[i] ^ passPhrase[i]));
            }
            return builder.ToString();
        }

        public static string Base64Encode(string plainText)
        {
            var plainTextBytes = System.Text.Encoding.UTF8.GetBytes(plainText);
            return System.Convert.ToBase64String(plainTextBytes);
        }

        public static string Base64Decode(string base64EncodedData)
        {
            var base64EncodedBytes = System.Convert.FromBase64String(base64EncodedData);
            return System.Text.Encoding.UTF8.GetString(base64EncodedBytes);
        }

        public static byte[] EncryptOrDecryptToBase64Byte(string plainText, string passPhrase)
        {
            var v = EncryptOrDecrypt(plainText, passPhrase);
            var s64 = Base64Encode(v);
            return Convert.FromBase64String(s64);
        }
    }
}
