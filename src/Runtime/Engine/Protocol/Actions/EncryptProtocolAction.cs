using System;
using System.Text;
using System.Security.Cryptography;

using Echo.Network.Tcp;
using Echo.Runtime.Engine.Cryptography;

namespace Echo.Runtime.Engine.Protocol.Actions
{
    public class EncryptProtocolAction : IProtocolAction
    {
        public TcpData Execute(TcpData tcpData)
        {
            if (tcpData == null || tcpData.Data == null || tcpData.RemoteEndpoint == null)
                return null;

            Session session = SessionManager.GetInstance().Find(m => m.Connection.RemoteEndpoint == tcpData.RemoteEndpoint);
            if (session == null)
                throw new SessionException();
            
            // Decrypt the client cryptokey using server private asymmetric key.
            byte[] encClientCK = Convert.FromBase64String(Encoding.UTF8.GetString(tcpData.Data).Substring(8));
            
            RSA rsa = RSA.Create();
            rsa.ImportParameters((RSAParameters)session.Bag[Session.BAG_SERVER_KEYPAIR]);

            byte[] clientCK = rsa.Decrypt(encClientCK, RSAEncryptionPadding.OaepSHA1);

            // Store the client cryptokey in session.            
            session.Bag.Add(Session.BAG_CRYPTOKEY_CLIENT, clientCK);

            // Create server cryptokey and store in session.
            byte[] serverCK = Encoding.UTF8.GetBytes(Guid.NewGuid().ToString("N"));
            session.Bag.Add(Session.BAG_CRYPTOKEY_SERVER, serverCK);

            // Encrypt server cryptokey with client's public asymmetric key.
            rsa = RSA.Create();
            rsa.ImportParameters((RSAParameters)session.Bag[Session.BAG_CLIENT_PUBLICKEY]);

            byte[] encServerCK = rsa.Encrypt(serverCK, RSAEncryptionPadding.OaepSHA1);

            // Cleanup session bag.
            session.Bag.Remove(Session.BAG_SERVER_KEYPAIR);
            session.Bag.Remove(Session.BAG_CLIENT_PUBLICKEY);

            // Respond to client: OK <server-cryptokey>
            byte[] dataout = Encoding.UTF8.GetBytes(String.Format("OK {0}\r\n", Convert.ToBase64String(encServerCK)));
            return new TcpData(dataout, session.Connection.RemoteEndpoint, TcpDataDirection.Outbound);
        }
    }
}
