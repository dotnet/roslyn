// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Imaging.Interop;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Microsoft.VisualStudio.Text;

namespace Microsoft.VisualStudio.LanguageServices.DocumentOutline
{
    using SymbolKind = LanguageServer.Protocol.SymbolKind;

    internal sealed class DocumentSymbolUIItem : INotifyPropertyChanged
    {
        private bool _isSelected;
        private bool _isExpanded;

        public string Name { get; }

        public ImmutableArray<DocumentSymbolUIItem> Children { get; set; }

        public SnapshotSpan RangeSpan { get; }
        public SnapshotSpan SelectionRangeSpan { get; }

        public SymbolKind SymbolKind { get; }
        public ImageMoniker ImageMoniker { get; }

        public bool IsExpanded
        {
            get => _isExpanded;
            set
            {
                if (_isExpanded != value)
                {
                    _isExpanded = value;
                    NotifyPropertyChanged();
                }
            }
        }
        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                if (_isSelected != value)
                {
                    _isSelected = value;
                    NotifyPropertyChanged();
                }
            }
        }

        public DocumentSymbolUIItem(DocumentSymbolData documentSymbolData)
        {
            this.Name = documentSymbolData.Name;
            this.Children = ImmutableArray<DocumentSymbolUIItem>.Empty;
            this.SymbolKind = documentSymbolData.SymbolKind;
            this.ImageMoniker = GetImageMoniker(documentSymbolData.SymbolKind);
            this.IsExpanded = true;
            this.IsSelected = false;
            this.RangeSpan = documentSymbolData.RangeSpan;
            this.SelectionRangeSpan = documentSymbolData.SelectionRangeSpan;
        }

        private void NotifyPropertyChanged([CallerMemberName] string? propertyName = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        public event PropertyChangedEventHandler? PropertyChanged;

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
    }
}
