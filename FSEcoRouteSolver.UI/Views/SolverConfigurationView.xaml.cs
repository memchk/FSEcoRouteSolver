using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace FSEcoRouteSolver.UI.Views
{
    public class SolverConfigurationView : UserControl
    {
        public SolverConfigurationView()
        {
            this.InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
