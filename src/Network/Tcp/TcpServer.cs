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
        //public event EventHandler<HttpRequestEventArgs> RequestReceived;

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
            Log.Info("Listening for incoming TCP/IP connections");

            SocketAsyncEventArgs e = new SocketAsyncEventArgs();
            SocketAwaitable awaitable = new SocketAwaitable(e);

            while (true)
            {
                Log.Debug("Ready to accept new connection");

                await Socket.AcceptAsync(awaitable);
                if (e.AcceptSocket != null)
                {
                    Log.InfoFormat("Connection accepted from '{0}'", e.AcceptSocket.RemoteEndPoint);

                    if (ConnectionAccepted != null)
                        ConnectionAccepted(this, new TcpConnectionEventArgs(new TcpConnection(e.AcceptSocket)));
                    
                    Task.Factory.StartNew(Receive, e.AcceptSocket);
                }
            }
        }

        private async Task Receive(object state)
        {
            Socket socket = state as Socket;
            if (socket == null)
                return;

            //SocketData data = new SocketData();
            //SocketAsyncEventArgs e = new SocketAsyncEventArgs();
            //SocketAwaitable awaitable = new SocketAwaitable(e);

            //e.SetBuffer(new byte[0x1000], 0, 0x1000);

            //while (true)
            //{
            //    Log.InfoFormat("Ready to to receive data from connection '{0}'", socket.RemoteEndPoint);

            //    await socket.ReceiveAsync(awaitable);
            //    bool endOfTransmission = (e.BytesTransferred <= 0);

            //    Log.DebugFormat("Data [{0} byte(s)] received from from '{1}'", e.BytesTransferred, socket.RemoteEndPoint);

            //    for (int i = 0; (i < e.BytesTransferred && !endOfTransmission); i++)
            //    {
            //        switch (data.State)
            //        {
            //            case SocketFrameState.ReceiveHeaders:
            //                {
            //                    data.Header.WriteByte(e.Buffer[i]);
            //                    if (e.Buffer[i] == (byte)SocketControlCharacters.EOT)
            //                        endOfTransmission = true;

            //                    // Check if transmission of http-headers is complete.
            //                    if (e.Buffer[i] == (byte)SocketControlCharacters.CR)
            //                    {
            //                        byte[] delimiters = new byte[] { (byte)SocketControlCharacters.CR, (byte)SocketControlCharacters.LF, (byte)SocketControlCharacters.CR };
            //                        byte[] bytes = new byte[delimiters.Length];
            //                        long pos = data.Header.Position;

            //                        data.Header.Seek(-1 * delimiters.Length, SeekOrigin.Current);
            //                        data.Header.Read(bytes, 0, bytes.Length);
            //                        data.Header.Position = pos;

            //                        if (delimiters.Length == bytes.Length)
            //                        {
            //                            bool headersReceived = true;
            //                            for (int j = 0; j < delimiters.Length; j++)
            //                            {
            //                                if (delimiters[j] != bytes[j])
            //                                    headersReceived = false;
            //                            }

            //                            if (headersReceived)
            //                                data.State = SocketFrameState.HeadersReceived;
            //                        }
            //                    }

            //                    // Parse headers to see if there is any body-content to be received.
            //                    if (data.State == SocketFrameState.HeadersReceived)
            //                    {
            //                        Log.DebugFormat("All data headers received from '{0}'", socket.RemoteEndPoint);

            //                        data.Header.Position = 0;
            //                        StreamReader parser = new StreamReader(data.Header);
            //                        string l = parser.ReadLine();
            //                        while (l != null)
            //                        {
            //                            if (l.Contains("Content-Length:"))
            //                            {
            //                                string[] n = l.Split(':');
            //                                if (n.Length == 2)
            //                                    data.ContentLength = long.Parse(n[1].Trim(' '));

            //                                break;
            //                            }

            //                            l = parser.ReadLine();
            //                        }

            //                        if (data.ContentLength == 0)
            //                            endOfTransmission = true;
            //                        else
            //                            data.State = SocketFrameState.ReceiveContent;
            //                    }

            //                    break;
            //                }
            //            case SocketFrameState.ReceiveContent:
            //                {
            //                    data.ContentBytesTransfered++;
            //                    data.Content.WriteByte(e.Buffer[i]);

            //                    if (data.ContentBytesTransfered == data.ContentLength)
            //                        data.State = SocketFrameState.ContentReceived;

            //                    if (e.Buffer[i] == (byte)SocketControlCharacters.EOT || data.State == SocketFrameState.ContentReceived)
            //                        endOfTransmission = true;

            //                    break;
            //                }
            //        }
            //    }

            //    if (endOfTransmission)
            //    {
            //        Log.InfoFormat("End of transmission [{0} byte(s)] received from '{1}'", data.Length, socket.RemoteEndPoint);

            //        if (RequestReceived != null)
            //            RequestReceived(this, new HttpRequestEventArgs(new HttpRequest(data.Header.ToArray(), data.Content.ToArray(), socket.RemoteEndPoint)));

            //        data.Reset();

            //        break;
            //    }
            //}
        }

    }
}