using System;

using Echo.Network;
using Echo.Network.Tcp;

namespace Echo.Runtime.Engine.Protocol
{
    public interface IProtocolAction
    {
        TcpData Execute(TcpData tcpData);
    }
}