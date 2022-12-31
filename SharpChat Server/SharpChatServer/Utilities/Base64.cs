using System.Text;

namespace SharpChatServer.Utilities
{
    public static class Base64
    {
        public static string Encode(string toEncode)
        {
            return Convert.ToBase64String(Encoding.UTF8.GetBytes(toEncode));
        }

        public static string Decode(string toDecode)
        {
            return Encoding.UTF8.GetString(Convert.FromBase64String(toDecode));
        }
    }
}
