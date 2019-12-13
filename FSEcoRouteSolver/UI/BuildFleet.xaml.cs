// <copyright file="BuildFleet.xaml.cs" company="Carson Page">
// Copyright (c) Carson Page. All rights reserved.
// </copyright>

namespace FSEcoRouteSolver.UI
{
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Windows;
    using FSEcoRouteSolver.FSE;

    /// <summary>
    /// Interaction logic for BuildFleet.xaml.
    /// </summary>
    public partial class BuildFleet : Window
    {
        internal BuildFleet(List<AircraftConfig> aircraftList, List<OwnedAircraft> ownedAircraft)
        {
            this.InitializeComponent();
            this.cAircraftList.ItemsSource = aircraftList;
            if (ownedAircraft != null)
            {
                this.AircraftFleet = new ObservableCollection<OwnedAircraft>(ownedAircraft);
            }
            else
            {
                this.AircraftFleet = new ObservableCollection<OwnedAircraft>();
            }

            this.tFleet.ItemsSource = this.AircraftFleet;
            this.tFleet.CanUserDeleteRows = true;
            this.tFleet.CanUserAddRows = false;
        }

        internal ObservableCollection<OwnedAircraft> AircraftFleet { get; private set; }

        private void AddAircraft_Click(object sender, RoutedEventArgs e)
        {
            var aircraft = (AircraftConfig)this.cAircraftList.SelectedItem;
            if (aircraft != null)
            {
                var ownedAircraft = new OwnedAircraft
                {
                    AircraftConfig = aircraft,
                };
                this.AircraftFleet.Add(ownedAircraft);
            }
        }

        private void BContinue_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
        }
    }
}
