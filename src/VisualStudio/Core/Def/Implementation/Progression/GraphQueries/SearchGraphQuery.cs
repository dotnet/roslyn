// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Microsoft.VisualStudio.GraphModel;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Progression
{
    internal sealed class SearchGraphQuery : IGraphQuery
    {
        private static readonly char[] DotCharacterArray = new[] { '.' };

        private readonly string _lastPatternPart;
        private readonly string _entireSearchPattern;

        public SearchGraphQuery(string searchPattern)
        {
            // If the pattern has a dot in it, then when we search the compilations, we actually
            // want to only filter by the last name portion of hte pattern (as the compilers 
            // only support returning the basic name of a symbol when searching and filtering 
            // them).  Then, once we have all the results the compiler returned, we check and
            // verify they match against the full dotted pattern.
            if (searchPattern.IndexOf('.') >= 0)
            {
                var patternParts = searchPattern.Split(DotCharacterArray, StringSplitOptions.RemoveEmptyEntries);
                _lastPatternPart = patternParts.LastOrDefault();
            }

            _entireSearchPattern = searchPattern;
            _lastPatternPart = _lastPatternPart ?? searchPattern;
        }

        public async Task<GraphBuilder> GetGraphAsync(Solution solution, IGraphContext context, CancellationToken cancellationToken)
        {
            var graphBuilder = await GraphBuilder.CreateForInputNodesAsync(solution, context.InputNodes, cancellationToken).ConfigureAwait(false);

            var searchTasks = solution.Projects.Select(p => ProcessProjectAsync(p, graphBuilder, cancellationToken)).ToArray();
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

                    foreach (var result in results)
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        var symbol = result.Item1;

                        if (symbol is INamedTypeSymbol)
                        {
                            await AddLinkedNodeForType(project, (INamedTypeSymbol)symbol, graphBuilder, symbol.DeclaringSyntaxReferences.Select(d => d.SyntaxTree)).ConfigureAwait(false);
                        }
                        else
                        {
                            await AddLinkedNodeForMemberAsync(project, symbol, graphBuilder).ConfigureAwait(false);
                        }
                    }
                }
            }
        }

        private async Task<GraphNode> AddLinkedNodeForType(Project project, INamedTypeSymbol namedType, GraphBuilder graphBuilder, IEnumerable<SyntaxTree> syntaxTrees)
        {
            // If this named type is contained in a parent type, then just link farther up
            if (namedType.ContainingType != null)
            {
                var parentTypeNode = await AddLinkedNodeForType(project, namedType.ContainingType, graphBuilder, syntaxTrees).ConfigureAwait(false);
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

            var parentTypeNode = await AddLinkedNodeForType(project, member.ContainingType, graphBuilder, trees).ConfigureAwait(false);
            var memberNode = await graphBuilder.AddNodeForSymbolAsync(member, relatedNode: parentTypeNode).ConfigureAwait(false);
            graphBuilder.AddLink(parentTypeNode, GraphCommonSchema.Contains, memberNode);

            return memberNode;
        }

        internal async Task<IEnumerable<ValueTuple<ISymbol, IEnumerable<PatternMatch>>>> FindNavigableSourceSymbolsAsync(
            Project project, CancellationToken cancellationToken)
        {
            var results = new List<ValueTuple<ISymbol, IEnumerable<PatternMatch>>>();

            var patternMatcher = new PatternMatcher();
            var symbols = await SymbolFinder.FindSourceDeclarationsAsync(
                project, k => patternMatcher.MatchPattern(k, _lastPatternPart) != null, SymbolFilter.TypeAndMember, cancellationToken).ConfigureAwait(false);

            symbols = symbols.Where(s =>
                !s.IsConstructor()
                && !s.IsStaticConstructor() // not constructors, they get matched on type name
                && !(s is INamespaceSymbol) // not namespaces
                && s.Locations.Any(loc => loc.IsInSource)); // only source symbols

            foreach (var symbol in symbols)
            {
                cancellationToken.ThrowIfCancellationRequested();

                // If the search query included a dotted pattern, then verify that this symbol, and 
                // it's container, matches the dotted pattern.
                var matches = _lastPatternPart == null
                    ? patternMatcher.MatchPattern(GetSearchName(symbol), _entireSearchPattern)
                    : patternMatcher.MatchPattern(GetSearchName(symbol), GetContainer(symbol), _entireSearchPattern);

                if (matches == null)
                {
                    continue;
                }

                results.Add(ValueTuple.Create(symbol, matches));

                // also report matching constructors (using same match result as type)
                var namedType = symbol as INamedTypeSymbol;
                if (namedType != null)
                {
                    foreach (var constructor in namedType.Constructors)
                    {
                        // only constructors that were explicitly declared
                        if (!constructor.IsImplicitlyDeclared)
                        {
                            results.Add(ValueTuple.Create((ISymbol)constructor, matches));
                        }
                    }
                }

                // report both parts of partial methods
                var method = symbol as IMethodSymbol;
                if (method != null && method.PartialImplementationPart != null)
                {
                    results.Add(ValueTuple.Create((ISymbol)method, matches));
                }
            }

            return results;
        }

        public static readonly SymbolDisplayFormat DottedNameFormat =
            new SymbolDisplayFormat(
                globalNamespaceStyle: SymbolDisplayGlobalNamespaceStyle.Omitted,
                typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
                delegateStyle: SymbolDisplayDelegateStyle.NameOnly,
                extensionMethodStyle: SymbolDisplayExtensionMethodStyle.StaticMethod,
                propertyStyle: SymbolDisplayPropertyStyle.NameOnly);

        private static string GetContainer(ISymbol symbol)
        {
            var container = symbol.ContainingSymbol;
            if (container == null)
            {
                return null;
            }

            return container.ToDisplayString(DottedNameFormat);
        }

        private static string GetSearchName(ISymbol symbol)
        {
            if (symbol.IsConstructor() || symbol.IsStaticConstructor())
            {
                return symbol.ContainingType.Name;
            }
            else if (symbol.IsIndexer() && symbol.Name == WellKnownMemberNames.Indexer)
            {
                return "this";
            }

            return symbol.Name;
        }
    }
}
