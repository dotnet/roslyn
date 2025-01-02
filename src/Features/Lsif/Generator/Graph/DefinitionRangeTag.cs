// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using LSP = Roslyn.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServerIndexFormat.Generator.Graph;

internal sealed class DefinitionRangeTag : RangeTag
{
    public DefinitionRangeTag(string text, LSP.SymbolKind kind, LSP.Range fullRange)
        : base(type: "definition", text)
    {
        Kind = kind;
        FullRange = fullRange;
    }

    public LSP.SymbolKind Kind { get; }

    // Note: this is not a Range vertex here, since it doesn't have an ID or labels, but just the LSP Range type.
    public LSP.Range FullRange { get; }
}
