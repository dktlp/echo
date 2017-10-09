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

        public TcpConnection(Socket socket)
        {
            Socket = socket;
        }

        public void Send(byte[] data)
        {
            if (data == null)
                return;

            //Log.DebugFormat("Ready to send data to '{0}'", Socket.RemoteEndPoint);

            //// Build response data to be sent
            //byte[] data = null;
            //using (MemoryStream memory = new MemoryStream())
            //{
            //    StreamWriter output = new StreamWriter(memory);

            //    output.WriteLine("HTTP/1.1" + response.Status);
            //    foreach (KeyValuePair<string, string> header in response.Headers)
            //    {
            //        output.WriteLine(header.Key + ": " + header.Value);
            //    }
            //    output.WriteLine("");

            //    memory.Write(response.Content, 0, response.Content.Length);
            //    data = memory.ToArray();

            //    output.Close();
            //}

            //Log.DebugFormat("Data [{0} byte(s)] will be sent to '{1}'", data.Length, Socket.RemoteEndPoint);

            //Socket.Send(data);
            //Log.InfoFormat("End of transmission [{0} byte(s)] sent to '{1}'", data.Length, Socket.RemoteEndPoint);
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