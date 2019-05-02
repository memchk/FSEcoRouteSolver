using System;
using System.Collections.Generic;
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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace FSEcoRouteSolver
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void BSolve_Click(object sender, RoutedEventArgs e)
        {
            RouteProblem rp = new RouteProblem(tICAO.Text, "A766DDB58CD76287");
            tResult.Text = rp.Solve(
                int.Parse(tNumberOfAircraft.Text), 
                int.Parse(tCPNM.Text), 
                int.Parse(tPaxCapacity.Text), 
                (int)Math.Round(double.Parse(tMaxLength.Text)*100), 
                int.Parse(tMaxSolveTime.Text),
                (int)Math.Round(double.Parse(tMaxEnrouteTime.Text) * 100),
                int.Parse(tSpd.Text)
            );

        }
    }
}
