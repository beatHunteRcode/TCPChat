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

namespace TCPChat
{
    class Server
    {       
        const string ip = "127.0.0.1";
        const int port = 0451;
        const int listenersNumb = 10;
        private static List<Socket> listenersList = new List<Socket>();

        static void Main(string[] args)
        {
            try
            {
                IPEndPoint TCPEndPoint = new IPEndPoint(IPAddress.Parse(ip), port);

                Socket TCPSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                TCPSocket.Bind(TCPEndPoint);

                TCPSocket.Listen(listenersNumb);
                while (true)
                {
                    var listener = TCPSocket.Accept();
                    listenersList.Add(listener);
                    Thread thread = new Thread(() =>
                    {
                        byte[] buffer = new byte[256];
                        int bufferSize = 0;
                        StringBuilder data = new StringBuilder();
                        User user = new User();
                        welcomeMessage(listener, buffer, bufferSize, data, ref user);
                        broadcastNewUserMessage(listenersList, listener, user);
                        while (listener.Connected)
                        {
                            try
                            {
                                do
                                {
                                    data.Clear();
                                    Array.Clear(buffer, 0, buffer.Length);
                                    bufferSize = listener.Receive(buffer);
                                    data.Append(Encoding.UTF8.GetString(buffer, 0, bufferSize));
                                    user = JsonConvert.DeserializeObject<User>(data.ToString());
                                    Console.WriteLine($"[{getCurrentTime()}] {user.Name}: {user.Message}");

                                    broadcastMessage(listener, buffer);
                                }
                                while (listener.Available > 0);
                            }
                            catch
                            {
                                disconnectMessage(listener, user);
                            }

                        }
                    });
                    thread.Start();
                }
            }
            finally
            {
                Console.ReadKey();
            }
        }

        public static void welcomeMessage(Socket listener, byte[] buffer, int bufferSize, StringBuilder data, ref User user)
        {
            data.Clear();
            bufferSize = listener.Receive(buffer);
            data.Append(Encoding.UTF8.GetString(buffer, 0, bufferSize));
            user = JsonConvert.DeserializeObject<User>(data.ToString());
            Console.WriteLine($"[{getCurrentTime()}] {user.Name} has been connected.");
        }

        private static void broadcastNewUserMessage(List<Socket> listenersList, Socket currentListener, User user)
        {
            foreach (Socket listener in listenersList)
            {
                if (!listener.Equals(currentListener))
                {
                    user.Message = "has been connected.";
                    string json = JsonConvert.SerializeObject(user);
                    var data = Encoding.UTF8.GetBytes(json);
                    listener.Send(data);
                }
            }
        }

        private static void broadcastMessage(Socket currentListener, byte[] data)
        {
            foreach (Socket listener in listenersList)
            {
                if (!listener.Equals(currentListener)) listener.Send(data);
            }
        }

        public static void disconnectMessage(Socket listener, User user)
        {
            Console.WriteLine($"{user.Name} has been disconnected.");
            listenersList.Remove(listener);
        }

        private static string getCurrentTime()
        {
            return DateTime.Now.ToString("HH:mm");
        }
    }
}
