// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Packaging;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.SymbolSearch;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.AddImport
{
    internal abstract partial class AbstractAddImportFeatureService<TSimpleNameSyntax>
    {
        private partial class SymbolReferenceFinder
        {
            private const string AttributeSuffix = nameof(Attribute);

            private readonly string _diagnosticId;
            private readonly Document _document;
            private readonly SemanticModel _semanticModel;

            private readonly ISet<INamespaceSymbol> _namespacesInScope;
            private readonly ISyntaxFactsService _syntaxFacts;
            private readonly AbstractAddImportFeatureService<TSimpleNameSyntax> _owner;

            private readonly SyntaxNode _node;
            private readonly ISymbolSearchService _symbolSearchService;
            private readonly bool _searchReferenceAssemblies;
            private readonly ImmutableArray<PackageSource> _packageSources;

            public SymbolReferenceFinder(
                AbstractAddImportFeatureService<TSimpleNameSyntax> owner,
                Document document, SemanticModel semanticModel,
                string diagnosticId, SyntaxNode node,
                ISymbolSearchService symbolSearchService,
                bool searchReferenceAssemblies,
                ImmutableArray<PackageSource> packageSources,
                CancellationToken cancellationToken)
            {
                _owner = owner;
                _document = document;
                _semanticModel = semanticModel;
                _diagnosticId = diagnosticId;
                _node = node;

                _symbolSearchService = symbolSearchService;
                _searchReferenceAssemblies = searchReferenceAssemblies;
                _packageSources = packageSources;

                if (_searchReferenceAssemblies || packageSources.Length > 0)
                {
                    Contract.ThrowIfNull(symbolSearchService);
                }

                _syntaxFacts = document.GetLanguageService<ISyntaxFactsService>();

                _namespacesInScope = GetNamespacesInScope(cancellationToken);
            }

            private ISet<INamespaceSymbol> GetNamespacesInScope(CancellationToken cancellationToken)
            {
                // Add all the namespaces brought in by imports/usings.
                var set = _owner.GetImportNamespacesInScope(_semanticModel, _node, cancellationToken);

                // Also add all the namespaces we're contained in.  We don't want
                // to add imports for these namespaces either.
                for (var containingNamespace = _semanticModel.GetEnclosingNamespace(_node.SpanStart, cancellationToken);
                     containingNamespace != null;
                     containingNamespace = containingNamespace.ContainingNamespace)
                {
                    set.Add(MapToCompilationNamespaceIfPossible(containingNamespace));
                }

                return set;
            }

            private INamespaceSymbol MapToCompilationNamespaceIfPossible(INamespaceSymbol containingNamespace)
                => _semanticModel.Compilation.GetCompilationNamespace(containingNamespace) ?? containingNamespace;

            internal Task<ImmutableArray<SymbolReference>> FindInAllSymbolsInStartingProjectAsync(
                bool exact, CancellationToken cancellationToken)
            {
                var searchScope = new AllSymbolsProjectSearchScope(
                    _owner, _document.Project, exact, cancellationToken);
                return DoAsync(searchScope);
            }

            internal Task<ImmutableArray<SymbolReference>> FindInSourceSymbolsInProjectAsync(
                ConcurrentDictionary<Project, AsyncLazy<IAssemblySymbol>> projectToAssembly,
                Project project, bool exact, CancellationToken cancellationToken)
            {
                var searchScope = new SourceSymbolsProjectSearchScope(
                    _owner, projectToAssembly, project, exact, cancellationToken);
                return DoAsync(searchScope);
            }

            internal Task<ImmutableArray<SymbolReference>> FindInMetadataSymbolsAsync(
                IAssemblySymbol assembly, ProjectId assemblyProjectId, PortableExecutableReference metadataReference,
                bool exact, CancellationToken cancellationToken)
            {
                var searchScope = new MetadataSymbolsSearchScope(
                    _owner, _document.Project.Solution, assembly, assemblyProjectId,
                    metadataReference, exact, cancellationToken);
                return DoAsync(searchScope);
            }

            private async Task<ImmutableArray<SymbolReference>> DoAsync(SearchScope searchScope)
            {
                searchScope.CancellationToken.ThrowIfCancellationRequested();

                // Spin off tasks to do all our searching in parallel
                var tasks = new List<Task<ImmutableArray<SymbolReference>>>
                {
                    GetReferencesForMatchingTypesAsync(searchScope),
                    GetReferencesForMatchingNamespacesAsync(searchScope),
                    GetReferencesForMatchingFieldsAndPropertiesAsync(searchScope),
                    GetReferencesForMatchingExtensionMethodsAsync(searchScope),
                };

                // Searching for things like "Add" (for collection initializers) and "Select"
                // (for extension methods) should only be done when doing an 'exact' search.
                // We should not do fuzzy searches for these names.  In this case it's not
                // like the user was writing Add or Select, but instead we're looking for
                // viable symbols with those names to make a collection initializer or 
                // query expression valid.
                if (searchScope.Exact)
                {
                    tasks.Add(GetReferencesForCollectionInitializerMethodsAsync(searchScope));
                    tasks.Add(GetReferencesForQueryPatternsAsync(searchScope));
                    tasks.Add(GetReferencesForDeconstructAsync(searchScope));
                    tasks.Add(GetReferencesForGetAwaiterAsync(searchScope));
                }

                await Task.WhenAll(tasks).ConfigureAwait(false);
                searchScope.CancellationToken.ThrowIfCancellationRequested();

                var allReferences = ArrayBuilder<SymbolReference>.GetInstance();
                foreach (var task in tasks)
                {
                    var taskResult = await task.ConfigureAwait(false);
                    allReferences.AddRange(taskResult);
                }

                return DeDupeAndSortReferences(allReferences.ToImmutableAndFree());
            }

            private ImmutableArray<SymbolReference> DeDupeAndSortReferences(ImmutableArray<SymbolReference> allReferences)
            {
                return allReferences
                    .Distinct()
                    .Where(NotNull)
                    .Where(NotGlobalNamespace)
                    .OrderBy((r1, r2) => r1.CompareTo(_document, r2))
                    .ToImmutableArray();
            }

            private void CalculateContext(
                TSimpleNameSyntax nameNode, ISyntaxFactsService syntaxFacts, out string name, out int arity,
                out bool inAttributeContext, out bool hasIncompleteParentMember, out bool looksGeneric)
            {
                // Has to be a simple identifier or generic name.
                syntaxFacts.GetNameAndArityOfSimpleName(nameNode, out name, out arity);

                inAttributeContext = syntaxFacts.IsAttributeName(nameNode);
                hasIncompleteParentMember = syntaxFacts.HasIncompleteParentMember(nameNode);
                looksGeneric = syntaxFacts.LooksGeneric(nameNode);
            }

            /// <summary>
            /// Searches for types that match the name the user has written.  Returns <see cref="SymbolReference"/>s
            /// to the <see cref="INamespaceSymbol"/>s or <see cref="INamedTypeSymbol"/>s those types are
            /// contained in.
            /// </summary>
            private async Task<ImmutableArray<SymbolReference>> GetReferencesForMatchingTypesAsync(SearchScope searchScope)
            {
                searchScope.CancellationToken.ThrowIfCancellationRequested();
                if (!_owner.CanAddImportForType(_diagnosticId, _node, out var nameNode))
                {
                    return ImmutableArray<SymbolReference>.Empty;
                }

                CalculateContext(
                    nameNode, _syntaxFacts,
                    out var name, out var arity, out var inAttributeContext,
                    out var hasIncompleteParentMember, out var looksGeneric);

                if (ExpressionBinds(nameNode, checkForExtensionMethods: false, cancellationToken: searchScope.CancellationToken))
                {
                    // If the expression bound, there's nothing to do.
                    return ImmutableArray<SymbolReference>.Empty;
                }

                var symbols = await searchScope.FindDeclarationsAsync(name, nameNode, SymbolFilter.Type).ConfigureAwait(false);

                // also lookup type symbols with the "Attribute" suffix if necessary.
                if (inAttributeContext)
                {
                    var attributeSymbols = await searchScope.FindDeclarationsAsync(name + AttributeSuffix, nameNode, SymbolFilter.Type).ConfigureAwait(false);

                    symbols = symbols.AddRange(
                        attributeSymbols.Select(r => r.WithDesiredName(r.DesiredName.GetWithoutAttributeSuffix(isCaseSensitive: false))));
                }

                var typeSymbols = OfType<ITypeSymbol>(symbols);

                // Only keep symbols which are accessible from the current location.
                var accessibleTypeSymbols = typeSymbols.WhereAsArray(
                    s => ArityAccessibilityAndAttributeContextAreCorrect(
                        s.Symbol, arity, inAttributeContext,
                        hasIncompleteParentMember, looksGeneric));

                // These types may be contained within namespaces, or they may be nested 
                // inside generic types.  Record these namespaces/types if it would be 
                // legal to add imports for them.

                var typesContainedDirectlyInNamespaces = accessibleTypeSymbols.WhereAsArray(s => s.Symbol.ContainingSymbol is INamespaceSymbol);
                var typesContainedDirectlyInTypes = accessibleTypeSymbols.WhereAsArray(s => s.Symbol.ContainingType != null);

                var namespaceReferences = GetNamespaceSymbolReferences(searchScope,
                    typesContainedDirectlyInNamespaces.SelectAsArray(r => r.WithSymbol(r.Symbol.ContainingNamespace)));

                var typeReferences = typesContainedDirectlyInTypes.SelectAsArray(
                    r => searchScope.CreateReference(r.WithSymbol(r.Symbol.ContainingType)));

                return namespaceReferences.Concat(typeReferences);
            }

            private bool ArityAccessibilityAndAttributeContextAreCorrect(
                ITypeSymbol symbol,
                int arity,
                bool inAttributeContext,
                bool hasIncompleteParentMember,
                bool looksGeneric)
            {
                if (inAttributeContext && !symbol.IsAttribute())
                {
                    return false;
                }

                if (!symbol.IsAccessibleWithin(_semanticModel.Compilation.Assembly))
                {
                    return false;
                }

                if (looksGeneric && symbol.GetTypeArguments().Length == 0)
                {
                    return false;
                }

                return arity == 0 || symbol.GetArity() == arity || hasIncompleteParentMember;
            }

            /// <summary>
            /// Searches for namespaces that match the name the user has written.  Returns <see cref="SymbolReference"/>s
            /// to the <see cref="INamespaceSymbol"/>s those namespaces are contained in.
            /// </summary>
            private async Task<ImmutableArray<SymbolReference>> GetReferencesForMatchingNamespacesAsync(
                SearchScope searchScope)
            {
                searchScope.CancellationToken.ThrowIfCancellationRequested();
                if (_owner.CanAddImportForNamespace(_diagnosticId, _node, out var nameNode))
                {
                    _syntaxFacts.GetNameAndArityOfSimpleName(nameNode, out var name, out var arity);

                    if (arity == 0 &&
                        !ExpressionBinds(nameNode, checkForExtensionMethods: false, cancellationToken: searchScope.CancellationToken))
                    {
                        var symbols = await searchScope.FindDeclarationsAsync(name, nameNode, SymbolFilter.Namespace).ConfigureAwait(false);
                        var namespaceSymbols = OfType<INamespaceSymbol>(symbols);
                        var containingNamespaceSymbols = OfType<INamespaceSymbol>(symbols).SelectAsArray(s => s.WithSymbol(s.Symbol.ContainingNamespace));

                        return GetNamespaceSymbolReferences(searchScope, containingNamespaceSymbols);
                    }
                }

                return ImmutableArray<SymbolReference>.Empty;
            }

            /// <summary>
            /// Specialized finder for the "Color Color" case.  Used when we have "Color.Black" and "Color"
            /// bound to a Field/Property, but not a type.  In this case, we want to look for namespaces
            /// containing 'Color' as if we import them it can resolve this issue.
            /// </summary>
            private async Task<ImmutableArray<SymbolReference>> GetReferencesForMatchingFieldsAndPropertiesAsync(
                SearchScope searchScope)
            {
                searchScope.CancellationToken.ThrowIfCancellationRequested();
                if (_owner.CanAddImportForMethod(_diagnosticId, _syntaxFacts, _node, out var nameNode) &&
                    nameNode != null)
                {
                    // We have code like "Color.Black".  "Color" bound to a 'Color Color' property, and
                    // 'Black' did not bind.  We want to find a type called 'Color' that will actually
                    // allow 'Black' to bind.
                    var syntaxFacts = _document.GetLanguageService<ISyntaxFactsService>();
                    if (syntaxFacts.IsNameOfMemberAccessExpression(nameNode))
                    {
                        var expression =
                            syntaxFacts.GetExpressionOfMemberAccessExpression(nameNode.Parent, allowImplicitTarget: true) ??
                            syntaxFacts.GetTargetOfMemberBinding(nameNode.Parent);
                        if (expression is TSimpleNameSyntax)
                        {
                            // Check if the expression before the dot binds to a property or field.
                            var symbol = _semanticModel.GetSymbolInfo(expression, searchScope.CancellationToken).GetAnySymbol();
                            if (symbol?.Kind == SymbolKind.Property || symbol?.Kind == SymbolKind.Field)
                            {
                                // Check if we have the 'Color Color' case.
                                var propertyOrFieldType = symbol.GetSymbolType();
                                if (propertyOrFieldType is INamedTypeSymbol propertyType &&
                                    Equals(propertyType.Name, symbol.Name))
                                {
                                    // Try to look up 'Color' as a type.
                                    var symbolResults = await searchScope.FindDeclarationsAsync(
                                        symbol.Name, (TSimpleNameSyntax)expression, SymbolFilter.Type).ConfigureAwait(false);

                                    // Return results that have accessible members.
                                    var namedTypeSymbols = OfType<INamedTypeSymbol>(symbolResults);
                                    var name = nameNode.GetFirstToken().ValueText;
                                    var namespaceResults =
                                        namedTypeSymbols.WhereAsArray(sr => HasAccessibleStaticFieldOrProperty(sr.Symbol, name))
                                                        .SelectAsArray(sr => sr.WithSymbol(sr.Symbol.ContainingNamespace));

                                    return GetNamespaceSymbolReferences(searchScope, namespaceResults);
                                }
                            }
                        }
                    }
                }

                return ImmutableArray<SymbolReference>.Empty;
            }

            private bool HasAccessibleStaticFieldOrProperty(INamedTypeSymbol namedType, string fieldOrPropertyName)
            {
                return namedType.GetMembers(fieldOrPropertyName)
                                .Any(m => (m is IFieldSymbol || m is IPropertySymbol) &&
                                          m.IsStatic &&
                                          m.IsAccessibleWithin(_semanticModel.Compilation.Assembly));
            }

            /// <summary>
            /// Searches for extension methods that match the name the user has written.  Returns
            /// <see cref="SymbolReference"/>s to the <see cref="INamespaceSymbol"/>s that contain
            /// the static classes that those extension methods are contained in.
            /// </summary>
            private async Task<ImmutableArray<SymbolReference>> GetReferencesForMatchingExtensionMethodsAsync(SearchScope searchScope)
            {
                searchScope.CancellationToken.ThrowIfCancellationRequested();
                if (_owner.CanAddImportForMethod(_diagnosticId, _syntaxFacts, _node, out var nameNode) &&
                    nameNode != null)
                {
                    searchScope.CancellationToken.ThrowIfCancellationRequested();

                    // See if the name binds.  If it does, there's nothing further we need to do.
                    if (!ExpressionBinds(nameNode, checkForExtensionMethods: true, cancellationToken: searchScope.CancellationToken))
                    {
                        _syntaxFacts.GetNameAndArityOfSimpleName(nameNode, out var name, out var arity);
                        if (name != null)
                        {
                            var symbols = await searchScope.FindDeclarationsAsync(name, nameNode, SymbolFilter.Member).ConfigureAwait(false);

                            var methodSymbols = OfType<IMethodSymbol>(symbols);

                            var extensionMethodSymbols = GetViableExtensionMethods(
                                methodSymbols, nameNode.Parent, searchScope.CancellationToken);

                            var namespaceSymbols = extensionMethodSymbols.SelectAsArray(s => s.WithSymbol(s.Symbol.ContainingNamespace));
                            return GetNamespaceSymbolReferences(searchScope, namespaceSymbols);
                        }
                    }
                }

                return ImmutableArray<SymbolReference>.Empty;
            }

            private ImmutableArray<SymbolResult<IMethodSymbol>> GetViableExtensionMethods(
                ImmutableArray<SymbolResult<IMethodSymbol>> methodSymbols,
                SyntaxNode expression, CancellationToken cancellationToken)
            {
                return GetViableExtensionMethodsWorker(methodSymbols).WhereAsArray(
                    s => _owner.IsViableExtensionMethod(s.Symbol, expression, _semanticModel, _syntaxFacts, cancellationToken));
            }

            private ImmutableArray<SymbolResult<IMethodSymbol>> GetViableExtensionMethods(
                ImmutableArray<SymbolResult<IMethodSymbol>> methodSymbols, ITypeSymbol typeSymbol)
            {
                return GetViableExtensionMethodsWorker(methodSymbols).WhereAsArray(
                    s => _owner.IsViableExtensionMethod(s.Symbol, typeSymbol));
            }

            private ImmutableArray<SymbolResult<IMethodSymbol>> GetViableExtensionMethodsWorker(
                ImmutableArray<SymbolResult<IMethodSymbol>> methodSymbols)
            {
                return methodSymbols.WhereAsArray(
                    s => s.Symbol.IsExtensionMethod &&
                         s.Symbol.IsAccessibleWithin(_semanticModel.Compilation.Assembly));
            }

            /// <summary>
            /// Searches for extension methods exactly called 'Add'.  Returns
            /// <see cref="SymbolReference"/>s to the <see cref="INamespaceSymbol"/>s that contain
            /// the static classes that those extension methods are contained in.
            /// </summary>
            private async Task<ImmutableArray<SymbolReference>> GetReferencesForCollectionInitializerMethodsAsync(SearchScope searchScope)
            {
                searchScope.CancellationToken.ThrowIfCancellationRequested();
                if (!_owner.CanAddImportForMethod(_diagnosticId, _syntaxFacts, _node, out var nameNode))
                {
                    return ImmutableArray<SymbolReference>.Empty;
                }

                _syntaxFacts.GetNameAndArityOfSimpleName(_node, out var name, out var arity);
                if (name != null || !_owner.IsAddMethodContext(_node, _semanticModel))
                {
                    return ImmutableArray<SymbolReference>.Empty;
                }

                var symbols = await searchScope.FindDeclarationsAsync(
                    nameof(IList.Add), nameNode: null, filter: SymbolFilter.Member).ConfigureAwait(false);

                // Note: there is no desiredName for these search results.  We're searching for
                // extension methods called "Add", but we have no intention of renaming any 
                // of the existing user code to that name.
                var methodSymbols = OfType<IMethodSymbol>(symbols).SelectAsArray(s => s.WithDesiredName(null));

                var viableMethods = GetViableExtensionMethods(
                    methodSymbols, _node.Parent, searchScope.CancellationToken);

                return GetNamespaceSymbolReferences(searchScope,
                    viableMethods.SelectAsArray(m => m.WithSymbol(m.Symbol.ContainingNamespace)));
            }

            /// <summary>
            /// Searches for extension methods exactly called 'Select'.  Returns
            /// <see cref="SymbolReference"/>s to the <see cref="INamespaceSymbol"/>s that contain
            /// the static classes that those extension methods are contained in.
            /// </summary>
            private async Task<ImmutableArray<SymbolReference>> GetReferencesForQueryPatternsAsync(SearchScope searchScope)
            {
                searchScope.CancellationToken.ThrowIfCancellationRequested();

                if (_owner.CanAddImportForQuery(_diagnosticId, _node))
                {
                    var type = _owner.GetQueryClauseInfo(_semanticModel, _node, searchScope.CancellationToken);
                    if (type != null)
                    {
                        // find extension methods named "Select"
                        return await GetReferencesForExtensionMethodAsync(
                            searchScope, nameof(Enumerable.Select), type).ConfigureAwait(false);
                    }
                }

                return ImmutableArray<SymbolReference>.Empty;
            }

            /// <summary>
            /// Searches for extension methods exactly called 'GetAwaiter'.  Returns
            /// <see cref="SymbolReference"/>s to the <see cref="INamespaceSymbol"/>s that contain
            /// the static classes that those extension methods are contained in.
            /// </summary>
            private async Task<ImmutableArray<SymbolReference>> GetReferencesForGetAwaiterAsync(SearchScope searchScope)
            {
                searchScope.CancellationToken.ThrowIfCancellationRequested();

                if (_owner.CanAddImportForGetAwaiter(_diagnosticId, _syntaxFacts, _node))
                {
                    var type = _owner.GetAwaitInfo(_semanticModel, _syntaxFacts, _node);
                    if (type != null)
                    {
                        return await GetReferencesForExtensionMethodAsync(searchScope, WellKnownMemberNames.GetAwaiter, type,
                            m => m.IsValidGetAwaiter()).ConfigureAwait(false);
                    }
                }

                return ImmutableArray<SymbolReference>.Empty;
            }

            /// <summary>
            /// Searches for extension methods exactly called 'Deconstruct'.  Returns
            /// <see cref="SymbolReference"/>s to the <see cref="INamespaceSymbol"/>s that contain
            /// the static classes that those extension methods are contained in.
            /// </summary>
            private async Task<ImmutableArray<SymbolReference>> GetReferencesForDeconstructAsync(SearchScope searchScope)
            {
                searchScope.CancellationToken.ThrowIfCancellationRequested();

                if (_owner.CanAddImportForDeconstruct(_diagnosticId, _node))
                {
                    var type = _owner.GetDeconstructInfo(_semanticModel, _node, searchScope.CancellationToken);
                    if (type != null)
                    {
                        // Note: we could check that the extension methods have the right number of out-params.  
                        // But that would involve figuring out what we're trying to deconstruct into.  For now
                        // we'll just be permissive, with the assumption that there won't be that many matching
                        // 'Deconstruct' extension methods for the type of node that we're on.
                        return await GetReferencesForExtensionMethodAsync(
                            searchScope, "Deconstruct", type,
                            m => m.ReturnsVoid).ConfigureAwait(false);
                    }
                }

                return ImmutableArray<SymbolReference>.Empty;
            }

            private async Task<ImmutableArray<SymbolReference>> GetReferencesForExtensionMethodAsync(
                SearchScope searchScope, string name, ITypeSymbol type, Func<IMethodSymbol, bool> predicate = null)
            {
                var symbols = await searchScope.FindDeclarationsAsync(
                    name, nameNode: null, filter: SymbolFilter.Member).ConfigureAwait(false);

                // Note: there is no "desiredName" when doing this.  We're not going to do any
                // renames of the user code.  We're just looking for an extension method called 
                // "Select", but that name has no bearing on the code in question that we're
                // trying to fix up.
                var methodSymbols = OfType<IMethodSymbol>(symbols).SelectAsArray(s => s.WithDesiredName(null));
                var viableExtensionMethods = GetViableExtensionMethods(methodSymbols, type);

                if (predicate != null)
                {
                    viableExtensionMethods = viableExtensionMethods.WhereAsArray(s => predicate(s.Symbol));
                }

                var namespaceSymbols = viableExtensionMethods.SelectAsArray(s => s.WithSymbol(s.Symbol.ContainingNamespace));

                return GetNamespaceSymbolReferences(searchScope, namespaceSymbols);
            }

            protected bool ExpressionBinds(
                TSimpleNameSyntax nameNode, bool checkForExtensionMethods, CancellationToken cancellationToken)
            {
                // See if the name binds to something other then the error type. If it does, there's nothing further we need to do.
                // For extension methods, however, we will continue to search if there exists any better matched method.
                cancellationToken.ThrowIfCancellationRequested();
                var symbolInfo = _semanticModel.GetSymbolInfo(nameNode, cancellationToken);
                if (symbolInfo.CandidateReason == CandidateReason.OverloadResolutionFailure && !checkForExtensionMethods)
                {
                    return true;
                }

                return symbolInfo.Symbol != null;
            }

            private ImmutableArray<SymbolReference> GetNamespaceSymbolReferences(
                SearchScope scope, ImmutableArray<SymbolResult<INamespaceSymbol>> namespaces)
            {
                var references = ArrayBuilder<SymbolReference>.GetInstance();

                foreach (var namespaceResult in namespaces)
                {
                    var symbol = namespaceResult.Symbol;
                    var mappedResult = namespaceResult.WithSymbol(MapToCompilationNamespaceIfPossible(namespaceResult.Symbol));
                    var namespaceIsInScope = _namespacesInScope.Contains(mappedResult.Symbol);
                    if (!symbol.IsGlobalNamespace && !namespaceIsInScope)
                    {
                        references.Add(scope.CreateReference(mappedResult));
                    }
                }

                return references.ToImmutableAndFree();
            }

            private ImmutableArray<SymbolResult<T>> OfType<T>(ImmutableArray<SymbolResult<ISymbol>> symbols) where T : ISymbol
            {
                return symbols.WhereAsArray(s => s.Symbol is T)
                              .SelectAsArray(s => s.WithSymbol((T)s.Symbol));
            }
        }
    }
}
