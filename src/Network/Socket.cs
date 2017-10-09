using System;
using System.Net.Sockets;

namespace Echo.Network
{
    // Usage: https://blogs.msdn.microsoft.com/pfxteam/2011/12/15/awaiting-socket-operations/

    public static class SocketEx
    {
        public static SocketAwaitable ReceiveAsync(this Socket socket, SocketAwaitable awaitable)
        {
            awaitable.Reset();
            if (!socket.ReceiveAsync(awaitable.EventArgs))
                awaitable.IsCompleted = true;

            return awaitable;
        }

        public static SocketAwaitable SendAsync(this Socket socket, SocketAwaitable awaitable)
        {
            awaitable.Reset();
            if (!socket.SendAsync(awaitable.EventArgs))
                awaitable.IsCompleted = true;

            return awaitable;
        }

        public static SocketAwaitable AcceptAsync(this Socket socket, SocketAwaitable awaitable)
        {
            awaitable.Reset();
            if (!socket.AcceptAsync(awaitable.EventArgs))
                awaitable.IsCompleted = true;

            return awaitable;
        }
    }
}