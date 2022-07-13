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

    internal sealed class DocumentSymbolData
    {
        public string Name { get; }
        public SymbolKind SymbolKind { get; }
        public SnapshotSpan RangeSpan { get; }
        public SnapshotSpan SelectionRangeSpan { get; }
        public ImmutableArray<DocumentSymbolData> Children { get; }

        public DocumentSymbolData(DocumentSymbol documentSymbol, ITextSnapshot originalSnapshot, ImmutableArray<DocumentSymbolData> children)
        {
            Name = documentSymbol.Name;
            SymbolKind = documentSymbol.Kind;
            RangeSpan = GetSymbolRangeSpan(documentSymbol.Range);
            SelectionRangeSpan = GetSymbolRangeSpan(documentSymbol.SelectionRange);
            Children = children;

            SnapshotSpan GetSymbolRangeSpan(Range symbolRange)
            {
                var originalStartPosition = originalSnapshot.GetLineFromLineNumber(symbolRange.Start.Line).Start.Position + symbolRange.Start.Character;
                var originalEndPosition = originalSnapshot.GetLineFromLineNumber(symbolRange.End.Line).Start.Position + symbolRange.End.Character;

                return new SnapshotSpan(originalSnapshot, Span.FromBounds(originalStartPosition, originalEndPosition));
            }
        }

        public DocumentSymbolData(DocumentSymbolData documentSymbolData, ImmutableArray<DocumentSymbolData> children)
        {
            Name = documentSymbolData.Name;
            SymbolKind = documentSymbolData.SymbolKind;
            RangeSpan = documentSymbolData.RangeSpan;
            SelectionRangeSpan = documentSymbolData.SelectionRangeSpan;
            Children = children;
        }
    }
}
