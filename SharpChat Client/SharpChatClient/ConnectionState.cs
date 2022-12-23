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
        HANDSHAKE_ENC_CHOOSE = 1,
        HANDSHAKE_KEY_EXCHANGE = 2,
        LOGIN_SEND_USERNAME = 3,
        LOGIN_SEND_PASSWORD = 4,
        GET_AUTH_RESULT = 5,
        GET_CHAT_HISTORY = 6,
        CONNECTED = 7
    }
}
