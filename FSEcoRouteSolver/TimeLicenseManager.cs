// <copyright file="TimeLicenseManager.cs" company="Carson Page">
// Copyright (c) Carson Page. All rights reserved.
// </copyright>

namespace FSEcoRouteSolver
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net.Http;
    using System.Text;
    using System.Threading.Tasks;
    using System.Windows;
    using FSEcoRouteSolver.API;

    internal class TimeLicenseManager
    {
        private readonly TimeClient timeClient = new TimeClient(new HttpClient());

        private TimeLicenseManager()
        {
        }

        public static TimeLicenseManager Instance { get; } = new TimeLicenseManager();

        public async Task<bool> VerifyStatus()
        {
            var response = await this.timeClient.Timezone2Async("Etc", "UTC");
            var dateTime = DateTime.Parse(response.Datetime);

            return dateTime < new DateTime(2020, 2, 28);
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
