using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSEcoRouteSolver
{
    class RoutingProblemParameters
    {
        public int NumAircraft { get; set; }

        public int PaxCapacity { get; set; }

        public int CostPerNM { get; set; }

        public int MaxLength { get; set; }

        public int MaxSolveSec { get; set; }

        public int MaxTimeEnroute { get; set; }

        public int AircraftSpeed { get; set; }

        public string HubICAO { get; set; }
    }
}
