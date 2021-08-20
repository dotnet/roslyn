// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.ComponentModel;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Imaging.Interop;
using Microsoft.VisualStudio.LanguageServices.Implementation.MoveStaticMembers;
using Microsoft.VisualStudio.LanguageServices.Implementation.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.MoveStaticMembers
{
    internal class MoveStaticMembersDialogViewModel : AbstractNotifyPropertyChanged
    {
        public StaticMemberSelectionViewModel MemberSelectionViewModel { get; }

        private readonly ISyntaxFacts _syntaxFacts;

        private readonly string _sourceTypeName;

        public MoveStaticMembersDialogViewModel(
            StaticMemberSelectionViewModel memberSelectionViewModel,
            string defaultType,
            ImmutableArray<TypeNameItem> availableTypes,
            string sourceTypeName,
            ISyntaxFacts syntaxFacts)
        {
            MemberSelectionViewModel = memberSelectionViewModel;
            _syntaxFacts = syntaxFacts ?? throw new ArgumentNullException(nameof(syntaxFacts));
            _searchText = defaultType;
            AvailableTypes = availableTypes;
            _destinationName = new TypeNameItem(defaultType);
            _sourceTypeName = sourceTypeName;

            PropertyChanged += MoveMembersToTypeDialogViewModel_PropertyChanged;
            // Set message and icon + shownTypes
            OnSearchTextUpdated();
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
                case nameof(SelectedIndex):
                    OnSelectedIdnexUpdated();
                    break;
            }
        }

        private void OnSelectedIdnexUpdated()
        {
            // User changed their selection in the dropdown
            if (SelectedIndex == -1)
            {
                // removed selection, search text will handle the dropdown
                // even if the text hasn't changed
                OnSearchTextUpdated();
            }
            else
            {
                DestinationName = ShownTypes[SelectedIndex];
            }
        }

        private void OnSearchTextUpdated()
        {
            if (SelectedIndex != -1 && SearchText == ShownTypes[SelectedIndex].TypeName)
            {
                DestinationName = ShownTypes[SelectedIndex];
            }
            else
            {
                // Search text is not selecting anything, create a destination for the
                // contents of the text.
                DestinationName = new TypeNameItem(SearchText);
                ShownTypes = AvailableTypes.WhereAsArray(t => t.TypeName.Contains(SearchText));
                if (_isValidName)
                {
                    ShownTypes = ShownTypes.Insert(0, DestinationName);
                }
            }
        }

        private void OnDestinationUpdated()
        {
            var isNewType = DestinationName.IsNew;
            _isValidName = !isNewType || IsValidType(DestinationName!.TypeName);

            if (_isValidName && isNewType)
            {
                Icon = KnownMonikers.StatusInformation;
                Message = ServicesVSResources.A_new_type_will_be_created;
                ShowMessage = true;
            }
            else if (!_isValidName)
            {
                Icon = KnownMonikers.StatusInvalid;
                Message = ServicesVSResources.Invalid_type_name;
                ShowMessage = true;
            }
            else
            {
                ShowMessage = false;
            }
        }

        private bool IsValidType(string typeName)
        {
            if (string.IsNullOrEmpty(typeName) ||typeName == _sourceTypeName)
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

        private int _selectedIndex = -1;
        public int SelectedIndex
        {
            get => _selectedIndex;
            set => SetProperty(ref _selectedIndex, value);
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
