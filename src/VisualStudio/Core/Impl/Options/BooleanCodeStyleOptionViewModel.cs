using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Options
{
    /// <summary>
    /// This class maps a boolean option into a codestyle option 
    /// that binds to the codestyle options UI.
    /// </summary>
    /// <remarks>
    /// This exists to support options that are implemented as boolean
    /// options in the non UI layers, <see cref="Option{Boolean}"/>. 
    /// In future, if such options are moved to use <see cref="SimpleCodeStyleOption"/>, 
    /// this class can be completely deleted.
    /// </remarks>
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
            // presently, these options do not support notification styles.
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
