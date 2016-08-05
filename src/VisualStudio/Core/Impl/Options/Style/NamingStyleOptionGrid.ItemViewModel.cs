using Microsoft.CodeAnalysis.Diagnostics.Analyzers.NamingStyles;
using Microsoft.VisualStudio.LanguageServices.Implementation.Options.Style.NamingPreferences;
using Microsoft.VisualStudio.LanguageServices.Implementation.Utilities;
using System.Collections.Generic;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Options.Style
{
    internal partial class NamingStyleOptionGrid
    {
        private class ItemViewModel : AbstractNotifyPropertyChanged
        {
            public ItemViewModel()
            {
                Specifications = new List<SymbolSpecification>();
                NamingStyles = new List<NamingStyle>();
                NotificationPreferences = new List<EnforcementLevel>();
            }

            private SymbolSpecification _selectedSpecification;
            private NamingStyle _selectedNamingStyle;
            private EnforcementLevel _selectedNotification;

            public IEnumerable<SymbolSpecification> Specifications { get; set; }
            public IEnumerable<NamingStyle> NamingStyles { get; set; }
            public IEnumerable<EnforcementLevel> NotificationPreferences { get; set; }

            public SymbolSpecification SelectedSpecification
            {
                get
                {
                    return _selectedSpecification;
                }
                set
                {
                    SetProperty(ref _selectedSpecification, value);
                }
            }

            public NamingStyle SelectedStyle
            {
                get
                {
                    return _selectedNamingStyle;
                }
                set
                {
                    SetProperty(ref _selectedNamingStyle, value);
                }
            }
            public EnforcementLevel SelectedNotificationPreference
            {
                get
                {
                    return _selectedNotification;
                }
                set
                {
                    SetProperty(ref _selectedNotification, value);
                }
            }

            public bool IsComplete()
            {
                return SelectedSpecification != null && SelectedStyle != null && SelectedNotificationPreference != null;
            }
        }
    }
}
