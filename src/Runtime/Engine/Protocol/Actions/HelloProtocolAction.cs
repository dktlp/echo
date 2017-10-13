using System;
using System.Text;
using System.Security.Cryptography;

using Echo.Network.Tcp;
using Echo.Runtime.Engine.Cryptography;

namespace Echo.Runtime.Engine.Protocol.Actions
{
    public class HelloProtocolAction : IProtocolAction
    {
        public TcpData Execute(TcpData tcpData)
        {
            if (tcpData == null || tcpData.Data == null || tcpData.RemoteEndpoint == null)
                return null;

            Session session = SessionManager.GetInstance().Find(m => m.Connection.RemoteEndpoint == tcpData.RemoteEndpoint);
            if (session == null)
                throw new SessionException();

            // Client's public key for asymmetric encryption (extracted from data sent by client).
            RSAParameters clientkey = RSAParametersHelper.FromBase64(Encoding.UTF8.GetString(tcpData.Data).Substring(6));
            
            // Temporarily store the client's public key in the session.
            session.Bag.Add(Session.BAG_CLIENT_PUBLICKEY, clientkey);

            // Create server public/private key and temporarily store in session.
            RSAParameters serverkey = RSA.Create().ExportParameters(true);
            session.Bag.Add(Session.BAG_SERVER_KEYPAIR, serverkey);

            // Respond to client: OK <server-public-key>
            byte[] dataout = Encoding.UTF8.GetBytes(String.Format("OK {0}\r\n", RSAParametersHelper.ToBase64(serverkey)));
            return new TcpData(dataout, session.Connection.RemoteEndpoint, TcpDataDirection.Outbound);            
        }
    }
}
