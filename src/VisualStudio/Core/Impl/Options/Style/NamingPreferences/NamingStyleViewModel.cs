// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.Diagnostics.Analyzers.NamingStyles;
using Microsoft.CodeAnalysis.Notification;
using Microsoft.VisualStudio.LanguageServices.Implementation.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Options.Style.NamingPreferences
{
    internal class NamingStyleViewModel : AbstractNotifyPropertyChanged
    {
        private NamingStyle _style;

        public NamingStyleViewModel(NamingStyle style, INotificationService notificationService)
        {
            _notificationService = notificationService;
            _style = style;
            this.ID = style.ID;
            this.RequiredPrefix = style.Prefix;
            this.RequiredSuffix = style.Suffix;
            this.WordSeparator = style.WordSeparator;
            this.FirstWordGroupCapitalization = (int)style.CapitalizationScheme;
            this.NamingConventionName = style.Name;

            CapitalizationSchemes = new List<CapitalizationDisplay>
                {
                    new CapitalizationDisplay(Capitalization.PascalCase, ServicesVSResources.CapitalizationStyleExample_PascalCase),
                    new CapitalizationDisplay(Capitalization.CamelCase, ServicesVSResources.CapitalizationStyleExample_CamelCase),
                    new CapitalizationDisplay(Capitalization.FirstUpper, ServicesVSResources.CapitalizationStyleExample_FirstWordUpper),
                    new CapitalizationDisplay(Capitalization.AllUpper, ServicesVSResources.CapitalizationStyleExample_AllUpper),
                    new CapitalizationDisplay(Capitalization.AllLower, ServicesVSResources.CapitalizationStyleExample_AllLower)
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

        private string _namingConventionName;
        public string NamingConventionName
        {
            get { return _namingConventionName; }
            set { SetProperty(ref _namingConventionName, value); }
        }

        public string CurrentConfiguration
        {
            get
            {
                return _style.CreateName(new[] { ServicesVSResources.IdentifierWord_Example, ServicesVSResources.IdentifierWord_Identifier });
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

        private int _firstWordGroupCapitalization;
        internal readonly INotificationService _notificationService;

        public int FirstWordGroupCapitalization
        {
            get
            {
                return _firstWordGroupCapitalization;
            }
            set
            {
                _style.CapitalizationScheme = (Capitalization)value;
                if (SetProperty(ref _firstWordGroupCapitalization, value))
                {
                    NotifyPropertyChanged(nameof(CurrentConfiguration));
                }
            }
        }

        internal bool TrySubmit()
        {
            if (string.IsNullOrWhiteSpace(NamingConventionName))
            {
                _notificationService.SendNotification(ServicesVSResources.EnterATitleForThisNamingStyle);
                return false;
            }

            return true;
        }

        internal NamingStyle GetNamingStyle()
        {
            _style.Name = NamingConventionName;
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
