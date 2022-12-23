using System.Text.Json;

namespace SharpChatServer.Networking
{
    public class Packet : IDisposable
    {
        public PacketType packetType { get; set; }
        public Dictionary<string, string> data { get; set; }

        public Packet(PacketType packetType)
        {
            this.packetType = packetType;
            data = new Dictionary<string, string>();
        }

        public void addData(string key, string value, bool replace = false)
        {
            if (data.ContainsKey(key))
            {
                if (replace)
                {
                    data[key] = value;
                }
            }
            else
            {
                data.Add(key: key, value: value);
            }
        }

        public string getData(string key)
        {
            return data[key: key];
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
                return new Packet(PacketType.UNKNOWN);
            }
        }

        public void Dispose()
        {

        }
    }
}