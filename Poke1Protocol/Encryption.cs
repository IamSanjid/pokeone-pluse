using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Management;
using Microsoft.Win32;
using System.Web;

namespace Poke1Protocol
{
    public static class StringCipher
    {
        public static string EncryptOrDecrypt(string plainText, string passPhrase)
        {
            var builder = new StringBuilder();

            if (plainText.Length > passPhrase.Length)
                return null;

            for (int i = 0; i < plainText.Length; ++i)
            {
                builder.Append((char)(plainText[i] ^ passPhrase[i]));
            }
            return builder.ToString();
        }

        public static byte[] Xor(byte[] first, byte[] second)
        {
            if (first.Length > second.Length) return null;
            var bytes = new List<byte>();
            for (int i = 0; i < first.Length; ++i)
            {
                bytes.Add((byte)(first[i] ^ second[i]));
            }

            return bytes.ToArray();
        }

        public static string Base64Encode(string plainText, Encoding encoder = null)
        {
            if (encoder is null)
                encoder = Encoding.ASCII;
            var plainTextBytes = encoder.GetBytes(plainText);
            return System.Convert.ToBase64String(plainTextBytes);
        }

        public static string Base64Decode(string base64EncodedData, Encoding encoder = null)
        {
            var base64EncodedBytes = System.Convert.FromBase64String(base64EncodedData);
            if (encoder is null)
                encoder = Encoding.ASCII;
            return encoder.GetString(base64EncodedBytes);
        }

        public static byte[] EncryptOrDecryptToBase64Byte(string plainText, string passPhrase)
        {
            var v = EncryptOrDecrypt(plainText, passPhrase);
            var s64 = Base64Encode(v);
            return Convert.FromBase64String(s64);
        }

        ///<summary>
        /// Base 64 Encoding with URL and Filename Safe Alphabet using UTF-8 character set.
        ///</summary>
        ///<param name="str">The origianl string</param>
        ///<returns>The Base64 encoded string</returns>
        public static string Base64ForUrlEncode(string str)
        {
            byte[] encbuff = Encoding.UTF8.GetBytes(str);
            return HttpServerUtility.UrlTokenEncode(encbuff);
        }
        ///<summary>
        /// Decode Base64 encoded string with URL and Filename Safe Alphabet using UTF-8.
        ///</summary>
        ///<param name="str">Base64 code</param>
        ///<returns>The decoded string.</returns>
        public static string Base64ForUrlDecode(string str)
        {
            byte[] decbuff = HttpServerUtility.UrlTokenDecode(str);
            return Encoding.UTF8.GetString(decbuff);
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
            
            return Guid.NewGuid().ToString().ToSHA1().Hexdigest().ToLowerInvariant();
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

    public class RC4Stream
    {
        public RC4Stream(byte[] seedKey, int drop = 0)
        {
            _seedKey = seedKey;
            Initialize();
            if (drop > 0)
            {
                do
                {
                    GetNextKeyByte();
                    drop--;
                }
                while (drop > 0);
            }
        }

        private void Initialize()
        {
            _i = 0;
            _j = 0;
            _sbox = new int[N];

            int[] tempArray = new int[N];

            int i = 0;
            do
            {
                int index = i % _seedKey.Length;
                tempArray[i] = _seedKey[index];
                _sbox[i] = i;
                ++i;
            } while (i != N);

            int j = 0, k = 0;

            do
            {
                k = (_sbox[j] + k + tempArray[j]) % N;

                int t_j = _sbox[j];
                int t_k = _sbox[k];

                _sbox[k] = t_j;
                _sbox[j] = t_k;

                ++j;
            } while (j != N);
        }

        private byte GetNextKeyByte()
        {
            _i = (_i + 1) % N;
            _j = (_sbox[_i] + _j) % N;

            int t_j_val = _sbox[_j];
            int t_i_val = _sbox[_i];

            _sbox[_i] = t_j_val;
            _sbox[_j] = t_i_val;

            return (byte)_sbox[(_sbox[_j] + _sbox[_i]) % N];
        }

        public byte[] Crypt(byte[] data)
        {
            if (data is null || data.Length <= 0) return data;

            int i = 0;
            byte[] chiper = new byte[data.Length];

            do
            {
                chiper[i] = (byte)(data[i] ^ GetNextKeyByte());
                i++;
            } while (i < data.Length);

            return chiper;
        }

        private const int N = 256;

        private int[] _sbox;

        private readonly byte[] _seedKey;

        private int _i;

        private int _j;
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

        public static string ConvertToAnotherEncoding(this string text, Encoding from, Encoding to)
        {
            if (from is null || to is null) return null;

            if (from.Equals(to)) return text;

            var textBytes = from.GetBytes(text);
            return to.GetString(textBytes);
        }

        public static byte[] ToByteArray(this string text, Encoding encoder = null)
        {
            if (string.IsNullOrEmpty(text))
                return null;
            if (encoder is null) encoder = Encoding.UTF8;

            return encoder.GetBytes(text);
        }
    }
}
