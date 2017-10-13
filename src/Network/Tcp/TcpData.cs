using System;
using System.Collections.Generic;
using System.Net;

namespace Echo.Network.Tcp
{
    public class TcpData
    {
        public long Id { get; private set; }
        public byte[] Data { get; private set; }
        public EndPoint RemoteEndpoint { get; private set; }
        public TcpDataDirection Direction { get; private set; }

        public TcpData(byte[] data, EndPoint remoteEndpoint)
            : this (data, remoteEndpoint, TcpDataDirection.Inbound)
        {
        }
            public TcpData(byte[] data, EndPoint remoteEndpoint, TcpDataDirection direction)
        {
            Id = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            Data = data;
            RemoteEndpoint = remoteEndpoint;
            Direction = direction;
        }
    }
}