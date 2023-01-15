using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharpChatClient
{
    /// <summary>
    /// Represents the current state of the connection
    /// </summary>
    public enum ConnectionState
    {
        UNKNOWN = 0,
        HANDSHAKE = 1,
        CONNECTED = 2
    }
}
