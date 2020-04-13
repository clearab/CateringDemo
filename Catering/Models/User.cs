using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Catering.Models
{
    public class User
    {
        public User()
        {
            this.Lunch = new Lunch();
        }
        public string Id { get; set; }
        public string Name { get; set; }
        public Lunch Lunch { get; set; }
    }
}
