using System;
using System.Collections.Generic;
using System.Text;

using log4net;

using Echo.Network;
using Echo.Network.Tcp;
using Echo.Runtime.Engine.Protocol.Actions;

namespace Echo.Runtime.Engine.Protocol
{
    public static class ProtocolActionFactory
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(TcpConnection));

        public static IProtocolAction Create(TcpData tcpData)
        {
            string data = Encoding.UTF8.GetString(tcpData.Data);
            int p = data.IndexOf(' ');
            if (p != -1)
                data = data.Substring(0, p).TrimEnd().ToUpper();

            switch(data)
            {
                case "HELLO":
                    {
                        return new HelloProtocolAction();
                    }
                case "ENCRYPT":
                    {
                        return new EncryptProtocolAction();
                    }
                default:
                    {
                        throw new Exception("Invalid protocol action received from client");
                    }
            }
        }
    }
}