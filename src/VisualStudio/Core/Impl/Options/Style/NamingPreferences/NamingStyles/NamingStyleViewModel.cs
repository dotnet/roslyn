// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.Diagnostics.Analyzers.NamingStyles;
using Microsoft.CodeAnalysis.Notification;
using Microsoft.VisualStudio.LanguageServices.Implementation.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Options.Style.NamingPreferences
{
    internal class NamingStyleViewModel : AbstractNotifyPropertyChanged, INamingStylesInfoDialogViewModel
    {
        private readonly MutableNamingStyle _style;
        private readonly INotificationService _notificationService;

        public NamingStyleViewModel(MutableNamingStyle style, bool canBeDeleted, INotificationService notificationService)
        {
            _notificationService = notificationService;
            _style = style;
            ID = style.ID;
            RequiredPrefix = style.Prefix;
            RequiredSuffix = style.Suffix;
            WordSeparator = style.WordSeparator;
            ItemName = style.Name;
            CanBeDeleted = canBeDeleted;

            CapitalizationSchemes = new List<CapitalizationDisplay>
                {
                    new CapitalizationDisplay(Capitalization.PascalCase, ServicesVSResources.Pascal_Case_Name),
                    new CapitalizationDisplay(Capitalization.CamelCase, ServicesVSResources.camel_Case_Name),
                    new CapitalizationDisplay(Capitalization.FirstUpper, ServicesVSResources.First_word_upper),
                    new CapitalizationDisplay(Capitalization.AllUpper, ServicesVSResources.ALL_UPPER),
                    new CapitalizationDisplay(Capitalization.AllLower, ServicesVSResources.all_lower)
                };

            CapitalizationSchemeIndex = CapitalizationSchemes.IndexOf(CapitalizationSchemes.Single(s => s.Capitalization == style.CapitalizationScheme));
        }

        public IList<CapitalizationDisplay> CapitalizationSchemes { get; set; }

        private int _capitalizationSchemeIndex;
        public int CapitalizationSchemeIndex
        {
            get
            {
                return _capitalizationSchemeIndex;
            }
            set
            {
                _style.CapitalizationScheme = CapitalizationSchemes[value].Capitalization;
                if (SetProperty(ref _capitalizationSchemeIndex, value))
                {
                    NotifyPropertyChanged(nameof(CurrentConfiguration));
                }
            }
        }

        public Guid ID { get; }

        private string _itemName;
        public string ItemName
        {
            get { return _itemName; }
            set { SetProperty(ref _itemName, value); }
        }

        public string CurrentConfiguration
        {
            get
            {
                return _style.NamingStyle.CreateName([ServicesVSResources.example, ServicesVSResources.identifier]);
            }
            set
            {
            }
        }

        private string _requiredPrefix;

        public string RequiredPrefix
        {
            get
            {
                return _requiredPrefix;
            }
            set
            {
                _style.Prefix = value;
                if (SetProperty(ref _requiredPrefix, value))
                {
                    NotifyPropertyChanged(nameof(CurrentConfiguration));
                }
            }
        }

        private string _requiredSuffix;
        public string RequiredSuffix
        {
            get
            {
                return _requiredSuffix;
            }
            set
            {
                _style.Suffix = value;
                if (SetProperty(ref _requiredSuffix, value))
                {
                    NotifyPropertyChanged(nameof(CurrentConfiguration));
                }
            }
        }

        private string _wordSeparator;
        public string WordSeparator
        {
            get
            {
                return _wordSeparator;
            }
            set
            {
                _style.WordSeparator = value;
                if (SetProperty(ref _wordSeparator, value))
                {
                    NotifyPropertyChanged(nameof(CurrentConfiguration));
                }
            }
        }

        public bool CanBeDeleted { get; set; }

        internal bool TrySubmit()
        {
            if (string.IsNullOrWhiteSpace(ItemName))
            {
                _notificationService.SendNotification(ServicesVSResources.Enter_a_title_for_this_Naming_Style);
                return false;
            }

            return true;
        }

        internal MutableNamingStyle GetNamingStyle()
        {
            _style.Name = ItemName;
            return _style;
        }

        // For screen readers
        public override string ToString() => ItemName;

        public class CapitalizationDisplay
        {
            public Capitalization Capitalization { get; set; }
            public string Name { get; set; }

            public CapitalizationDisplay(Capitalization capitalization, string name)
            {
                Capitalization = capitalization;
                Name = name;
            }

            // For screen readers
            public override string ToString()
                => Name;
        }
    }
}
