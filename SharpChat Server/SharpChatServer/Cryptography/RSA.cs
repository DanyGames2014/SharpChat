using System.Security.Cryptography;
using System.Text;
using System.Xml.Serialization;

namespace SharpChatServer.Cryptography
{
    /// <summary>
    /// Class that has definitions for the RSA Encryption Algorithm
    /// </summary>
    public static class RSA
    {
        /// <summary>
        /// Extracts a string representation of a key from an xml serialized key
        /// </summary>
        /// <param name="publicKey">XML Serialized key</param>
        /// <returns>String representation of the key</returns>
        public static string GetKeyString(RSAParameters publicKey)
        {
            var stringWriter = new StringWriter();
            var xmlSerializer = new XmlSerializer(typeof(RSAParameters));
            xmlSerializer.Serialize(stringWriter, publicKey);
            return stringWriter.ToString();
        }

        /// <summary>
        /// Encrypts an string using the defined key
        /// </summary>
        /// <param name="textToEncrypt">Text To Encrypt</param>
        /// <param name="publicKeyString">Key to Encrypt it with</param>
        /// <returns>An encrypted string</returns>
        public static string EncryptRSA(string textToEncrypt, string publicKeyString)
        {
            var bytesToEncrypt = Encoding.UTF8.GetBytes(textToEncrypt);

            using (var rsa = new RSACryptoServiceProvider(2048))
            {
                rsa.FromXmlString(publicKeyString.ToString());
                var encryptedData = rsa.Encrypt(bytesToEncrypt, true);
                var base64Encrypted = Convert.ToBase64String(encryptedData);
                return base64Encrypted;
            }
        }

        /// <summary>
        /// Decrypts an string using the defined key
        /// </summary>
        /// <param name="textToDecrypt">Text to Decrypt</param>
        /// <param name="privateKeyString">Key to decrypt with</param>
        /// <returns>A decrypted string</returns>
        public static string DecryptRSA(string textToDecrypt, string privateKeyString)
        {
            var bytesToDescrypt = Encoding.UTF8.GetBytes(textToDecrypt);

            using (var rsa = new RSACryptoServiceProvider(2048))
            {
                rsa.FromXmlString(privateKeyString);

                var resultBytes = Convert.FromBase64String(textToDecrypt);
                var decryptedBytes = rsa.Decrypt(resultBytes, true);
                var decryptedData = Encoding.UTF8.GetString(decryptedBytes);
                return decryptedData.ToString();
            }
        }
    }
}
