// <copyright file="OwnedAircraft.cs" company="Carson Page">
// Copyright (c) Carson Page. All rights reserved.
// </copyright>

namespace FSEcoRouteSolver
{
    using FSEcoRouteSolver.FSE;

    internal class OwnedAircraft
    {
        public string Registration { get; set; } = string.Empty;

        public AircraftConfig AircraftConfig { get; set; }
    }
}
