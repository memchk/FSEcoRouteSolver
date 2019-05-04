// <copyright file="MainWindow.xaml.cs" company="Carson Page">
// Copyright (c) Carson Page. All rights reserved.
// </copyright>

namespace FSEcoRouteSolver
{
    using System;
    using System.Threading.Tasks;
    using System.Windows;

    /// <summary>
    /// Interaction logic for MainWindow.xaml.
    /// </summary>
    public partial class MainWindow : Window
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MainWindow"/> class.
        /// </summary>
        public MainWindow()
        {
            this.InitializeComponent();
        }

        private async void BSolve_ClickAsync(object sender, RoutedEventArgs e)
        {
            var solveTime = int.Parse(this.tMaxSolveTime.Text);
            var numOfAircraft = int.Parse(this.tNumberOfAircraft.Text);
            var cpnm = (int)Math.Round(double.Parse(this.tCPNM.Text) * 100);
            var paxCapacity = int.Parse(this.tPaxCapacity.Text);
            var maxLength = (int)Math.Round(double.Parse(this.tMaxLength.Text) * 100);
            var maxTime = (int)Math.Round(double.Parse(this.tMaxEnrouteTime.Text) * 100);
            var spd = int.Parse(this.tSpd.Text);
            var icao = this.tICAO.Text;

            this.pSolveTime.IsIndeterminate = true;
            this.tResult.Text = "Solver running for " + solveTime + " seconds. Results will appear here.";

            var routingParameters = new RoutingProblemParameters
            {
                NumAircraft = numOfAircraft,
                CostPerNM = cpnm,
                PaxCapacity = paxCapacity,
                AircraftSpeed = spd,
                MaxSolveSec = solveTime,
                MaxLength = maxLength,
                MaxTimeEnroute = maxTime,
                HubICAO = icao,
            };

            var solveTask = Task.Run(() =>
            {
                RouteProblem rp = new RouteProblem(routingParameters, "BFE0D0F58A7F8EC6");
                rp.EnableBookingFee();
                return rp.Solve();
            });

            this.tResult.Text = await solveTask;
            this.pSolveTime.IsIndeterminate = false;
        }
    }
}
