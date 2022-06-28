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
            PrependedNamespace = string.IsNullOrEmpty(prependedNamespace) ? prependedNamespace : prependedNamespace + ".";

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
            var fullyQualifiedTypeName = PrependedNamespace + DestinationName;
            var isNewType = !_existingNames.Contains(fullyQualifiedTypeName);
            CanSubmit = isNewType && IsValidType(fullyQualifiedTypeName);

            if (CanSubmit)
            {
                Icon = KnownMonikers.StatusInformation;
                Message = ServicesVSResources.New_Type_Name_colon;
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

        public string PrependedNamespace { get; }

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

        private bool _canSubmit = true;
        public bool CanSubmit
        {
            get => _canSubmit;
            set => SetProperty(ref _canSubmit, value);
        }
    }
}
