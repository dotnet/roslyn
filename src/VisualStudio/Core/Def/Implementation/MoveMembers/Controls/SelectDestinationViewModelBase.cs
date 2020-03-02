using System.ComponentModel;
using System.Windows.Controls;
using Microsoft.CodeAnalysis;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.MoveMembers.Controls
{
    internal interface ISelectDestinationViewModel : INotifyPropertyChanged
    {
        INamedTypeSymbol SelectedDestination { get; }
        bool IsValid { get; }
        UserControl CreateUserControl();
    }
}
