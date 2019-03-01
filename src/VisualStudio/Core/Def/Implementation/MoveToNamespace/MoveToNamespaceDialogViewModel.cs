// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using Microsoft.VisualStudio.Imaging.Interop;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.LanguageServices.Implementation.Utilities;
using Microsoft.VisualStudio.Imaging;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.MoveToNamespace
{
    class MoveToNamespaceDialogViewModel : AbstractNotifyPropertyChanged
    {
        public MoveToNamespaceDialogViewModel(
            IGlyphService glyphService,
            string defaultNamespace,
            ImmutableArray<string> availableNamespaces)
        {
            NamespaceName = defaultNamespace;
            AvailableNamespaces = availableNamespaces;

            this.PropertyChanged += MoveToNamespaceDialogViewModel_PropertyChanged;
        }

        private void MoveToNamespaceDialogViewModel_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            switch(e.PropertyName)
            {
                case nameof(NamespaceName):
                    OnNamespaceUpdated();
                    break;
            }
        }

        public void OnNamespaceUpdated()
        {
            var isNewNamespace = !AvailableNamespaces.Contains(NamespaceName);
            var isValidName = !isNewNamespace || true /* ToDo: Determine valid namespace if new*/;

            if (isNewNamespace && isValidName)
            {
                Icon = KnownMonikers.StatusInformation;
                Message = "Something about creating a new namespace goes here.";
                ShowMessage = true;
            }
            else if (!isValidName)
            {
                Icon = KnownMonikers.StatusError;
                Message = "Something about this being an invalid namespace";
                ShowMessage = true;
            }
            else
            {
                ShowMessage = false;
            }
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
    }
}
