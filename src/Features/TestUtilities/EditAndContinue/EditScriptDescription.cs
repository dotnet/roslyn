// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.Differencing;
using Microsoft.CodeAnalysis.Shared.Extensions;
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
        => VerifyEdits(Array.Empty<string>());

    public void VerifyEdits(params string[] expected)
        => AssertEx.Equal(expected, Edits.Select(e => e.GetDebuggerDisplay()), itemSeparator: ",\r\n", itemInspector: static s =>
        {
            var maxQuoteRun = 0;
            var currentRun = 0;
            foreach (var c in s)
            {
                if (c == '"')
                {
                    currentRun++;
                    maxQuoteRun = Math.Max(maxQuoteRun, currentRun);
                }
                else
                {
                    currentRun = 0;
                }
            }

            if (maxQuoteRun >= 1 || s.ContainsLineBreak())
            {
                var quoteBlock = new string('"', Math.Max(maxQuoteRun + 1, 3));
                return $"""
                    {quoteBlock}
                    {s}
                    {quoteBlock}
                    """;
            }
            else
            {
                return $"""
                    "{s}"
                    """;
            }
        });

    public void VerifyEdits(params EditKind[] expected)
        => AssertEx.Equal(expected, Edits.Select(e => e.Kind));
}
