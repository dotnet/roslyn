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
        /// <summary>
        /// Obsolete.  Use <see cref="FindSymbolAtPositionAsync(SemanticModel, int, Workspace, CancellationToken)"/>.
        /// </summary>
        [Obsolete("Use FindSymbolAtPositionAsync instead.")]
        public static ISymbol FindSymbolAtPosition(
            SemanticModel semanticModel,
            int position,
            Workspace workspace,
            CancellationToken cancellationToken = default)
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
        public static async Task<ISymbol> FindSymbolAtPositionAsync(
            SemanticModel semanticModel,
            int position,
            Workspace workspace,
            CancellationToken cancellationToken = default)
        {
            var semanticInfo = await GetSemanticInfoAtPositionAsync(
                semanticModel, position, workspace, cancellationToken: cancellationToken).ConfigureAwait(false);
            return semanticInfo.GetAnySymbol(includeType: false);
        }

        internal static async Task<TokenSemanticInfo> GetSemanticInfoAtPositionAsync(
            SemanticModel semanticModel,
            int position,
            Workspace workspace,
            CancellationToken cancellationToken)
        {
            var token = await GetTokenAtPositionAsync(semanticModel, position, workspace, cancellationToken).ConfigureAwait(false);

            if (token != default &&
                token.Span.IntersectsWith(position))
            {
                return semanticModel.GetSemanticInfo(token, workspace, cancellationToken);
            }

            return TokenSemanticInfo.Empty;
        }

        private static Task<SyntaxToken> GetTokenAtPositionAsync(
            SemanticModel semanticModel,
            int position,
            Workspace workspace,
            CancellationToken cancellationToken)
        {
            var syntaxTree = semanticModel.SyntaxTree;
            var syntaxFacts = workspace.Services.GetLanguageServices(semanticModel.Language).GetService<ISyntaxFactsService>();

            return syntaxTree.GetTouchingTokenAsync(position, syntaxFacts.IsBindableToken, cancellationToken, findInsideTrivia: true);
        }

        public static async Task<ISymbol> FindSymbolAtPositionAsync(
            Document document,
            int position,
            CancellationToken cancellationToken = default)
        {
            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            return await FindSymbolAtPositionAsync(semanticModel, position, document.Project.Solution.Workspace, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Finds the definition symbol declared in source code for a corresponding reference symbol. 
        /// Returns null if no such symbol can be found in the specified solution.
        /// </summary>
        public static async Task<ISymbol> FindSourceDefinitionAsync(
            ISymbol symbol, Solution solution, CancellationToken cancellationToken = default)
        {
            var result = await FindSourceDefinitionAsync(
                SymbolAndProjectId.Create(symbol, projectId: null),
                solution, cancellationToken).ConfigureAwait(false);
            return result.Symbol;
        }

        internal static Task<SymbolAndProjectId> FindSourceDefinitionAsync(
            SymbolAndProjectId symbolAndProjectId, Solution solution, CancellationToken cancellationToken = default)
        {
            var symbol = symbolAndProjectId.Symbol;
            if (symbol != null)
            {
                symbol = symbol.GetOriginalUnreducedDefinition();
                symbolAndProjectId = symbolAndProjectId.WithSymbol(symbol);
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
                        return FindSourceDefinitionWorkerAsync(symbolAndProjectId, solution, cancellationToken);
                }
            }

            return SpecializedTasks.Default<SymbolAndProjectId>();
        }

        private static async Task<SymbolAndProjectId> FindSourceDefinitionWorkerAsync(
            SymbolAndProjectId symbolAndProjectId,
            Solution solution,
            CancellationToken cancellationToken)
        {
            var symbol = symbolAndProjectId.Symbol;
            // If it's already in source, then we might already be done
            if (InSource(symbol))
            {
                // If our symbol doesn't have a containing assembly, there's nothing better we can do to map this
                // symbol somewhere else. The common case for this is a merged INamespaceSymbol that spans assemblies.
                if (symbol.ContainingAssembly == null)
                {
                    return symbolAndProjectId;
                }

                // Just because it's a source symbol doesn't mean we have the final symbol we actually want. In retargeting cases,
                // the retargeted symbol is from "source" but isn't equal to the actual source definition in the other project. Thus,
                // we only want to return symbols from source here if it actually came from a project's compilation's assembly. If it isn't
                // then we have a retargeting scenario and want to take our usual path below as if it was a metadata reference
                foreach (var sourceProject in solution.Projects)
                {

                    // If our symbol is actually a "regular" source symbol, then we know the compilation is holding the symbol alive
                    // and thus TryGetCompilation is sufficient. For another example of this pattern, see Solution.GetProject(IAssemblySymbol)
                    // which we happen to call below.
                    if (sourceProject.TryGetCompilation(out var compilation))
                    {
                        if (symbol.ContainingAssembly.Equals(compilation.Assembly))
                        {
                            return SymbolAndProjectId.Create(symbol, sourceProject.Id);
                        }
                    }
                }
            }
            else if (!symbol.Locations.Any(loc => loc.IsInMetadata))
            {
                // We have a symbol that's neither in source nor metadata
                return default;
            }

            var project = solution.GetProject(symbol.ContainingAssembly, cancellationToken);
            if (project != null && project.SupportsCompilation)
            {
                var symbolId = symbol.GetSymbolKey();
                var compilation = await project.GetCompilationAsync(cancellationToken).ConfigureAwait(false);
                var result = symbolId.Resolve(compilation, ignoreAssemblyKey: true, cancellationToken: cancellationToken);

                if (result.Symbol != null && InSource(result.Symbol))
                {
                    return SymbolAndProjectId.Create(result.Symbol, project.Id);
                }
                else
                {
                    return SymbolAndProjectId.Create(result.CandidateSymbols.FirstOrDefault(InSource), project.Id);
                }
            }

            return default;
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
        public static IEnumerable<TSymbol> FindSimilarSymbols<TSymbol>(TSymbol symbol, Compilation compilation, CancellationToken cancellationToken = default)
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

            // We may be talking about different compilations.  So do not try to resolve locations.
            var result = new HashSet<TSymbol>();
            var resolution = key.Resolve(compilation, resolveLocations: false, cancellationToken: cancellationToken);
            foreach (var current in resolution.OfType<TSymbol>())
            {
                result.Add(current);
            }

            return result;
        }
    }
}
