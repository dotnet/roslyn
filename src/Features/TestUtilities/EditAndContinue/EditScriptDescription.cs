// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.Differencing;
using Roslyn.Test.Utilities;

namespace Microsoft.CodeAnalysis.EditAndContinue.UnitTests;

internal readonly struct EditScriptDescription(string oldMarkedSource, string newMarkedSource, EditScript<SyntaxNode> edits)
{
    public readonly string OldMarkedSource = oldMarkedSource;
    public readonly string NewMarkedSource = newMarkedSource;
    public readonly EditScript<SyntaxNode> Script = edits;

    public Match<SyntaxNode> Match => Script.Match;
    public ImmutableArray<Edit<SyntaxNode>> Edits => Script.Edits;

    public SyntaxMapDescription.Mapping GetSyntaxMap()
        => GetSyntaxMaps().Single();

    public SyntaxMapDescription GetSyntaxMaps()
        => new(OldMarkedSource, NewMarkedSource, Match);

    public void VerifyEdits()
        => AssertEx.Equal(expected, Array.Empty<string>());

    public void VerifyEdits(params string[] expected)
        => AssertEx.Equal(expected, Edits.Select(e => e.GetDebuggerDisplay()), itemSeparator: ",\r\n", itemInspector: s => $"\"{s}\"");

    public void VerifyEdits(params EditKind[] expected)
        => AssertEx.Equal(expected, Edits.Select(e => e.Kind));
}
