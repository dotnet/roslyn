using System.Collections.ObjectModel;
using System.ComponentModel;

namespace Microsoft.CodeAnalysis.InlineRename.UI.SmartRename
{
    internal class TestViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        public ObservableCollection<string> SuggestedNames { get; } = new ObservableCollection<string>() { "Hello", "World", "Bar", "Goo", "XYZ" };

        public bool HasSuggestions => true;
    }
}
