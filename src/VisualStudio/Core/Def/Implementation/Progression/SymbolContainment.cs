// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Progression
{
    internal static class SymbolContainment
    {
        public static async Task<IEnumerable<SyntaxNode>> GetContainedSyntaxNodesAsync(Document document, CancellationToken cancellationToken)
        {
            var progressionLanguageService = document.GetLanguageService<IProgressionLanguageService>();
            if (progressionLanguageService == null)
            {
                return SpecializedCollections.EmptyEnumerable<SyntaxNode>();
            }

            var syntaxTree = await document.GetSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);
            var root = await syntaxTree.GetRootAsync(cancellationToken).ConfigureAwait(false);
            return progressionLanguageService.GetTopLevelNodesFromDocument(root, cancellationToken);
        }

        public static async Task<IEnumerable<ISymbol>> GetContainedSymbolsAsync(Document document, CancellationToken cancellationToken)
        {
            var syntaxNodes = await GetContainedSyntaxNodesAsync(document, cancellationToken).ConfigureAwait(false);
            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var symbols = new List<ISymbol>();

            foreach (var syntaxNode in syntaxNodes)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var symbol = semanticModel.GetDeclaredSymbol(syntaxNode, cancellationToken);
                if (symbol != null &&
                    !string.IsNullOrEmpty(symbol.Name) &&
                    IsTopLevelSymbol(symbol))
                {
                    symbols.Add(symbol);
                }
            }

            return symbols;
        }

        private static bool IsTopLevelSymbol(ISymbol symbol)
        {
            switch (symbol.Kind)
            {
                case SymbolKind.NamedType:
                case SymbolKind.Method:
                case SymbolKind.Field:
                case SymbolKind.Property:
                case SymbolKind.Event:
                    return true;

                default:
                    return false;
            }
        }

        public static IEnumerable<ISymbol> GetContainedSymbols(ISymbol symbol)
        {

            if (symbol is INamedTypeSymbol namedType)
            {
                foreach (var member in namedType.GetMembers())
                {
                    if (member.IsImplicitlyDeclared)
                    {
                        continue;
                    }


                    if (member is IMethodSymbol { AssociatedSymbol: { } } method)
                    {
                        continue;
                    }

                    if (!string.IsNullOrEmpty(member.Name))
                    {
                        yield return member;
                    }
                }
            }
        }
    }
}
