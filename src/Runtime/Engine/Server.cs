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

        // TODO !!!!! -> Queue<object>
        private Queue<object> Messages { get; set; }
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
            Messages = new Queue<object>();
            ThreadCancellationToken = new CancellationTokenSource();

            TcpServer = new TcpServer(new IPEndPoint(IPAddress.Any, SERVER_PORT));
            TcpServer.ConnectionAccepted += OnConnectionAccepted;
            //TcpServer.RequestReceived += OnRequestReceived;
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
                   
                Messages?.Clear();
                Messages = null;

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

        //private void OnRequestReceived(object sender, TcpRequestEventArgs e)
        //{
        //    if (e.Request == null)
        //        return;

        //    Log.DebugFormat("Request '{0}' received from '{1}'", e.Request.ID, e.Request.RemoteEndpoint);

        //    lock(SyncLock)
        //    {
        //        Messages.Enqueue(e.Request);
        //        Log.InfoFormat("Request '{0}' queued for processing", e.Request.ID);
        //    }
        //}

        private void OnConnectionAccepted(object sender, TcpConnectionEventArgs e)
        {
            if (e.AcceptedConnection != null)
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
                        queueLength = Messages.Count;
                }

                Log.DebugFormat("Process message in queue [length: {0}]", queueLength);
                if (queueLength >= HTTP_TRAFFIC_WARNING_THRESHOLD)
                    Log.WarnFormat("Traffic on this server is high [queue length: {0}]", queueLength);

                // TODO !!!!
                object message = null;
                lock(SyncLock)
                    message = Messages.Dequeue();

                //Log.DebugFormat("Message '{0}' dequeued and ready to be processed.", message.ID);
                Task.Factory.StartNew(ProcessMessage, message);    
            }

            Log.InfoFormat("Thread '{0}' stopped", Thread.CurrentThread.Name);
        }

        private void ProcessMessage(object state)
        {
            //HttpMessage message = state as HttpMessage;
            //if (message == null)
            //    return;

            // Process request message.
            //if (message is HttpRequest)
            //{
            //    Log.InfoFormat("Process request '{0}' -> '{1} {2}' from '{3}'", message.ID, ((HttpRequest)message).Method, ((HttpRequest)message).Uri, message.RemoteEndpoint);

            //    // TODO: Implement Actions which are executed globally, fx check JWT-token

            //    try
            //    {
            //        //IAction action = ActionFactory.Create((HttpRequest)message);
            //        //HttpResponse response = action.Execute((HttpRequest)message);
            //        //if (response != null)
            //        //{
            //        //    lock (SyncLock)
            //        //    {
            //        //        Messages.Enqueue(response);
            //        //        Log.InfoFormat("Response '{0}' queued for processing", response.ID);
            //        //    }
            //        //}
            //    }
            //    catch (Exception e)
            //    {
            //        Log.ErrorFormat("Error occurred while processing request; {0}", e.Message);
            //    }
            //    finally
            //    {
            //    }
            //}

            //// Process response message.
            //if (message is HttpResponse)
            //{
            //    Log.InfoFormat("Process response '{0}' for '{1}'; result: {2}", message.ID, message.RemoteEndpoint, ((HttpResponse)message).Status);

            //    HttpConnection connection = Connections.Find(m => m.RemoteEndpoint == message.RemoteEndpoint);
            //    if (connection != null)
            //    {
            //        try
            //        {
            //            connection.Send((HttpResponse)message);

            //            if (message.Headers.ContainsKey("Connection"))
            //            {
            //                if (message.Headers["Connection"] == "close")
            //                {
            //                    connection.Close();
            //                    Connections.Remove(connection);
            //                }
            //            }
            //        }
            //        catch (Exception e)
            //        {
            //            Log.ErrorFormat("Error occurred while processing response; {0}", e.Message);

            //        }
            //    }
            //    else
            //        Log.WarnFormat("Connection for client '{0}' not found", message.RemoteEndpoint);                
            //}

            //Log.InfoFormat("Processing of message '{0}' complete", message.ID);
        }

        private void GarbageCycleThread(object state)
        {
            CancellationToken tct = (CancellationToken)state;

            Log.Info(String.Format("Thread '{0}' started", Thread.CurrentThread.Name));

            while (!tct.IsCancellationRequested)
            {
                Thread.Sleep(THREAD_INTERVAL_GARC);

                Log.Info("Garbage cycle started");
                
                // TODO: Write some code here.
                // Fx, close idle client connections




                Log.Info("Garbage cycle completed");
            }

            Log.Info(String.Format("Thread '{0}' stopped", Thread.CurrentThread.Name));
        }
    }
}