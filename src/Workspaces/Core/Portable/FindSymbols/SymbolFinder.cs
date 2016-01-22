// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.FindSymbols
{
    public static partial class SymbolFinder
    {
        public static ISymbol FindSymbolAtPosition(
            SemanticModel semanticModel,
            int position,
            Workspace workspace,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            return FindSymbolAtPositionAsync(semanticModel, position, workspace, cancellationToken).WaitAndGetResult(cancellationToken);
        }

        /// <summary>
        /// Finds the symbol that is associated with a position in the text of a document.
        /// </summary>
        /// <param name="semanticModel">The semantic model associated with the document.</param>
        /// <param name="position">The character position within the document.</param>
        /// <param name="workspace">A workspace to provide context.</param>
        /// <param name="cancellationToken">A CancellationToken.</param>
        public static Task<ISymbol> FindSymbolAtPositionAsync(
            SemanticModel semanticModel,
            int position,
            Workspace workspace,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            return FindSymbolAtPositionAsync(semanticModel, position, workspace, bindLiteralsToUnderlyingType: false, cancellationToken: cancellationToken);
        }

        internal static async Task<ISymbol> FindSymbolAtPositionAsync(
            SemanticModel semanticModel,
            int position,
            Workspace workspace,
            bool bindLiteralsToUnderlyingType,
            CancellationToken cancellationToken)
        {
            var syntaxTree = semanticModel.SyntaxTree;
            var syntaxFacts = workspace.Services.GetLanguageServices(semanticModel.Language).GetService<ISyntaxFactsService>();
            var token = await syntaxTree.GetTouchingTokenAsync(position, syntaxFacts.IsBindableToken, cancellationToken, findInsideTrivia: true).ConfigureAwait(false);

            if (token != default(SyntaxToken))
            {
                if (token.Span.IntersectsWith(position))
                {
                    return semanticModel.GetSymbols(token, workspace, bindLiteralsToUnderlyingType, cancellationToken).FirstOrDefault();
                }
            }

            return null;
        }

        public static async Task<ISymbol> FindSymbolAtPositionAsync(
            Document document,
            int position,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            return await FindSymbolAtPositionAsync(semanticModel, position, document.Project.Solution.Workspace, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Finds the definition symbol declared in source code for a corresponding reference symbol. 
        /// Returns null if no such symbol can be found in the specified solution.
        /// </summary>
        public static Task<ISymbol> FindSourceDefinitionAsync(
            ISymbol symbol, Solution solution, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (symbol != null)
            {
                symbol = symbol.GetOriginalUnreducedDefinition();
                switch (symbol.Kind)
                {
                    case SymbolKind.Event:
                    case SymbolKind.Field:
                    case SymbolKind.Method:
                    case SymbolKind.Local:
                    case SymbolKind.NamedType:
                    case SymbolKind.Parameter:
                    case SymbolKind.Property:
                    case SymbolKind.TypeParameter:
                    case SymbolKind.Namespace:
                        return FindSourceDefinitionWorkerAsync(symbol, solution, cancellationToken);
                }
            }

            return SpecializedTasks.Default<ISymbol>();
        }

        private static async Task<ISymbol> FindSourceDefinitionWorkerAsync(
            ISymbol symbol,
            Solution solution,
            CancellationToken cancellationToken)
        {
            // If it's already in source, then just return it.
            if (InSource(symbol))
            {
                return symbol;
            }

            // If it's not in metadata, then we can't map it back to source.
            if (!symbol.Locations.Any(loc => loc.IsInMetadata))
            {
                return null;
            }

            var project = solution.GetProject(symbol.ContainingAssembly, cancellationToken);
            if (project != null)
            {
                var symbolId = symbol.GetSymbolKey();
                var compilation = await project.GetCompilationAsync(cancellationToken).ConfigureAwait(false);
                var result = symbolId.Resolve(compilation, ignoreAssemblyKey: true, cancellationToken: cancellationToken);

                if (result.Symbol != null && InSource(result.Symbol))
                {
                    return result.Symbol;
                }
                else
                {
                    return result.CandidateSymbols.FirstOrDefault(InSource);
                }
            }
            else
            {
                return null;
            }
        }

        private static bool InSource(ISymbol symbol)
        {
            if (symbol == null)
            {
                return false;
            }

            return symbol.Locations.Any(loc => loc.IsInSource);
        }

        /// <summary>
        /// Finds symbols in the given compilation that are similar to the specified symbol.
        /// 
        /// A found symbol may be the exact same symbol instance if the compilation is the origin of the specified symbol, 
        /// or it may be a different symbol instance if the compilation is not the originating compilation.
        /// 
        /// Multiple symbols may be returned if there are ambiguous matches.
        /// No symbols may be returned if the compilation does not define or have access to a similar symbol.
        /// </summary>
        /// <param name="symbol">The symbol to find corresponding matches for.</param>
        /// <param name="compilation">A compilation to find the corresponding symbol within. The compilation may or may not be the origin of the symbol.</param>
        /// <param name="cancellationToken">A CancellationToken.</param>
        /// <returns></returns>
        public static IEnumerable<TSymbol> FindSimilarSymbols<TSymbol>(TSymbol symbol, Compilation compilation, CancellationToken cancellationToken = default(CancellationToken))
            where TSymbol : ISymbol
        {
            if (symbol == null)
            {
                throw new ArgumentNullException(nameof(symbol));
            }

            if (compilation == null)
            {
                throw new ArgumentNullException(nameof(compilation));
            }

            var key = symbol.GetSymbolKey();
            return key.Resolve(compilation, cancellationToken: cancellationToken).GetAllSymbols().OfType<TSymbol>();
        }
    }
}
