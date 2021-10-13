using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace TCPClient
{
    class Client
    {
        static void Main(string[] args)
        {
            try
            {
                const string ip = "127.0.0.1";
                const int port = 0451;

                Console.Write("Who are you?: ");
                User clientUser = new User(Console.ReadLine());
                User serverUser = new User();
                IPEndPoint TCPEndPoint = new IPEndPoint(IPAddress.Parse(ip), port);

                Socket TCPSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                Console.WriteLine("Connecting to the TCPChat...");
                do
                {
                    try
                    {

                        TCPSocket.Connect(TCPEndPoint);
                    }
                    catch (SocketException)
                    {
                        Console.WriteLine("No adress to connect. Reconnecting...");
                    }
                }
                while (!TCPSocket.Connected);

                Console.WriteLine("Connected.");
                Console.WriteLine($"Hi, {clientUser.Name}. Welcome to the TCPChat! Type 'exit()' to quit the TCPChat.");
                Console.WriteLine("");
                var data = new byte[256];
                string json = JsonConvert.SerializeObject(clientUser);
                data = Encoding.UTF8.GetBytes(json);
                TCPSocket.Send(data);

                Thread receiveThread = new Thread(() =>
                {
                    byte[] buffer = new byte[256];
                    int bufferSize = 0;
                    StringBuilder sb = new StringBuilder();
                    while (true)
                    {
                        sb.Clear();
                        Array.Clear(buffer, 0, buffer.Length);
                        bufferSize = TCPSocket.Receive(buffer);
                        sb.Append(Encoding.UTF8.GetString(buffer, 0, bufferSize));
                        serverUser = JsonConvert.DeserializeObject<User>(sb.ToString());
                        Console.WriteLine($"[{getCurrentTime()}] {serverUser.Name}: {serverUser.Message}");
                    }
                });
                receiveThread.Start();

                do
                {
                    clientUser.Message = Console.ReadLine();
                    json = JsonConvert.SerializeObject(clientUser);
                    data = Encoding.UTF8.GetBytes(json);
                    TCPSocket.Send(data);
                }
                while (clientUser.Message != "exit()");
                Console.WriteLine("Disconnected.");
                TCPSocket.Shutdown(SocketShutdown.Both);
                TCPSocket.Close();

            }
            finally
            {
                Console.ReadKey();
            }
        }

        private static string getCurrentTime()
        {
            return DateTime.Now.ToString("HH:mm");
        }

    }
}
