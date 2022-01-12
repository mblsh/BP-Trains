using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BPTrains
{
    public class Move
    {
        public int StartTime { get; set; }
        public Station StartStation { get; set; }
        public Train Train { get; set; }
        public Route Route { get; set; }
        public List<Package> PickUps { get; } = new List<Package>();
        public List<Package> DropOffs { get; } = new List<Package>();

    }
}
