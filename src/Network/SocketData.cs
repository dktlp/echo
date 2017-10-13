using System;
using System.IO;

namespace Echo.Network
{
    // TODO: Implement buffer management to avoid heap fragmentation:
    // http://ahuwanya.net/blog/post/Buffer-Pooling-for-NET-Socket-Operations

    public class SocketData
    {
        public MemoryStream Header { get; private set; }
        public MemoryStream Data { get; private set; }
        public SocketFrameState State { get; set; }

        public SocketData()
        {
            Reset();
        }

        public long Length
        {
            get
            {
                return Header.Length + Data.Length;
            }
        }

        public void Reset()
        {
            Reset(SocketFrameState.ReceiveData);
        }

        public void Reset(SocketFrameState frameState)
        {
            Header?.Dispose();
            Data?.Dispose();

            Header = new MemoryStream();
            Data = new MemoryStream();
            State = frameState;
        }
    }
}