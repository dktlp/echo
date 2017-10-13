using System;
using System.Collections.Generic;

using Echo;
using Echo.Network;
using Echo.Network.Tcp;

namespace Echo.Runtime.Engine
{
    public sealed class Session
    {
        public const string BAG_CLIENT_PUBLICKEY = "ClientPublicKey";
        public const string BAG_SERVER_KEYPAIR = "ServerKeyPair";
        public const string BAG_CRYPTOKEY_CLIENT = "ClientCryptoKey";
        public const string BAG_CRYPTOKEY_SERVER = "ServerCryptoKey";

        // TODO: User, Connection, CryptoKey, etc.

        public uint Id { get; private set; }
        public TcpConnection Connection { get; set; }
        public Dictionary<string, object> Bag { get; private set; }

        public Session(uint id)
        {
            Id = id;
            Connection = null;
            Bag = new Dictionary<string, object>();
        }
    }
}