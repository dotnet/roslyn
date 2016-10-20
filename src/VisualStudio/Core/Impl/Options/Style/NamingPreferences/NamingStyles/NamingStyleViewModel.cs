// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
        private NamingStyle _style;
        private readonly INotificationService _notificationService;

        public NamingStyleViewModel(NamingStyle style, bool canBeDeleted, INotificationService notificationService)
        {
            _notificationService = notificationService;
            _style = style;
            this.ID = style.ID;
            this.RequiredPrefix = style.Prefix;
            this.RequiredSuffix = style.Suffix;
            this.WordSeparator = style.WordSeparator;
            this.ItemName = style.Name;
            this.CanBeDeleted = canBeDeleted;

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

        public Guid ID { get; internal set; }

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
                return _style.CreateName(new[] { ServicesVSResources.example, ServicesVSResources.identifier });
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

        internal NamingStyle GetNamingStyle()
        {
            _style.Name = ItemName;
            _style.ID = ID;
            return _style;
        }

        public class CapitalizationDisplay
        {
            public Capitalization Capitalization { get; set; }
            public string Name { get; set; }

            public CapitalizationDisplay(Capitalization capitalization, string name)
            {
                this.Capitalization = capitalization;
                this.Name = name;
            }
        }
    }
}
