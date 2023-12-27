// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.EditAndContinue.UnitTests;

internal sealed class SemanticEditDescription(
    SemanticEditKind kind,
    Func<Compilation, ISymbol> symbolProvider,
    Func<Compilation, ITypeSymbol>? partialType,
    IEnumerable<(TextSpan, TextSpan)>? syntaxMap,
    IEnumerable<RuntimeRudeEditDescription>? rudeEdits,
    bool hasSyntaxMap,
    Func<Compilation, ISymbol>? deletedSymbolContainerProvider)
{
    public readonly SemanticEditKind Kind = kind;
    public readonly Func<Compilation, ISymbol> SymbolProvider = symbolProvider;
    public readonly Func<Compilation, ITypeSymbol>? PartialType = partialType;
    public readonly Func<Compilation, ISymbol>? DeletedSymbolContainerProvider = deletedSymbolContainerProvider;

    /// <summary>
    /// If specified the node mappings will be validated against the actual syntax map function.
    /// </summary>
    public IEnumerable<(TextSpan oldSpan, TextSpan newSpan, RuntimeRudeEditDescription? runtimeRudeEdit)>? GetSyntaxMap()
        => HasSyntaxMap ? GetSyntaxMapWithRudeEdits(syntaxMap, rudeEdits) : null;

    public readonly bool HasSyntaxMap = hasSyntaxMap;

    private static IEnumerable<(TextSpan oldSpan, TextSpan newSpan, RuntimeRudeEditDescription? runtimeRudeEdit)> GetSyntaxMapWithRudeEdits(IEnumerable<(TextSpan, TextSpan)>? syntaxMap, IEnumerable<RuntimeRudeEditDescription>? rudeEdits)
    {
        if (syntaxMap == null)
        {
            yield break;
        }

        var markerId = 0;
        foreach (var (oldSpan, newSpan) in syntaxMap)
        {
            yield return (oldSpan, newSpan, rudeEdits?.SingleOrDefault(e => e.MarkerId == markerId));
            markerId++;
        }
    }
}
