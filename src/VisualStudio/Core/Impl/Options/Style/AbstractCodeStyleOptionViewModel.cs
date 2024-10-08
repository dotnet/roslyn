// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.Options;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.LanguageServices.Implementation.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Options
{
    /// <summary>
    /// This class acts as a base for any view model that  binds to the codestyle options UI.
    /// </summary>
    /// <remarks>
    /// This supports databinding of: 
    /// Description
    /// list of CodeStyle preferences
    /// list of Notification preferences 
    /// selected code style preference
    /// selected notification preference
    /// plus, styling for visual elements.
    /// </remarks>
    internal abstract class AbstractCodeStyleOptionViewModel : AbstractNotifyPropertyChanged
    {
        protected AbstractOptionPreviewViewModel Info { get; }
        public IOption2 Option { get; }

        public string Description { get; set; }
        public double DescriptionMargin { get; set; } = 12d;
        public string GroupName { get; set; }
        public string GroupNameAndDescription { get; set; }
        public List<CodeStylePreference> Preferences { get; set; }
        public List<NotificationOptionViewModel> NotificationPreferences { get; set; }

        public abstract CodeStylePreference SelectedPreference { get; set; }
        public abstract string GetPreview();

        public virtual NotificationOptionViewModel SelectedNotificationPreference
        {
            get { return NotificationPreferences.First(); }
            set { }
        }

        public AbstractCodeStyleOptionViewModel(
            IOption2 option,
            string description,
            AbstractOptionPreviewViewModel info,
            string groupName,
            List<CodeStylePreference> preferences = null,
            List<NotificationOptionViewModel> notificationPreferences = null)
        {
            Info = info;
            Option = option;
            Description = description;
            Preferences = preferences ?? GetDefaultPreferences();
            NotificationPreferences = notificationPreferences ?? GetDefaultNotifications();
            GroupName = groupName;
            GroupNameAndDescription = $"{groupName}, {description}";
        }

        private static List<NotificationOptionViewModel> GetDefaultNotifications()
        {
            return
            [
                new NotificationOptionViewModel(NotificationOption2.Silent, KnownMonikers.None),
                new NotificationOptionViewModel(NotificationOption2.Suggestion, KnownMonikers.StatusInformation),
                new NotificationOptionViewModel(NotificationOption2.Warning, KnownMonikers.StatusWarning),
                new NotificationOptionViewModel(NotificationOption2.Error, KnownMonikers.StatusError)
            ];
        }

        private static List<CodeStylePreference> GetDefaultPreferences()
        {
            return
            [
                new CodeStylePreference(ServicesVSResources.Yes, isChecked: true),
                new CodeStylePreference(ServicesVSResources.No, isChecked: false),
            ];
        }
    }
}
