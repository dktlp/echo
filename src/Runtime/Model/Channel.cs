using System;
using System.Collections.Generic;

namespace Echo.Runtime.Model
{
    public class Channel
    {
        public string Id { get; private set; }
        public string Name { get; set; }
        public List<User> Users { get; private set; }

        public Channel()
        {
            Id = Guid.NewGuid().ToString("N");
            Users = new List<User>();
            Name = null;            
        }
    }
}