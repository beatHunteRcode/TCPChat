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

namespace TCPServer
{
    public class Server
    {
        readonly string ip;
        readonly int port;
        const int listenersNumb = 10;
        private static readonly List<Socket> listenersList = new List<Socket>();
        private static readonly List<String> nicknamesList = new List<string>();

        public Socket TCPSocket, clientSocket;
        byte[] _buffer;
        public Server(string ip, int port)
        {
            this.ip = ip;
            this.port = port;
        }

        public void Run()
        {
            Console.WriteLine("Launching server...");
            try
            {
                IPEndPoint TCPEndPoint = new IPEndPoint(IPAddress.Parse(ip), port);

                TCPSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                TCPSocket.Bind(TCPEndPoint);
                Console.WriteLine("Server has successfully launched");
                TCPSocket.Listen(listenersNumb);
                Console.WriteLine("Server is running");
                while (true)
                {
                    TCPSocket.BeginAccept(new AsyncCallback(AcceptCallBack), null);
                    //listenersList.Add(clientSocket);
                    //CreateUserThread(clientSocket);
                }
            }
            finally
            {
                Console.ReadKey();
            }
        }

        private void AcceptCallBack(IAsyncResult ar)
        {
            try
            {
                clientSocket = TCPSocket.EndAccept(ar);
                _buffer = new byte[clientSocket.ReceiveBufferSize];
                clientSocket.BeginReceive(_buffer, 0, _buffer.Length, SocketFlags.None, new AsyncCallback(ReceiveCallback), null);
            }
            catch(Exception e)
            {
                Console.WriteLine(e.StackTrace);
            }
        }

        private void ReceiveCallback(IAsyncResult ar)
        {
            Message message = ToMessage(_buffer, _buffer.Length);
            HandleMessage(clientSocket, message, ref _buffer);
        }

        private Message ReceiveMessage(Socket socket, byte[] buffer)
        {
            int bufferSize = socket.Receive(buffer);
            return ToMessage(buffer, bufferSize);
        }

        private void PrintMessage(Message message)
        {
            switch (message.Type)
            {
                case MessageType.TEXT:
                    Console.WriteLine($"[{message.Time}] {message.UserName}: {message.Text}");
                    break;
                case MessageType.FILE:
                    Console.WriteLine($"[{message.Time}] {message.UserName} has just uploaded a file: {message.AttachedFileName}");
                    break;
                default:
                    break;
            }
        }

        public bool IsNicknameValid(string nickname)
        {
            return !nicknamesList.Contains(nickname);
        }

        private void BroadcastNewUserMessage(List<Socket> listenersList, Socket currentListener, string newUsername)
        {
            foreach (Socket listener in listenersList)
            {
                if (!listener.Equals(currentListener))
                {
                    Message message = new Message(MessageType.NEW_USER_CONNECTED, GetCurrentTime(), newUsername, null, null, null);
                    SendMessage(listener, message);
                }
            }
        }

        private void BroadcastMessage(Socket currentListener, Message message)
        {
            foreach (Socket listener in listenersList)
            {
                if (!listener.Equals(currentListener)) SendMessage(listener, message);
            }
        }

        public void DisconnectUser(Socket listener, string username)
        {
            BroadcastMessage(listener, new Message(MessageType.USER_DISCONNECTED, GetCurrentTime(), username, null, null, null));
            Console.WriteLine($"[{GetCurrentTime()}] {username} has been disconnected");
            nicknamesList.Remove(username);
            listenersList.Remove(listener);
            listener.Shutdown(SocketShutdown.Both);
        }

        private void CreateUserThread(Socket listener)
        {
            Thread userThread = new Thread(() =>
            {
                string userName = null;
                try
                {
                    byte[] buffer = new byte[Resourсes.TEMP_BUFFER_SIZE];
                    do
                    {
                        if (buffer.Length < Resourсes.TEMP_BUFFER_SIZE)
                        {
                            buffer = new byte[Resourсes.TEMP_BUFFER_SIZE];
                        }
                        Message message = ReceiveMessage(listener, buffer);
                        userName = message.UserName;
                        HandleMessage(listener, message, ref buffer);
                    } while (listener.Connected);
                }
                catch
                {
                    DisconnectUser(listener, userName);
                }
            });
            userThread.Start();
        }
        private string GetCurrentTime()
        {
            return DateTime.Now.ToString("HH:mm");
        }

        private void SendMessage(Socket listener, Message message)
        {
            if (message.Type == MessageType.FILE)
            {
                string filePath = message.Text.Split(' ')[1];
                byte[] sendingFile = File.ReadAllBytes(filePath);
                message.AttachedFileName = filePath;
                message.AttachedFileData = sendingFile;
            }
            string jsonString = JsonConvert.SerializeObject(message);
            byte[] outcomingMain = Encoding.UTF8.GetBytes(jsonString);

            //Message messageWithLength = new Message(MessageType.LENGTH, null, message.UserName, outcomingMain.Length.ToString(), null, null);
            //string jsonLength = JsonConvert.SerializeObject(messageWithLength);
            //byte[] outcomingLength = Encoding.UTF8.GetBytes(jsonLength);

            listener.BeginSend(outcomingMain, 0, outcomingMain.Length, SocketFlags.None, new AsyncCallback(SendCallback), null);

            //Message acceptingLengthMessage = ReceiveMessage(listener);
            //HandleMessage(acceptingLengthMessage);

            //listener.Send(outcomingMain);


        }

        private void SendCallback(IAsyncResult ar)
        {
            try
            {
                TCPSocket.EndSend(ar);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.StackTrace);
            }
        }

        private void SendMessageWithoutLength(Socket listener, Message message)
        {
            string jsonString = JsonConvert.SerializeObject(message);
            byte[] outcomingMessage = Encoding.UTF8.GetBytes(jsonString);
            listener.Send(outcomingMessage);
        }

        private Message ToMessage(byte[] buffer, int bufferSize)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(Encoding.UTF8.GetString(buffer, 0, bufferSize));
            Message message = JsonConvert.DeserializeObject<Message>(sb.ToString());
            return message;
        }

        private void HandleMessage(Socket listener, Message message, ref byte[] buffer)
        {
            try
            {
                switch (message.Type)
                {
                    case MessageType.CONNECTION_FAILED:
                        break;
                    case MessageType.DISCONNECT:
                        DisconnectUser(listener, message.UserName);
                        break;
                    case MessageType.LENGTH:
                        //SendMessage(listener, new Message(MessageType.OK, null, Resourсes.SERVER_NAME, null, null, null));
                        buffer = new byte[Convert.ToInt32(message.Text)];
                        break;
                    case MessageType.NEW_USER_CONNECTED:
                        if (!IsNicknameValid(message.UserName))
                        {
                            SendMessageWithoutLength(listener, new Message(MessageType.WRONG_NICKNAME, null, null, null, null, null));
                            listenersList.Remove(listener);
                            listener.Shutdown(SocketShutdown.Both);
                        }
                        else
                        {
                            nicknamesList.Add(message.UserName);
                            Console.WriteLine($"[{GetCurrentTime()}] {message.UserName} has been connected.");
                            SendMessageWithoutLength(listener, new Message(MessageType.WELCOME, GetCurrentTime(), Resourсes.SERVER_NAME, null, null, null));
                            BroadcastNewUserMessage(listenersList, listener, message.UserName);
                        }
                        break;
                    case MessageType.OK:
                        //continue
                        break;
                    case MessageType.TEXT:
                        message.Time = GetCurrentTime();
                        PrintMessage(message);
                        BroadcastMessage(listener, message);
                        break;
                    case MessageType.FILE:
                        message.Time = GetCurrentTime();
                        PrintMessage(message);
                        File.WriteAllBytes(message.AttachedFileName, message.AttachedFileData);
                        BroadcastMessage(listener, message);
                        break;
                    case MessageType.USER_DISCONNECTED:
                        break;
                    case MessageType.WELCOME:
                        break;
                    case MessageType.WRONG_NICKNAME:
                        break;
                    case MessageType.MESSAGE_NOT_SENT:
                        break;
                    default:
                        break;
                }
            }
            catch (NullReferenceException)
            {
                Console.WriteLine($"[{GetCurrentTime()}] {message.UserName} has been aborted connection.");
            }
        }
    }
}
