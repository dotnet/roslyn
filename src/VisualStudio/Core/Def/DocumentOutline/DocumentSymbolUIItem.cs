// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Imaging.Interop;
using Microsoft.VisualStudio.Text;

namespace Microsoft.VisualStudio.LanguageServices.DocumentOutline
{
    using SymbolKind = LanguageServer.Protocol.SymbolKind;

    internal sealed class DocumentSymbolUIItem : INotifyPropertyChanged
    {
        private readonly IThreadingContext _threadingContext;
        public string Name { get; }
        public ImmutableArray<DocumentSymbolUIItem> Children { get; }
        public SnapshotSpan RangeSpan { get; }
        public SnapshotSpan SelectionRangeSpan { get; }
        public SymbolKind SymbolKind { get; }
        public ImageMoniker ImageMoniker { get; }
        // _isSelected and _isExpanded should only be mutated/read from the UI thread.
        private bool _isSelected;
        private bool _isExpanded;

        public bool IsExpanded
        {
            get
            {
                _threadingContext.ThrowIfNotOnUIThread();
                return _isExpanded;
            }
            set
            {
                _threadingContext.ThrowIfNotOnUIThread();
                if (_isExpanded != value)
                {
                    _isExpanded = value;
                    NotifyPropertyChanged();
                }
            }
        }
        public bool IsSelected
        {
            get
            {
                _threadingContext.ThrowIfNotOnUIThread();
                return _isSelected;
            }
            set
            {
                _threadingContext.ThrowIfNotOnUIThread();
                if (_isSelected != value)
                {
                    _isSelected = value;
                    NotifyPropertyChanged();
                }
            }
        }

        public DocumentSymbolUIItem(DocumentSymbolData documentSymbolData, ImmutableArray<DocumentSymbolUIItem> children, IThreadingContext threadingContext)
        {
            _threadingContext = threadingContext;
            Name = documentSymbolData.Name;
            Children = children;
            SymbolKind = documentSymbolData.SymbolKind;
            ImageMoniker = GetImageMoniker(documentSymbolData.SymbolKind);
            IsExpanded = true;
            IsSelected = false;
            RangeSpan = documentSymbolData.RangeSpan;
            SelectionRangeSpan = documentSymbolData.SelectionRangeSpan;
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
