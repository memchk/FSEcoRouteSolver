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
        private List<OwnedAircraft> ownedFleet;

        /// <summary>
        /// Initializes a new instance of the <see cref="MainWindow"/> class.
        /// </summary>
        public MainWindow(string apiKey)
        {
            this.InitializeComponent();
            this.apiKey = apiKey;
            this.aircraftList = Aircraft.ListFromFSEconomy(apiKey);
            this.ownedFleet = new List<OwnedAircraft>();
        }

        private async void BSolve_ClickAsync(object sender, RoutedEventArgs e)
        {
            TimeLicenseManager.Instance.VerifyOrHalt();
            var buildFleet = new BuildFleet(this.aircraftList, this.ownedFleet);

            if (!double.TryParse(this.tMaxEnrouteTime.Text, out double maxTimeD))
            {
                MessageBox.Show("Invalid Enroute Time! Must be entered as a decimal number.");
                return;
            }

            var maxTime = (int)Math.Round(maxTimeD * 100);

            if (!double.TryParse(this.tMaxLength.Text, out double maxLengthD))
            {
                MessageBox.Show("Invalid Max Length! Must be entered as a decimal number.");
                return;
            }

            var maxLength = (int)Math.Round(maxLengthD * 100);

            buildFleet.ShowDialog();

            if (buildFleet.DialogResult == true)
            {
                var solveTime = int.Parse(this.tMaxSolveTime.Text);
                this.pSolveTime.IsIndeterminate = true;
                this.tResult.Text = "Solver running for " + solveTime + " seconds. Results will appear here.";

                var icao = this.tICAO.Text;

                // var maxTime = (int)Math.Round(double.Parse(this.tMaxEnrouteTime.Text) * 100);
                // var maxLength = (int)Math.Round(double.Parse(this.tMaxLength.Text) * 100)
                this.ownedFleet = buildFleet.AircraftFleet.ToList();

                var routingParameters = new RoutingProblemParameters
                {
                    Fleet = this.ownedFleet,
                    HubICAO = icao,
                    MaxTimeEnroute = maxTime,
                    MaxLength = maxLength,
                    MaxSolveSec = solveTime,
                    IncludeSeaports = (bool)this.cSeaport.IsChecked,
                };

                var solveTask = Task.Run(async () =>
                {
                    RouteProblem rp = await RouteProblem.CreateProblem(routingParameters, this.apiKey);
                    return rp.Solve();
                });

                this.tResult.Text = await solveTask;
            }

            this.pSolveTime.IsIndeterminate = false;
        }
    }
}
