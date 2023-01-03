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
        public string Name { get; }
        public ImmutableArray<DocumentSymbolDataViewModel> Children { get; }
        public int StartPosition => RangeSpan.Start;

        /// <summary>
        /// The total range of the symbol including leading/trailing trivia
        /// </summary>
        public SnapshotSpan RangeSpan { get; }

        /// <summary>
        /// The range that represents what should be selected in the editor for this item.
        /// Typically, this is the identifier name for the symbol
        /// </summary>
        public SnapshotSpan SelectionRangeSpan { get; }
        public SymbolKind SymbolKind { get; }
        public ImageMoniker ImageMoniker { get; }

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
            string name,
            ImmutableArray<DocumentSymbolDataViewModel> children,
            SnapshotSpan rangeSpan,
            SnapshotSpan selectionRangeSpan,
            SymbolKind symbolKind,
            ImageMoniker imageMoniker,
            bool isExpanded,
            bool isSelected)
        {
            Name = name;
            Children = children;
            SymbolKind = symbolKind;
            ImageMoniker = imageMoniker;
            _isExpanded = isExpanded;
            _isSelected = isSelected;
            RangeSpan = rangeSpan;
            SelectionRangeSpan = selectionRangeSpan;
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
                   (RangeSpan.Span.Start, RangeSpan.Span.End, Name, SymbolKind) ==
                   (other.RangeSpan.Span.Start, other.RangeSpan.Span.End, other.Name, other.SymbolKind));

        public override int GetHashCode()
            => (RangeSpan.Span.Start, RangeSpan.Span.End, Name, SymbolKind).GetHashCode();
    }
}
