// <copyright file="FSEconomyClient.cs" company="Carson Page">
// Copyright (c) Carson Page. All rights reserved.
// </copyright>

namespace FSEcoRouteSolver.FSE
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Net.Http;
    using System.Text;
    using System.Threading.Tasks;
    using System.Xml;
    using System.Xml.Serialization;

    /// <summary>
    /// FS Economy Data Feed API Client.
    /// </summary>
    public class FSEconomyClient
    {
        public static readonly AsyncLazy<IReadOnlyDictionary<string, IcaoRecord>> ICAOData =
           new AsyncLazy<IReadOnlyDictionary<string, IcaoRecord>>(() =>
           {
               return GetICAOData();
           });

        private static readonly HttpClient HttpClient = new HttpClient();
        private static readonly XmlSerializer ErrSerializer = new XmlSerializer(typeof(FSEError));
        private static readonly string BaseUrl = @"http://server.fseconomy.net/data";

        public FSEconomyClient(string apiKey)
        {
            this.ApiKey = apiKey;
            this.AircraftConfigs =
                new AsyncLazy<IReadOnlyDictionary<string, AircraftConfig>>(() =>
                {
                    return this.GetAircraftConfigsAsync();
                });
        }

        /// <summary>
        /// Gets lazy loaded collection of FS Economy aircraft configurations. On first use, calls FSE to retrieve data, then its cached.
        /// </summary>
        public AsyncLazy<IReadOnlyDictionary<string, AircraftConfig>> AircraftConfigs { get; }

        /// <summary>
        /// Gets or sets the API key to make calls to the FSE Data Feed.
        /// </summary>
        public string ApiKey { get; set; }

        public Task<IcaoJobsFrom> GetJobsFromAsync(string icao)
        {
            var uri = string.Format(BaseUrl + @"?userkey={0}&format=xml&query=icao&search=jobsfrom&icaos={1}", this.ApiKey, icao);
            return GetRequest<IcaoJobsFrom>(uri);
        }

        public Task<IcaoJobsTo> GetJobsToAsync(string icao)
        {
            var uri = string.Format(BaseUrl + @"?userkey={0}&format=xml&query=icao&search=jobsto&icaos={1}", this.ApiKey, icao);
            return GetRequest<IcaoJobsTo>(uri);
        }

        public Task<Payments> GetPaymentsAsync(int month, int year)
        {
            if (month < 1 || month > 12)
            {
                throw new ArgumentOutOfRangeException("month");
            }

            var uri = string.Format(BaseUrl + @"?userkey={0}&format=xml&query=payments&search=monthyear&readaccesskey={0}&month={1}&year={2}", this.ApiKey, month, year);
            return GetRequest<Payments>(uri);
        }

        private static async Task<IReadOnlyDictionary<string, IcaoRecord>> GetICAOData()
        {
            StreamReader tmp;
            try
            {
                tmp = File.OpenText(@"./icaodata.csv");
            }
            catch (FileNotFoundException)
            {
                using (var icaoData = File.Create(@"./icaodata.csv"))
                {
                    var stream = await HttpClient.GetStreamAsync(@"http://server.fseconomy.net/static/library/datafeed_icaodata.zip");
                    var zip = new System.IO.Compression.ZipArchive(stream);
                    var csvStream = zip.GetEntry("icaodata.csv").Open();
                    await csvStream.CopyToAsync(icaoData);
                    csvStream.Dispose();
                    zip.Dispose();
                    stream.Dispose();
                }
            }
            finally
            {
                tmp = File.OpenText(@"./icaodata.csv");
            }

            var dictonary = new CsvHelper.CsvReader(tmp).GetRecords<IcaoRecord>().ToDictionary((x) => x.Icao);
            return dictonary;
        }

        private static async Task<TResult> GetRequest<TResult>(string uri)
        {
            var resSerializer = new XmlSerializer(typeof(TResult));
            using (var httpResponse = await HttpClient.GetAsync(uri))
            {
                var stream = await httpResponse.Content.ReadAsStreamAsync();
                var xmlReader = XmlReader.Create(stream);
                if (resSerializer.CanDeserialize(xmlReader))
                {
                    var res = (TResult)resSerializer.Deserialize(xmlReader);
                    return res;
                }
                else
                {
                    var err = (FSEError)ErrSerializer.Deserialize(xmlReader);
                    throw new ApiException<FSEError>(err);
                }
            }
        }

        private async Task<IReadOnlyDictionary<string, AircraftConfig>> GetAircraftConfigsAsync()
        {
            var uri = string.Format(BaseUrl + @"?userkey={0}&format=xml&query=aircraft&search=configs", this.ApiKey);
            return (await GetRequest<AircraftConfigItems>(uri)).ToDictionary(x => x.MakeModel);
        }
    }
}
