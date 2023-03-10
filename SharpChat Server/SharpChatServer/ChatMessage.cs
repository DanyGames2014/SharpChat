using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharpChatServer
{
    /// <summary>
    /// Represents a single message in the chat manager
    /// </summary>
    public struct ChatMessage
    {
        public string senderName { get; set; }
        public string message { get; set; }
        public long unixTimestamp { get; set; }

        public ChatMessage(string senderName, string message, long unixTimestamp)
        {
            this.senderName = senderName;
            this.message = message;
            this.unixTimestamp = unixTimestamp;
        }
    }
}
