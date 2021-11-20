using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TCPServer
{
    class TCPServer
    {
        const string ip = "127.0.0.1";
        const int port = 0451;

        public static void Main(string[] args) {
            Server server = new Server(ip, port);
            server.Run();
        }
    }
}
