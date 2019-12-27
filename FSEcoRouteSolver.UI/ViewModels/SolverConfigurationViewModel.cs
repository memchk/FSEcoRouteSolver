using System;
using System.Collections.Generic;
using System.Text;
using System.Reactive;
using ReactiveUI;
using System.Reactive.Linq;

namespace FSEcoRouteSolver.UI.ViewModels
{
    public class SolverConfigurationViewModel : ViewModelBase
    {   
        string hubIcao;
        double maxDistanceNM;
        double maxTimeHr;

        public SolverConfigurationViewModel()
        {
            var _allowSolve = this.WhenAnyValue(
                x => x.HubIcao,
                x => x.MaxDistanceNM,
                x => x.MaxTimeHr, 
                (h, d, t) =>
                    !string.IsNullOrWhiteSpace(h) &&
                    d > 0 && 
                    t > 0
                )
                .DistinctUntilChanged();
            
            Solve = ReactiveCommand.Create(() => {}, _allowSolve);
        }

        public string HubIcao {
            get => hubIcao;
            set => this.RaiseAndSetIfChanged(ref hubIcao, value);
        }

        public double MaxDistanceNM {
            get => maxDistanceNM;
            set => this.RaiseAndSetIfChanged(ref maxDistanceNM, value);
        }

        public double MaxTimeHr {
            get => maxTimeHr;
            set => this.RaiseAndSetIfChanged(ref maxTimeHr, value);
        }

        public ReactiveCommand<Unit, Unit> Solve { get; }
    }
}
