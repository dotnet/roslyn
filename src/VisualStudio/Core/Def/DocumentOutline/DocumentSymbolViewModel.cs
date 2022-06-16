// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.ObjectModel;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Imaging.Interop;
using Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.VisualStudio.LanguageServices
{
    using SymbolKind = LanguageServer.Protocol.SymbolKind;

    internal class DocumentSymbolViewModel
    {
        public string Name { get; }

        public List<DocumentSymbolViewModel> Children { get; set; }

        public int StartLine { get; }
        public int StartChar { get; }
        public int EndLine { get; }
        public int EndChar { get; }

        public SymbolKind SymbolKind { get; }
        public ImageMoniker ImgMoniker { get; }

        public bool IsExpanded { get; set; }
        public bool IsSelected { get; set; }

        public DocumentSymbolViewModel(DocumentSymbol documentSymbol)
        {
            this.Name = documentSymbol.Name;
            this.Children = new List<DocumentSymbolViewModel>();
            this.SymbolKind = documentSymbol.Kind;
            this.ImgMoniker = GetImageMoniker(documentSymbol.Kind);
            this.IsExpanded = true;
            this.IsSelected = false;
            this.StartLine = documentSymbol.Range.Start.Line;
            this.StartChar = documentSymbol.Range.Start.Character;
            this.EndLine = documentSymbol.Range.End.Line;
            this.EndChar = documentSymbol.Range.End.Character;
        }

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
