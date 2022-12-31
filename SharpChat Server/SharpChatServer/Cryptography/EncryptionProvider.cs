using SharpChatServer.Networking;
using System.Security.Cryptography;
using SharpChatServer.Utilities;

namespace SharpChatServer.Cryptography
{
    public class EncryptionProvider
    {
        RSACryptoServiceProvider cryptoServiceProvider;

        ClientHandler client;

        public string LocalPrivateKey;
        public string LocalPublicKey;
        public string RemotePublicKey;

        public EncryptionProvider(ClientHandler client)
        {
            this.client = client;

            cryptoServiceProvider = new RSACryptoServiceProvider(2048);
            LocalPrivateKey = RSA.GetKeyString(cryptoServiceProvider.ExportParameters(true));
            LocalPublicKey = RSA.GetKeyString(cryptoServiceProvider.ExportParameters(false));
            RemotePublicKey = string.Empty;
        }

        public Packet Encrypt(Packet packet)
        {
            switch (packet.packetType)
            {
                case PacketType.UNKNOWN:
                    goto default;

                case PacketType.PUBLIC_KEY_REQUEST:
                    goto default;

                case PacketType.PUBLIC_KEY:
                    goto default;

                case PacketType.REQUEST_AUTH:
                    goto default;

                case PacketType.AUTH:
                    switch (client.encryptionMethod)
                    {
                        case EncryptionMethod.NONE:
                            return packet;

                        case EncryptionMethod.RSA:
                            packet.addData("username", RSA.EncryptRSA(packet.getData("username"), RemotePublicKey), true);
                            packet.addData("password", RSA.EncryptRSA(packet.getData("password"), RemotePublicKey), true);
                            return packet;

                        case EncryptionMethod.VERNAM:
                            using (var temp = packet)
                            {
                                string username = temp.getData("username");
                                string password = temp.getData("password");
                                KeyValuePair<string, string> username_cipher = Vernam.VernamCipher(username);
                                KeyValuePair<string, string> password_cipher = Vernam.VernamCipher(password);
                                packet.addData("username", Base64.Encode(username_cipher.Value), true);
                                packet.addData("username_key", Base64.Encode(username_cipher.Key), true);
                                packet.addData("password", Base64.Encode(password_cipher.Value), true);
                                packet.addData("password_key", Base64.Encode(password_cipher.Key), true);
                                return packet;
                            }

                        case EncryptionMethod.VERNAM_RSA:
                            using (var temp = packet)
                            {
                                string username = temp.getData("username");
                                string password = temp.getData("password");
                                KeyValuePair<string, string> username_cipher = Vernam.VernamCipher(username);
                                KeyValuePair<string, string> password_cipher = Vernam.VernamCipher(password);
                                packet.addData("username", Base64.Encode(username_cipher.Value), true);
                                packet.addData("username_key", RSA.EncryptRSA(username_cipher.Key, RemotePublicKey), true);
                                packet.addData("password", Base64.Encode(password_cipher.Value), true);
                                packet.addData("password_key", RSA.EncryptRSA(password_cipher.Key, RemotePublicKey), true);
                                return packet;
                            }
                    }
                    break;

                case PacketType.AUTH_RESULT:
                    goto default;

                case PacketType.CHAT_WELCOME:
                    goto default;

                case PacketType.GET_MESSAGE_SINCE:
                    goto default;

                case PacketType.MESSAGE:
                    switch (client.encryptionMethod)
                    {
                        case EncryptionMethod.NONE:
                            return packet;

                        case EncryptionMethod.RSA:
                            packet.addData("message", RSA.EncryptRSA(packet.getData("message"), RemotePublicKey), true);
                            return packet;

                        case EncryptionMethod.VERNAM:
                            using (var temp = packet)
                            {
                                string message = temp.getData("message");
                                KeyValuePair<string, string> message_cipher = Vernam.VernamCipher(message);
                                packet.addData("message", Base64.Encode(message_cipher.Value), true);
                                packet.addData("message_key", Base64.Encode(message_cipher.Key), true);
                                return packet;
                            }

                        case EncryptionMethod.VERNAM_RSA:
                            using (var temp = packet)
                            {
                                string message = temp.getData("message");
                                KeyValuePair<string, string> message_cipher = Vernam.VernamCipher(message);
                                packet.addData("message", Base64.Encode(message_cipher.Value), true);
                                packet.addData("message_key", RSA.EncryptRSA(message_cipher.Key, RemotePublicKey), true);
                                return packet;
                            }
                    }
                    break;

                case PacketType.COMMAND:
                    switch (client.encryptionMethod)
                    {
                        case EncryptionMethod.NONE:
                            return packet;

                        case EncryptionMethod.RSA:
                            packet.addData("command", RSA.EncryptRSA(packet.getData("command"), RemotePublicKey), true);
                            return packet;

                        case EncryptionMethod.VERNAM:
                            using (var temp = packet)
                            {
                                string command = temp.getData("command");
                                KeyValuePair<string, string> command_cipher = Vernam.VernamCipher(command);
                                packet.addData("command", Base64.Encode(command_cipher.Value), true);
                                packet.addData("command_key", Base64.Encode(command_cipher.Key), true);
                                return packet;
                            }

                        case EncryptionMethod.VERNAM_RSA:
                            using (var temp = packet)
                            {
                                string command = temp.getData("command");
                                KeyValuePair<string, string> command_cipher = Vernam.VernamCipher(command);
                                packet.addData("command", Base64.Encode(command_cipher.Value), true);
                                packet.addData("command_key", RSA.EncryptRSA(command_cipher.Key, RemotePublicKey), true);
                                return packet;
                            }
                    }
                    break;

                default:
                    return packet;
            }

            return packet;
        }

        public Packet Decrypt(Packet packet)
        {
            switch (packet.packetType)
            {
                case PacketType.UNKNOWN:
                    goto default;

                case PacketType.PUBLIC_KEY_REQUEST:
                    goto default;

                case PacketType.PUBLIC_KEY:
                    goto default;

                case PacketType.REQUEST_AUTH:
                    goto default;

                case PacketType.AUTH:
                    switch (client.encryptionMethod)
                    {
                        case EncryptionMethod.NONE:
                            return packet;

                        case EncryptionMethod.RSA:
                            packet.addData("username", RSA.DecryptRSA(packet.getData("username"), LocalPrivateKey), true);
                            packet.addData("password", RSA.DecryptRSA(packet.getData("password"), LocalPrivateKey), true);
                            return packet;

                        case EncryptionMethod.VERNAM:
                            using (var temp = packet)
                            {
                                packet.addData("username", Vernam.VernamDecipher(new KeyValuePair<string, string>(Base64.Decode(packet.getData("username_key")), Base64.Decode(packet.getData("username")))), true);
                                packet.addData("password", Vernam.VernamDecipher(new KeyValuePair<string, string>(Base64.Decode(packet.getData("password_key")), Base64.Decode(packet.getData("password")))), true);
                                return packet;
                            }

                        case EncryptionMethod.VERNAM_RSA:
                            using (var temp = packet)
                            {
                                packet.addData("username", Vernam.VernamDecipher(new KeyValuePair<string, string>(RSA.DecryptRSA(packet.getData("username_key"), LocalPrivateKey), Base64.Decode(packet.getData("username")))), true);
                                packet.addData("password", Vernam.VernamDecipher(new KeyValuePair<string, string>(RSA.DecryptRSA(packet.getData("password_key"), LocalPrivateKey), Base64.Decode(packet.getData("password")))), true);
                                return packet;
                            }
                    }
                    break;

                case PacketType.AUTH_RESULT:
                    goto default;

                case PacketType.CHAT_WELCOME:
                    goto default;

                case PacketType.GET_MESSAGE_SINCE:
                    goto default;

                case PacketType.MESSAGE:
                    switch (client.encryptionMethod)
                    {
                        case EncryptionMethod.NONE:
                            return packet;

                        case EncryptionMethod.RSA:
                            packet.addData("message", RSA.DecryptRSA(packet.getData("message"), LocalPrivateKey), true);
                            return packet;

                        case EncryptionMethod.VERNAM:
                            using (var temp = packet)
                            {
                                packet.addData("message", Vernam.VernamDecipher(new KeyValuePair<string, string>(Base64.Decode(packet.getData("message_key")), Base64.Decode(packet.getData("message")))), true);
                                return packet;
                            }

                        case EncryptionMethod.VERNAM_RSA:
                            using (var temp = packet)
                            {
                                packet.addData("message", Vernam.VernamDecipher(new KeyValuePair<string, string>(RSA.DecryptRSA(packet.getData("message_key"), LocalPrivateKey), Base64.Decode(packet.getData("message")))), true);
                                return packet;
                            }
                    }
                    break;

                case PacketType.COMMAND:
                    switch (client.encryptionMethod)
                    {
                        case EncryptionMethod.NONE:
                            return packet;

                        case EncryptionMethod.RSA:
                            packet.addData("command", RSA.DecryptRSA(packet.getData("command"), LocalPrivateKey), true);
                            return packet;

                        case EncryptionMethod.VERNAM:
                            using (var temp = packet)
                            {
                                packet.addData("command", Vernam.VernamDecipher(new KeyValuePair<string, string>(Base64.Decode(packet.getData("command_key")), Base64.Decode(packet.getData("command")))), true);
                                return packet;
                            }

                        case EncryptionMethod.VERNAM_RSA:
                            using (var temp = packet)
                            {
                                packet.addData("command", Vernam.VernamDecipher(new KeyValuePair<string, string>(RSA.DecryptRSA(packet.getData("command_key"), LocalPrivateKey), Base64.Decode(packet.getData("command")))), true);
                                return packet;
                            }
                    }
                    break;

                default:
                    return packet;
            }

            return packet;
        }
    }
}
