using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace SharpChatClient
{
    public class Client
    {
        // TCP Networking
        TcpClient tcpClient;
        NetworkStream ns;
        StreamReader sr;
        StreamWriter sw;

        // State
        ConnectionState state = ConnectionState.HANDSHAKE_ENC_CHOOSE;
        bool run = true;

        // User Details
        public string username = "pablo";
        string password = "pablo";

        // Cryptography
        EncryptionProvider encryptionProvider;
        EncryptionMethod encryptionMethod;

        public Client()
        {
            encryptionMethod = EncryptionMethod.RSA;

            encryptionProvider = new(encryptionMethod, this);

            state = ConnectionState.HANDSHAKE_ENC_CHOOSE;
        }

        public void Send(Packet packet)
        {
            sw.WriteLine(encryptionProvider.Encrypt(packet));
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
                        case ConnectionState.HANDSHAKE_ENC_CHOOSE:
                            sw.WriteLine((int)encryptionMethod);
                            state = ConnectionState.HANDSHAKE_KEY_EXCHANGE;
                            break;

                        case ConnectionState.HANDSHAKE_KEY_EXCHANGE:
                            using (var packet = new Packet(PacketType.RECEIVE_CLIENT_PUBLIC_KEY, "", encryptionProvider.ClientPublicKey, 0))
                            {
                                Send(packet);
                            }
                            state = ConnectionState.LOGIN_SEND_USERNAME;
                            break;

                        case ConnectionState.LOGIN_SEND_USERNAME:
                            using (var packet = new Packet(PacketType.CLIENT_USERNAME,"",username, 0))
                            {
                                Send(packet);
                            }
                            state = ConnectionState.LOGIN_SEND_PASSWORD;
                            break;

                        case ConnectionState.LOGIN_SEND_PASSWORD:
                            using (var packet = new Packet(PacketType.CLIENT_PASSWORD, "", password, 0))
                            {
                                Send(packet);
                            }
                            state = ConnectionState.GET_AUTH_RESULT;
                            break;

                        case ConnectionState.GET_AUTH_RESULT:
                            state = ConnectionState.GET_CHAT_HISTORY;
                            break;

                        case ConnectionState.GET_CHAT_HISTORY:
                            state = ConnectionState.CONNECTED;
                            break;

                        case ConnectionState.CONNECTED:
                            using (var packet = new Packet(PacketType.MESSAGE, username, "TestMessage", 0))
                            {
                                Send(packet);
                            }
                            break;
                    }
                }
                catch (IOException ex)
                {
                    while (!tcpClient.Connected)
                    {
                        Console.WriteLine("Connection Fail, Retrying");
                        state = ConnectionState.HANDSHAKE_ENC_CHOOSE;
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
                tcpClient.Connect("127.0.0.1", 65525);
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
