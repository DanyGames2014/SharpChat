namespace SharpChatServer.Networking
{
    public enum PacketType
    {
        // Unknown Packet Type, usually used when error handling
        // No Data Stored
        UNKNOWN = 0,

        // Packet used to ask the other side to provide their RSA Public Key
        // No Data Stored
        PUBLIC_KEY_REQUEST = 1,

        // Packet used to provide the other side with this side RSA Public Key
        // publicKey - RSA Public Key
        PUBLIC_KEY = 2,

        // Packet used by the Server to ask for the Client to authentificate
        // No Data Stored
        REQUEST_AUTH = 3,

        // Packet used by the Client to provide Server with Authentification Details
        // username - Username
        // password - Password
        AUTH = 4,

        // Packet used by the Server to tell client if Authentification was succesfull
        // result - The result of authentification, either true or false
        AUTH_RESULT = 5,

        // Packet used by the Server to welcome into the chatroom
        // The Client is now able to access the chatroom messages
        // Client should upon receiving this Packet request past messages if they desire so
        // motd - Message Of The Day
        CHAT_WELCOME = 6,

        // Packet used by the Client to receive all messages since a specified timestamp
        // timestamp - The timestamp frm which to get messages
        GET_MESSAGE_SINCE = 7,

        // Packet used to carry normal messages
        // senderName - Nickname of the sender
        // message - The message itself
        // timestamp - Unix Milliseconds Timestamp when the message was sent
        MESSAGE = 8,

        // Packet used to carry a command from Client to Server
        // command - The command itself to carry out
        COMMAND = 9
    }
}
