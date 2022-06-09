// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.ObjectModel;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Imaging.Interop;

namespace Microsoft.VisualStudio.LanguageServices
{
    using SymbolKind = LanguageServer.Protocol.SymbolKind;

    internal class DocSymbol
    {
        public string Name { get; set; }

        public ObservableCollection<DocSymbol> Children { get; set; }

        public int StartLine { get; set; }
        public int StartChar { get; set; }
        public int EndLine { get; set; }
        public int EndChar { get; set; }

        public SymbolKind SymbolKind { get; set; }
        public ImageMoniker ImgMoniker { get; set; }

        public bool IsExpanded { get; set; }
        public bool IsSelected { get; set; }

        public DocSymbol(string name, SymbolKind symbolKind, int startLine, int startChar, int endLine, int endChar)
        {
            this.Name = name;
            this.Children = new ObservableCollection<DocSymbol>();
            this.SymbolKind = symbolKind;
            this.ImgMoniker = GetImageMoniker(symbolKind);
            this.IsExpanded = true;
            this.IsSelected = false;
            this.StartLine = startLine;
            this.StartChar = startChar;
            this.EndLine = endLine;
            this.EndChar = endChar;
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
