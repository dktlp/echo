using System;
using System.Collections.Generic;

namespace Echo.Runtime.Model
{
    public class User
    {
        public string Id { get; private set; }
        public string Name { get; set; }

        public User()
        {
            Id = Guid.NewGuid().ToString("N");
            Name = null;
        }
    }
}