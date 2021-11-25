using Newtonsoft.Json;
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.IO;

namespace TCPClient
{

    // State object for receiving data from remote device.  
    public class StateObject
    {
        // Client socket.  
        public Socket workSocket = null;
        // Size of receive buffer.  
        public const int BufferSize = Resourсes.TEMP_BUFFER_SIZE;
        // Receive buffer.  
        public byte[] buffer = new byte[BufferSize];
        // Received data string.  
        public StringBuilder sb = new StringBuilder();
    }
    public class Client
    {

        // ManualResetEvent instances signal completion.  
        private static ManualResetEvent connectDone =
            new ManualResetEvent(false);
        private static ManualResetEvent sendDone =
            new ManualResetEvent(false);
        private static ManualResetEvent receiveDone =
            new ManualResetEvent(false);

        static Socket TCPSocket;
        IPEndPoint TCPEndPoint;
        //byte[] _buffer;
        public Client(string ip, int port)
        {
            TCPEndPoint = new IPEndPoint(IPAddress.Parse(ip), port);
        }

        public void Run()
        {
            try
            {
                EnterToServer();
                
            }
            catch(Exception e)
            {
                Console.WriteLine(e.Message);
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

            //SockAsyncEventArgs.SetBuffer(outcomingMain, 0, outcomingMain.Length);
            //listener.SendAsync(SockAsyncEventArgs);

            listener.BeginSend(outcomingMain, 0, outcomingMain.Length, 0, new AsyncCallback(SendCallback), listener);

            //Message acceptingLengthMessage = ReceiveMessage(listener);
            //HandleMessage(acceptingLengthMessage);



        }

        private void SendCallback(IAsyncResult ar)
        {
            try
            {
                Socket client = (Socket) ar.AsyncState; 
                int bytesSent = client.EndSend(ar);
                sendDone.Set();
            }
            catch(Exception e)
            {
                Console.WriteLine(e.Message);
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
                            ReceiveMessage(TCPSocket);
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
            try
            {
                StateObject state = (StateObject)ar.AsyncState;
                Socket handler = state.workSocket;

                int bytesRead = handler.EndReceive(ar);

                if (bytesRead > 0)
                {
                    Message message = ToMessage(state.buffer, state.buffer.Length);
                    HandleMessage(TCPSocket, message, state.buffer);
                    receiveDone.Set();
                }

            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        private void ReceiveMessage(Socket socket)
        {
            StateObject state = new StateObject();
            state.workSocket = socket;
            socket.BeginReceive(state.buffer, 0, state.buffer.Length, SocketFlags.None, new AsyncCallback(ReceiveCallback), state);
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
            do
            {
                try
                {
                    if (Resourсes.USERNAME == null)
                    {
                        Console.Write("Who are you?: ");
                        Resourсes.USERNAME = Console.ReadLine();
                    }
                    TCPSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                    TCPSocket.BeginConnect(TCPEndPoint, new AsyncCallback(ConnectCallback), TCPSocket);
                    connectDone.WaitOne();
                    //HandleMessage(TCPSocket, m, ref buffer);
                }
                catch (SocketException)
                {
                    Console.WriteLine(Resourсes.NO_ADRESS_PHRASE);
                }
                catch(Exception e)
                {
                    Console.WriteLine(e.Message);
                    Console.WriteLine(e.StackTrace);
                }
            } while (!TCPSocket.Connected);
        }

        private void ConnectCallback(IAsyncResult ar)
        {
            try
            {
                // Retrieve the socket from the state object.  
                Socket client = (Socket)ar.AsyncState;
                // Complete the connection.  
                client.EndConnect(ar);
                //connectDone.Set();
                Console.WriteLine(Resourсes.CONNECTING_PHRASE);
                SendMessage(TCPSocket, new Message(MessageType.NEW_USER_CONNECTED, null, Resourсes.USERNAME, null, null, null));
                sendDone.WaitOne();
                ReceiveMessage(TCPSocket);
                receiveDone.WaitOne();
                CreateThreads();
            }
            catch(Exception e)
            {
                Console.WriteLine(e.Message);
                Console.WriteLine(e.StackTrace);
            }
        }

        private void HandleMessage(Socket listener, Message message, byte[] buffer)
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
