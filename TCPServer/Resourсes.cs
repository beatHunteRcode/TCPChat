using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TCPServer
{
    class Resourсes
    {
        public const int TEMP_BUFFER_SIZE = 256;
        public const int SLEEP_TIME_MS = 100;

        public const string SERVER_NAME = "Server";

        public const int TYPE_FIELD_SIZE = 2;                           // 2 bytes for Type
        public const int LENGTH_FIELD_SIZE = 4;                         // 4 bytes for Length 
        public const int TIME_FIELD_SIZE = 16;                          // 16 bytes for Time
        public const int USERNAME_FIELD_SIZE = 512;                     // 512 bytes for Username
        public const int TEXT_FIELD_SIZE = 4194304;                     // 4 194 304 bytes = 1 MB for Text
        public const int ATTACHED_FILE_NAME_FIELD_SIZE = 512;           // 512 bytes for Attached File Name
        public const int ATTACHED_FILE_DATA_FIELD_SIZE = 1073741824;    // 1 073 741 824 bytes = 1 GB for Attached File Data (max file size = 1 GB)

    }
}
