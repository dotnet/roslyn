// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CodeFixes.AddImport
{
    internal abstract partial class AbstractAddImportCodeFixProvider<TSimpleNameSyntax>
    {
        private class SymbolReferenceFinder
        {
            private readonly CancellationToken _cancellationToken;
            private readonly Diagnostic _diagnostic;
            private readonly Document _document;
            private readonly SemanticModel _semanticModel;

            private readonly INamedTypeSymbol _containingType;
            private readonly ISymbol _containingTypeOrAssembly;
            private readonly ISet<INamespaceSymbol> _namespacesInScope;
            private readonly ISyntaxFactsService _syntaxFacts;
            private readonly AbstractAddImportCodeFixProvider<TSimpleNameSyntax> _owner;

            private readonly SyntaxNode _node;

            public SymbolReferenceFinder(
                AbstractAddImportCodeFixProvider<TSimpleNameSyntax> owner,
                Document document, SemanticModel semanticModel, Diagnostic diagnostic, SyntaxNode node, CancellationToken cancellationToken)
            {
                _owner = owner;
                _document = document;
                _semanticModel = semanticModel;
                _diagnostic = diagnostic;
                _node = node;
                _cancellationToken = cancellationToken;

                _containingType = semanticModel.GetEnclosingNamedType(node.SpanStart, cancellationToken);
                _containingTypeOrAssembly = _containingType ?? (ISymbol)semanticModel.Compilation.Assembly;
                _namespacesInScope = owner.GetNamespacesInScope(semanticModel, node, cancellationToken);
                _syntaxFacts = document.Project.LanguageServices.GetService<ISyntaxFactsService>();
            }

            internal Task<List<SymbolReference>> FindInAllProjectSymbolsAsync(
                Project project, bool exact)
            {
                var searchScope = new AllSymbolsProjectSearchScope(project, exact, _cancellationToken);
                return DoAsync(searchScope);
            }

            internal Task<List<SymbolReference>> FindInSourceProjectSymbolsAsync(
                ConcurrentDictionary<Project, AsyncLazy<IAssemblySymbol>> projectToAssembly,
                Project project, bool exact)
            {
                var searchScope = new SourceSymbolsProjectSearchScope(projectToAssembly, project, exact, _cancellationToken);
                return DoAsync(searchScope);
            }

            internal Task<List<SymbolReference>> FindInMetadataAsync(
                Solution solution, IAssemblySymbol assembly, PortableExecutableReference metadataReference, bool exact)
            {
                var searchScope = new MetadataSymbolsSearchScope(solution, assembly, metadataReference, exact, _cancellationToken);
                return DoAsync(searchScope);
            }

            private async Task<List<SymbolReference>> DoAsync(SearchScope searchScope)
            {
                // Spin off tasks to do all our searching.
                var tasks = new List<Task<IList<SymbolReference>>>
                {
                    this.GetNamespacesForMatchingTypesAsync(searchScope),
                    this.GetMatchingTypesAsync(searchScope),
                    this.GetNamespacesForMatchingNamespacesAsync(searchScope),
                    this.GetNamespacesForMatchingFieldsAndPropertiesAsync(searchScope),
                    this.GetNamespacesForMatchingExtensionMethodsAsync(searchScope),
                };

                // Searching for things like "Add" (for collection initializers) and "Select"
                // (for extension methods) should only be done when doing an 'exact' search.
                // We should not do fuzzy searches for these names.  In this case it's not
                // like the user was writing Add or Select, but instead we're looking for
                // viable symbols with those names to make a collection initializer or 
                // query expression valid.
                if (searchScope.Exact)
                {
                    tasks.Add(this.GetNamespacesForCollectionInitializerMethodsAsync(searchScope));
                    tasks.Add(this.GetNamespacesForQueryPatternsAsync(searchScope));
                }

                await Task.WhenAll(tasks).ConfigureAwait(false);
                _cancellationToken.ThrowIfCancellationRequested();

                List<SymbolReference> allReferences = null;
                foreach (var task in tasks)
                {
                    var taskResult = task.Result;
                    if (taskResult?.Count > 0)
                    {
                        allReferences = allReferences ?? new List<SymbolReference>();
                        allReferences.AddRange(taskResult);
                    }
                }

                return DeDupeAndSortReferences(allReferences);
            }

            private List<SymbolReference> DeDupeAndSortReferences(List<SymbolReference> allReferences)
            {
                if (allReferences == null)
                {
                    return null;
                }

                return allReferences
                    .Distinct()
                    .Where(NotNull)
                    .Where(NotGlobalNamespace)
                    .Order()
                    .ToList();
            }

            private async Task<IList<SymbolReference>> GetNamespacesForMatchingTypesAsync(SearchScope searchScope)
            {
                TSimpleNameSyntax nameNode;
                if (!_owner.CanAddImportForType(_diagnostic, _node, out nameNode))
                {
                    return null;
                }

                string name;
                int arity;
                bool inAttributeContext, hasIncompleteParentMember;
                CalculateContext(nameNode, _syntaxFacts, out name, out arity, out inAttributeContext, out hasIncompleteParentMember);

                var symbols = await GetTypeSymbols(searchScope, name, nameNode, inAttributeContext).ConfigureAwait(false);
                if (symbols == null)
                {
                    return null;
                }

                return GetNamespacesForMatchingTypesAsync(searchScope, arity, inAttributeContext, hasIncompleteParentMember, symbols);
            }

            private async Task<IList<SymbolReference>> GetMatchingTypesAsync(SearchScope searchScope)
            {
                TSimpleNameSyntax nameNode;
                if (!_owner.CanAddImportForType(_diagnostic, _node, out nameNode))
                {
                    return null;
                }

                string name;
                int arity;
                bool inAttributeContext, hasIncompleteParentMember;
                CalculateContext(nameNode, _syntaxFacts, out name, out arity, out inAttributeContext, out hasIncompleteParentMember);

                var symbols = await GetTypeSymbols(searchScope, name, nameNode, inAttributeContext).ConfigureAwait(false);
                if (symbols == null)
                {
                    return null;
                }

                return GetMatchingTypes(searchScope, name, arity, inAttributeContext, symbols, hasIncompleteParentMember);
            }

            private async Task<IEnumerable<SearchResult<ITypeSymbol>>> GetTypeSymbols(
                SearchScope searchScope,
                string name,
                TSimpleNameSyntax nameNode,
                bool inAttributeContext)
            {
                if (_cancellationToken.IsCancellationRequested)
                {
                    return null;
                }

                if (ExpressionBinds(nameNode, checkForExtensionMethods: false))
                {
                    return null;
                }

                var symbols = await searchScope.FindDeclarationsAsync(name, nameNode, SymbolFilter.Type).ConfigureAwait(false);

                // also lookup type symbols with the "Attribute" suffix.
                if (inAttributeContext)
                {
                    var attributeSymbols = await searchScope.FindDeclarationsAsync(name + "Attribute", nameNode, SymbolFilter.Type).ConfigureAwait(false);

                    symbols = symbols.Concat(
                        attributeSymbols.Select(r => r.WithDesiredName(r.DesiredName.GetWithoutAttributeSuffix(isCaseSensitive: false))));
                }

                return OfType<ITypeSymbol>(symbols);
            }

            protected bool ExpressionBinds(TSimpleNameSyntax nameNode, bool checkForExtensionMethods)
            {
                // See if the name binds to something other then the error type. If it does, there's nothing further we need to do.
                // For extension methods, however, we will continue to search if there exists any better matched method.
                _cancellationToken.ThrowIfCancellationRequested();
                var symbolInfo = _semanticModel.GetSymbolInfo(nameNode, _cancellationToken);
                if (symbolInfo.CandidateReason == CandidateReason.OverloadResolutionFailure && !checkForExtensionMethods)
                {
                    return true;
                }

                return symbolInfo.Symbol != null;
            }

            private async Task<IList<SymbolReference>> GetNamespacesForMatchingNamespacesAsync(
                SearchScope searchScope)
            {
                TSimpleNameSyntax nameNode;
                if (!_owner.CanAddImportForNamespace(_diagnostic, _node, out nameNode))
                {
                    return null;
                }

                string name;
                int arity;
                _syntaxFacts.GetNameAndArityOfSimpleName(nameNode, out name, out arity);

                if (arity > 0)
                {
                    return null;
                }

                if (ExpressionBinds(nameNode, checkForExtensionMethods: false))
                {
                    return null;
                }

                var symbols = await searchScope.FindDeclarationsAsync(name, nameNode, SymbolFilter.Namespace).ConfigureAwait(false);

                return GetProposedNamespaces(
                    searchScope, OfType<INamespaceSymbol>(symbols).Select(s => s.WithSymbol(s.Symbol.ContainingNamespace)));
            }

            private async Task<IList<SymbolReference>> GetNamespacesForMatchingExtensionMethodsAsync(SearchScope searchScope)
            {
                TSimpleNameSyntax nameNode;
                if (!_owner.CanAddImportForMethod(_diagnostic, _syntaxFacts, _node, out nameNode))
                {
                    return null;
                }

                if (nameNode == null)
                {
                    return null;
                }

                var symbols = await GetSymbolsAsync(searchScope, nameNode).ConfigureAwait(false);
                var extensionMethods = FilterForExtensionMethods(searchScope, nameNode.Parent, symbols);

                return extensionMethods.ToList();
            }

            private async Task<IList<SymbolReference>> GetNamespacesForCollectionInitializerMethodsAsync(SearchScope searchScope)
            {
                TSimpleNameSyntax nameNode;
                if (!_owner.CanAddImportForMethod(_diagnostic, _syntaxFacts, _node, out nameNode))
                {
                    return null;
                }

                var methodSymbols = await GetAddMethodsAsync(searchScope, _node.Parent).ConfigureAwait(false);
                return GetProposedNamespaces(searchScope, methodSymbols.Select(m => m.WithSymbol(m.Symbol.ContainingNamespace)));
            }

            private async Task<IList<SymbolReference>> GetNamespacesForMatchingFieldsAndPropertiesAsync(
                SearchScope searchScope)
            {
                TSimpleNameSyntax nameNode;
                if (!_owner.CanAddImportForMethod(_diagnostic, _syntaxFacts, _node, out nameNode))
                {
                    return null;
                }

                if (nameNode == null)
                {
                    return null;
                }

                var symbols = await GetSymbolsAsync(searchScope, nameNode).ConfigureAwait(false);
                if (symbols != null)
                {
                    return FilterForFieldsAndProperties(searchScope, nameNode.Parent, symbols);
                }

                return null;
            }

            private async Task<IList<SymbolReference>> GetNamespacesForQueryPatternsAsync(SearchScope searchScope)
            {
                if (!_owner.CanAddImportForQuery(_diagnostic, _node))
                {
                    return null;
                }

                ITypeSymbol type = _owner.GetQueryClauseInfo(_semanticModel, _node, _cancellationToken);
                if (type == null)
                {
                    return null;
                }

                // find extension methods named "Select"
                var symbols = await searchScope.FindDeclarationsAsync("Select", nameNode: null, filter: SymbolFilter.Member).ConfigureAwait(false);

                // Note: there is no "desiredName" when doing this.  We're not going to do any
                // renames of the user code.  We're just looking for an extension method called 
                // "Select", but that name has no bearing on the code in question that we're
                // trying to fix up.
                var extensionMethodSymbols = OfType<IMethodSymbol>(symbols)
                    .Where(s => s.Symbol.IsExtensionMethod && _owner.IsViableExtensionMethod(s.Symbol, type))
                    .Select(s => s.WithDesiredName(null))
                    .ToList();

                return GetProposedNamespaces(
                    searchScope, extensionMethodSymbols.Select(s => s.WithSymbol(s.Symbol.ContainingNamespace)));
            }

            private List<SymbolReference> GetMatchingTypes(
                SearchScope searchScope, string name, int arity, bool inAttributeContext, IEnumerable<SearchResult<ITypeSymbol>> symbols, bool hasIncompleteParentMember)
            {
                var accessibleTypeSymbols = symbols
                    .Where(s => ArityAccessibilityAndAttributeContextAreCorrect(
                                    _semanticModel, s.Symbol, arity,
                                    inAttributeContext, hasIncompleteParentMember))
                    .ToList();

                return GetProposedTypes(searchScope, name, accessibleTypeSymbols);
            }

            private List<SymbolReference> GetNamespacesForMatchingTypesAsync(
                SearchScope searchScope, int arity, bool inAttributeContext, bool hasIncompleteParentMember,
                IEnumerable<SearchResult<ITypeSymbol>> symbols)
            {
                var accessibleTypeSymbols = symbols
                    .Where(s => s.Symbol.ContainingSymbol is INamespaceSymbol
                                && ArityAccessibilityAndAttributeContextAreCorrect(
                                    _semanticModel, s.Symbol, arity,
                                    inAttributeContext, hasIncompleteParentMember))
                    .ToList();

                return GetProposedNamespaces(searchScope, accessibleTypeSymbols.Select(s => s.WithSymbol(s.Symbol.ContainingNamespace)));
            }

            private List<SymbolReference> GetProposedNamespaces(SearchScope scope, IEnumerable<SearchResult<INamespaceSymbol>> namespaces)
            {
                // We only want to offer to add a using if we don't already have one.
                return
                    namespaces.Where(n => !n.Symbol.IsGlobalNamespace)
                              .Select(n => n.WithSymbol(_semanticModel.Compilation.GetCompilationNamespace(n.Symbol) ?? n.Symbol))
                              .Where(n => n.Symbol != null && !_namespacesInScope.Contains(n.Symbol))
                              .Select(n => scope.CreateReference(n))
                              .ToList();
            }

            private List<SymbolReference> GetProposedTypes(SearchScope searchScope, string name, List<SearchResult<ITypeSymbol>> accessibleTypeSymbols)
            {
                List<SymbolReference> result = null;
                if (accessibleTypeSymbols != null)
                {
                    foreach (var typeSymbol in accessibleTypeSymbols)
                    {
                        if (typeSymbol.Symbol?.ContainingType != null)
                        {
                            result = result ?? new List<SymbolReference>();
                            result.Add(searchScope.CreateReference(typeSymbol.WithSymbol(typeSymbol.Symbol.ContainingType)));
                        }
                    }
                }

                return result;
            }

            private Task<IEnumerable<SearchResult<ISymbol>>> GetSymbolsAsync(SearchScope searchScope, TSimpleNameSyntax nameNode)
            {
                _cancellationToken.ThrowIfCancellationRequested();

                // See if the name binds.  If it does, there's nothing further we need to do.
                if (ExpressionBinds(nameNode, checkForExtensionMethods: true))
                {
                    return SpecializedTasks.EmptyEnumerable<SearchResult<ISymbol>>();
                }

                string name;
                int arity;
                _syntaxFacts.GetNameAndArityOfSimpleName(nameNode, out name, out arity);
                if (name == null)
                {
                    return SpecializedTasks.EmptyEnumerable<SearchResult<ISymbol>>();
                }

                return searchScope.FindDeclarationsAsync(name, nameNode, SymbolFilter.Member);
            }

            private IEnumerable<SymbolReference> FilterForExtensionMethods(
                SearchScope searchScope, SyntaxNode expression, IEnumerable<SearchResult<ISymbol>> symbols)
            {
                var extensionMethodSymbols = OfType<IMethodSymbol>(symbols)
                    .Where(s => s.Symbol.IsExtensionMethod &&
                                s.Symbol.IsAccessibleWithin(_semanticModel.Compilation.Assembly) == true &&
                                _owner.IsViableExtensionMethod(s.Symbol, expression, _semanticModel, _syntaxFacts, _cancellationToken))
                    .ToList();

                return GetProposedNamespaces(
                    searchScope, extensionMethodSymbols.Select(s => s.WithSymbol(s.Symbol.ContainingNamespace)));
            }

            private IList<SymbolReference> FilterForFieldsAndProperties(
                SearchScope searchScope, SyntaxNode expression, IEnumerable<SearchResult<ISymbol>> symbols)
            {
                var propertySymbols = OfType<IPropertySymbol>(symbols)
                    .Where(property => property.Symbol.IsAccessibleWithin(_semanticModel.Compilation.Assembly) == true &&
                                       _owner.IsViableProperty(property.Symbol, expression, _semanticModel, _syntaxFacts, _cancellationToken))
                    .ToList();

                var fieldSymbols = OfType<IFieldSymbol>(symbols)
                    .Where(field => field.Symbol.IsAccessibleWithin(_semanticModel.Compilation.Assembly) == true &&
                                    _owner.IsViableField(field.Symbol, expression, _semanticModel, _syntaxFacts, _cancellationToken))
                    .ToList();

                return GetProposedNamespaces(
                    searchScope,
                        propertySymbols.Select(s => s.WithSymbol(s.Symbol.ContainingNamespace)).Concat(
                        fieldSymbols.Select(s => s.WithSymbol(s.Symbol.ContainingNamespace))));
            }

            private async Task<IEnumerable<SearchResult<IMethodSymbol>>> GetAddMethodsAsync(
                SearchScope searchScope, SyntaxNode expression)
            {
                string name;
                int arity;
                _syntaxFacts.GetNameAndArityOfSimpleName(_node, out name, out arity);
                if (name != null || !_owner.IsAddMethodContext(_node, _semanticModel))
                {
                    return SpecializedCollections.EmptyEnumerable<SearchResult<IMethodSymbol>>();
                }

                // Note: there is no desiredName for these search results.  We're searching for
                // extension methods called "Add", but we have no intention of renaming any 
                // of the existing user code to that name.
                var symbols = await searchScope.FindDeclarationsAsync("Add", nameNode: null, filter: SymbolFilter.Member).ConfigureAwait(false);
                return OfType<IMethodSymbol>(symbols)
                    .Where(s => s.Symbol.IsExtensionMethod &&
                                s.Symbol.IsAccessibleWithin(_semanticModel.Compilation.Assembly) == true &&
                                     _owner.IsViableExtensionMethod(s.Symbol, expression, _semanticModel, _syntaxFacts, _cancellationToken))
                    .Select(s => s.WithDesiredName(null));
            }

            private IEnumerable<SearchResult<T>> OfType<T>(IEnumerable<SearchResult<ISymbol>> symbols) where T : ISymbol
            {
                return symbols.Where(s => s.Symbol is T).Select(s => s.WithSymbol((T)s.Symbol));
            }
        }
    }
}