﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Options
{
    /// <summary>
    /// This view model binds to a code style option UI that
    /// has a checkbox for selection and a combobox for notification levels.
    /// </summary>
    /// <remarks>
    /// At the features level, this maps to <see cref="CodeStyleOption{T}"/>
    /// </remarks>
    internal class CheckBoxWithComboOptionViewModel : AbstractCheckBoxViewModel
    {
        private NotificationOptionViewModel _selectedNotificationOption;

        public IList<NotificationOptionViewModel> NotificationOptions { get; }

        public CheckBoxWithComboOptionViewModel(IOption option, string description, string preview, AbstractOptionPreviewViewModel info, OptionSet options, IList<NotificationOptionViewModel> items)
            : this(option, description, preview, preview, info, options, items)
        {
        }

        public CheckBoxWithComboOptionViewModel(IOption option, string description, string truePreview, string falsePreview, AbstractOptionPreviewViewModel info, OptionSet options, IList<NotificationOptionViewModel> items)
            : base(option, description, truePreview, falsePreview, info)
        {
            NotificationOptions = items;

            var codeStyleOption = ((CodeStyleOption<bool>)options.GetOption(new OptionKey(option, option.IsPerLanguage ? info.Language : null)));
            SetProperty(ref _isChecked, codeStyleOption.Value);

            var notificationViewModel = items.Where(i => i.Notification.Severity == codeStyleOption.Notification.Severity).Single();
            SetProperty(ref _selectedNotificationOption, notificationViewModel);
        }

        public override bool IsChecked
        {
            get
            {
                return _isChecked;
            }

            set
            {
                SetProperty(ref _isChecked, value);
                Info.SetOptionAndUpdatePreview(new CodeStyleOption<bool>(_isChecked, _selectedNotificationOption.Notification), Option, GetPreview());
            }
        }

        public NotificationOptionViewModel SelectedNotificationOption
        {
            get
            {
                return _selectedNotificationOption;
            }
            set
            {
                SetProperty(ref _selectedNotificationOption, value);
                Info.SetOptionAndUpdatePreview(new CodeStyleOption<bool>(_isChecked, _selectedNotificationOption.Notification), Option, GetPreview());
            }
        }
    }
}
