using System;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Echo.Network
{
    public sealed class SocketAwaitable : INotifyCompletion
    {
        private readonly static Action SENTINEL = () => { };

        internal Action continuation;

        internal SocketAsyncEventArgs EventArgs { get; set; }

        public SocketAwaitable GetAwaiter() { return this; }
        public bool IsCompleted { get; internal set; }

        public SocketAwaitable(SocketAsyncEventArgs e)
        {
            if (e == null)
                throw new ArgumentNullException("e");

            EventArgs = e;
            EventArgs.Completed += delegate
            {
                var prev = continuation ?? Interlocked.CompareExchange(ref continuation, SENTINEL, null);
                if (prev != null)
                    prev();
            };
        }

        internal void Reset()
        {
            EventArgs.AcceptSocket = null;

            // TODO: Might be necessary to reset buffers as well?!

            IsCompleted = false;
            continuation = null;
        }

        public void OnCompleted(Action continuation)
        {
            if (this.continuation == SENTINEL || Interlocked.CompareExchange(ref this.continuation, continuation, null) == SENTINEL)
                Task.Run(continuation);
        }

        public void GetResult()
        {
            if (EventArgs.SocketError != SocketError.Success)
                throw new SocketException((int)EventArgs.SocketError);
        }
    }
}