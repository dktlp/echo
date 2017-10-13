using System;
using System.Collections.Generic;

namespace Echo.Network.Tcp
{
    public class TcpDataEventArgs : EventArgs
    {
        public TcpData TcpData { get; private set; }

        public TcpDataEventArgs(TcpData tcpData)
            : base()
        {
            TcpData = tcpData;
        }
    }
}