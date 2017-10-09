using System;
using System.IO;

namespace Echo.Network
{
    // TODO: Implement buffer management to avoid heap fragmentation:
    // http://ahuwanya.net/blog/post/Buffer-Pooling-for-NET-Socket-Operations

    public class SocketData
    {
        public MemoryStream Header { get; private set; }
        public MemoryStream Content { get; private set; }
        public SocketFrameState State { get; set; }
        public long ContentLength { get; set; }
        public long ContentBytesTransfered { get; set; }

        public SocketData()
        {
            Reset();
        }

        public long Length
        {
            get
            {
                return Header.Length + Content.Length;
            }
        }

        public void Reset()
        {
            Header?.Dispose();
            Content?.Dispose();

            Header = new MemoryStream();
            Content = new MemoryStream();
            State = SocketFrameState.ReceiveHeaders;
            ContentLength = 0;
            ContentBytesTransfered = 0;
        }
    }
}