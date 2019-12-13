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
    using FSEcoRouteSolver.FSE;

    internal class PaymentLicenseManager
    {
        private readonly TimeClient timeClient = new TimeClient(new HttpClient());
        private readonly FSEconomyClient fseClient;

        public PaymentLicenseManager(FSEconomyClient fseClient)
        {
            this.fseClient = fseClient;
        }

        public async Task<bool> VerifyStatus()
        {
            try
            {
                var response = await this.timeClient.Timezone2Async("Etc", "UTC");
                var dateTime = DateTime.Parse(response.Datetime);
                try
                {
                    var paymentCurrent = await this.fseClient.GetPaymentsAsync(dateTime.Month, dateTime.Year);
                    var prevMonth = dateTime.AddMonths(-1);
                    var paymentPrevious = await this.fseClient.GetPaymentsAsync(prevMonth.Month, prevMonth.Year);

                    return paymentPrevious
                        .Concat(paymentCurrent)
                        .Any(r => (r.To == "Page Planner" && r.Amount >= 10000 && dateTime.Subtract(r.Date).Days <= 30));
                }
                catch (FSE.ApiException<FSEError>)
                {
                    MessageBox.Show("Invalid API Key, or you have hit FSE API limits. Try reseting your key.");
                    Application.Current.Shutdown();
                    return false;
                }
            }
            catch (API.ApiException)
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
    }
}
