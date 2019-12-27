using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace FSEcoRouteSolver.UI.Views
{
    public class AircraftSelectionView : UserControl
    {
        public AircraftSelectionView()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}