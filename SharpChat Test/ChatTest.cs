using SharpChatServer.Cryptography;
using SharpChatServer.Networking;
using System.Security.Cryptography;

namespace ChatUnitTest
{
    [TestClass]
    public class ChatTest
    {
        /// <summary>
        /// Tests if a random string ciphered using the Vernam cipher methods,
        /// which is then deciphered using the Vernam decipher method is the
        /// same as input string
        /// </summary>
        [TestMethod ("Vernam Cipher Test")]
        public void VernamCipherAndDecipherReturnsOriginalString()
        {
            string testString = Utilities.randomString(128);

            var cipher = Vernam.VernamCipher(testString);

            Assert.IsNotNull(cipher);

            var decipher = Vernam.VernamDecipher(cipher);

            Assert.IsNotNull(decipher);

            Assert.AreEqual(testString, decipher);
        }

        /// <summary>
        /// Tests if a random key of various sizes that is generated is not
        /// null and is the correct size
        /// </summary>
        [TestMethod ("Random Key Generation Test")]
        public void RandomKeyIsCorrectLength()
        {
            var key = Vernam.generateKey(2048);
            Assert.IsNotNull(key);
            Assert.AreEqual(key.Length, 2048);
        }

        /// <summary>
        /// Tests if a serialized packet that is then serialized contains
        /// the correct data using randomly generated set of test data
        /// </summary>
        [TestMethod ("Packet Serialization and Deserialiazation")]
        public void PacketSerializationAndDeserialization()
        {
            PacketType type = (PacketType)new Random().Next(0, 1);
            string sender = Utilities.randomString(16);
            string message = Utilities.randomString(256);
            long unixTimestamp = 0;

            Packet toSerialize = new Packet(type, sender, message, unixTimestamp);

            string SerializedPacket = toSerialize.Serialize();
            Assert.IsNotNull(SerializedPacket);

            Packet deserialized = Packet.Deserialize(SerializedPacket);
            Assert.IsNotNull(deserialized);

            Assert.AreEqual(deserialized.packetType, type);
            Assert.AreEqual(deserialized.message, message);
            Assert.AreEqual(deserialized.sender, sender);
            Assert.AreEqual(deserialized.unixTimestamp, unixTimestamp);

        }

        [TestMethod ("RSA Encryption and Decryption")]
        public void RsaEncryptionAndDecryption()
        {
            RSACryptoServiceProvider cryptoServiceProvider = new RSACryptoServiceProvider(2048);
            string PrivateKey = SharpChatServer.Cryptography.RSA.GetKeyString(cryptoServiceProvider.ExportParameters(true));
            string PublicKey = SharpChatServer.Cryptography.RSA.GetKeyString(cryptoServiceProvider.ExportParameters(false));

            string testMessage = "A testing message to test the functionality of RSA Encryption and Decryption";

            string encrypted = SharpChatServer.Cryptography.RSA.EncryptRSA(testMessage,PublicKey);

            string decrypted = SharpChatServer.Cryptography.RSA.DecryptRSA(encrypted, PrivateKey);

            Assert.AreEqual(testMessage,decrypted);
        }
    }
}