using System;
using System.Collections.Generic;

namespace Echo.Runtime.Engine
{
    public class SessionException : Exception
    {
        public SessionException()
            : base("Invalid session.")
        {
        }
    }
}
