namespace FSEcoRouteSolver.UI
{
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Windows;

    /// <summary>
    /// Interaction logic for BuildFleet.xaml
    /// </summary>
    public partial class BuildFleet : Window
    {
        internal BuildFleet(List<Aircraft> aircraftList)
        {
            this.InitializeComponent();
            this.cAircraftList.ItemsSource = aircraftList;
            this.AircraftFleet = new ObservableCollection<OwnedAircraft>();
            this.tFleet.ItemsSource = this.AircraftFleet;
        }

        internal ObservableCollection<OwnedAircraft> AircraftFleet { get; private set; }

        private void AddAircraft_Click(object sender, RoutedEventArgs e)
        {
            var aircraft = (Aircraft)this.cAircraftList.SelectedItem;
            if (aircraft != null)
            {
                var ownedAircraft = new OwnedAircraft(aircraft);
                this.AircraftFleet.Add(ownedAircraft);
            }
        }

        private void BContinue_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
        }
    }
}
