// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Microsoft.CodeAnalysis.Text;
using Newtonsoft.Json;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.LanguageServerIndexFormat.Generator.Graph;

internal sealed class RangeBasedDocumentSymbol(Id<Range> id, TextSpan span)
{
    [JsonProperty("id")]
    public Id<Range> Id { get; } = id;

    /// <summary>
    /// The Roslyn <see cref="TextSpan"/> of the symbol, which is used only to produce the structure easily. Not serialized.
    /// </summary>
    [JsonIgnore]
    public TextSpan Span { get; } = span;

    [JsonProperty("children")]
    public List<RangeBasedDocumentSymbol>? Children { get; internal set; }

    public static void AddNestedFromDocumentOrderTraversal(List<RangeBasedDocumentSymbol> list, RangeBasedDocumentSymbol symbol)
    {
        // If this is the first entry, just add it
        if (list.Count == 0)
        {
            list.Add(symbol);
        }
        else
        {
            // We are producing the symbols in document order, so we know that the new symbol is either inside the previous one,
            // or is after that one. As a result we'll check the last one only.
            var last = list[^1];

            Contract.ThrowIfTrue(symbol.Span.Start < last.Span.Start, "We didn't produce symbols in document order, so we're not going to correctly structure the result.");

            if (last.Span.Contains(symbol.Span))
            {
                last.Children ??= [];
                AddNestedFromDocumentOrderTraversal(last.Children, symbol);
            }
            else
            {
                list.Add(symbol);
            }
        }
    }
}
