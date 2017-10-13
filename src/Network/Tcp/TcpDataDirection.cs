using System;

namespace Echo.Network.Tcp
{
    public enum TcpDataDirection : byte
    {
        Inbound = 0x1,
        Outbound = 0x2
    }
}