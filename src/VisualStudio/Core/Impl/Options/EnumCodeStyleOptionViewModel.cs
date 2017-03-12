// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Options
{
    /// <summary>
    /// This class represents the view model for a <see cref="CodeStyleOption{T}"/>
    /// that binds to the codestyle options UI.  Note that the T here is expected to be an enum
    /// type.  
    /// 
    /// Important.  The order of the previews and preferences provided should match the order
    /// of enum members of T.  
    /// </summary>
    internal class EnumCodeStyleOptionViewModel<T> : AbstractCodeStyleOptionViewModel
        where T : struct
    {
        private static readonly ImmutableArray<T> s_enumValues =
            ImmutableArray.Create((T[])Enum.GetValues(typeof(T)));

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
                    var index = Preferences.IndexOf(value);
                    var enumValue = s_enumValues[index];

                    Info.SetOptionAndUpdatePreview(
                        new CodeStyleOption<T>(
                            enumValue, _selectedNotificationPreference.Notification),
                        Option, GetPreview());
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
                    var index = Preferences.IndexOf(SelectedPreference);
                    var enumValue = s_enumValues[index];

                    Info.SetOptionAndUpdatePreview(
                        new CodeStyleOption<T>(
                            enumValue, _selectedNotificationPreference.Notification),
                        Option, GetPreview());
                }
            }
        }

        public override bool NotificationsAvailable => true;

        public override string GetPreview()
        {
            var index = Preferences.IndexOf(SelectedPreference);
            return Previews[index];
        }

        public EnumCodeStyleOptionViewModel(
            Option<CodeStyleOption<T>> option,
            string description,
            string[] previews,
            AbstractOptionPreviewViewModel info,
            OptionSet options,
            string groupName,
            List<CodeStylePreference> preferences)
            : base(option, description, previews, info, options, groupName, preferences)
        {
            var codeStyleOption = options.GetOption(option);

            var enumIndex = s_enumValues.IndexOf(codeStyleOption.Value);
            _selectedPreference = Preferences[enumIndex];

            var notificationViewModel = NotificationPreferences.Single(i => i.Notification.Value == codeStyleOption.Notification.Value);
            _selectedNotificationPreference = NotificationPreferences.Single(p => p.Notification.Value == notificationViewModel.Notification.Value);

            NotifyPropertyChanged(nameof(SelectedPreference));
            NotifyPropertyChanged(nameof(SelectedNotificationPreference));
        }
    }
}