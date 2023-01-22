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
    internal sealed class DocumentSymbolDataViewModel : INotifyPropertyChanged, IEquatable<DocumentSymbolDataViewModel>
    {
        private readonly DocumentSymbolData _data;

        public string Name => _data.Name;
        public ImmutableArray<DocumentSymbolDataViewModel> Children { get; init; }
        public int StartPosition => RangeSpan.Start;

        /// <summary>
        /// The total range of the symbol including leading/trailing trivia
        /// </summary>
        public SnapshotSpan RangeSpan => _data.RangeSpan;

        /// <summary>
        /// The range that represents what should be selected in the editor for this item.
        /// Typically, this is the identifier name for the symbol
        /// </summary>
        public SnapshotSpan SelectionRangeSpan => _data.SelectionRangeSpan;
        public SymbolKind SymbolKind => _data.SymbolKind;
        public ImageMoniker ImageMoniker => _data.SymbolKind.GetImageMoniker();

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
            _data = data;
            Children = children;
            _isExpanded = isExpanded;
            _isSelected = isSelected;
        }

        private void NotifyPropertyChanged([CallerMemberName] string? propertyName = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

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

        public static bool operator ==(DocumentSymbolDataViewModel left, DocumentSymbolDataViewModel right)
            => left.Equals(right);

        public static bool operator !=(DocumentSymbolDataViewModel left, DocumentSymbolDataViewModel right)
            => !(left == right);

        public override bool Equals(object obj)
            => Equals(obj as DocumentSymbolDataViewModel);

        public bool Equals(DocumentSymbolDataViewModel? other)
            => (object)this == other ||
                 (other is not null &&
                   (RangeSpan.Span, Name, SymbolKind) ==
                   (other.RangeSpan.Span, other.Name, other.SymbolKind));

        public override int GetHashCode()
            => (RangeSpan.Span.Start, RangeSpan.Span.End, Name, SymbolKind).GetHashCode();
    }
}
