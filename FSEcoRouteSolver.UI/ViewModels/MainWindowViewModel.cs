using System;
using System.Collections.Generic;
using System.Text;
using ReactiveUI;

namespace FSEcoRouteSolver.UI.ViewModels
{
    public class MainWindowViewModel : ViewModelBase
    {   
        private ViewModelBase currentPage;

        public MainWindowViewModel()
        {
            CurrentPage = SolverConfiguration = new SolverConfigurationViewModel();
            AircraftSelection = new AircraftSelectionViewModel();
            
            SolverConfiguration.Solve.Subscribe(_ => {
                CurrentPage = AircraftSelection;
            });
        }

        public ViewModelBase CurrentPage {
            get => currentPage;
            set => this.RaiseAndSetIfChanged(ref currentPage, value);
        }

        public SolverConfigurationViewModel SolverConfiguration { get; }
        public AircraftSelectionViewModel AircraftSelection { get; }
    }
}
