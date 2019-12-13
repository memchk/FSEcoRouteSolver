// <copyright file="IcaoRecord.cs" company="Carson Page">
// Copyright (c) Carson Page. All rights reserved.
// </copyright>

namespace FSEcoRouteSolver.FSE
{
    using CsvHelper.Configuration.Attributes;

    public sealed class IcaoRecord
    {
        [Name("icao")]
        public string Icao { get; set; }

        [Name("lat")]
        public double Latitude { get; set; }

        [Name("lon")]
        public double Longitude { get; set; }

        [Name("type")]
        public string Type { get; set; }
    }
}