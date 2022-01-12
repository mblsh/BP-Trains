using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BPTrains
{
    public class Route // weighted edge
    {
        public Station Start { get; set; } // vertice1
        public Station Destination { get; set; } // vertice2
        public string Name { get; set; }
        public int TravelTime { get; set; } // weight
    }
}
