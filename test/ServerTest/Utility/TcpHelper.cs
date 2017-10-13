using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;

namespace Echo.Test.ServerTest.Utility
{
    public static class TcpHelper
    {
        public static void WaitForDataAvailable(NetworkStream tcpStream)
        {
            WaitForDataAvailable(tcpStream, 10);
        }

        public static void WaitForDataAvailable(NetworkStream tcpStream, int waitSeconds)
        {
            long start = DateTime.UtcNow.Ticks;
            while (!tcpStream.DataAvailable)
            {
                TimeSpan wait = new TimeSpan(DateTime.UtcNow.Ticks).Subtract(new TimeSpan(start));
                if (wait.Seconds > waitSeconds)
                    throw new TimeoutException("Waiting for data timed out");
            }
        }
    }
}