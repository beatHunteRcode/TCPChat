using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TCPServer
{
    public enum MessageType : short
    {
        CONNECTION_FAILED = 0,
        WRONG_NICKNAME = 1,
        DISCONNECT = 2,
        MESSAGE_NOT_SENT = 3,

        WELCOME = 10,
        TEXT = 11,
        FILE = 12,

        OK = 20,

        NEW_USER_CONNECTED = 21,
        USER_DISCONNECTED = 22,
        LENGTH = 23,
    }
}
