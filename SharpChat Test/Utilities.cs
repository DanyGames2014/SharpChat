namespace ChatUnitTest
{
    public static class Utilities
    {
        public static string randomString(int length)
        {
            string validChars = @"abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ123456789";
            Random rnd = new();
            string result = string.Empty;

            for (int i = 0; i < length; i++)
            {
                int randomNum = rnd.Next(0, validChars.Length - 1);
                result += validChars[randomNum];
            }
            return result;
        }
    }
}
