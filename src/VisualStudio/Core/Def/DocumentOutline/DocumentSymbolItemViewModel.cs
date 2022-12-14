// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Imaging.Interop;
using Microsoft.VisualStudio.Text;

namespace Microsoft.VisualStudio.LanguageServices.DocumentOutline
{
    using SymbolKind = LanguageServer.Protocol.SymbolKind;

    internal sealed class DocumentSymbolItemViewModel : INotifyPropertyChanged, IEquatable<DocumentSymbolItemViewModel>
    {
        public string Name { get; }
        public ImmutableArray<DocumentSymbolItemViewModel> Children { get; }
        public int StartPosition => RangeSpan.Start;
        public SnapshotSpan RangeSpan { get; }
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

        public DocumentSymbolItemViewModel(DocumentSymbolData documentSymbolData, ImmutableArray<DocumentSymbolItemViewModel> children)
            : this(
                  documentSymbolData.Name,
                  children,
                  documentSymbolData.RangeSpan,
                  documentSymbolData.SelectionRangeSpan,
                  documentSymbolData.SymbolKind,
                  GetImageMoniker(documentSymbolData.SymbolKind),
                  isExpanded: true,
                  isSelected: false)
        {
        }

        private DocumentSymbolItemViewModel(
            string name,
            ImmutableArray<DocumentSymbolItemViewModel> children,
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

        internal DocumentSymbolItemViewModel WithChildren(ImmutableArray<DocumentSymbolItemViewModel> newChildren)
            => new(Name, newChildren, RangeSpan, SelectionRangeSpan, SymbolKind, ImageMoniker, IsExpanded, IsSelected);

        private static ImageMoniker GetImageMoniker(SymbolKind symbolKind)
        {
            return symbolKind switch
            {
                SymbolKind.File => KnownMonikers.IconFile,
                SymbolKind.Module => KnownMonikers.Module,
                SymbolKind.Namespace => KnownMonikers.Namespace,
                SymbolKind.Class => KnownMonikers.Class,
                SymbolKind.Package => KnownMonikers.Package,
                SymbolKind.Method => KnownMonikers.Method,
                SymbolKind.Property => KnownMonikers.Property,
                SymbolKind.Field => KnownMonikers.Field,
                SymbolKind.Constructor => KnownMonikers.Method,
                SymbolKind.Enum => KnownMonikers.Enumeration,
                SymbolKind.Interface => KnownMonikers.Interface,
                SymbolKind.Function => KnownMonikers.Method,
                SymbolKind.Variable => KnownMonikers.LocalVariable,
                SymbolKind.Constant => KnownMonikers.Constant,
                SymbolKind.String => KnownMonikers.String,
                SymbolKind.Number => KnownMonikers.Numeric,
                SymbolKind.Boolean => KnownMonikers.BooleanData,
                SymbolKind.Array => KnownMonikers.Field,
                SymbolKind.Object => KnownMonikers.SelectObject,
                SymbolKind.Key => KnownMonikers.Key,
                SymbolKind.Null => KnownMonikers.SelectObject,
                SymbolKind.EnumMember => KnownMonikers.EnumerationItemPublic,
                SymbolKind.Struct => KnownMonikers.Structure,
                SymbolKind.Event => KnownMonikers.Event,
                SymbolKind.Operator => KnownMonikers.Operator,
                SymbolKind.TypeParameter => KnownMonikers.Type,
                _ => KnownMonikers.SelectObject,
            };
        }

        public override bool Equals(object obj)
            => Equals(obj as DocumentSymbolItemViewModel);

        public static bool operator ==(DocumentSymbolItemViewModel left, DocumentSymbolItemViewModel right)
            => (object)left == right || (left is not null && left.Equals(right));

        public static bool operator !=(DocumentSymbolItemViewModel left, DocumentSymbolItemViewModel right)
            => !(left == right);

        public bool Equals(DocumentSymbolItemViewModel? other)
            => (object)this == other ||
                 (other is not null &&
                   (RangeSpan.Span.Start, RangeSpan.Span.End, Name, SymbolKind) ==
                   (other.RangeSpan.Span.Start, other.RangeSpan.Span.End, other.Name, other.SymbolKind));

        public override int GetHashCode()
            => (RangeSpan.Span.Start, RangeSpan.Span.End, Name, SymbolKind).GetHashCode();
    }
}
