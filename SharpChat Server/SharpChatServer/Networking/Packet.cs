using System.Text.Json;

namespace ChatThreadTest.Networking
{
    public class Packet : IDisposable
    {
        public PacketType packetType { get; set; }
        public string message { get; set; }
        public string sender { get; set; }
        public long unixTimestamp { get; set; }
        public string key { get; set; }

        public Packet(PacketType packetType, string sender, string message, long unixTimestamp, string key="")
        {
            this.packetType = packetType;
            this.message = message;
            this.sender = sender;
            this.unixTimestamp = unixTimestamp;
            this.key = key;
        }

        public string Serialize()
        {
            try
            {
                return JsonSerializer.Serialize(value: this);
            }
            catch (Exception)
            {
                throw;
            }
        }

        public static Packet Deserialize(string json)
        {
            try
            {
                var des = JsonSerializer.Deserialize<Packet>(json: json);

                if (des != null)
                {
                    return des;
                }
                else
                {
                    throw new InvalidDataException();
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        public void Dispose()
        {

        }
    }
}