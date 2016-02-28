using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Options
{
    internal class BooleanCodeStyleOptionViewModel : AbstractCodeStyleOptionViewModel
    {
        public BooleanCodeStyleOptionViewModel(
            IOption option, 
            string description, 
            string truePreview, 
            string falsePreview, 
            AbstractOptionPreviewViewModel info, 
            OptionSet options, 
            string groupName, 
            List<CodeStylePreference> preferences = null, 
            List<NotificationOptionViewModel> notificationPreferences = null) 
            : base(option, description, truePreview, falsePreview, info, options, groupName, preferences, notificationPreferences)
        {
            var booleanOption = (bool)options.GetOption(new OptionKey(option, option.IsPerLanguage ? info.Language : null));
            _selectedPreference = Preferences.Single(c => c.IsChecked == booleanOption);

            NotifyPropertyChanged(nameof(SelectedPreference));
        }

        public override bool NotificationsAvailable => false;

        public override NotificationOptionViewModel SelectedNotificationPreference
        {
            get
            {
                throw new NotSupportedException();
            }

            set
            {
                throw new NotSupportedException();
            }
        }

        private CodeStylePreference _selectedPreference;
        public override CodeStylePreference SelectedPreference
        {
            get
            {
                return _selectedPreference;
            }
            set
            {
                if (SetProperty(ref _selectedPreference, value))
                {
                    Info.SetOptionAndUpdatePreview(_selectedPreference.IsChecked, Option, GetPreview());
                }
            }
        }
    }
}
