// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.ComponentModel;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Imaging.Interop;
using Microsoft.VisualStudio.LanguageServices.Implementation.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.MoveStaticMembers;

internal sealed class MoveStaticMembersDialogViewModel : AbstractNotifyPropertyChanged
{
    public StaticMemberSelectionViewModel MemberSelectionViewModel { get; }

    private readonly ISyntaxFacts _syntaxFacts;
    private readonly string _prependedNamespace;

    public MoveStaticMembersDialogViewModel(
        StaticMemberSelectionViewModel memberSelectionViewModel,
        string defaultType,
        ImmutableArray<TypeNameItem> availableTypes,
        string prependedNamespace,
        ISyntaxFacts syntaxFacts)
    {
        MemberSelectionViewModel = memberSelectionViewModel;
        _syntaxFacts = syntaxFacts ?? throw new ArgumentNullException(nameof(syntaxFacts));
        _searchText = defaultType;
        _prependedNamespace = string.IsNullOrEmpty(prependedNamespace) ? prependedNamespace : prependedNamespace + ".";

        _destinationName = new TypeNameItem(_prependedNamespace + defaultType);
        AvailableTypes = availableTypes;

        PropertyChanged += MoveMembersToTypeDialogViewModel_PropertyChanged;
        OnDestinationUpdated();
    }

    public string TypeName_NamespaceOnly
    {
        get
        {
            var lastDot = _destinationName.FullyQualifiedTypeName.LastIndexOf('.');
            return lastDot >= 0 ? _destinationName.FullyQualifiedTypeName[0..(lastDot + 1)] : "";
        }
    }

    public string TypeName_NameOnly
    {
        get
        {
            var lastDot = _destinationName.FullyQualifiedTypeName.LastIndexOf('.');
            return lastDot >= 0 ? _destinationName.FullyQualifiedTypeName[(lastDot + 1)..] : _destinationName.FullyQualifiedTypeName;
        }
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

    private void OnSearchTextUpdated()
    {
        var foundItem = AvailableTypes.FirstOrDefault(t => t.FullyQualifiedTypeName == SearchText);
        if (foundItem is null)
        {
            DestinationName = new(_prependedNamespace + SearchText);
        }
        else
        {
            DestinationName = foundItem;
        }

        NotifyPropertyChanged(nameof(TypeName_NameOnly));
        NotifyPropertyChanged(nameof(TypeName_NamespaceOnly));
    }

    public void OnDestinationUpdated()
    {
        if (!_destinationName.IsNew)
        {
            CanSubmit = true;
            ShowMessage = false;
            return;
        }

        CanSubmit = IsValidType(_destinationName.FullyQualifiedTypeName);

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
            return false;

        foreach (var identifier in typeName.Split('.'))
        {
            if (!_syntaxFacts.IsValidIdentifier(identifier))
                return false;
        }

        return true;
    }

    public ImmutableArray<TypeNameItem> AvailableTypes { get; }

    private TypeNameItem _destinationName;
    public TypeNameItem DestinationName
    {
        get => _destinationName;
        private set => SetProperty(ref _destinationName, value);
    }

    private ImageMoniker _icon;
    public ImageMoniker Icon
    {
        get => _icon;
        private set => SetProperty(ref _icon, value);
    }
    public string? Message
    {
        get;
        private set => SetProperty(ref field, value);
    }
    public bool ShowMessage
    {
        get;
        private set => SetProperty(ref field, value);
    } = false;
    public bool CanSubmit
    {
        get;
        set => SetProperty(ref field, value);
    } = true;

    private string _searchText;
    public string SearchText
    {
        get => _searchText;
        set => SetProperty(ref _searchText, value);
    }
}
