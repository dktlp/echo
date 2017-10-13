using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Net;
using System.IO;

using log4net;

namespace Echo.Network.Tcp
{
    public class TcpConnection
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(TcpConnection));

        private Socket Socket { get; set; }
                
        public EndPoint RemoteEndpoint
        {
            get
            {
                return Socket.RemoteEndPoint;
            }
        }

        public bool IsConnected
        {
            get
            {
                return Socket.Connected;
            }
        }

        public TcpConnection(Socket socket)
        {
            Socket = socket;
        }

        public void Send(byte[] data)
        {
            if (data == null)
                return;

            int count = Socket.Send(data);
            Log.DebugFormat("TCP data packet [{0} byte(s)] sent to '{1}'", count, Socket.RemoteEndPoint);
        }

        public void Close()
        {
            try
            {
                Log.Info(String.Format("Closing connection with client '{0}'", Socket.RemoteEndPoint));

                if (Socket.Connected)
                    Socket.Shutdown(SocketShutdown.Both);

                Socket?.Dispose();
            }
            catch (ObjectDisposedException)
            {
                // Exception intentionally suppressed.
            }
        }
    }
}