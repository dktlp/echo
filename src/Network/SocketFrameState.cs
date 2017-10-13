using System;

namespace Echo.Network
{
    public enum SocketFrameState : byte
    {
        ReceiveHeaders = 0x01,        
        HeadersReceived = 0x2,
        ReceiveData = 0x03,
        DataReceived = 0x04
    }
}