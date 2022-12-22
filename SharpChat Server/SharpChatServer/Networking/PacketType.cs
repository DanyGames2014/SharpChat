namespace ChatThreadTest.Networking
{
    public enum PacketType
    {
        UNKNOWN = 0,
        REQUEST_CLIENT_PUBLIC_KEY = 1,
        RECEIVE_CLIENT_PUBLIC_KEY = 2,
        ASK_USERNAME = 3,
        CLIENT_USERNAME = 4,
        ASK_PASSWORD = 5,
        CLIENT_PASSWORD = 6,
        AUTH_RESULT = 7,
        CHAT_WELCOME = 8,
        GETSINCE = 9,
        MESSAGE = 10
    }
}
