﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
        static EnumCodeStyleOptionViewModel()
        {
            Contract.ThrowIfFalse(typeof(T).IsEnum);
        }

        private readonly ImmutableArray<T> _enumValues;
        private readonly ImmutableArray<string> _previews;

        private CodeStylePreference _selectedPreference;
        private NotificationOptionViewModel _selectedNotificationPreference;

        public EnumCodeStyleOptionViewModel(
            Option<CodeStyleOption<T>> option,
            string description,
            T[] enumValues,
            string[] previews,
            AbstractOptionPreviewViewModel info,
            OptionSet options,
            string groupName,
            List<CodeStylePreference> preferences)
            : base(option, description, info, options, groupName, preferences)
        {
            Debug.Assert(preferences.Count == enumValues.Length);
            Debug.Assert(previews.Length == enumValues.Length);

            var expectedEnumValues = Enum.GetValues(typeof(T));
            Debug.Assert(expectedEnumValues.Length == enumValues.Length, "Enum was updated, but UI wasn't.");

            _enumValues = enumValues.ToImmutableArray();
            _previews = previews.ToImmutableArray();

            var codeStyleOption = options.GetOption(option);

            var enumIndex = _enumValues.IndexOf(codeStyleOption.Value);
            _selectedPreference = Preferences[enumIndex];

            var notificationViewModel = NotificationPreferences.Single(i => i.Notification.Value == codeStyleOption.Notification.Value);
            _selectedNotificationPreference = NotificationPreferences.Single(p => p.Notification.Value == notificationViewModel.Notification.Value);

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
                        new CodeStyleOption<T>(
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
                        new CodeStyleOption<T>(
                            enumValue, _selectedNotificationPreference.Notification),
                        Option, GetPreview());
                }
            }
        }
    }
}
