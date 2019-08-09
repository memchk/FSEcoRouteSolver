// <copyright file="MainWindow.xaml.cs" company="Carson Page">
// Copyright (c) Carson Page. All rights reserved.
// </copyright>

namespace FSEcoRouteSolver.UI
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Linq;
    using System.Threading.Tasks;
    using System.Windows;

    /// <summary>
    /// Interaction logic for MainWindow.xaml.
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly string apiKey;
        private readonly List<Aircraft> aircraftList;

        /// <summary>
        /// Initializes a new instance of the <see cref="MainWindow"/> class.
        /// </summary>
        public MainWindow(string apiKey)
        {
            this.InitializeComponent();
            this.apiKey = apiKey;
            this.aircraftList = Aircraft.ListFromFSEconomy(apiKey);
        }

        private async void BSolve_ClickAsync(object sender, RoutedEventArgs e)
        {

            var buildFleet = new BuildFleet(this.aircraftList);
            buildFleet.ShowDialog();

            var solveTime = int.Parse(this.tMaxSolveTime.Text);

            this.pSolveTime.IsIndeterminate = true;
            this.tResult.Text = "Solver running for " + solveTime + " seconds. Results will appear here.";

            var icao = this.tICAO.Text;
            var maxTime = (int)Math.Round(double.Parse(this.tMaxEnrouteTime.Text) * 100);
            var maxLength = (int)Math.Round(double.Parse(this.tMaxLength.Text) * 100);

            if (buildFleet.DialogResult == true)
            {
                var routingParameters = new RoutingProblemParameters
                {
                    Fleet = buildFleet.AircraftFleet.ToList(),
                    HubICAO = icao,
                    MaxTimeEnroute = maxTime,
                    MaxLength = maxLength,
                    MaxSolveSec = solveTime,
                };

                var solveTask = Task.Run(() =>
                {
                    RouteProblem rp = new RouteProblem(routingParameters, this.apiKey);
                    rp.EnableBookingFee();
                    return rp.Solve();
                });

                this.tResult.Text = await solveTask;
            }

            this.pSolveTime.IsIndeterminate = false;
        }

    }
}
