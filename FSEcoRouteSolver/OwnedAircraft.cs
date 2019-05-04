// <copyright file="OwnedAircraft.cs" company="Carson Page">
// Copyright (c) Carson Page. All rights reserved.
// </copyright>

namespace FSEcoRouteSolver
{
    internal class OwnedAircraft : Aircraft
    {
        public string Registration { get; set; } = string.Empty;

        public string Location { get; set; } = string.Empty;
    }
}
