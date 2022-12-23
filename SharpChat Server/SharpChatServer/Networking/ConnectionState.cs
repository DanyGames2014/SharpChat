﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharpChatServer.Networking
{
    public enum ConnectionState
    {
        UNKNOWN = 0,
        HANDSHAKE_SEND = 1,
        HANDSHAKE_RECV = 2,
        CONNECTED = 3
    }

    public enum LegacyConnectionState
    {
        LOGIN_ASK_USERNAME = 1,
        LOGIN_REC_USERNAME = 2,
        LOGIN_ASK_PASSWORD = 3,
        LOGIN_REC_PASSWORD = 4,
        LOGIN_AUTH_USER = 5,
        CONNECT_TO_CHAT = 6,
        CONNECTED = 7
    }
}
