// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.ComponentModel;
using System.Linq;
using Microsoft.CodeAnalysis;
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

        private readonly string _sourceTypeName;

        public MoveStaticMembersDialogViewModel(
            StaticMemberSelectionViewModel memberSelectionViewModel,
            string defaultType,
            ImmutableArray<TypeNameItem> availableTypes,
            string sourceTypeName,
            string containingNamespace,
            ISyntaxFacts syntaxFacts)
        {
            MemberSelectionViewModel = memberSelectionViewModel;
            _syntaxFacts = syntaxFacts ?? throw new ArgumentNullException(nameof(syntaxFacts));
            _searchText = defaultType;
            AvailableTypes = availableTypes;
            _destinationName = new TypeNameItem(defaultType);
            _sourceTypeName = sourceTypeName;
            _prependedNamespace = containingNamespace + ".";

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
                case nameof(SelectedIndex):
                case nameof(SearchText):
                    OnSearchTextOrIndexUpdated();
                    break;
            }
        }

        private void OnSearchTextOrIndexUpdated()
        {
            // we either typed something or changed the selection
            if (SelectedIndex != -1 && SearchText == AvailableTypes[SelectedIndex].TypeName)
            {
                // Search text is not selecting anything, create a destination for the
                // contents of the text.
                DestinationName = AvailableTypes[SelectedIndex];
            }
            else
            {
                DestinationName = new TypeNameItem(SearchText);
            }
        }

        private void OnDestinationUpdated()
        {
            var isNewType = DestinationName.IsNew;
            CanSubmit = !isNewType || IsValidType(DestinationName!.TypeName);

            if (CanSubmit && isNewType)
            {
                Icon = KnownMonikers.StatusInformation;
                Message = ServicesVSResources.New_Type_Name_colon;
                ShowMessage = true;
            }
            else if (!CanSubmit)
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
            if (string.IsNullOrEmpty(typeName) ||
                typeName == _sourceTypeName ||
                AvailableTypes.Any(t => t.TypeName == (PrependedNamespace + typeName)))
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

        private string _prependedNamespace;
        public string PrependedNamespace
        {
            get => _prependedNamespace;
            set => SetProperty(ref _prependedNamespace, value);
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

        private bool _canSubmit = true;
        public bool CanSubmit
        {
            get => _canSubmit;
            set => SetProperty(ref _canSubmit, value);
        }
    }
}
