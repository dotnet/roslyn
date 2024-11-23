// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.AddImport;

internal abstract partial class AbstractAddImportFeatureService<TSimpleNameSyntax>
{
    private readonly struct SearchResult(string? desiredName, TSimpleNameSyntax nameNode, IReadOnlyList<string> nameParts, double weight)
    {
        public readonly IReadOnlyList<string> NameParts = nameParts;

        // How good a match this was.  0 means it was a perfect match.  Larger numbers are less 
        // and less good.
        public readonly double Weight = weight;

        // The desired name to change the user text to if this was a fuzzy (spell-checking) match.
        public readonly string? DesiredName = desiredName;

        // The node to convert to the desired name
        public readonly TSimpleNameSyntax NameNode = nameNode;

        public SearchResult(SymbolResult<INamespaceOrTypeSymbol> result)
            : this(result.DesiredName, result.NameNode, INamespaceOrTypeSymbolExtensions.GetNameParts(result.Symbol), result.Weight)
        {
        }

        public bool DesiredNameDiffersFromSourceName()
        {
            return !string.IsNullOrEmpty(DesiredName) &&
                NameNode != null &&
                NameNode.GetFirstToken().ValueText != DesiredName;
        }

        public bool DesiredNameDiffersFromSourceNameOnlyByCase()
        {
            Debug.Assert(DesiredNameDiffersFromSourceName());
            return StringComparer.OrdinalIgnoreCase.Equals(
                NameNode.GetFirstToken().ValueText, DesiredName);
        }

        public bool DesiredNameMatchesSourceName(Document document)
        {
            if (!DesiredNameDiffersFromSourceName())
            {
                // Names match in any language.
                return true;
            }

            var syntaxFacts = document.GetRequiredLanguageService<ISyntaxFactsService>();

            // Names differ.  But in a case insensitive language they may match.
            if (!syntaxFacts.IsCaseSensitive &&
                DesiredNameDiffersFromSourceNameOnlyByCase())
            {
                return true;
            }

            // Name are totally different in any language.
            return false;
        }
    }

    private readonly struct SymbolResult<T>(string desiredName, TSimpleNameSyntax nameNode, T symbol, double weight) where T : ISymbol
    {
        // The symbol that matched the string being searched for.
        public readonly T Symbol = symbol;

        // How good a match this was.  0 means it was a perfect match.  Larger numbers are less 
        // and less good.
        public readonly double Weight = weight;

        // The desired name to change the user text to if this was a fuzzy (spell-checking) match.
        public readonly string DesiredName = desiredName;

        // The node to convert to the desired name
        public readonly TSimpleNameSyntax NameNode = nameNode;

        public SymbolResult<T2> WithSymbol<T2>(T2 symbol) where T2 : ISymbol
            => new(DesiredName, NameNode, symbol, Weight);

        internal SymbolResult<T> WithDesiredName(string desiredName)
            => new(desiredName, NameNode, Symbol, Weight);
    }

    private struct SymbolResult
    {
        public static SymbolResult<T> Create<T>(string desiredName, TSimpleNameSyntax nameNode, T symbol, double weight) where T : ISymbol
            => new(desiredName, nameNode, symbol, weight);
    }
}
