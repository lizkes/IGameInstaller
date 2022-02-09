using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace IGameInstaller.Helper
{
    public class CryptoHelper
    {
        public static byte[] AesEncrypt(string content)
        {
            using var aes = Aes.Create();
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;
            aes.BlockSize = 128;
            aes.IV = Base64Decode("3rqBYyUB02E5HLOCI2i/2A==");
            aes.Key = Base64Decode("DP/B868Op9Ataw0l2YGtaS822jt26XWv7e3vMVa5zFI=");
            ICryptoTransform encryptor = aes.CreateEncryptor(aes.Key, aes.IV);

            using var msEncrypt = new MemoryStream();
            using var csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write);
            {
                using var swEncrypt = new StreamWriter(csEncrypt);
                swEncrypt.Write(content);
            }
            var encrypted = msEncrypt.ToArray();
            return encrypted;
        }

        public static byte[] Base64Decode(string base64EncodedData)
        {
            return Convert.FromBase64String(base64EncodedData);
        }

        public static string Base64Encode(byte[] bytes)
        {
            return Convert.ToBase64String(bytes);
        }
    }
}
