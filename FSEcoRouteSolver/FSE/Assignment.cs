// <copyright file="Assignment.cs" company="Carson Page">
// Copyright (c) Carson Page. All rights reserved.
// </copyright>

namespace FSEcoRouteSolver.FSE
{
    public sealed class Assignment
    {
        public string FromIcao { get; set; }

        public string ToIcao { get; set; }

        public int Amount { get; set; }

        public decimal Pay { get; set; }

        public bool PtAssignment { get; set; }

        public string Commodity { get; set; }

        public int Id { get; set; }
    }
}