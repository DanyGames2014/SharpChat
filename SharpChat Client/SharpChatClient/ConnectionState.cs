using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharpChatClient
{
    public enum ConnectionState
    {
        UNKNOWN = 0,
        HANDSHAKE = 1,
        CONNECTED = 2
    }
}
