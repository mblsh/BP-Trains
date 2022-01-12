using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BPTrains
{
    public class Package
    {
        public string Name { get; set; }
        public Station PickUp { get; set; }
        public Station DropOff { get; set; }
        public int Weight { get; set; }

    }
}
