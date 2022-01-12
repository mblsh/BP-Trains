using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BPTrains
{
    public class Station // vertice
    {
        public string Name { get; set; }
        public List<Route> Connections { get; } = new List<Route>(); 
    }
}
