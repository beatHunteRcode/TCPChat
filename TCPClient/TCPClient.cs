using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TCPClient
{
    class TCPClient
    {
        const string ip = "127.0.0.1";
        const int port = 0451;

        public static void Main(string[] args)
        {
            Client client = new Client(ip, port);
            client.Run();
        }
    }
}
