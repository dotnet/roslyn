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
            ImmutableArray<TypeNameItem> availableTypes,
            ISyntaxFacts syntaxFacts)
        {
            MemberSelectionViewModel = memberSelectionViewModel;
            _syntaxFacts = syntaxFacts ?? throw new ArgumentNullException(nameof(syntaxFacts));
            _searchText = defaultType;
            AvailableTypes = availableTypes;
            _destinationName = new TypeNameItem(defaultType);

            PropertyChanged += MoveMembersToTypeDialogViewModel_PropertyChanged;
            // Set message and icon + shownTypes
            OnDestinationUpdated();
        }

        private void MoveMembersToTypeDialogViewModel_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(DestinationName):
                    OnDestinationUpdated();
                    break;
                case nameof(SearchText):
                    OnSearchTextUpdated();
                    break;
            }
        }

        public void OnSearchTextUpdated()
        {
            DestinationName = new TypeNameItem(SearchText);
        }

        public void OnDestinationUpdated()
        {
            // if they deselect an element, the destination will be set to null
            // But it will be set again from searchText update
            if (DestinationName is null)
            {
                return;
            }
            var isNewType = DestinationName.IsNew;
            _isValidName = !isNewType || IsValidType(DestinationName!.TypeName);

            if (_isValidName)
            {
                Icon = KnownMonikers.StatusInformation;
                Message = ServicesVSResources.A_new_type_will_be_created;
                ShowMessage = true;
                ShownTypes = AvailableTypes
                    .Insert(0, DestinationName!)
                    .Where(t => t.TypeName.Contains(SearchText))
                    .ToImmutableArray();
            }
            else
            {
                Icon = KnownMonikers.StatusInvalid;
                Message = ServicesVSResources.Invalid_type_name;
                ShowMessage = true;
                ShownTypes = ImmutableArray.Create<TypeNameItem>();
            }
            else
            {
                ShowMessage = false;
                ShownTypes = AvailableTypes
                    .Where(t => t.TypeName.Contains(DestinationName!.TypeName))
                    .ToImmutableArray();
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

        private TypeNameItem _destinationName;
        public TypeNameItem DestinationName
        {
            get => _destinationName;
            set => SetProperty(ref _destinationName, value);
        }

        private string _searchText;
        public string SearchText
        {
            get => _searchText;
            set => SetProperty(ref _searchText, value);
        }

        private ImmutableArray<TypeNameItem> _shownTypes;
        public ImmutableArray<TypeNameItem> ShownTypes
        {
            get => _shownTypes;
            private set
            {
                SetProperty(ref _shownTypes, value);
            }
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
