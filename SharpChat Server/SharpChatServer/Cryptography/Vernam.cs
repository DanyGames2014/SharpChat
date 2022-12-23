namespace SharpChatServer.Cryptography
{
    public static class Vernam
    {
        public static int[] generateKey(int size)
        {
            Random rnd = new Random();
            int[] key = new int[size];

            for (int i = 0; i < key.Length; i++)
            {
                key[i] = (int)rnd.NextInt64(0, 1024);
            }

            return key;
        }
        public static KeyValuePair<string, string> VernamCipher(string toCipher)
        {
            // Init Arrays
            int[] key = generateKey(toCipher.Length);

            char[] toEncode = toCipher.ToCharArray();

            char[] encoded = new char[toEncode.Length];

            string StringKey = string.Empty;

            // Encode Chars
            for (int i = 0; i < toEncode.Length; i++)
            {
                encoded[i] = (char)(toEncode[i] + (char)key[i]);
            }

            // Encode Keys to String
            for (int i = 0; i < key.Length; i++)
            {
                StringKey += (char)key[i];
                StringKey += ";";
            }

            // Return
            return new KeyValuePair<string, string>(StringKey, new string(encoded));
        }

        public static string VernamDecipher(KeyValuePair<string, string> toDecipher)
        {
            // Init Arrays
            string[] KeyString = toDecipher.Key.Split(";");

            char[] key = new char[KeyString.Length - 1];

            for (int i = 0; i < key.Length; i++)
            {
                try { key[i] = KeyString[i][0]; } catch (Exception) { }
            }

            char[] decodedChars = new char[key.Length];

            for (int i = 0; i < decodedChars.Length; i++)
            {
                try { decodedChars[i] = (char)(toDecipher.Value[i] - key[i]); } catch (Exception) { }
            }

            return new string(decodedChars);
        }
    }
}
