using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace FSEcoRouteSolver.UI
{
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
