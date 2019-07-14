// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.AddImport
{
    internal abstract partial class AbstractAddImportFeatureService<TSimpleNameSyntax>
    {
        private readonly struct SearchResult
        {
            public readonly IReadOnlyList<string> NameParts;

            // How good a match this was.  0 means it was a perfect match.  Larger numbers are less 
            // and less good.
            public readonly double Weight;

            // The desired name to change the user text to if this was a fuzzy (spell-checking) match.
            public readonly string DesiredName;

            // The node to convert to the desired name
            public readonly TSimpleNameSyntax NameNode;

            public SearchResult(SymbolResult<INamespaceOrTypeSymbol> result)
                : this(result.DesiredName, result.NameNode, INamespaceOrTypeSymbolExtensions.GetNameParts(result.Symbol), result.Weight)
            {
            }

            public SearchResult(string desiredName, TSimpleNameSyntax nameNode, IReadOnlyList<string> nameParts, double weight)
            {
                DesiredName = desiredName;
                Weight = weight;
                NameNode = nameNode;
                NameParts = nameParts;
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

                var syntaxFacts = document.GetLanguageService<ISyntaxFactsService>();

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

        private readonly struct SymbolResult<T> where T : ISymbol
        {
            // The symbol that matched the string being searched for.
            public readonly T Symbol;

            // How good a match this was.  0 means it was a perfect match.  Larger numbers are less 
            // and less good.
            public readonly double Weight;

            // The desired name to change the user text to if this was a fuzzy (spell-checking) match.
            public readonly string DesiredName;

            // The node to convert to the desired name
            public readonly TSimpleNameSyntax NameNode;

            public SymbolResult(string desiredName, TSimpleNameSyntax nameNode, T symbol, double weight)
            {
                DesiredName = desiredName;
                Symbol = symbol;
                Weight = weight;
                NameNode = nameNode;
            }

            public SymbolResult<T2> WithSymbol<T2>(T2 symbol) where T2 : ISymbol
            {
                return new SymbolResult<T2>(DesiredName, NameNode, symbol, Weight);
            }

            internal SymbolResult<T> WithDesiredName(string desiredName)
            {
                return new SymbolResult<T>(desiredName, NameNode, Symbol, Weight);
            }
        }

        private struct SymbolResult
        {
            public static SymbolResult<T> Create<T>(string desiredName, TSimpleNameSyntax nameNode, T symbol, double weight) where T : ISymbol
            {
                return new SymbolResult<T>(desiredName, nameNode, symbol, weight);
            }
        }
    }
}
