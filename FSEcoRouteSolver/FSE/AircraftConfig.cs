// <copyright file="AircraftConfig.cs" company="Carson Page">
// Copyright (c) Carson Page. All rights reserved.
// </copyright>
namespace FSEcoRouteSolver.FSE
{
    using System;
    using System.Xml.Serialization;

    public sealed class AircraftConfig
    {
        public string MakeModel { get; set; }

        public int CruiseSpeed { get; set; }

        [XmlElement("GPH")]
        public double GallonsPerHour { get; set; }

        public int Seats { get; set; }

        public int Crew { get; set; }

        [XmlIgnore]
        public int Passengers { get => this.Seats - this.Crew - 1; }

        public double CostPerNM(double fuelPrice)
        {
            return (this.GallonsPerHour * fuelPrice) / this.CruiseSpeed;
        }

        public int CostPerNMCents(double fuelPrice)
        {
            return (int)Math.Round(this.CostPerNM(fuelPrice) * 100);
        }
    }
}
