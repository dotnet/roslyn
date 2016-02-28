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
    internal abstract class AbstractCodeStyleOptionViewModel : AbstractNotifyPropertyChanged
    {
        // not data binding
        private readonly string _truePreview;
        private readonly string _falsePreview;
        protected AbstractOptionPreviewViewModel Info { get; }
        public IOption Option { get; }

        // data binding

        // this property is temporarily required because not all code styles implement notification preferences.
        public abstract bool NotificationsAvailable { get; }
        public abstract CodeStylePreference SelectedPreference { get; set; }
        public abstract NotificationOptionViewModel SelectedNotificationPreference { get; set; }
        public string Description { get; set; }
        public double DescriptionMargin { get; set; }
        public string GroupName { get; set; }

        public List<CodeStylePreference> Preferences { get; set; }
        public List<NotificationOptionViewModel> NotificationPreferences { get; set; }

        public virtual string GetPreview() => SelectedPreference.IsChecked? _truePreview : _falsePreview;

        public AbstractCodeStyleOptionViewModel(
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
            DescriptionMargin = 12d;
            GroupName = groupName;
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
