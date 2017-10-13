using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.IO;

using log4net;

using Echo;
using Echo.Network;
using Echo.Network.Tcp;
using Echo.Runtime.Engine.Protocol;

namespace Echo.Runtime.Engine
{
    public class Server : IDisposable
    {
        // TODO: Make relevant constants configurable.

        private const int THREAD_INTERVAL_PROC = 500;
        private const int THREAD_INTERVAL_GARC = 10000;
        private const int SERVER_PORT = 8389;
        private const int HTTP_TRAFFIC_WARNING_THRESHOLD = 1000;

        private static readonly ILog Log = LogManager.GetLogger(typeof(Server));

        public ServerState State { get; private set; }

        private Queue<TcpData> TcpDataQueue { get; set; }
        private List<Thread> Threads { get; set; }
        private List<TcpConnection> Connections { get; set; }
        private TcpServer TcpServer { get; set; }
        private object SyncLock { get; set; }
        private CancellationTokenSource ThreadCancellationToken { get; set; }

        public Server()
        {
            SyncLock = new object();
            State = ServerState.Unknown;
            Threads = new List<Thread>();
            Connections = new List<TcpConnection>();
            TcpDataQueue = new Queue<TcpData>();
            ThreadCancellationToken = new CancellationTokenSource();

            TcpServer = new TcpServer(new IPEndPoint(IPAddress.Any, SERVER_PORT));
            TcpServer.ConnectionAccepted += OnConnectionAccepted;
            TcpServer.DataReceived += OnDataReceived;
        }

        ~Server()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (State == ServerState.Started)
                Stop();

            // Free managed resources
            if (disposing)
            {
                Threads?.Clear();
                Threads = null;

                Connections?.Clear();
                Connections = null;
                   
                TcpDataQueue?.Clear();
                TcpDataQueue = null;

                ThreadCancellationToken?.Dispose();
                ThreadCancellationToken = null;

                TcpServer = null;
                SyncLock = null;
            }
            // Free native resources here if there are any
        }

        public void Configure()
        {
            Threads.Add(new Thread(new ParameterizedThreadStart(ProcessThread))
            {
                Name = "PROC",
                IsBackground = true,
            });
            
            Threads.Add(new Thread(new ParameterizedThreadStart(GarbageCycleThread))
            {
                Name = "GARC",
                IsBackground = true
            });

            // TODO: Read configuration.

            State = ServerState.Stopped;

            Log.Info("Server configuration loaded");
        }

        public void Start()
        {
            if (State == ServerState.Unknown)
                Configure();

            foreach (Thread thread in Threads)
            {
                if (thread.ThreadState.HasFlag((ThreadState.Unstarted)))
                    thread.Start(ThreadCancellationToken.Token);
            }

            TcpServer.Start();
            
            State = ServerState.Started;

            Log.Info("Server started");
        }

        public void Stop()
        {
            if (State != ServerState.Started)
                return;

            foreach (TcpConnection connection in Connections)
            {
                connection.Close();
            }

            foreach (Thread thread in Threads)
            {
                Log.Info(String.Format("Thread '{0}' is being aborted/stopped", thread.Name));
            }

            ThreadCancellationToken.Cancel();

            foreach (Thread thread in Threads)
            {
                if (thread != null && thread.ThreadState == ThreadState.Running)
                    thread.Join();
            }
            
            TcpServer.Stop();

            State = ServerState.Stopped;

            Log.Info("Server stopped");
        }

        public void WaitForExit()
        {
            ThreadCancellationToken.Token.WaitHandle.WaitOne();
        }

        private void OnDataReceived(object sender, TcpDataEventArgs e)
        {
            if (e.TcpData == null || e.TcpData.Data == null || e.TcpData.RemoteEndpoint == null)
                return;
            
            Log.DebugFormat("TCP data packet '{0}' received from '{1}'", e.TcpData.Id, e.TcpData.RemoteEndpoint);

            lock (SyncLock)
            {
                TcpDataQueue.Enqueue(e.TcpData);
                Log.InfoFormat("TCP data packet '{0}' queued for processing", e.TcpData.Id);
            }
        }

        private void OnConnectionAccepted(object sender, TcpConnectionEventArgs e)
        {
            if (e.AcceptedConnection == null)
                return;

            SessionManager.GetInstance().NewSession(e.AcceptedConnection);
            Connections.Add(e.AcceptedConnection);
        }

        private void ProcessThread(object state)
        {
            CancellationToken tct = (CancellationToken)state;

            Log.InfoFormat("Thread '{0}' started", Thread.CurrentThread.Name);

            while (!tct.IsCancellationRequested)
            {
                int queueLength = 0;
                while (queueLength == 0)
                {
                    Thread.Sleep(THREAD_INTERVAL_PROC);
                    lock(SyncLock)
                        queueLength = TcpDataQueue.Count;
                }

                Log.DebugFormat("Process message in queue [length: {0}]", queueLength);
                if (queueLength >= HTTP_TRAFFIC_WARNING_THRESHOLD)
                    Log.WarnFormat("Traffic on this server is high [queue length: {0}]", queueLength);
                
                TcpData tcpData = null;
                lock(SyncLock)
                    tcpData = TcpDataQueue.Dequeue();

                Log.DebugFormat("TCP data packet '{0}' dequeued and ready to be processed.", tcpData.Id);
                Task.Factory.StartNew(ProcessTcpPacket, tcpData);    
            }

            Log.InfoFormat("Thread '{0}' stopped", Thread.CurrentThread.Name);
        }

        private void ProcessTcpPacket(object state)
        {
            TcpData tcpData = state as TcpData;
            if (tcpData == null)
                return;

            Log.InfoFormat("Process TCP data packet '{0}' from '{1}' in direction '{2}'", tcpData.Id, tcpData.RemoteEndpoint, tcpData.Direction);

            switch (tcpData.Direction)
            {
                // Process inbound data
                case TcpDataDirection.Inbound:
                    {
                        try
                        {
                            IProtocolAction action = ProtocolActionFactory.Create(tcpData);
                            if (action == null)
                                return;

                            // TODO: Consistency check that Action is valid in this context

                            TcpData tcpResponseData = action.Execute(tcpData);
                            if (tcpResponseData != null)
                            {
                                lock(SyncLock)
                                    TcpDataQueue.Enqueue(tcpResponseData);
                            }
                        }
                        catch (Exception e)
                        {
                            Log.ErrorFormat("Error occurred while processing TCP data packet; {0}", e.Message);
                        }
                        finally
                        {

                        }
                        
                        break;
                    }
                // Process outbound data
                case TcpDataDirection.Outbound:
                    {
                        TcpConnection connection = Connections.Find(m => m.RemoteEndpoint == tcpData.RemoteEndpoint);
                        if (connection != null)
                        {
                            try
                            {
                                connection.Send(tcpData.Data);
                            }
                            catch (Exception e)
                            {
                                Log.ErrorFormat("Error occurred while processing response; {0}", e.Message);
                            }
                        }
                        else
                            Log.WarnFormat("Connection for client '{0}' not found", tcpData.RemoteEndpoint);

                        break;
                    }
            }

            Log.InfoFormat("Processing of TCP data packet '{0}' complete", tcpData.Id);            
        }

        private void GarbageCycleThread(object state)
        {
            CancellationToken tct = (CancellationToken)state;

            Log.Info(String.Format("Thread '{0}' started", Thread.CurrentThread.Name));

            while (!tct.IsCancellationRequested)
            {
                Thread.Sleep(THREAD_INTERVAL_GARC);

                Log.Info("Garbage cycle started");

                // TODO: Detect idle connections.

                // Remove all idle or closed connections.
                List<TcpConnection> closedConnections = Connections.FindAll(m => !m.IsConnected);
                foreach (TcpConnection closedConnection in closedConnections)
                {
                    Connections.Remove(closedConnection);

                    List<Session> closedSessions = SessionManager.GetInstance().FindAll(m => m.Connection == closedConnection);
                    foreach (Session closedSession in closedSessions)
                    {                        
                        SessionManager.GetInstance().Remove(closedSession);
                        Log.InfoFormat("Session '{0}' was idle or closed by remote peer", closedSession.Id);

                        // TODO: When a connection is removed, then the corresponding user + channel subscription.
                    }
                }

                Log.Info("Garbage cycle completed");
            }

            Log.Info(String.Format("Thread '{0}' stopped", Thread.CurrentThread.Name));
        }
    }
}