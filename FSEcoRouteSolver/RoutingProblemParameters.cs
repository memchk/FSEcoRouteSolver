// <copyright file="RoutingProblemParameters.cs" company="Carson Page">
// Copyright (c) Carson Page. All rights reserved.
// </copyright>

namespace FSEcoRouteSolver
{
    using System.Collections.Generic;

    internal class RoutingProblemParameters
    {
        public int MaxLength { get; set; }

        public int MaxSolveSec { get; set; }

        public int MaxTimeEnroute { get; set; }

        public string HubICAO { get; set; }

        public List<OwnedAircraft> Fleet { get; set; }
    }
}
