// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.Options;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.LanguageServices.Implementation.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Options
{
    // I'm a row in this table.
    internal class SimpleCodeStyleOptionViewModel : AbstractNotifyPropertyChanged
    {
        private readonly string _truePreview;
        private readonly string _falsePreview;

        // data binding
        public string Description { get; set; }
        public double DescriptionMargin { get; set; }
        public bool IsVisible { get; set; }
        public string GroupName { get; set; }

        public List<CodeStylePreference> Preferences { get; set; }

        private CodeStylePreference _selectedPreference;
        public CodeStylePreference SelectedPreference
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

        public List<NotificationOptionViewModel> NotificationPreferences { get; set; }

        private NotificationOptionViewModel _selectedNotificationPreference;
        public NotificationOptionViewModel SelectedNotificationPreference
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

        // not data binding
        protected AbstractOptionPreviewViewModel Info { get; }
        public IOption Option { get; }

        //public static SimpleCodeStyleOptionViewModel Header(string text)
        //{
        //    return new SimpleCodeStyleOptionViewModel(text);
        //}

        internal virtual string GetPreview() => _selectedPreference.IsChecked ? _truePreview : _falsePreview;

        //public SimpleCodeStyleOptionViewModel(IOption option, string description, string preview, AbstractOptionPreviewViewModel info, OptionSet options)
        //    : this(option, description, preview, preview, info, options)
        //{
        //}

        //private SimpleCodeStyleOptionViewModel(string header)
        //{
        //    Description = header;
        //    Preferences = GetDefaultPreferences();
        //    NotificationPreferences = GetDefaultNotifications();
        //    _selectedPreference = null;
        //    _selectedNotificationPreference = null;
        //    IsVisible = false;
        //    DescriptionMargin = default(double);
        //}

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
        {
            _truePreview = truePreview;
            _falsePreview = falsePreview;

            Info = info;
            Option = option;
            Description = description;
            Preferences = preferences ?? GetDefaultPreferences();
            NotificationPreferences = notificationPreferences ?? GetDefaultNotifications();
            IsVisible = true;
            DescriptionMargin = 12d;
            GroupName = groupName;

            var codeStyleOption = ((SimpleCodeStyleOption)options.GetOption(new OptionKey(option, option.IsPerLanguage ? info.Language : null)));
            _selectedPreference = Preferences.Single(c => c.IsChecked == codeStyleOption.IsChecked);

            var notificationViewModel = NotificationPreferences.Single(i => i.Notification.Value == codeStyleOption.Notification.Value);
            _selectedNotificationPreference = NotificationPreferences.Single(p => p.Notification.Value == notificationViewModel.Notification.Value);

            NotifyPropertyChanged(nameof(SelectedPreference));
            NotifyPropertyChanged(nameof(SelectedNotificationPreference));
        }

        private static List<NotificationOptionViewModel> GetDefaultNotifications()
        {
            return new List<NotificationOptionViewModel>
            {
                new NotificationOptionViewModel(NotificationOption.None, KnownMonikers.None),
                new NotificationOptionViewModel(NotificationOption.Info, KnownMonikers.StatusInformation),
                new NotificationOptionViewModel(NotificationOption.Warning, KnownMonikers.StatusWarning),
                new NotificationOptionViewModel(NotificationOption.Error, KnownMonikers.StatusError)
            };
        }

        private static List<CodeStylePreference> GetDefaultPreferences()
        {
            return new List<CodeStylePreference>
            {
                // TODO: move to resx for loc.
                new CodeStylePreference("Yes", true),
                new CodeStylePreference("No", false),
            };
        }
    }
}
