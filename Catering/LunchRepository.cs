using Catering.Models;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;

namespace Catering
{
    public class LunchRepository
    {
        private readonly List<User> _orders = new List<User>();
        private object _lock = new object();
        private int _maxCount = 5;

        public void UpsertOrder(User user)
        {
            lock(_lock)
            {
                for(int i = 0; i< _orders.Count; i++)
                {
                    if (_orders[i].Id == user.Id)
                    {
                        _orders.RemoveAt(i);
                        break;
                    }
                }

                // prune to 5
                if (_orders.Count >= _maxCount)
                {
                    while(_orders.Count > _maxCount)
                    {
                        _orders.RemoveAt(0);
                    }
                }

                _orders.Add(user);
            }
        }

        public IList<User> GetLatestOrders()
        {
            lock (_lock)
            {
                return _orders.ToImmutableList();
            }
        }


    }
}
