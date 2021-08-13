// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.ComponentModel;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Imaging.Interop;
using Microsoft.VisualStudio.LanguageServices.Implementation.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.MoveStaticMembers
{
    internal class MoveStaticMembersDialogViewModel : AbstractNotifyPropertyChanged
    {
        public StaticMemberSelectionViewModel MemberSelectionViewModel { get; }

        private readonly ISyntaxFacts _syntaxFacts;

        private readonly ImmutableArray<string> _existingNames;

        private readonly string _prependedNamespace;

        public MoveStaticMembersDialogViewModel(
            StaticMemberSelectionViewModel memberSelectionViewModel,
            string defaultType,
            ImmutableArray<string> existingNames,
            string prependedNamespace,
            ISyntaxFacts syntaxFacts)
        {
            MemberSelectionViewModel = memberSelectionViewModel;
            _syntaxFacts = syntaxFacts ?? throw new ArgumentNullException(nameof(syntaxFacts));
            _destinationName = defaultType;
            _existingNames = existingNames;
            _prependedNamespace = prependedNamespace;
            _fullyQualifiedTypeName = string.Join(".", _prependedNamespace, _destinationName);

            PropertyChanged += MoveMembersToTypeDialogViewModel_PropertyChanged;
            OnDestinationUpdated();
        }

        private void MoveMembersToTypeDialogViewModel_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(DestinationName):
                    OnDestinationUpdated();
                    break;
            }
        }

        public void OnDestinationUpdated()
        {
            // TODO change once we allow movement to existing types
            FullyQualifiedTypeName = string.Join(".", _prependedNamespace, _destinationName);
            var isNewType = !_existingNames.Contains(FullyQualifiedTypeName);
            _isValidName = isNewType && IsValidType(FullyQualifiedTypeName);

            if (_isValidName)
            {
                Icon = KnownMonikers.StatusInformation;
                Message = string.Format(ServicesVSResources.New_type_name_colon_0, FullyQualifiedTypeName);
                ShowMessage = true;
            }
            else
            {
                Icon = KnownMonikers.StatusInvalid;
                Message = ServicesVSResources.Invalid_type_name;
                ShowMessage = true;
            }
        }

        private bool IsValidType(string typeName)
        {
            if (string.IsNullOrEmpty(typeName))
            {
                return false;
            }

            foreach (var identifier in typeName.Split('.'))
            {
                if (_syntaxFacts.IsValidIdentifier(identifier))
                {
                    continue;
                }

                return false;
            }

            return true;
        }

        private string _destinationName;
        public string DestinationName
        {
            get => _destinationName;
            set => SetProperty(ref _destinationName, value);
        }

        private ImageMoniker _icon;
        public ImageMoniker Icon
        {
            get => _icon;
            private set => SetProperty(ref _icon, value);
        }

        private string? _message;
        public string? Message
        {
            get => _message;
            private set => SetProperty(ref _message, value);
        }

        private bool _showMessage = false;
        public bool ShowMessage
        {
            get => _showMessage;
            private set => SetProperty(ref _showMessage, value);
        }

        private bool _isValidName = true;
        public bool CanSubmit
        {
            get => _isValidName && MemberSelectionViewModel.CheckedMembers.Length > 0;
        }

        private string _fullyQualifiedTypeName;
        public string FullyQualifiedTypeName
        {
            get => _fullyQualifiedTypeName;
            private set => SetProperty(ref _fullyQualifiedTypeName, value);
        }
    }
}
