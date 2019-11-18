// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.VisualStudio.GraphModel;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Progression
{
    internal sealed class SearchGraphQuery : IGraphQuery
    {
        private readonly string _searchPattern;

        public SearchGraphQuery(string searchPattern)
        {
            _searchPattern = searchPattern;
        }

        public async Task<GraphBuilder> GetGraphAsync(Solution solution, IGraphContext context, CancellationToken cancellationToken)
        {
            var graphBuilder = await GraphBuilder.CreateForInputNodesAsync(solution, context.InputNodes, cancellationToken).ConfigureAwait(false);

            var searchTasks = solution.Projects
                .Where(p => p.FilePath != null)
                .Select(p => ProcessProjectAsync(p, graphBuilder, cancellationToken))
                .ToArray();
            await Task.WhenAll(searchTasks).ConfigureAwait(false);

            return graphBuilder;
        }

        private async Task ProcessProjectAsync(Project project, GraphBuilder graphBuilder, CancellationToken cancellationToken)
        {
            var cacheService = project.Solution.Services.CacheService;
            if (cacheService != null)
            {
                using (cacheService.EnableCaching(project.Id))
                {
                    var results = await FindNavigableSourceSymbolsAsync(project, cancellationToken).ConfigureAwait(false);

                    foreach (var symbol in results)
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        if (symbol is INamedTypeSymbol namedType)
                        {
                            await AddLinkedNodeForTypeAsync(project, namedType, graphBuilder, symbol.DeclaringSyntaxReferences.Select(d => d.SyntaxTree)).ConfigureAwait(false);
                        }
                        else
                        {
                            await AddLinkedNodeForMemberAsync(project, symbol, graphBuilder).ConfigureAwait(false);
                        }
                    }
                }
            }
        }

        private async Task<GraphNode> AddLinkedNodeForTypeAsync(Project project, INamedTypeSymbol namedType, GraphBuilder graphBuilder, IEnumerable<SyntaxTree> syntaxTrees)
        {
            // If this named type is contained in a parent type, then just link farther up
            if (namedType.ContainingType != null)
            {
                var parentTypeNode = await AddLinkedNodeForTypeAsync(project, namedType.ContainingType, graphBuilder, syntaxTrees).ConfigureAwait(false);
                var typeNode = await graphBuilder.AddNodeForSymbolAsync(namedType, relatedNode: parentTypeNode).ConfigureAwait(false);
                graphBuilder.AddLink(parentTypeNode, GraphCommonSchema.Contains, typeNode);

                return typeNode;
            }
            else
            {
                // From here, we can link back up to the containing project item
                var typeNode = await graphBuilder.AddNodeForSymbolAsync(namedType, contextProject: project, contextDocument: null).ConfigureAwait(false);

                foreach (var tree in syntaxTrees)
                {
                    var document = project.Solution.GetDocument(tree);
                    Contract.ThrowIfNull(document);

                    var documentNode = graphBuilder.AddNodeForDocument(document);
                    graphBuilder.AddLink(documentNode, GraphCommonSchema.Contains, typeNode);
                }

                return typeNode;
            }
        }

        private async Task<GraphNode> AddLinkedNodeForMemberAsync(Project project, ISymbol member, GraphBuilder graphBuilder)
        {
            Contract.ThrowIfNull(member.ContainingType);

            var trees = member.DeclaringSyntaxReferences.Select(d => d.SyntaxTree);

            var parentTypeNode = await AddLinkedNodeForTypeAsync(project, member.ContainingType, graphBuilder, trees).ConfigureAwait(false);
            var memberNode = await graphBuilder.AddNodeForSymbolAsync(member, relatedNode: parentTypeNode).ConfigureAwait(false);
            graphBuilder.AddLink(parentTypeNode, GraphCommonSchema.Contains, memberNode);

            return memberNode;
        }

        internal async Task<ImmutableArray<ISymbol>> FindNavigableSourceSymbolsAsync(
            Project project, CancellationToken cancellationToken)
        {
            var declarations = await DeclarationFinder.FindSourceDeclarationsWithPatternAsync(
                project, _searchPattern, SymbolFilter.TypeAndMember, cancellationToken).ConfigureAwait(false);

            var symbols = declarations.SelectAsArray(d => d.Symbol);

            var results = ArrayBuilder<ISymbol>.GetInstance();

            foreach (var symbol in symbols)
            {
                cancellationToken.ThrowIfCancellationRequested();

                // Ignore constructors and namespaces.  We don't want to expose them through this API.
                if (symbol.IsConstructor() ||
                    symbol.IsStaticConstructor() ||
                    symbol is INamespaceSymbol)
                {
                    continue;
                }

                // Ignore symbols that have no source location.  We don't want to expose them through this API.
                if (!symbol.Locations.Any(loc => loc.IsInSource))
                {
                    continue;
                }

                results.Add(symbol);

                // also report matching constructors (using same match result as type)
                if (symbol is INamedTypeSymbol namedType)
                {
                    foreach (var constructor in namedType.Constructors)
                    {
                        // only constructors that were explicitly declared
                        if (!constructor.IsImplicitlyDeclared)
                        {
                            results.Add(constructor);
                        }
                    }
                }

                // report both parts of partial methods
                if (symbol is IMethodSymbol { PartialImplementationPart: { } } method)
                {
                    results.Add(method);
                }
            }

            return results.ToImmutableAndFree();
        }
    }
}
