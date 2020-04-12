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
            this.lunch = new Lunch();
        }
        public string Name { get; set; }
        public Lunch lunch { get; set; }
    }
}
