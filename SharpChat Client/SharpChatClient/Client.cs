using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json;

namespace SharpChatClient
{
    public class Client
    {
        // TCP Networking
        TcpClient tcpClient;
        NetworkStream ns;
        StreamReader sr;
        StreamWriter sw;
        List<Packet> messageBuffer;
        public string serverAddress;
        public int serverPort;

        // State
        ConnectionState state;
        bool run = true;
        bool auth = false;

        // User Details
        public string username;
        public string password;

        // Cryptography
        EncryptionProvider encryptionProvider;
        public EncryptionMethod encryptionMethod;

        // User Input
        Thread userInput;

        public Client()
        {
            encryptionMethod = EncryptionMethod.VERNAM;

            encryptionProvider = new(this);

            state = ConnectionState.HANDSHAKE;

            messageBuffer = new List<Packet>();

            userInput = new Thread(ConsoleHandler);
        }

        public void Send(Packet packet)
        {
            messageBuffer.Add(packet);

            SendBuffer();
        }

        public void SendBuffer()
        {
            if (tcpClient.Connected)
            {
                foreach (var item in messageBuffer)
                {
                    //Console.WriteLine("SENT : " + item.Serialize());
                    sw.WriteLine(item.Serialize());
                    sw.Flush();
                }
                messageBuffer.Clear();
            }
        }

        public void ConsoleHandler()
        {
            while (run)
            {
                string msg = Console.ReadLine()+"";
                Packet packet = new Packet(PacketType.MESSAGE);
                packet.addData("message", msg);
                packet.addData("senderName", username);
                packet.addData("timestamp", DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()+"");
                encryptionProvider.Encrypt(packet);
                Send(packet);
            }
        }

        public void Run()
        {
            Connect(true);
            while (run)
            {
                try
                {
                    switch (state)
                    {
                        case ConnectionState.HANDSHAKE:
                            sw.WriteLine("no");
                            state = ConnectionState.CONNECTED;
                            break;

                        case ConnectionState.CONNECTED:
                            // If Data from server is avalible, handle that first
                            while (ns.DataAvailable)
                            {
                                // Read the message
                                var message = sr.ReadLine();
                                //Console.WriteLine("RECEIVED : " + message);
                                Packet packet;

                                // Try to parse the message as a Serialized Packet
                                try
                                {
                                    packet = Packet.Deserialize(message);

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
                                            encryptionProvider.RemotePublicKey = packet.getData("publicKey");
                                            break;

                                        case PacketType.REQUEST_AUTH:
                                            using (var tempPacket = new Packet(PacketType.AUTH))
                                            {
                                                tempPacket.addData("username", username);
                                                tempPacket.addData("password", password);
                                                encryptionProvider.Encrypt(tempPacket);
                                                Send(tempPacket);
                                            }
                                            break;

                                        case PacketType.AUTH_RESULT:
                                            using (var temp = packet)
                                            {
                                                var result = Convert.ToBoolean(packet.getData("result"));
                                                auth = result;
                                            }
                                            break;

                                        case PacketType.CHAT_WELCOME:
                                            using (var tempPacket = new Packet(PacketType.GET_MESSAGE_SINCE))
                                            {
                                                tempPacket.addData("timestamp", "0");
                                                Console.WriteLine(packet.getData("motd"));
                                                Send(tempPacket);
                                                userInput.Start();
                                            }
                                            break;

                                        case PacketType.MESSAGE:
                                            using (var tempPacket = encryptionProvider.Decrypt(packet))
                                            {
                                                Console.WriteLine("<{0}> {1}", tempPacket.getData("senderName"), tempPacket.getData("message"));
                                            }
                                            break;
                                    }
                                }
                                catch (Exception)
                                {
                                    throw;
                                    // If the parsing fails, it is probably a plain text message meant for dumb terminals, we ignore that.
                                }
                            }
                            break;

                        default:
                            break;
                    }
                    
                }
                catch (IOException ex)
                {
                    while (!tcpClient.Connected)
                    {
                        Console.WriteLine("Connection Fail, Retrying");
                        state = ConnectionState.HANDSHAKE;
                        Thread.Sleep(5000);
                        Connect(false);
                    }
                }
                catch (Exception e)
                {
                    Console.Clear();
                    Console.WriteLine(e.Message);
                    throw;
                }
            }
        }

        public void Connect(bool exitOnFail)
        {
            try
            {
                tcpClient = new();
                tcpClient.Connect(serverAddress, serverPort);
                ns = tcpClient.GetStream();
                sw = new StreamWriter(ns);
                sr = new StreamReader(ns);
                sw.AutoFlush = true;
            }
            catch (InvalidOperationException e)
            {
                tcpClient.Dispose();
                ns.Dispose();
                sw.Dispose();
                sr.Dispose();
                return;
            }

            catch (Exception e)
            {

                Console.Clear();
                Console.Error.WriteLine("Connection Failed!");
                Console.Error.WriteLine(e.Message);

                if (exitOnFail)
                {
                    Console.Error.WriteLine("Exiting Application!");
                    Environment.Exit(1);
                }

                return;
            }
        }
    }
}
