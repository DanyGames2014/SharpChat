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
        public EncryptionMethod encryptionMethod;
        public bool auth = false;
        public bool authAsk = false;
        public bool keyAsk = false;
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

        // Buffers
        public List<string> outgoingBuffer;
        public List<string> incomingBuffer;

        public ClientHandler(TcpClient tcpClient, Server server)
        {
            // Assign References to Server and Logger and chat Manager
            this.server = server;
            logger = server.logger;
            chatManager = server.chatManager;

            // TCP Networking
            client = tcpClient;
            ns = client.GetStream();
            sw = new(ns);
            sw.AutoFlush = true;
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

            // Initialize Buffer
            outgoingBuffer = new List<string>();
            incomingBuffer = new List<string>();

            sw.WriteLine("NekoChat/1.0 ("+Environment.OSVersion+")\n");
        }

        public void Send(Packet packet)
        {
            outgoingBuffer.Add(packet.Serialize());
            SendBuffer();
        }

        public void Send(string message)
        {
            outgoingBuffer.Add(message);
            SendBuffer();
        }

        public void SendBuffer()
        {
            foreach (var item in outgoingBuffer)
            {
                sw.WriteLine(item);
                logger.WriteLine("SENT : " + item, LogLevel.NET_DEBUG);
            }
            outgoingBuffer.Clear();
        }

        public void Receive()
        {
            while(ns.DataAvailable)
            {
                string msg = sr.ReadLine()+"";
                logger.WriteLine("RECEIVED : " + msg,  LogLevel.NET_DEBUG);
                if(msg.Length > 0)
                {
                    incomingBuffer.Add(msg);
                }
            }
        }

        public string ReadBuffer(bool destructive = true)
        {
            logger.WriteDebug("Incoming Buffer Size : " + incomingBuffer.Count);
            if (BufferAvalible())
            {
                string msg = incomingBuffer[0];
                if(destructive)
                {
                    incomingBuffer.Remove(msg);
                }
                return msg;
            }
            else
            {
                return "";
            }
        }

        public bool BufferAvalible() => (incomingBuffer.Count > 0);

        public void CleanBuffer()
        {
            for (int i = 0; i < incomingBuffer.Count; i++)
            {
                if (incomingBuffer[i].Length == 0)
                {
                    incomingBuffer.RemoveAt(i);
                    i--;
                }
            }
        }

        public void Run()
        {
            if (timeout.ElapsedMilliseconds < 60000)
            {
                try
                {
                    Receive();
                    switch (state)
                    {
                        case ConnectionState.HANDSHAKE_SEND:
                            sw.WriteLine("Do you wish to connect in a Dumb Terminal mode? The connection will not be secure and some features may not be accesible (yes/no)");
                            logger.WriteDebug("Asking Client for Dumb Terminal Mode");
                            CleanBuffer();
                            state = ConnectionState.HANDSHAKE_RECV;
                            timeout.Restart();
                            break;

                        case ConnectionState.HANDSHAKE_RECV:
                            if (BufferAvalible())
                            {
                                timeout.Stop();
                                var temp = ReadBuffer();
                                if (temp.Length > 0)
                                {
                                    if (temp.Contains("yes", StringComparison.InvariantCultureIgnoreCase))
                                    {
                                        dumbTerminal = true;
                                        logger.WriteDebug("Client connected with Dumb Terminal Mode");
                                        state = ConnectionState.CONNECTED;
                                    }
                                    else if (temp.Contains("no", StringComparison.InvariantCultureIgnoreCase))
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
                                        if (BufferAvalible())
                                        {
                                            timeout.Stop();
                                            username = ReadBuffer();
                                            legacyState = LegacyConnectionState.LOGIN_ASK_PASSWORD;
                                        }
                                        break;

                                    case LegacyConnectionState.LOGIN_ASK_PASSWORD:
                                        sw.WriteLine("Please Enter your Password : ");
                                        legacyState = LegacyConnectionState.LOGIN_REC_PASSWORD;
                                        timeout.Restart();
                                        break;

                                    case LegacyConnectionState.LOGIN_REC_PASSWORD:
                                        if (BufferAvalible())
                                        {
                                            timeout.Stop();
                                            password = ReadBuffer();
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
                                        // Upon succesful authentification, register the client handler to receive messages from Chat Manager
                                        lock (server.chatManager.subscribedThreads)
                                        {
                                            server.chatManager.subscribedThreads.Add(subscribeList);
                                        }

                                        sw.WriteLine("Welcome to the Chat Room!");
                                        sw.WriteLine("Chat History isn't supported on your client");
                                        legacyState = LegacyConnectionState.CONNECTED;
                                        chatManager.Add("SYSTEM", username + " has connected to the chatroom");
                                        break;

                                    case LegacyConnectionState.CONNECTED:
                                        // Check for messages from client, if present, handle those first
                                        if (BufferAvalible())
                                        {
                                            string message = ReadBuffer();

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
                                                    sw.WriteLine("<" + item.senderName + "> " + item.message);
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

                                // If Data is Avalible from Client Process it
                                if (BufferAvalible())
                                {
                                    using (Packet packet = Packet.Deserialize(ReadBuffer()))
                                    {
                                        switch (packet.packetType)
                                        {
                                            case PacketType.PUBLIC_KEY_REQUEST:
                                                logger.WriteDebug("Received Public Key request");
                                                using (var tempPacket = new Packet(PacketType.PUBLIC_KEY))
                                                {
                                                    tempPacket.addData("publicKey", encryptionProvider.LocalPublicKey);
                                                    Send(tempPacket);
                                                    logger.WriteDebug("Sent Public Key");
                                                }
                                                break;

                                            case PacketType.PUBLIC_KEY:
                                                timeout.Stop();

                                                logger.WriteDebug("Received Client Public Key");
                                                using (var tempPacket = packet)
                                                {
                                                    encryptionProvider.RemotePublicKey = tempPacket.getData("publicKey");
                                                    encryptionMethod = EncryptionMethod.VERNAM;
                                                }
                                                break;

                                            case PacketType.AUTH:
                                                using (var tempPacket = encryptionProvider.Decrypt(packet))
                                                {
                                                    timeout.Stop();

                                                    logger.WriteDebug("Received Client Authentification");
                                                    logger.WriteLine("DECRYPTED : " + tempPacket.Serialize(), LogLevel.NET_DEBUG);

                                                    Packet result = new Packet(PacketType.AUTH_RESULT);

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
                                                        authAsk = false;
                                                        Send(result);
                                                    }
                                                    // Authentification passed
                                                    else
                                                    {
                                                        // Upon succesful authentification, register the client handler to receive messages from Chat Manager
                                                        lock (server.chatManager.subscribedThreads)
                                                        {
                                                            server.chatManager.subscribedThreads.Add(subscribeList);
                                                        }

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

                                // Check if the Server has client Public Key
                                if (encryptionProvider.RemotePublicKey.Equals(string.Empty))
                                {
                                    if (keyAsk)
                                    {
                                        return;
                                    }
                                    else
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

                                        keyAsk = true;

                                        timeout.Start();
                                    }
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
                                            Packet message = new(PacketType.MESSAGE);
                                            message.addData("senderName",item.senderName);
                                            message.addData("message", item.message);
                                            message.addData("timestamp", item.unixTimestamp.ToString());
                                            Send(encryptionProvider.Encrypt(message));
                                        }
                                    }
                                    subscribeList.Clear();
                                }

                                if (!auth && !encryptionProvider.RemotePublicKey.Equals(string.Empty))
                                {
                                    if (authAsk)
                                    {
                                        return;
                                    }
                                    else
                                    {
                                        using (var tempPacket = new Packet(PacketType.REQUEST_AUTH))
                                        {
                                            Send(tempPacket);
                                            logger.WriteDebug("Asking for user to authentificate");
                                        }

                                        authAsk = true;

                                        timeout.Start();
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
