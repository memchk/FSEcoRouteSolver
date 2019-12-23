﻿// <copyright file="StartUp.xaml.cs" company="Carson Page">
// Copyright (c) Carson Page. All rights reserved.
// </copyright>

namespace FSEcoRouteSolver.UI
{
    using System;
    using System.Net;
    using System.Net.Sockets;
    using System.Windows;
    using FSEcoRouteSolver.FSE;

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

        private void BStart_Click(object sender, RoutedEventArgs e)
        {
            this.bStart.IsEnabled = false;
            var apiKey = this.tAPIKey.Text;
            if (apiKey.Length != 0)
            {
                Properties.Settings.Default.APIKey = apiKey;
                Properties.Settings.Default.Save();

                var fseClient = new FSEconomyClient(apiKey);
                var mainWindow = new MainWindow(fseClient);
                mainWindow.Show();
                mainWindow.Activate();
                this.Close();
            }
        }
    }
}