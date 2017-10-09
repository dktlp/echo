using System;

namespace Echo.Runtime.Engine
{
    public enum ServerState : byte
    {
        Unknown = 0x0,
        Started = 0x1,
        Stopped = 0x2
    }
}