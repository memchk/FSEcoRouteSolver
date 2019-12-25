using System;
using System.Collections.Generic;
using System.Text;

namespace FSEcoRouteSolver.UI.ViewModels
{
    public class MainWindowViewModel : ViewModelBase
    {
        public ViewModelBase CurrentPage => new SolverConfigurationViewModel();
    }
}
