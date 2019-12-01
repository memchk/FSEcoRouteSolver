// <copyright file="StartUp.xaml.cs" company="Carson Page">
// Copyright (c) Carson Page. All rights reserved.
// </copyright>

namespace FSEcoRouteSolver.UI
{
    using System;
    using System.Net;
    using System.Net.Sockets;
    using System.Windows;

    /// <summary>
    /// Interaction logic for StartUp.xaml.
    /// </summary>
    public partial class StartUp : Window
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="StartUp"/> class.
        /// </summary>
        public StartUp()
        {
            this.InitializeComponent();

            var apiKey = Properties.Settings.Default.APIKey;
            if (apiKey.Length != 0)
            {
                this.tAPIKey.Text = apiKey;
            }
        }

        private async void BStart_Click(object sender, RoutedEventArgs e)
        {
            this.bStart.IsEnabled = false;
            var apiKey = this.tAPIKey.Text;
            if (apiKey.Length != 0)
            {
                Properties.Settings.Default.APIKey = apiKey;
                Properties.Settings.Default.Save();

                var licManager = new PaymentLicenseManager(apiKey);
                var status = await licManager.VerifyStatus();
                if (!status)
                {
                    MessageBox.Show("Please send $10000 to the FSE Group 'Page Planner' FROM YOUR PERSONAL ACCOUNT, and restart." +
                        "This will provide a license for 30 days.");
                    Application.Current.Shutdown();
                }

                var mainWindow = new MainWindow(apiKey);
                mainWindow.Show();
                mainWindow.Activate();
                this.Close();
            }
        }
    }
}