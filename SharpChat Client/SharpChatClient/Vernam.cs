namespace SharpChatClient
{
    /// <summary>
    /// Class containing the algorithm for the vernam encryption
    /// </summary>
    public static class Vernam
    {
        /// <summary>
        /// Generates a key for the vernam encryption of an defined site
        /// </summary>
        /// <param name="size">Size of the key</param>
        /// <returns>Generated Key</returns>
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

        /// <summary>
        /// Cipher a string and generate a key to decipher it
        /// </summary>
        /// <param name="toCipher">A string to cipher</param>
        /// <returns>Ciphered string and the key to decipher it</returns>
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
                encoded[i] = (char)((char)toEncode[i] + (char)key[i]);
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

        /// <summary>
        /// Decipher a string using supplied key
        /// </summary>
        /// <param name="toDecipher">String to decipher alongside a key</param>
        /// <returns>Deciphered String</returns>
        public static string VernamDecipher(KeyValuePair<string, string> toDecipher)
        {
            // Init Arrays
            string[] KeyString = toDecipher.Key.Split(";");

            char[] key = new char[KeyString.Length-1];

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
