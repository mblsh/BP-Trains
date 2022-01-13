using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BPTrains
{
    public class Train
    {
        public string Name { get; set; }
        public Station CurrentStation { get; set; }
        public int Capacity { get; set; }
        public List<Package> Packages { get; } = new List<Package>();

        public int FreeCapacity
        {
            get
            {
                return Capacity - Packages.Sum(p => p.Weight);
            }
        }

        public int TimeSpentEnRoute { get; set; }

        public bool Parked { get; set; } = false;
    }
}
