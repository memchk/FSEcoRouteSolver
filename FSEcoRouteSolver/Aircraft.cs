// <copyright file="Aircraft.cs" company="Carson Page">
// Copyright (c) Carson Page. All rights reserved.
// </copyright>

namespace FSEcoRouteSolver
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Text;
    using System.Threading.Tasks;
    using CsvHelper;
    using CsvHelper.Configuration.Attributes;

    internal class Aircraft
    {
        [Name("GPH")]
        public double GallonsPerHour { get; private set; }

        public string MakeModel { get; private set; }

        public int Crew { get; private set; }

        public int Seats { get; private set; }

        public int CruiseSpeed { get; private set; }

        /// <summary>
        /// Get a list of in game aircraft given an FSE key.
        /// </summary>
        /// <param name="api_key">FSEcnomy API Key.</param>
        /// <returns>A list of aircraft.</returns>
        public static List<Aircraft> ListFromFSEconomy(string api_key)
        {
            var webClient = new WebClient();
            var aircraftCsv = new CsvReader(new StringReader(webClient.DownloadString(string.Format("http://server.fseconomy.net/data?userkey={0}&format=csv&query=aircraft&search=configs", api_key))));
            aircraftCsv.Configuration.IncludePrivateMembers = true;
            return aircraftCsv.GetRecords<Aircraft>().ToList();
        }

        public double CostPerNM(double fuelPrice)
        {
            return (this.GallonsPerHour * fuelPrice) / this.CruiseSpeed;
        }
    }
}
