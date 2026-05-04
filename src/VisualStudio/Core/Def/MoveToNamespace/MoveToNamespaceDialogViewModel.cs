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

namespace Microsoft.VisualStudio.LanguageServices.Implementation.MoveToNamespace;

internal sealed class MoveToNamespaceDialogViewModel : AbstractNotifyPropertyChanged, IDataErrorInfo
{
    private readonly ISyntaxFacts _syntaxFacts;

    public MoveToNamespaceDialogViewModel(
        string defaultNamespace,
        ImmutableArray<string> availableNamespaces,
        ISyntaxFacts syntaxFacts,
        ImmutableArray<string> namespaceHistory)
    {
        _syntaxFacts = syntaxFacts ?? throw new ArgumentNullException(nameof(syntaxFacts));
        _namespaceName = defaultNamespace;
        AvailableNamespaces =
        [
            .. namespaceHistory.Select(n => new NamespaceItem(true, n))
,
            .. availableNamespaces.Except(namespaceHistory).Select(n => new NamespaceItem(false, n)),
        ];

        PropertyChanged += MoveToNamespaceDialogViewModel_PropertyChanged;
    }

    private void MoveToNamespaceDialogViewModel_PropertyChanged(object sender, PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(NamespaceName):
                OnNamespaceUpdated();
                break;
        }
    }

    public void OnNamespaceUpdated()
    {
        var isNewNamespace = !AvailableNamespaces.Any(static (i, self) => i.Namespace == self.NamespaceName, this);
        var isValidName = !isNewNamespace || IsValidNamespace(NamespaceName);

        if (isNewNamespace && isValidName)
        {
            Icon = KnownMonikers.StatusInformation;
            Message = ServicesVSResources.A_new_namespace_will_be_created;
            ShowMessage = true;
            CanSubmit = true;
        }
        else if (!isValidName)
        {
            Icon = KnownMonikers.StatusInvalid;
            Message = ServicesVSResources.This_is_an_invalid_namespace;
            ShowMessage = true;
            CanSubmit = false;
        }
        else
        {
            ShowMessage = false;
            CanSubmit = true;
        }
    }

    private bool IsValidNamespace(string namespaceName)
    {
        if (string.IsNullOrEmpty(namespaceName))
        {
            return false;
        }

        foreach (var identifier in namespaceName.Split('.'))
        {
            if (_syntaxFacts.IsValidIdentifier(identifier))
            {
                continue;
            }

            return false;
        }

        return true;
    }

    private string _namespaceName;
    public string NamespaceName
    {
        get => _namespaceName;
        set => SetProperty(ref _namespaceName, value);
    }

    public ImmutableArray<NamespaceItem> AvailableNamespaces { get; }

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
        private set => SetProperty(ref field, value);
    } = true;

    public string Error => CanSubmit ? string.Empty : Message ?? string.Empty;

    public string this[string columnName]
        => columnName switch
        {
            nameof(NamespaceName) => Error,
            _ => string.Empty
        };

}
