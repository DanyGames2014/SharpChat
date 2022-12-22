using ChatThreadTest.Networking;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace ChatThreadTest.Cryptography
{
    internal class EncryptionProvider
    {
        RSACryptoServiceProvider cryptoServiceProvider;

        ClientHandler clientHandler;

        string ServerPrivateKey;
        public string ServerPublicKey;
        public string ClientPublicKey;

        public EncryptionMethod encryptionMethod;

        public EncryptionProvider(EncryptionMethod encryptionMethod, ClientHandler clientHandler)
        {
            this.encryptionMethod = encryptionMethod;

            this.clientHandler = clientHandler;

            cryptoServiceProvider = new RSACryptoServiceProvider(2048);
            ServerPrivateKey = RSA.GetKeyString(cryptoServiceProvider.ExportParameters(true));
            ServerPublicKey = RSA.GetKeyString(cryptoServiceProvider.ExportParameters(false));
        }

        public string Encrypt(Packet packet)
        {
            switch(encryptionMethod)
            {
                case EncryptionMethod.NONE:
                    return "<"+packet.sender+"> " + packet.message;

                case EncryptionMethod.RSA:
                    string messageRSA = packet.message;
                    packet.message = RSA.EncryptRSA(messageRSA, ClientPublicKey);
                    return packet.Serialize();

                case EncryptionMethod.VERNAM:
                    string messageVER = packet.message;
                    var verMessage = Vernam.VernamCipher(messageVER);
                    packet.message = verMessage.Value;
                    packet.key = verMessage.Key;
                    return packet.Serialize();

                case EncryptionMethod.VERNAM_RSA:
                    string messageVERRSA = packet.message;
                    var verrsaMessage = Vernam.VernamCipher(messageVERRSA);
                    packet.message = verrsaMessage.Value;
                    packet.key = RSA.EncryptRSA(verrsaMessage.Key, ClientPublicKey);
                    return packet.Serialize();
            }

            return "";
        }

        public Packet Decrypt(string toDecrypt)
        {
            switch (encryptionMethod)
            {
                case EncryptionMethod.NONE:
                    Packet Packet_NONE = new Packet(PacketType.MESSAGE, clientHandler.username, toDecrypt, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
                    return Packet_NONE;

                case EncryptionMethod.RSA:
                    Packet Packet_RSA = JsonSerializer.Deserialize<Packet>(toDecrypt);
                    string Packet_RSA_Message = RSA.DecryptRSA(Packet_RSA.message, ServerPrivateKey);
                    Packet_RSA.sender = Packet_RSA_Message;
                    return Packet_RSA;

                case EncryptionMethod.VERNAM:
                    Packet Packet_VERNAM = JsonSerializer.Deserialize<Packet>(toDecrypt);
                    string Packet_VERNAM_Message = Vernam.VernamDecipher(new KeyValuePair<string, string>(Packet_VERNAM.key,Packet_VERNAM.message));
                    Packet_VERNAM.message = Packet_VERNAM_Message;
                    return Packet_VERNAM;

                case EncryptionMethod.VERNAM_RSA:
                    Packet Packet_VERNAM_RSA = JsonSerializer.Deserialize<Packet>(toDecrypt);
                    string Packet_VERNAM_RSA_Key = RSA.DecryptRSA(Packet_VERNAM_RSA.key,ServerPrivateKey);
                    string Packet_VERNAM_RSA_Message = Vernam.VernamDecipher(new KeyValuePair<string, string>(Packet_VERNAM_RSA_Key,Packet_VERNAM_RSA.message));
                    Packet_VERNAM_RSA.message = Packet_VERNAM_RSA_Message;
                    return Packet_VERNAM_RSA;
            }

            throw new ArgumentOutOfRangeException();
        }
    }
}
