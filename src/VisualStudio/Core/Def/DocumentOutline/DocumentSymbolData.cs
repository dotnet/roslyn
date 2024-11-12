// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.Text;

namespace Microsoft.VisualStudio.LanguageServices.DocumentOutline;

using SymbolKind = Roslyn.LanguageServer.Protocol.SymbolKind;

/// <summary>
/// Represents the immutable symbol returned from the LSP request to get document symbols, but mapped into
/// editor/text-snapshot concepts.
/// </summary>
internal sealed record DocumentSymbolData(
    string Name,
    SymbolKind SymbolKind,
    Glyph Glyph,
    SnapshotSpan RangeSpan,
    SnapshotSpan SelectionRangeSpan,
    ImmutableArray<DocumentSymbolData> Children);
