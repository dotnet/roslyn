// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.Options;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.LanguageServices.Implementation.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Options
{
    internal class SimpleCodeStyleOptionViewModel : AbstractCodeStyleOptionViewModel
    {
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
                    Info.SetOptionAndUpdatePreview(new SimpleCodeStyleOption(_selectedPreference.IsChecked, _selectedNotificationPreference.Notification), Option, GetPreview());
                }
            }
        }

        private NotificationOptionViewModel _selectedNotificationPreference;
        public override NotificationOptionViewModel SelectedNotificationPreference
        {
            get
            {
                return _selectedNotificationPreference;
            }

            set
            {
                if (SetProperty(ref _selectedNotificationPreference, value))
                {
                    Info.SetOptionAndUpdatePreview(new SimpleCodeStyleOption(_selectedPreference.IsChecked, _selectedNotificationPreference.Notification), Option, GetPreview());
                }
            }
        }

        public override bool NotificationsAvailable => true;

        public SimpleCodeStyleOptionViewModel(
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
            var codeStyleOption = ((SimpleCodeStyleOption)options.GetOption(new OptionKey(option, option.IsPerLanguage ? info.Language : null)));
            _selectedPreference = Preferences.Single(c => c.IsChecked == codeStyleOption.IsChecked);

            var notificationViewModel = NotificationPreferences.Single(i => i.Notification.Value == codeStyleOption.Notification.Value);
            _selectedNotificationPreference = NotificationPreferences.Single(p => p.Notification.Value == notificationViewModel.Notification.Value);

            NotifyPropertyChanged(nameof(SelectedPreference));
            NotifyPropertyChanged(nameof(SelectedNotificationPreference));
        }
    }
}
