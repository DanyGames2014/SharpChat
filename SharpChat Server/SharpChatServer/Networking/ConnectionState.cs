using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatThreadTest.Networking
{
    internal enum ConnectionState
    {
        UNKNOWN = 0,            // Default Connection State in case something happens
        PREHANDSHAKE_CHECK = 1, // Used to check if the connection should be terminated
        HANDSHAKE_ENC_SEND = 2, // Send the Encryption method question
        HANDSHAKE_ENC_RECV = 3, // Await response to encryption question
        HANDSHAKE_ENC_SUCC = 4, // Encryption Negotiated

        HANDSHAKE_KEY_SEND = 6,
        HANDSHAKE_KEY_RECV = 7,

        LOGIN_ASK_USERNAME = 10,
        LOGIN_REC_USERNAME = 11,
        LOGIN_ASK_PASSWORD = 12,
        LOGIN_REC_PASSWORD = 13,

        AUTH_USER = 14,

        CONNECT_TO_CHAT = 15,
        CONNECTED = 16
    }
}
