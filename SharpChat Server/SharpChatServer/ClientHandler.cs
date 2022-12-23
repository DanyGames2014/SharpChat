using SharpChatServer.Cryptography;
using SharpChatServer.Networking;
using System.Diagnostics;
using System.Net.Sockets;

namespace SharpChatServer
{
    public class ClientHandler
    {
        // References to other modules
        private Server server;
        private Logger logger;

        // TCP Client
        public TcpClient client;
        private NetworkStream ns;
        private StreamWriter sw;
        private StreamReader sr;

        // Signalization
        public bool cleanable = true;
        public bool run = true;

        // State
        ConnectionState state;
        LegacyConnectionState legacyState;
        EncryptionMethod encryptionMethod;
        public bool auth = false;
        Stopwatch timeout;

        // Encryption Provider
        EncryptionProvider encryptionProvider;

        // Chat
        ChatManager chatManager;
        List<ChatMessage> subscribeList;

        // User Data
        public bool dumbTerminal;
        public string username;
        string password;

        public ClientHandler(TcpClient tcpClient, Server server)
        {
            // Assign References to Server and Logger and chat Manager
            this.server = server;
            logger = server.logger;
            chatManager = server.chatManager;

            // TCP Networking
            client = tcpClient;
            ns = client.GetStream();
            sw = new(ns) { AutoFlush = true };
            sr = new(ns);

            // Set the default Connection State
            state = ConnectionState.HANDSHAKE_SEND;
            legacyState = LegacyConnectionState.LOGIN_ASK_USERNAME;
            encryptionMethod = EncryptionMethod.NONE;
            auth = false;

            // Init Encryption Provider
            encryptionProvider = new(this);

            // Initilaized username and password
            username = string.Empty;
            password = string.Empty;

            // Create the stopwatch for timeout tracking
            timeout = new Stopwatch();

            // By default the handler is not cleanable
            cleanable = false;

            // Make a list to subsribe for chat message and register it in Chat Manager
            subscribeList = new List<ChatMessage>();
            lock (server.chatManager.subscribedThreads)
            {
                server.chatManager.subscribedThreads.Add(subscribeList);
            }
        }

        public void Send(Packet packet)
        {
            sw.WriteLine(packet.Serialize());
            sw.Flush();
        }

        public void Run()
        {
            if (timeout.ElapsedMilliseconds < 60000)
            {
                try
                {
                    switch(state)
                    {
                        case ConnectionState.HANDSHAKE_SEND:
                            sw.WriteLine("Do you wish to connect in a Dumb Terminal mode? The connection will not be secure and some features may not be accesible (yes/no)");
                            logger.WriteDebug("Asking Client for Dumb Terminal Mode");
                            state = ConnectionState.HANDSHAKE_RECV;
                            timeout.Restart();
                            break;

                        case ConnectionState.HANDSHAKE_RECV:
                            if(ns.DataAvailable)
                            {
                                timeout.Stop();
                                var temp = sr.ReadLine();
                                if (temp.Contains("yes", StringComparison.InvariantCultureIgnoreCase))
                                {
                                    dumbTerminal = true;
                                    logger.WriteDebug("Client connected with Dumb Terminal Mode");
                                    state = ConnectionState.CONNECTED;
                                }
                                else if(temp.Contains("no", StringComparison.InvariantCultureIgnoreCase))
                                {
                                    dumbTerminal = false;
                                    logger.WriteDebug("Client connected with Normal Mode");
                                    state = ConnectionState.CONNECTED;
                                }
                                else
                                {
                                    state = ConnectionState.HANDSHAKE_SEND;
                                }
                            }
                            break;

                        case ConnectionState.CONNECTED:
                            if (dumbTerminal)
                            {
                                // Dumb Terminal Mode
                                switch (legacyState)
                                {
                                    case LegacyConnectionState.LOGIN_ASK_USERNAME:
                                        sw.WriteLine("WARNING! Due to the connection mode your information isn't secure!");
                                        sw.WriteLine("Please Enter your Username : ");
                                        legacyState = LegacyConnectionState.LOGIN_REC_USERNAME;
                                        timeout.Restart();
                                        break;

                                    case LegacyConnectionState.LOGIN_REC_USERNAME:
                                        if (ns.DataAvailable)
                                        {
                                            timeout.Stop();
                                            username = sr.ReadLine() + "";
                                            legacyState = LegacyConnectionState.LOGIN_ASK_PASSWORD;
                                        }
                                        break;

                                    case LegacyConnectionState.LOGIN_ASK_PASSWORD:
                                        sw.WriteLine("Please Enter your Password : ");
                                        legacyState = LegacyConnectionState.LOGIN_REC_PASSWORD;
                                        timeout.Restart();
                                        break;

                                    case LegacyConnectionState.LOGIN_REC_PASSWORD:
                                        if (ns.DataAvailable)
                                        {
                                            timeout.Stop();
                                            password = sr.ReadLine() + "";
                                            legacyState = LegacyConnectionState.LOGIN_AUTH_USER;
                                        }
                                        break;

                                    case LegacyConnectionState.LOGIN_AUTH_USER:
                                        // Username Check
                                        if (
                                            // System name spoofing
                                            username.Contains("SYSTEM", StringComparison.CurrentCultureIgnoreCase) || 
                                            // Username length Check
                                            username.Length == 0
                                           )
                                        {
                                            sw.WriteLine("Auth Failed! Invalid Username");
                                            legacyState = LegacyConnectionState.LOGIN_ASK_USERNAME;
                                        }
                                        // Authentification passed
                                        else
                                        {
                                            sw.WriteLine("Auth Succesfull");
                                            legacyState = LegacyConnectionState.CONNECT_TO_CHAT;
                                        }
                                        break;

                                    case LegacyConnectionState.CONNECT_TO_CHAT:
                                        sw.WriteLine("Welcome to the Chat Room!");
                                        sw.WriteLine("Chat History isn't supported on your client");
                                        legacyState = LegacyConnectionState.CONNECTED;
                                        chatManager.Add("SYSTEM", username + " has connected to the chatroom");
                                        break;

                                    case LegacyConnectionState.CONNECTED:
                                        // Check for messages from client, if present, handle those first
                                        if (ns.DataAvailable)
                                        {
                                            string message = sr.ReadLine()+"";

                                            chatManager.Add(message: message, senderName: username);
                                        }

                                        // When done handling messages from client, send messages to client
                                        lock (subscribeList)
                                        {
                                            foreach (var item in subscribeList)
                                            {
                                                if (item.senderName.Equals(username, StringComparison.InvariantCultureIgnoreCase))
                                                {
                                                    // Ignore messages sent by the user himself
                                                }
                                                else
                                                {
                                                    sw.WriteLine("<"+item.senderName+"> " + item.message);
                                                }
                                            }
                                            subscribeList.Clear();
                                        }
                                        break;
                                    default:
                                        break;
                                }
                            }
                            else
                            {
                                // Normal Mode

                                // Prerun Checks
                                // Check if the Server has client Public Key
                                if (encryptionProvider.RemotePublicKey.Equals(string.Empty))
                                { 
                                    // Request Public Key from Client
                                    using (var tempPacket = new Packet(PacketType.PUBLIC_KEY_REQUEST))
                                    {
                                        Send(tempPacket);
                                        logger.WriteDebug("Asking for public Key");
                                    }

                                    // To be sure client has Server's Public key, send it
                                    using (var tempPacket = new Packet(PacketType.PUBLIC_KEY))
                                    {
                                        tempPacket.addData("publicKey", encryptionProvider.LocalPublicKey);
                                        Send(tempPacket);
                                        logger.WriteDebug("Sending public key voluntarily");
                                    }
                                }

                                if (!auth)
                                {
                                    using (var tempPacket = new Packet(PacketType.REQUEST_AUTH))
                                    {
                                        Send(tempPacket);
                                        logger.WriteDebug("Asking for user to authentificate");
                                    }
                                }

                                // If Data is Avalible from Client Process it
                                if (ns.DataAvailable) {
                                    using (Packet packet = Packet.Deserialize(sr.ReadLine() + ""))
                                    {
                                        switch (packet.packetType)
                                        {
                                            case PacketType.PUBLIC_KEY_REQUEST:
                                                using (var tempPacket = new Packet(PacketType.PUBLIC_KEY))
                                                {
                                                    tempPacket.addData("publicKey", encryptionProvider.LocalPublicKey);
                                                    Send(tempPacket);
                                                }
                                                break;

                                            case PacketType.PUBLIC_KEY:
                                                using (var tempPacket = packet)
                                                {
                                                    encryptionProvider.RemotePublicKey = tempPacket.getData("publicKey");
                                                }
                                                break;

                                            case PacketType.AUTH:
                                                using (var tempPacket = packet)
                                                {
                                                    logger.WriteDebug("AUTH RECEIVED");

                                                    Packet result = new Packet(PacketType.AUTH_RESULT);

                                                    encryptionProvider.Decrypt(packet);
                                                    username = packet.getData("username");
                                                    password = packet.getData("password");

                                                    // Username Check
                                                    if (
                                                        // System name spoofing
                                                        username.Contains("SYSTEM", StringComparison.CurrentCultureIgnoreCase) ||
                                                        // Username length Check
                                                        username.Length == 0
                                                       )
                                                    {
                                                        result.addData("result", "false");
                                                        Send(result);
                                                    }
                                                    // Authentification passed
                                                    else
                                                    {
                                                        result.addData("result", "true");
                                                        auth = true;

                                                        Packet chat_welcome = new Packet(PacketType.CHAT_WELCOME);
                                                        chat_welcome.addData("motd", "Welcome to the Chat Room!");
                                                        Send(result);
                                                        Send(chat_welcome);
                                                    }

                                                    
                                                }
                                                break;

                                            case PacketType.GET_MESSAGE_SINCE:
                                                using (var tempPacket = packet)
                                                {
                                                    var timestamp = Convert.ToInt64(packet.getData("timestamp"));

                                                    var messages = chatManager.GetFromTimestamp(timestamp);

                                                    foreach (var item in messages)
                                                    {
                                                        Packet tosend = new Packet(PacketType.MESSAGE);
                                                        tosend.addData("senderName", item.senderName);
                                                        tosend.addData("message", item.message);
                                                        tosend.addData("timestamp", item.unixTimestamp + "");
                                                        tosend = encryptionProvider.Encrypt(tosend);
                                                        Send(tosend);
                                                    }
                                                }
                                                break;

                                            case PacketType.MESSAGE:
                                                using (var tempPacket = packet)
                                                {
                                                    var message = encryptionProvider.Decrypt(tempPacket);

                                                    chatManager.Add(message.getData("senderName"), message.getData("message"), Convert.ToInt64(message.getData("timestamp")));
                                                }
                                                break;

                                        }
                                    }
                                }
                            }
                            break;

                        default:
                            break;


                    }
                }
                catch (IOException e)
                {
                    logger.WriteError("Connection error with client", e);
                    cleanable = true;
                }
                catch (Exception e)
                {
                    logger.WriteError("Error", e);
                }
            }
            else
            {
                logger.WriteDebug("Ending connection with client");
                cleanable = true;
            }
        }
    }
}
