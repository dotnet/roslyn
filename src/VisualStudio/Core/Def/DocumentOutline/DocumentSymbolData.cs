// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Microsoft.VisualStudio.Text;

namespace Microsoft.VisualStudio.LanguageServices.DocumentOutline
{
    using SymbolKind = LanguageServer.Protocol.SymbolKind;

    internal sealed record DocumentSymbolData(string Name, SymbolKind SymbolKind, SnapshotSpan RangeSpan, SnapshotSpan SelectionRangeSpan, ImmutableArray<DocumentSymbolData> Children)
    {
        public DocumentSymbolData(DocumentSymbol documentSymbol, SnapshotSpan rangeSpan, SnapshotSpan selectionRangeSpan, ImmutableArray<DocumentSymbolData> children)
            : this(documentSymbol.Name, documentSymbol.Kind, rangeSpan, selectionRangeSpan, children)
        {
        }
    }
}
