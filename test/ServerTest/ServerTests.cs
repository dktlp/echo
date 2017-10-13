using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Security.Cryptography;
using System.IO;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using Echo.Runtime;
using Echo.Runtime.Engine;
using Echo.Test.ServerTest.Utility;

namespace Echo.Test.ServerTest
{
    [TestClass]
    public class ServerTests
    {
        private static Echo.Runtime.Engine.Server Server = new Echo.Runtime.Engine.Server();
        private const int SERVER_PORT = 8389;
        private const string SERVER_ADDRESS = "127.0.0.1";

        [ClassInitialize()]
        public static void Initialize(TestContext testContext)
        {
            Server.Configure();
            Server.Start();
        }

        [ClassCleanup()]
        public static void Cleanup()
        {
            Server.Stop();
            Server.Dispose();
        }

        [TestMethod]
        public void ConnectTest()
        {
            TcpClient tcpClient = new TcpClient();
            tcpClient.Client.Connect(new IPEndPoint(IPAddress.Parse(SERVER_ADDRESS), SERVER_PORT));

            Assert.IsTrue(tcpClient.Connected);
        }

        [TestMethod]
        public void HandshakeTest()
        {
            TcpClient tcpClient = new TcpClient();
            tcpClient.Client.Connect(new IPEndPoint(IPAddress.Parse(SERVER_ADDRESS), SERVER_PORT));

            Assert.IsTrue(tcpClient.Connected);

            using (NetworkStream tcpStream = tcpClient.GetStream())
            {
                RSAParameters clientPK = CryptographyHelper.GetKeyPair();
                RSAParameters serverPK = new RSAParameters();

                // Client >> HELLO <public-key>
                byte[] outdata = Encoding.UTF8.GetBytes(String.Format("HELLO {0}\r\n", CryptographyHelper.ToBase64(clientPK)));
                tcpStream.Write(outdata, 0, outdata.Length);
                tcpStream.Flush();

                // Server >> OK <public-key>
                TcpHelper.WaitForDataAvailable(tcpStream);
                if (tcpStream.DataAvailable)
                {
                    byte[] indata = new byte[512];
                    int count = tcpStream.Read(indata, 0, indata.Length);

                    Assert.IsTrue(count > 0);

                    string action = Encoding.UTF8.GetString(indata).Substring(0, 2);
                    string base64 = Encoding.UTF8.GetString(indata).Substring(3, (count - (action.Length + 3)));

                    Assert.AreEqual<string>("OK", action);
                    Assert.IsNotNull(base64);

                    serverPK = CryptographyHelper.FromBase64(base64);

                    Assert.IsNotNull(serverPK);
                    Assert.IsNotNull(serverPK.Exponent);
                    Assert.IsNotNull(serverPK.Modulus);
                    Assert.IsTrue(serverPK.Exponent.Length == 3);
                    Assert.IsTrue(serverPK.Modulus.Length == 256);
                }

                byte[] clientCK = Encoding.UTF8.GetBytes(Guid.NewGuid().ToString("N"));

                RSA rsa = CryptographyHelper.CreateAsymmetricCryptographyProvider();
                rsa.ImportParameters(serverPK);

                byte[] encClientCK = rsa.Encrypt(clientCK, RSAEncryptionPadding.OaepSHA1);

                // Client >> ENCRYPT <crypto-key>
                outdata = Encoding.UTF8.GetBytes(String.Format("ENCRYPT {0}\r\n", Convert.ToBase64String(encClientCK)));
                tcpStream.Write(outdata, 0, outdata.Length);
                tcpStream.Flush();

                // Server >> OK <crypto-key>
                TcpHelper.WaitForDataAvailable(tcpStream);
                if (tcpStream.DataAvailable)
                {
                    byte[] indata = new byte[512];
                    int count = tcpStream.Read(indata, 0, indata.Length);

                    Assert.IsTrue(count > 0);

                    string action = Encoding.UTF8.GetString(indata).Substring(0, 2);
                    string base64 = Encoding.UTF8.GetString(indata).Substring(3, (count - (action.Length + 3)));

                    Assert.AreEqual<string>("OK", action);
                    Assert.IsNotNull(base64);

                    byte[] encServerCK = Convert.FromBase64String(base64);
                    
                    Assert.IsNotNull(encServerCK);
                    Assert.IsNotNull(encServerCK.Length == 256);

                    rsa = CryptographyHelper.CreateAsymmetricCryptographyProvider();
                    rsa.ImportParameters(clientPK);

                    byte[] serverCK = rsa.Decrypt(encServerCK, RSAEncryptionPadding.OaepSHA1);

                    Assert.IsNotNull(serverCK);
                    Assert.IsNotNull(serverCK.Length == 32);
                }

                Assert.IsTrue(tcpClient.Connected);
            }            

            if (tcpClient.Connected)
                tcpClient.Dispose();
        }
    }
}