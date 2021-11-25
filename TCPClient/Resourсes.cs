using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TCPClient
{
    class Resourсes
    {
        public const string WRONG_NICKNAME_PHRASE = "Current nickname is unavailable";
        public const string FAILED_TO_CONNECT_PHRASE = "Failed to connect";
        public const string DISCONNECT_PHRASE = "Disconnected";
        public const string CONNECTING_PHRASE = "Connecting to the TCPChat...";
        public const string NO_ADRESS_PHRASE = "No adress to connect. Reconnecting...";
        public const string SERVER_ABORTED_CONNECTION_PHRASE = "Server has been aborted connection.";
        public const string ERROR_DISCONNECTION_PHRASE = "You have been disconnected due to error";
        public const string EXIT_PHRASE = "exit()";
        public const string FILE_PHRASE = "!file";

        public static string USERNAME = null;

        public const int TEMP_BUFFER_SIZE = 256;
        public const int SLEEP_TIME_MS = 100;
    }
}
