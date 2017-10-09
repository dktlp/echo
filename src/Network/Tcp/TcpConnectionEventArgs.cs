using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Echo.Network.Tcp
{
    public class TcpConnectionEventArgs : EventArgs
    {
        public TcpConnection AcceptedConnection { get; private set; }

        public TcpConnectionEventArgs(TcpConnection acceptedConnection)
        {
            AcceptedConnection = acceptedConnection;
        }
    }
}