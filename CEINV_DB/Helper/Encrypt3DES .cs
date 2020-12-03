using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Web.Security;

namespace CEINV_DB.Helper
{
    class Encrypt3DES
    {
        // 解密
        public static string Decrypt(string password)
        {
            string strKey = "Encrypt3DES";
            TripleDESCryptoServiceProvider DES1 = new TripleDESCryptoServiceProvider();
            DES1.Key = UTF8Encoding.UTF8.GetBytes(FormsAuthentication.HashPasswordForStoringInConfigFile(strKey, "md5").Substring(0, 24));
            DES1.Mode = CipherMode.ECB;
            DES1.Padding = System.Security.Cryptography.PaddingMode.PKCS7;
            ICryptoTransform DESDecrypt = DES1.CreateDecryptor();
            byte[] Buffer1 = Convert.FromBase64String(password);
            string Decryptstring = UTF8Encoding.UTF8.GetString(DESDecrypt.TransformFinalBlock(Buffer1, 0, Buffer1.Length));

            return Decryptstring;
        }
        // 加密
        public static string Encryption(string password)
        {
            string strKey = "Encrypt3DES";
            TripleDESCryptoServiceProvider DES = new TripleDESCryptoServiceProvider();
            DES.Key = UTF8Encoding.UTF8.GetBytes(FormsAuthentication.HashPasswordForStoringInConfigFile(strKey, "md5").Substring(0, 24));
            DES.Mode = CipherMode.ECB;
            ICryptoTransform DESEncrypt = DES.CreateEncryptor();
            byte[] Buffer = UTF8Encoding.UTF8.GetBytes(password);
            string Encryptionstring = Convert.ToBase64String(DESEncrypt.TransformFinalBlock(Buffer, 0, Buffer.Length));

            return Encryptionstring;
        }
    }
}
