// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Media;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Notification;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.LanguageServices.Implementation.Utilities;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.ExtractInterface
{
    internal class ExtractInterfaceDialogViewModel : AbstractNotifyPropertyChanged
    {
        private readonly ISyntaxFactsService _syntaxFactsService;
        private readonly INotificationService _notificationService;
        private readonly List<string> _conflictingTypeNames;
        private readonly string _defaultNamespace;
        private readonly string _generatedNameTypeParameterSuffix;
        private readonly string _languageName;
        private readonly string _fileExtension;

        internal ExtractInterfaceDialogViewModel(
            ISyntaxFactsService syntaxFactsService,
            IGlyphService glyphService,
            INotificationService notificationService,
            string defaultInterfaceName,
            List<ISymbol> extractableMembers,
            List<string> conflictingTypeNames,
            string defaultNamespace,
            string generatedNameTypeParameterSuffix,
            string languageName,
            string fileExtension)
        {
            _syntaxFactsService = syntaxFactsService;
            _notificationService = notificationService;
            _interfaceName = defaultInterfaceName;
            _conflictingTypeNames = conflictingTypeNames;
            _fileExtension = fileExtension;
            _fileName = string.Format("{0}{1}", defaultInterfaceName, fileExtension);
            _defaultNamespace = defaultNamespace;
            _generatedNameTypeParameterSuffix = generatedNameTypeParameterSuffix;
            _languageName = languageName;

            MemberContainers = extractableMembers.Select(m => new MemberSymbolViewModel(m, glyphService)).OrderBy(s => s.MemberName).ToList();
        }

        internal bool TrySubmit()
        {
            var trimmedInterfaceName = InterfaceName.Trim();
            var trimmedFileName = FileName.Trim();

            if (!MemberContainers.Any(c => c.IsChecked))
            {
                SendFailureNotification(ServicesVSResources.YouMustSelectAtLeastOneMember);
                return false;
            }

            if (_conflictingTypeNames.Contains(trimmedInterfaceName))
            {
                SendFailureNotification(ServicesVSResources.InterfaceNameConflictsWithTypeName);
                return false;
            }

            if (!_syntaxFactsService.IsValidIdentifier(trimmedInterfaceName))
            {
                SendFailureNotification(string.Format(ServicesVSResources.InterfaceNameIsNotAValidIdentifier, _languageName));
                return false;
            }

            if (trimmedFileName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
            {
                SendFailureNotification(ServicesVSResources.IllegalCharactersInPath);
                return false;
            }

            if (!System.IO.Path.GetExtension(trimmedFileName).Equals(_fileExtension, StringComparison.OrdinalIgnoreCase))
            {
                SendFailureNotification(string.Format(ServicesVSResources.FileNameMustHaveTheExtension, _fileExtension));
                return false;
            }

            // TODO: Deal with filename already existing

            return true;
        }

        private void SendFailureNotification(string message)
        {
            _notificationService.SendNotification(message, severity: NotificationSeverity.Information);
        }

        internal void DeselectAll()
        {
            foreach (var memberContainer in MemberContainers)
            {
                memberContainer.IsChecked = false;
            }
        }

        internal void SelectAll()
        {
            foreach (var memberContainer in MemberContainers)
            {
                memberContainer.IsChecked = true;
            }
        }

        public List<MemberSymbolViewModel> MemberContainers { get; set; }

        private string _interfaceName;
        public string InterfaceName
        {
            get
            {
                return _interfaceName;
            }

            set
            {
                if (SetProperty(ref _interfaceName, value))
                {
                    FileName = string.Format("{0}{1}", value.Trim(), _fileExtension);
                    NotifyPropertyChanged("GeneratedName");
                }
            }
        }

        public string GeneratedName
        {
            get
            {
                return string.Format(
                    "{0}{1}{2}",
                    string.IsNullOrEmpty(_defaultNamespace) ? string.Empty : _defaultNamespace + ".",
                    _interfaceName.Trim(),
                    _generatedNameTypeParameterSuffix);
            }
        }

        private string _fileName;
        public string FileName
        {
            get { return _fileName; }
            set { SetProperty(ref _fileName, value); }
        }

        internal class MemberSymbolViewModel : AbstractNotifyPropertyChanged
        {
            private readonly IGlyphService _glyphService;

            public ISymbol MemberSymbol { get; }

            private static SymbolDisplayFormat s_memberDisplayFormat = new SymbolDisplayFormat(
                genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters,
                memberOptions: SymbolDisplayMemberOptions.IncludeParameters,
                parameterOptions: SymbolDisplayParameterOptions.IncludeType | SymbolDisplayParameterOptions.IncludeParamsRefOut | SymbolDisplayParameterOptions.IncludeOptionalBrackets,
                miscellaneousOptions: SymbolDisplayMiscellaneousOptions.EscapeKeywordIdentifiers | SymbolDisplayMiscellaneousOptions.UseSpecialTypes);

            public MemberSymbolViewModel(ISymbol symbol, IGlyphService glyphService)
            {
                this.MemberSymbol = symbol;
                _glyphService = glyphService;
                _isChecked = true;
            }

            private bool _isChecked;
            public bool IsChecked
            {
                get { return _isChecked; }
                set { SetProperty(ref _isChecked, value); }
            }

            public string MemberName
            {
                get { return MemberSymbol.ToDisplayString(s_memberDisplayFormat); }
            }

            public ImageSource Glyph
            {
                get { return MemberSymbol.GetGlyph().GetImageSource(_glyphService); }
            }
        }
    }
}
