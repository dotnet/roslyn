// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.ComponentModel;
using System.Linq;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Imaging.Interop;
using Microsoft.VisualStudio.LanguageServices.Implementation.CommonControls;
using Microsoft.VisualStudio.LanguageServices.Implementation.PullMemberUp.MainDialog;
using Microsoft.VisualStudio.LanguageServices.Implementation.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.MoveMembersToType
{
    internal class MoveMembersToTypeDialogViewModel : AbstractNotifyPropertyChanged
    {

        public StaticMemberSelectionViewModel MemberSelectionViewModel { get; }

        private ISyntaxFacts _syntaxFacts;


        public MoveMembersToTypeDialogViewModel(
            StaticMemberSelectionViewModel memberSelectionViewModel,
            string defaultType,
            ImmutableArray<string> availableTypes,
            ISyntaxFacts syntaxFacts,
            ImmutableArray<string> typeHistory)
        {
            MemberSelectionViewModel = memberSelectionViewModel;
            _syntaxFacts = syntaxFacts ?? throw new ArgumentNullException(nameof(syntaxFacts));
            _destinationName = defaultType;
            AvailableTypes = typeHistory.Select(n => new TypeNameItem(true, n))
                .Concat(availableTypes.Except(typeHistory).Select(n => new TypeNameItem(false, n)))
                .ToImmutableArray();

            PropertyChanged += MoveMembersToTypeDialogViewModel_PropertyChanged;
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
            var isNewType = !AvailableTypes.Any(i => i.TypeName == DestinationName);
            _isValidName = !isNewType || IsValidType(DestinationName);

            if (isNewType && _isValidName)
            {
                Icon = KnownMonikers.StatusInformation;
                Message = "[WIP] A new type will be created";
                ShowMessage = true;
            }
            else if (!_isValidName)
            {
                Icon = KnownMonikers.StatusInvalid;
                Message = "[WIP] Invalid Type Name";
                ShowMessage = true;
            }
            else
            {
                ShowMessage = false;
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

        public ImmutableArray<TypeNameItem> AvailableTypes { get; }

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

    }
}
