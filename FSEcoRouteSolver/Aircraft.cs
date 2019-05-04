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

    internal class Aircraft
    {
        public double GallonsPerHour { get; private set; }

        public static List<Aircraft> ListFromFSEconomy(string api_key)
        {
            var aircraftList = new List<Aircraft>();
            var webClient = new WebClient();
            var aircraftCsv = new CsvReader(new StringReader(webClient.DownloadString(string.Format("http://server.fseconomy.net/data?userkey={0}&format=xml&query=aircraft&search=configs", api_key))));
            return aircraftList;
        }
    }
}
