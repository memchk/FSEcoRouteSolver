// <copyright file="RoutingProblemParameters.cs" company="Carson Page">
// Copyright (c) Carson Page. All rights reserved.
// </copyright>

namespace FSEcoRouteSolver
{
    internal class RoutingProblemParameters
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
