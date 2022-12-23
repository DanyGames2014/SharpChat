using ChatThreadTest.Cryptography;
using ChatThreadTest.Networking;
using System.Diagnostics;
using System.Net.Sockets;

namespace ChatThreadTest
{
    internal class ClientHandler
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
        EncryptionMethod encryptionMethod = EncryptionMethod.NONE;
        Stopwatch timeoutStopwatch;

        // Encryption Provider
        EncryptionProvider encryption;

        // Chat
        ChatManager chatManager;
        long timestampOfLastMessageSentToClient;
        List<ChatMessage> subscribeList;

        // User Data
        public string username;
        string password;

        public ClientHandler(TcpClient tcpClient, Server server)
        {
            this.server = server;
            this.logger = server.logger;
            this.client = tcpClient;

            ns = client.GetStream();
            sw = new(ns) { AutoFlush= true };
            sr = new(ns);

            state = ConnectionState.HANDSHAKE_ENC_SEND;

            encryption = new(encryptionMethod, this);

            username = string.Empty;
            password = string.Empty;

            timeoutStopwatch = new Stopwatch();

            cleanable = false;

            chatManager = server.chatManager;

            subscribeList= new List<ChatMessage>();

            lock (server.chatManager.subscribedThreads)
            {
                server.chatManager.subscribedThreads.Add(subscribeList);
            }
        }

        public string SysMsg(string message)
        {
            Packet packet = new Packet(PacketType.MESSAGE, "SYSTEM", message, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
            return encryption.Encrypt(packet);
        }

        public Packet GetMsg()
        {
            return encryption.Decrypt(sr.ReadLine());
        }

        public void Run()
        {
            if (timeoutStopwatch.ElapsedMilliseconds < 60000)
            {
                try
                {
                    switch (state)
                    {
                        // Send the list of supported encryptions
                        case ConnectionState.HANDSHAKE_ENC_SEND:
                            sw.WriteLine("Please choose encryption method" +
                                "\r\n(0) NONE       - No encryption or encoding, only use if connecting through a dumb terminal." +
                                "\r\n(1) RSA        - RSA Encrypted Communication, not suitable for dumb terminals" +
                                "\r\n(2) VERNAM     - Messages Encrypted with Vernam Cipher, Key Distribution is done using unsafe method" +
                                "\r\n(3) VERNAM_RSA - Messages Encrypted with Vernam Cipher, Key is encrypted through RSA");

                            state = ConnectionState.HANDSHAKE_ENC_RECV;
                            timeoutStopwatch.Start();
                            break;
                        
                        // Receive the chosen encryption
                        case ConnectionState.HANDSHAKE_ENC_RECV:
                            if (ns.DataAvailable)
                            {
                                try
                                {
                                    encryptionMethod = Enum.Parse<EncryptionMethod>(sr.ReadLine() + "");
                                    state = ConnectionState.HANDSHAKE_ENC_SUCC;
                                }
                                catch (Exception)
                                {
                                    sw.WriteLine("Invalid Value!");
                                }

                                timeoutStopwatch.Restart();
                            }
                            break;
                        
                        // Encryption chosen
                        case ConnectionState.HANDSHAKE_ENC_SUCC:
                            timeoutStopwatch.Stop();
                            timeoutStopwatch.Reset();

                            encryption.encryptionMethod = encryptionMethod;
                            sw.WriteLine("Using chosen Encryption : " + encryptionMethod.ToString());

                            switch (encryptionMethod)
                            {
                                case EncryptionMethod.NONE:
                                    state = ConnectionState.LOGIN_ASK_USERNAME;
                                    break;
                                case EncryptionMethod.RSA:
                                    state = ConnectionState.HANDSHAKE_KEY_SEND;
                                    break;
                                case EncryptionMethod.VERNAM:
                                    state = ConnectionState.LOGIN_ASK_USERNAME;
                                    break;
                                case EncryptionMethod.VERNAM_RSA:
                                    state = ConnectionState.HANDSHAKE_KEY_SEND;
                                    break;
                                default:
                                    client.Close();
                                    cleanable = true;
                                    break;
                            }
                            break;

                        // If Chosen Encryption Method requires public key sharing, reuquest clients Public Key and Send the Server Public Key
                        case ConnectionState.HANDSHAKE_KEY_SEND:
                            Packet hskspacket = new Packet(PacketType.REQUEST_CLIENT_PUBLIC_KEY, "SYSTEM", encryption.ServerPublicKey, 0);
                            sw.WriteLine(hskspacket.Serialize());
                            timeoutStopwatch.Start();
                            state = ConnectionState.HANDSHAKE_KEY_RECV;
                            break;

                        // After Sending a request, Receive Client's Public Key
                        case ConnectionState.HANDSHAKE_KEY_RECV:
                            if (ns.DataAvailable)
                            {
                                timeoutStopwatch.Restart();
                                Packet hskr = GetMsg();

                                if(hskr != null)
                                {
                                    if(hskr.packetType == PacketType.RECEIVE_CLIENT_PUBLIC_KEY){
                                        encryption.ClientPublicKey = hskr.message;
                                        timeoutStopwatch.Stop();
                                    }
                                }
                            }
                            break;
                        
                        // Ask for Username
                        case ConnectionState.LOGIN_ASK_USERNAME:
                            if(encryptionMethod == EncryptionMethod.NONE)
                            {
                                sw.WriteLine("WARNING! Due to the nature of your chosen encryption method all information isn't transferred securely");
                                sw.WriteLine("Please Enter your Username : ");
                                state = ConnectionState.LOGIN_REC_USERNAME;
                                timeoutStopwatch.Start();
                            }
                            else
                            {
                                sw.WriteLine(new Packet(PacketType.ASK_USERNAME, "", "", 0).Serialize());
                                state = ConnectionState.LOGIN_REC_USERNAME;
                            }
                            break;

                        // Receive Username
                        case ConnectionState.LOGIN_REC_USERNAME:
                            
                            if (ns.DataAvailable)
                            {
                                if (encryptionMethod == EncryptionMethod.NONE)
                                {
                                    timeoutStopwatch.Restart();
                                    username = sr.ReadLine() + "";
                                    state = ConnectionState.LOGIN_ASK_PASSWORD;
                                    timeoutStopwatch.Restart();
                                }
                                else
                                {
                                    Packet lgrPacket = GetMsg();
                                    if(lgrPacket != null)
                                    {
                                        if (lgrPacket.packetType == PacketType.CLIENT_USERNAME)
                                        {
                                            username = lgrPacket.message;
                                        }
                                    }
                                }
                                
                            }
                            break;
                        
                        // Ask for Password
                        case ConnectionState.LOGIN_ASK_PASSWORD:
                            if (encryptionMethod == EncryptionMethod.NONE)
                            {
                                sw.WriteLine("Please Enter your Password : ");
                                state = ConnectionState.LOGIN_REC_PASSWORD;
                                timeoutStopwatch.Restart();
                            }
                            else
                            {
                                sw.WriteLine(new Packet(PacketType.ASK_PASSWORD, "", "", 0).Serialize());
                                state = ConnectionState.LOGIN_REC_PASSWORD;
                            }
                            break;

                        // Receive Password
                        case ConnectionState.LOGIN_REC_PASSWORD:
                            if (ns.DataAvailable)
                            {
                                if (encryptionMethod == EncryptionMethod.NONE)
                                {
                                    timeoutStopwatch.Restart();
                                    password = sr.ReadLine() + "";
                                    state = ConnectionState.AUTH_USER;
                                    timeoutStopwatch.Stop();

                                }
                                else
                                {
                                    Packet lgpPacket = GetMsg();
                                    if (lgpPacket != null)
                                    {
                                        if (lgpPacket.packetType == PacketType.CLIENT_PASSWORD)
                                        {
                                            password = lgpPacket.message;
                                        }
                                    }
                                }

                            }
                            break;

                        // Verify user login details, Currently not Implemented
                        // Currently only filters for empty names and spoofing of the sysem name
                        case ConnectionState.AUTH_USER:
                            if (username.Contains("SYSTEM", StringComparison.CurrentCultureIgnoreCase))
                            {
                                sw.WriteLine(new Packet(PacketType.AUTH_RESULT, "", "Auth Fail", 0));
                                state = ConnectionState.LOGIN_ASK_USERNAME;
                            }
                            else
                            {
                                if(username.Length == 0)
                                {
                                    sw.WriteLine(new Packet(PacketType.AUTH_RESULT, "", "Auth Fail", 0));
                                    state = ConnectionState.LOGIN_ASK_USERNAME;
                                }
                                else
                                {
                                    sw.WriteLine(new Packet(PacketType.AUTH_RESULT, "", "Auth OK", 0));
                                    state = ConnectionState.CONNECT_TO_CHAT;
                                }
                            }
                            break;

                        // Welcome the user to chat room and in case of smart clients send all the message that happened before joining
                        case ConnectionState.CONNECT_TO_CHAT:
                            if( encryptionMethod == EncryptionMethod.NONE )
                            {
                                sw.WriteLine(SysMsg("Welcome to the Chat Room!"));
                                sw.WriteLine(SysMsg("Chat History isn't supported on your client"));
                                state = ConnectionState.CONNECTED;
                                chatManager.Add("SYSTEM", username + " has connected to the chatroom");
                            }
                            else
                            {
                                sw.WriteLine(new Packet(PacketType.CHAT_WELCOME,"", "Welcome to the Chat Room!",0));
                                state = ConnectionState.CONNECTED;
                            }
                            break;


                        // Client is now connected to the Chat Room
                        case ConnectionState.CONNECTED:
                            // Check for messages from client, if present, handle those first
                            if (ns.DataAvailable)
                            {
                                Packet message = GetMsg();
                                switch (message.packetType)
                                {
                                    case PacketType.MESSAGE:
                                        chatManager.Add(message.sender, message.message);
                                        break;

                                    case PacketType.GETSINCE:
                                        List<ChatMessage> msgs = chatManager.GetFromTimestamp(timestamp: Convert.ToInt64(message.message));
                                        foreach (var item in msgs)
                                        {
                                            encryption.Encrypt(new Packet(packetType: PacketType.MESSAGE, item.senderName, item.message, item.unixTimestamp));
                                        }
                                        break;

                                    default:
                                        break;
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
                                        Packet packet = new Packet(PacketType.MESSAGE, item.senderName, item.message, item.unixTimestamp);
                                        sw.WriteLine(encryption.Encrypt(packet));
                                    }
                                }
                                subscribeList.Clear();
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
                catch( Exception e)
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
