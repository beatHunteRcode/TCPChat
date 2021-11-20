using Newtonsoft.Json;
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.IO;

namespace TCPClient
{
    public class Client
    {

        static Socket TCPSocket;
        IPEndPoint TCPEndPoint;
        readonly string ip;
        readonly int port;
        byte[] _buffer;
        public Client(string ip, int port)
        {
            this.ip = ip;
            this.port = port;
            TCPEndPoint = new IPEndPoint(IPAddress.Parse(ip), port);
        }

        public void Run()
        {
            try
            {
                EnterToServer();
                CreateThreads();
            }
            catch(Exception e)
            {
                Console.WriteLine(e.StackTrace);
                Console.ReadKey();
            }
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



        }

        private void SendCallback(IAsyncResult ar)
        {
            try
            {
                TCPSocket.EndSend(ar);
            }
            catch(Exception e)
            {
                Console.WriteLine(e.StackTrace);
            }
        }

        private void CreateThreads()
        {
            Thread sendingThread = new Thread(() =>
            {
                try
                {
                    using (TCPSocket)
                    {
                        do
                        {
                            string messageText = Console.ReadLine();
                            if (messageText.Contains(Resourсes.FILE_PHRASE))
                            {
                                SendMessage(TCPSocket, new Message(MessageType.FILE, null, Resourсes.USERNAME, messageText, null, null));
                            }
                            else if (messageText.Contains(Resourсes.EXIT_PHRASE))
                            {
                                SendMessage(TCPSocket, new Message(MessageType.DISCONNECT, null, Resourсes.USERNAME, null, null, null));
                                TCPSocket.Close();
                                Console.WriteLine(Resourсes.DISCONNECT_PHRASE);
                            }
                            else
                            {
                                SendMessage(TCPSocket, new Message(MessageType.TEXT, null, Resourсes.USERNAME, messageText, null, null));
                            }
                            //Message m = ReceiveMessage(TCPSocket);
                            //HandleMessage(m);
                        } while (TCPSocket.Connected);

                    }
                }
                catch (ObjectDisposedException)
                {
                    Console.WriteLine(Resourсes.SERVER_ABORTED_CONNECTION_PHRASE);
                    EnterToServer();
                    CreateThreads();
                }
                catch
                {
                    Console.WriteLine(Resourсes.ERROR_DISCONNECTION_PHRASE);
                }
            });
            sendingThread.Start();

            Thread receivingThread = new Thread(() =>
            {
                try
                {
                    using (TCPSocket)
                    {
                        byte[] buffer = new byte[Resourсes.TEMP_BUFFER_SIZE];
                        while (TCPSocket.Connected)
                        {
                            if (buffer.Length < Resourсes.TEMP_BUFFER_SIZE)
                            {
                                buffer = new byte[Resourсes.TEMP_BUFFER_SIZE];
                            }
                            ReceiveMessage(TCPSocket, buffer);
                            //HandleMessage(TCPSocket, message, ref buffer);
                        }
                    }
                }
                catch (SocketException)
                {

                }
            });
            receivingThread.Start();
        }

        private void ReceiveCallback(IAsyncResult ar)
        {
            Message message = ToMessage(_buffer, _buffer.Length);
            HandleMessage(TCPSocket, message, ref _buffer);
        }

        private void ReceiveMessage(Socket socket, byte[] buffer)
        {
            _buffer = new byte[TCPSocket.ReceiveBufferSize];
            socket.BeginReceive(_buffer, 0, _buffer.Length, SocketFlags.None, new AsyncCallback(ReceiveCallback), null);
        }

        //private Message ReceiveMessage(Socket socket)
        //{
        //    byte[] buffer = new byte[Resourсes.TEMP_BUFFER_SIZE];
        //    int bufferSize = socket.Receive(buffer);
        //    return ToMessage(buffer, bufferSize);
        //}

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
                case MessageType.NEW_USER_CONNECTED:
                    Console.WriteLine($"[{GetCurrentTime()}] {message.UserName} has been connected.");
                    break;
                case MessageType.USER_DISCONNECTED:
                    Console.WriteLine($"[{GetCurrentTime()}] {message.UserName} has been disconnected.");
                    break;
                default:
                    break;
            }


        }

        private string GetCurrentTime()
        {
            return DateTime.Now.ToString("HH:mm");
        }

        private Message ToMessage(byte[] buffer, int bufferSize)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(Encoding.UTF8.GetString(buffer, 0, bufferSize));
            Message message = JsonConvert.DeserializeObject<Message>(sb.ToString());
            return message;
        }

        private void EnterToServer()
        {
            do {
                try
                {
                    if (Resourсes.USERNAME == null)
                    {
                        Console.Write("Who are you?: ");
                        Resourсes.USERNAME = Console.ReadLine();
                    }
                    byte[] buffer = new byte[Resourсes.TEMP_BUFFER_SIZE];
                    TCPSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                    TCPSocket.BeginConnect(TCPEndPoint, new AsyncCallback(ConnectsCallback), null);
                    Console.WriteLine(Resourсes.CONNECTING_PHRASE);
                    SendMessage(TCPSocket, new Message(MessageType.NEW_USER_CONNECTED, null, Resourсes.USERNAME, null, null, null));
                    ReceiveMessage(TCPSocket, buffer);
                    //HandleMessage(TCPSocket, m, ref buffer);
                }
                catch (SocketException)
                {
                    Console.WriteLine(Resourсes.NO_ADRESS_PHRASE);
                }
            } while (!TCPSocket.Connected);
        }

        private void ConnectsCallback(IAsyncResult ar)
        {
            try
            {
                TCPSocket.EndConnect(ar);
            }
            catch
            {

            }
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
                        break;
                    case MessageType.LENGTH:
                        //SendMessage(listener, new Message(MessageType.OK, null, Resourсes.USERNAME, null, null, null));
                        buffer = new byte[Convert.ToInt32(message.Text)];
                        break;
                    case MessageType.NEW_USER_CONNECTED:
                        PrintMessage(message);
                        break;
                    case MessageType.OK:
                        //continue
                        break;
                    case MessageType.TEXT:
                        PrintMessage(message);
                        break;
                    case MessageType.FILE:
                        File.WriteAllBytes(message.AttachedFileName, message.AttachedFileData);
                        PrintMessage(message);
                        break;
                    case MessageType.USER_DISCONNECTED:
                        PrintMessage(message);
                        break;
                    case MessageType.WELCOME:
                        Console.WriteLine("Connected.");
                        Console.WriteLine($"Hi, {Resourсes.USERNAME}. Welcome to the TCPChat! Type 'exit()' to quit the TCPChat.");
                        Console.WriteLine("");
                        break;
                    case MessageType.WRONG_NICKNAME:
                        Console.WriteLine(Resourсes.WRONG_NICKNAME_PHRASE);
                        Resourсes.USERNAME = null;
                        TCPSocket.Close();
                        break;
                    case MessageType.MESSAGE_NOT_SENT:
                        break;
                    default:
                        Console.WriteLine(Resourсes.FAILED_TO_CONNECT_PHRASE);
                        TCPSocket.Close();
                        break;
                }
            }
            catch (NullReferenceException)
            {
                Console.WriteLine(Resourсes.SERVER_ABORTED_CONNECTION_PHRASE);
                EnterToServer();
            }
        }
    }
}
