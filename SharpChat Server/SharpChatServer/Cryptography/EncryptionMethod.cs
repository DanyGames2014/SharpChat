using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharpChatServer.Cryptography
{
    /// <summary>
    /// Enum to list avalible encryption methods
    /// </summary>
    public enum EncryptionMethod
    {
        NONE = 0,
        RSA = 1,
        VERNAM = 2,
        VERNAM_RSA = 3,
    }
}
