// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Microsoft.VisualStudio.Imaging.Interop;
using Microsoft.VisualStudio.Text;

namespace Microsoft.VisualStudio.LanguageServices.DocumentOutline
{
    using SymbolKind = LanguageServer.Protocol.SymbolKind;

    /// <summary>
    /// A ViewModel over <see cref="DocumentSymbolData"/>
    /// The only items that are mutable on this type are <see cref="IsExpanded"/> and <see cref="IsSelected"/>.
    /// It is expected that these can be modified from any thread with INotifyPropertyChanged notifications
    /// being marshalled to the correct thread by WPF if there needs to be a change to the visual presentation.
    /// </summary>
    internal sealed class DocumentSymbolDataViewModel : INotifyPropertyChanged
    {
        public DocumentSymbolData Data { get; }
        public ImmutableArray<DocumentSymbolDataViewModel> Children { get; init; }

        /// <summary>
        /// Necessary because we cannot convert to this type dynamically in WPF.
        /// </summary>
        public ImageMoniker ImageMoniker => Data.SymbolKind.GetImageMoniker();

        private bool _isExpanded;
        public bool IsExpanded
        {
            get => _isExpanded;
            set => SetProperty(ref _isExpanded, value);
        }

        private bool _isSelected;

        public bool IsSelected
        {
            get => _isSelected;
            set => SetProperty(ref _isSelected, value);
        }

        public DocumentSymbolDataViewModel(
            DocumentSymbolData data,
            ImmutableArray<DocumentSymbolDataViewModel> children,
            bool isExpanded,
            bool isSelected)
        {
            Data = data;
            Children = children;
            _isExpanded = isExpanded;
            _isSelected = isSelected;
        }

        private static readonly PropertyChangedEventArgs _isExpandedPropertyChangedEventArgs = new PropertyChangedEventArgs(nameof(IsExpanded));
        private static readonly PropertyChangedEventArgs _isSelectedPropertyChangedEventArgs = new PropertyChangedEventArgs(nameof(IsSelected));

        private void NotifyPropertyChanged([CallerMemberName] string? propertyName = null)
            => PropertyChanged?.Invoke(this, propertyName switch
            {
                nameof(IsExpanded) => _isExpandedPropertyChangedEventArgs,
                nameof(IsSelected) => _isSelectedPropertyChangedEventArgs,
                _ => new PropertyChangedEventArgs(propertyName)
            });

        public event PropertyChangedEventHandler? PropertyChanged;

        private void SetProperty(ref bool field, bool value, [CallerMemberName] string propertyName = "")
        {
            // Note: we do not lock here. Worst case is that we fire multiple
            //       NotifyPropertyChanged events which WPF can handle.
            if (field == value)
            {
                return;
            }

            field = value;
            NotifyPropertyChanged(propertyName);
        }
    }
}
