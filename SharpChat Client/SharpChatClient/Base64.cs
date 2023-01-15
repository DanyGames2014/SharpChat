using System.Text;

namespace SharpChatClient
{
    /// <summary>
    /// Methods to Base64 Encode and Decode
    /// </summary>
    public static class Base64
    {
        /// <summary>
        /// Encodes a string using Base64
        /// </summary>
        /// <param name="toEncode">String to encode</param>
        /// <returns>Base64 encoded string</returns>
        public static string Encode(string toEncode)
        {
            return Convert.ToBase64String(Encoding.UTF8.GetBytes(toEncode));
        }

        /// <summary>
        /// Decodes a string using Base64
        /// </summary>
        /// <param name="toDecode">Base64 String to decode</param>
        /// <returns>String decoded from base64</returns>
        public static string Decode(string toDecode)
        {
            return Encoding.UTF8.GetString(Convert.FromBase64String(toDecode));
        }
    }
}
