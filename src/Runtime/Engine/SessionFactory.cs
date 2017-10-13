using System;

namespace Echo.Runtime.Engine
{
    public static class SessionFactory
    {
        private static uint CurrentSessionId { get; set; }
        private static object SyncLock { get; set; }

        static SessionFactory()
        {
            CurrentSessionId = 0;
            SyncLock = new object();
        }

        public static Session Create()
        {
            uint newSessionId = 0;
            lock(SyncLock)
            {
                CurrentSessionId++;
                newSessionId = CurrentSessionId;
            }

            return new Session(newSessionId);
        }
    }
}