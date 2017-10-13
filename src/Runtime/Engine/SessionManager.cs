using System;
using System.Collections.Generic;

using log4net;

using Echo.Network.Tcp;

namespace Echo.Runtime.Engine
{
    public class SessionManager
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(TcpConnection));

        private List<Session> Sessions { get; set; }

        public SessionManager()
        {
            Sessions = new List<Session>();
        }

        public Session NewSession(TcpConnection tcpConnection)
        {
            Session session = SessionFactory.Create();
            session.Connection = tcpConnection;

            Log.InfoFormat("Session '{0}' created for connection '{1}'", session.Id, session.Connection.RemoteEndpoint);

            Sessions.Add(session);

            return session;
        }

        public List<Session> FindAll(Predicate<Session> match)
        {
            return Sessions.FindAll(match);
        }

        public Session Find(Predicate<Session> match)
        {
            return Sessions.Find(match);
        }

        public bool Remove(Session session)
        {
            Log.InfoFormat("Session '{0}' removed", session.Id);

            return Sessions.Remove(session);
        }

        #region Singleton

        private static SessionManager Instance { get; set; }
        private static object SyncLock { get; set; }

        static SessionManager()
        {
            Instance = null;
            SyncLock = new object();
        }

        public static SessionManager GetInstance()
        {
            lock(SyncLock)
            {
                if (Instance == null)
                    Instance = new SessionManager();
            }

            return Instance;
        }

        #endregion
    }
}