using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Management;
using Microsoft.Win32;

namespace Poke1Protocol
{
    public static class StringCipher
    {
        public static string EncryptOrDecrypt(string plainText, string passPhrase)
        {
            var builder = new StringBuilder();

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

        public static string GetRandomInfo()
        {
            //switch (new Random().Next(0, 2))
            //{
            //    case 0:
            //        return "5f49431533cc698e89915b5b7d0cbadf99231651";
            //    case 1:
            //        return "5fcd2b27a9c9dbd0c45974ebdcb7f25b2acb0759";
            //    case 2:
            //        return "4538a9070b456446cce4d43fbdfe42efff75213e";
            //    default:
            //        throw new Exception("Unexpected error occured.");
            //}
            ////var os = Environment.OSVersion;

            ////var osString = os.VersionString.Replace(os.Version.ToString(), "") + $"({os.Version}) " + (Environment.Is64BitOperatingSystem ? $"64bit" : "32bit");

            return new Guid().ToString().ToSHA1().Hexdigest().ToLowerInvariant();
        }

        public static string HKLM_GetString(string path, string key)
        {
            try
            {
                RegistryKey rk = Registry.LocalMachine.OpenSubKey(path);
                if (rk == null) return "";
                return (string)rk.GetValue(key);
            }
            catch { return ""; }
        }

        public static string FriendlyName()
        {
            string ProductName = HKLM_GetString(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion", "ProductName");
            string CSDVersion = HKLM_GetString(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion", "CSDVersion");
            if (ProductName != "")
            {
                return (ProductName.StartsWith("Microsoft") ? "" : "Microsoft ") + ProductName +
                       (CSDVersion != "" ? " " + CSDVersion : "");
            }
            return "";
        }
    }
    public static class StringExtentions
    {
        public static byte[] ToSHA1(this string text)
        {
            SHA1 sha = new SHA1CryptoServiceProvider();
            var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(text));
            return bytes;
        }

        public static string Encode(this string text)
        {
            var bytes = Encoding.Convert(Encoding.ASCII, Encoding.UTF8, Encoding.UTF8.GetBytes(text));
            return Encoding.UTF8.GetString(bytes);
        }

        public static string Hexdigest(this byte[] bytes)
        {
            string hexaHash = "";
            foreach (byte b in bytes)
            {
                hexaHash += String.Format("{0:x2}", b);
            }
            return hexaHash;
        }

        public static string ReverseString(this string text)
        {
            var reversedList = text.Reverse().ToList();
            var builder = new StringBuilder();
            foreach (var t in reversedList)
            {
                builder.Append(t);
            }

            return builder.ToString();
        }
    }
}
