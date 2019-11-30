// <copyright file="PaymentLicenseManager.cs" company="Carson Page">
// Copyright (c) Carson Page. All rights reserved.
// </copyright>

namespace FSEcoRouteSolver
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Net.Http;
    using System.Text;
    using System.Threading.Tasks;
    using System.Windows;
    using CsvHelper;
    using FSEcoRouteSolver.API;

    internal class PaymentLicenseManager
    {
        private readonly HttpClient httpClient = new HttpClient();
        private readonly TimeClient timeClient = new TimeClient(new HttpClient());
        private readonly string apiKey;

        public PaymentLicenseManager(string apiKey)
        {
            this.apiKey = apiKey;
        }

        public async Task<bool> VerifyStatus()
        {
            try
            {
                var response = await this.timeClient.Timezone2Async("Etc", "UTC");
                var dateTime = DateTime.Parse(response.Datetime);
                try
                {
                    var paymentDataCurrent = await this.httpClient.GetStringAsync(string.Format(
                       @"http://server.fseconomy.net/data?userkey={0}&format=csv&query=payments&search=monthyear&readaccesskey={0}&month={1}&year={2}",
                       this.apiKey,
                       dateTime.Month,
                       dateTime.Year));

                    var prevMonth = dateTime.AddMonths(-1);

                    var paymentDataPrev = await this.httpClient.GetStringAsync(string.Format(
                        @"http://server.fseconomy.net/data?userkey={0}&format=csv&query=payments&search=monthyear&readaccesskey={0}&month={1}&year={2}",
                        this.apiKey,
                        prevMonth.Month,
                        prevMonth.Year));

                    var paymentCsvCur = new CsvReader(new StringReader(paymentDataCurrent));
                    var paymentCsvPrev = new CsvReader(new StringReader(paymentDataPrev));

                    return paymentCsvPrev.GetRecords<PaymentRecord>()
                        .Concat(paymentCsvCur.GetRecords<PaymentRecord>())
                        .Any(r => (r.To == "Page Planner" && r.Amount >= 10000 && dateTime.Subtract(r.Date).Days <= 30));
                }
                catch (CsvHelperException)
                {
                    MessageBox.Show("Invalid API Key, or you have hit FSE API limits. Try reseting your key.");
                    Application.Current.Shutdown();
                    return false;
                }
            }
            catch (ApiException)
            {
                MessageBox.Show("Unable to fetch time data!");
                Application.Current.Shutdown();
                return false;
            }
        }

        public async void VerifyOrHalt()
        {
            if (!await this.VerifyStatus())
            {
                Application.Current.Shutdown();
            }
        }

        private class PaymentRecord
        {
            public string From { get; set; }

            public string To { get; set; }

            public float Amount { get; set; }

            public DateTime Date { get; set; }
        }
    }
}
