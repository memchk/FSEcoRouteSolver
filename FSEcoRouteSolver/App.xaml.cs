// <copyright file="App.xaml.cs" company="Carson Page">
// Copyright (c) Carson Page. All rights reserved.
// </copyright>

namespace FSEcoRouteSolver
{
    using System.Windows;
    using Microsoft.AppCenter;
    using Microsoft.AppCenter.Analytics;
    using Microsoft.AppCenter.Crashes;

    /// <summary>
    /// Interaction logic for App.xaml.
    /// </summary>
    public partial class App : Application
    {
        protected async override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            AppCenter.Start("cbc8caae-5d68-429a-9ccb-dffba64934f0", typeof(Crashes), typeof(Analytics));
            if (!await LicenseManager.Instance.VerifyStatus())
            {
                this.Shutdown();
            }
        }


    }
}
