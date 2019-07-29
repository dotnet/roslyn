// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using Microsoft.VisualStudio.Imaging.Interop;
using Microsoft.VisualStudio.LanguageServices.Implementation.Utilities;
using Microsoft.VisualStudio.Imaging;
using System;
using Microsoft.CodeAnalysis.LanguageServices;
using System.ComponentModel;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.MoveToNamespace
{
    class MoveToNamespaceDialogViewModel : AbstractNotifyPropertyChanged, IDataErrorInfo
    {
        private readonly ISyntaxFactsService _syntaxFactsService;

        public MoveToNamespaceDialogViewModel(
            string defaultNamespace,
            ImmutableArray<string> availableNamespaces,
            ISyntaxFactsService syntaxFactsService)
        {
            _syntaxFactsService = syntaxFactsService ?? throw new ArgumentNullException(nameof(syntaxFactsService));
            NamespaceName = defaultNamespace;
            AvailableNamespaces = availableNamespaces;

            PropertyChanged += MoveToNamespaceDialogViewModel_PropertyChanged;
        }

        private void MoveToNamespaceDialogViewModel_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
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
            var isNewNamespace = !AvailableNamespaces.Contains(NamespaceName);
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
                if (_syntaxFactsService.IsValidIdentifier(identifier))
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

        public ImmutableArray<string> AvailableNamespaces { get; }

        private ImageMoniker _icon;
        public ImageMoniker Icon
        {
            get => _icon;
            private set => SetProperty(ref _icon, value);
        }

        private string _message;
        public string Message
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
            private set => SetProperty(ref _canSubmit, value);
        }

        public string Error => CanSubmit ? string.Empty : Message;

        public string this[string columnName] =>
            columnName switch
            {
                nameof(NamespaceName) => CanSubmit ? string.Empty : Message,
                _ => string.Empty
            };

    }
}
