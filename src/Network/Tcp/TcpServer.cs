using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.IO;

using log4net;

using Echo;
using Echo.Network;

namespace Echo.Network.Tcp
{
    public class TcpServer
    {
        private const int SOCKET_BACKLOG = 100;

        private static readonly ILog Log = LogManager.GetLogger(typeof(TcpServer));

        private Socket Socket { get; set; }

        public IPEndPoint LocalEndpoint { get; private set; }

        public event EventHandler<TcpConnectionEventArgs> ConnectionAccepted;
        public event EventHandler<TcpDataEventArgs> DataReceived;

        public TcpServer(IPEndPoint localEndpoint)
        {
            LocalEndpoint = localEndpoint;            
        }

        public void Start()
        {
            Socket = new Socket(LocalEndpoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            Socket.Bind(LocalEndpoint);
            Socket.Listen(SOCKET_BACKLOG);

            Task.Factory.StartNew(Listen, TaskCreationOptions.LongRunning);
        }

        public void Stop()
        {
            if (Socket.Connected)
                Socket.Shutdown(SocketShutdown.Both);

            Socket?.Dispose();
        }

        private async Task Listen()
        {
            Log.InfoFormat("Listening for incoming TCP/IP connections on port '{0}'", LocalEndpoint.Port);

            SocketAsyncEventArgs e = new SocketAsyncEventArgs();
            SocketAwaitable awaitable = new SocketAwaitable(e);

            while (true)
            {
                Log.Debug("Ready to accept new connection");

                await Socket.AcceptAsync(awaitable);
                if (e.AcceptSocket != null)
                {
                    Log.InfoFormat("Connection accepted from '{0}'", e.AcceptSocket.RemoteEndPoint);
                    ConnectionAccepted?.Invoke(this, new TcpConnectionEventArgs(new TcpConnection(e.AcceptSocket)));

                    Task.Factory.StartNew(Receive, e.AcceptSocket);
                }
            }
        }

        private async Task Receive(object state)
        {
            Socket socket = state as Socket;
            if (socket == null)
                return;

            SocketData socketData = new SocketData() { State = SocketFrameState.ReceiveData };
            SocketAsyncEventArgs e = new SocketAsyncEventArgs();
            SocketAwaitable awaitable = new SocketAwaitable(e);

            e.SetBuffer(new byte[0x1000], 0, 0x1000);

            while (true)
            {
                Log.InfoFormat("Ready to to receive data from connection '{0}'", socket.RemoteEndPoint);

                await socket.ReceiveAsync(awaitable);
                bool endOfTransmission = (e.BytesTransferred <= 0);

                Log.DebugFormat("Data [{0} byte(s)] received from from '{1}'", e.BytesTransferred, socket.RemoteEndPoint);

                for (int i = 0; (i < e.BytesTransferred && !endOfTransmission); i++)
                {
                    switch (socketData.State)
                    {                        
                        case SocketFrameState.ReceiveData:
                            {
                                socketData.Data.WriteByte(e.Buffer[i]);

                                // Check if transmission of data is complete.
                                if (e.Buffer[i] == (byte)SocketControlCharacters.CR || e.Buffer[i] == (byte)SocketControlCharacters.LF)
                                {
                                    byte[] delimiters = new byte[] { (byte)SocketControlCharacters.CR, (byte)SocketControlCharacters.LF };
                                    byte[] bytes = new byte[delimiters.Length];
                                    long pos = socketData.Data.Position;

                                    socketData.Data.Seek(-1 * delimiters.Length, SeekOrigin.Current);
                                    socketData.Data.Read(bytes, 0, bytes.Length);
                                    socketData.Data.Position = pos;

                                    if (delimiters.Length == bytes.Length)
                                    {
                                        bool dataReceived = true;
                                        for (int j = 0; j < delimiters.Length; j++)
                                        {
                                            if (delimiters[j] != bytes[j])
                                                dataReceived = false;
                                        }

                                        if (dataReceived)
                                            socketData.State = SocketFrameState.DataReceived;
                                    }

                                    if (e.Buffer[i] == (byte)SocketControlCharacters.EOT || socketData.State == SocketFrameState.DataReceived)
                                        endOfTransmission = true;
                                }

                                break;
                            }
                    }
                }

                if (endOfTransmission)
                {
                    Log.InfoFormat("End of transmission [{0} byte(s)] received from '{1}'", socketData.Length, socket.RemoteEndPoint);
                    DataReceived?.Invoke(this, new TcpDataEventArgs(new TcpData(socketData.Data.ToArray(), socket.RemoteEndPoint)));

                    socketData.Reset();
                }
            }
        }

    }
}