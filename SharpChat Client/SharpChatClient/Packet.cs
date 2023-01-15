using System.Text.Json;

namespace SharpChatClient
{
    /// <summary>
    /// Represents a packet
    /// </summary>
    public class Packet : IDisposable
    {
        public PacketType packetType { get; set; }
        public Dictionary<string, string> data { get; set; }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="packetType">Type of the packet</param>
        public Packet(PacketType packetType)
        {
            this.packetType = packetType;
            data = new Dictionary<string, string>();
        }

        /// <summary>
        /// Adds data to the packet
        /// </summary>
        /// <param name="key">Key to store the data under</param>
        /// <param name="value">Value to store</param>
        /// <param name="replace">If to replace a value if it already exists, defaults to false</param>
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

        /// <summary>
        /// Gets data from the packet
        /// </summary>
        /// <param name="key">Key to get value from</param>
        /// <returns>The data stored under the key</returns>
        public string getData(string key)
        {
            return data[key: key];
        }

        /// <summary>
        /// Can be called on an instance of a packet to serialize it
        /// </summary>
        /// <returns>Serialised String representation of the packet</returns>
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

        /// <summary>
        /// Constructs a packet from a serialized representation
        /// </summary>
        /// <param name="json">Serialized packet</param>
        /// <returns>Deserialized packet</returns>
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