// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.Options;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Options
{
    /// <summary>
    /// This class represents the view model for a <see cref="CodeStyleOption2{T}"/>
    /// that binds to the codestyle options UI.  Note that the T here is expected to be an enum
    /// type.  
    /// 
    /// Important.  The order of the previews and preferences provided should match the order
    /// of enum members of T.  
    /// </summary>
    internal class EnumCodeStyleOptionViewModel<T> : AbstractCodeStyleOptionViewModel
        where T : struct
    {
        static EnumCodeStyleOptionViewModel()
            => Contract.ThrowIfFalse(typeof(T).IsEnum);

        private readonly ImmutableArray<T> _enumValues;
        private readonly ImmutableArray<string> _previews;

        private CodeStylePreference _selectedPreference;
        private NotificationOptionViewModel _selectedNotificationPreference;

        public EnumCodeStyleOptionViewModel(
            IOption2 option,
            string description,
            T[] enumValues,
            string[] previews,
            AbstractOptionPreviewViewModel info,
            OptionStore optionStore,
            string groupName,
            List<CodeStylePreference> preferences)
            : base(option, description, info, groupName, preferences)
        {
            Debug.Assert(preferences.Count == enumValues.Length);
            Debug.Assert(previews.Length == enumValues.Length);

            _enumValues = enumValues.ToImmutableArray();
            _previews = previews.ToImmutableArray();

            var codeStyleOption = optionStore.GetOption<CodeStyleOption2<T>>(option, option.IsPerLanguage ? info.Language : null);

            var enumIndex = _enumValues.IndexOf(codeStyleOption.Value);
            if (enumIndex < 0 || enumIndex >= Preferences.Count)
            {
                enumIndex = 0;
            }

            _selectedPreference = Preferences[enumIndex];

            var notificationViewModel = NotificationPreferences.Single(i => i.Notification.Severity == codeStyleOption.Notification.Severity);
            _selectedNotificationPreference = NotificationPreferences.Single(p => p.Notification.Severity == notificationViewModel.Notification.Severity);

            NotifyPropertyChanged(nameof(SelectedPreference));
            NotifyPropertyChanged(nameof(SelectedNotificationPreference));
        }

        public override string GetPreview()
        {
            var index = Preferences.IndexOf(SelectedPreference);
            return _previews[index];
        }

        public override CodeStylePreference SelectedPreference
        {
            get => _selectedPreference;

            set
            {
                if (SetProperty(ref _selectedPreference, value))
                {
                    var index = Preferences.IndexOf(value);
                    var enumValue = _enumValues[index];

                    Info.SetOptionAndUpdatePreview(
                        new CodeStyleOption2<T>(
                            enumValue, _selectedNotificationPreference.Notification),
                        Option, GetPreview());
                }
            }
        }

        public override NotificationOptionViewModel SelectedNotificationPreference
        {
            get => _selectedNotificationPreference;

            set
            {
                if (SetProperty(ref _selectedNotificationPreference, value))
                {
                    var index = Preferences.IndexOf(SelectedPreference);
                    var enumValue = _enumValues[index];

                    Info.SetOptionAndUpdatePreview(
                        new CodeStyleOption2<T>(
                            enumValue, _selectedNotificationPreference.Notification),
                        Option, GetPreview());
                }
            }
        }
    }
}
