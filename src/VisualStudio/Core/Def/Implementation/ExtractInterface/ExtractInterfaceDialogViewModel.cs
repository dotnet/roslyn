﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Notification;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.LanguageServices.Implementation.Utilities;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.ExtractInterface
{
    internal enum InterfaceDestination
    {
        CurrentFile,
        NewFile
    };

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

            MemberContainers = extractableMembers.Select(m => new MemberSymbolViewModel(m, glyphService)).OrderBy(s => s.SymbolName).ToList();
        }

        internal bool TrySubmit()
        {
            var trimmedInterfaceName = InterfaceName.Trim();
            var trimmedFileName = FileName.Trim();

            if (!MemberContainers.Any(c => c.IsChecked))
            {
                SendFailureNotification(ServicesVSResources.You_must_select_at_least_one_member);
                return false;
            }

            if (_conflictingTypeNames.Contains(trimmedInterfaceName))
            {
                SendFailureNotification(ServicesVSResources.Interface_name_conflicts_with_an_existing_type_name);
                return false;
            }

            if (!_syntaxFactsService.IsValidIdentifier(trimmedInterfaceName))
            {
                SendFailureNotification(string.Format(ServicesVSResources.Interface_name_is_not_a_valid_0_identifier, _languageName));
                return false;
            }

            if (trimmedFileName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
            {
                SendFailureNotification(ServicesVSResources.Illegal_characters_in_path);
                return false;
            }

            if (!System.IO.Path.GetExtension(trimmedFileName).Equals(_fileExtension, StringComparison.OrdinalIgnoreCase))
            {
                SendFailureNotification(string.Format(ServicesVSResources.File_name_must_have_the_0_extension, _fileExtension));
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
                    NotifyPropertyChanged(nameof(GeneratedName));
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

        private InterfaceDestination _destination = InterfaceDestination.NewFile;
        public InterfaceDestination Destination
        {
            get { return _destination; }
            set
            {
                if (SetProperty(ref _destination, value))
                {
                    NotifyPropertyChanged(nameof(FileNameEnabled));
                }
            }
        }

        public bool FileNameEnabled => Destination == InterfaceDestination.NewFile;

        internal class MemberSymbolViewModel : SymbolViewModel<ISymbol>
        {
            public MemberSymbolViewModel(ISymbol symbol, IGlyphService glyphService) : base(symbol, glyphService)
            {
            }
        }
    }
}
