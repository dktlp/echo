using System;

namespace Echo.Network
{
    public enum SocketFrameState : byte
    {
        ReceiveHeaders = 0x01,        
        HeadersReceived = 0x2,
        ReceiveContent = 0x03,
        ContentReceived = 0x04
    }
}