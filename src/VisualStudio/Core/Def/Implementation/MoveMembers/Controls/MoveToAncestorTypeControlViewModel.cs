using System.Collections.Immutable;
using System.Windows.Controls;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.LanguageServices.Implementation.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.MoveMembers.Controls
{
    internal class MoveToAncestorTypeControlViewModel : AbstractNotifyPropertyChanged, ISelectDestinationViewModel
    {
        public ImmutableArray<SymbolViewModel<INamedTypeSymbol>> Destinations { get; set; }

        public MoveToAncestorTypeControlViewModel(ImmutableArray<SymbolViewModel<INamedTypeSymbol>> destinations)
        {
            Destinations = destinations;
        }

        private SymbolViewModel<INamedTypeSymbol> _selectedDestination;
        public SymbolViewModel<INamedTypeSymbol> SelectedDestination
        {
            get => _selectedDestination;
            set => SetProperty(ref _selectedDestination, value, nameof(SelectedDestination));
        }

        // Since destinations are always an ancestor type the state is valid
        public bool IsValid => true;

        INamedTypeSymbol ISelectDestinationViewModel.SelectedDestination => SelectedDestination.Symbol;

        public UserControl CreateUserControl()
        => new MoveToAncestorTypeControl(this);
    }
}
