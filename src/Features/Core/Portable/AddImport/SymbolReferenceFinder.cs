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
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.SymbolSearch;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CodeFixes.AddImport
{
    internal abstract partial class AbstractAddImportCodeFixProvider<TSimpleNameSyntax>
    {
        private class SymbolReferenceFinder
        {
            private const string AttributeSuffix = "Attribute";

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
                Document document, SemanticModel semanticModel,
                Diagnostic diagnostic, SyntaxNode node,
                CancellationToken cancellationToken)
            {
                _owner = owner;
                _document = document;
                _semanticModel = semanticModel;
                _diagnostic = diagnostic;
                _node = node;

                _containingType = semanticModel.GetEnclosingNamedType(node.SpanStart, cancellationToken);
                _containingTypeOrAssembly = _containingType ?? (ISymbol)semanticModel.Compilation.Assembly;
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
                IAssemblySymbol assembly, PortableExecutableReference metadataReference,
                bool exact, CancellationToken cancellationToken)
            {
                var searchScope = new MetadataSymbolsSearchScope(
                    _owner, _document.Project.Solution, assembly, metadataReference, exact, cancellationToken);
                return DoAsync(searchScope);
            }

            private async Task<ImmutableArray<SymbolReference>> DoAsync(SearchScope searchScope)
            {
                searchScope.CancellationToken.ThrowIfCancellationRequested();

                // Spin off tasks to do all our searching in parallel
                var tasks = new List<Task<ImmutableArray<SymbolReference>>>
                {
                    this.GetReferencesForMatchingTypesAsync(searchScope),
                    this.GetReferencesForMatchingNamespacesAsync(searchScope),
                    this.GetReferencesForMatchingFieldsAndPropertiesAsync(searchScope),
                    this.GetReferencesForMatchingExtensionMethodsAsync(searchScope),
                };

                // Searching for things like "Add" (for collection initializers) and "Select"
                // (for extension methods) should only be done when doing an 'exact' search.
                // We should not do fuzzy searches for these names.  In this case it's not
                // like the user was writing Add or Select, but instead we're looking for
                // viable symbols with those names to make a collection initializer or 
                // query expression valid.
                if (searchScope.Exact)
                {
                    tasks.Add(this.GetReferencesForCollectionInitializerMethodsAsync(searchScope));
                    tasks.Add(this.GetReferencesForQueryPatternsAsync(searchScope));
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

            /// <summary>
            /// Searches for types that match the name the user has written.  Returns <see cref="SymbolReference"/>s
            /// to the <see cref="INamespaceSymbol"/>s or <see cref="INamedTypeSymbol"/>s those types are
            /// contained in.
            /// </summary>
            private async Task<ImmutableArray<SymbolReference>> GetReferencesForMatchingTypesAsync(SearchScope searchScope)
            {
                searchScope.CancellationToken.ThrowIfCancellationRequested();
                if (!_owner.CanAddImportForType(_diagnostic, _node, out var nameNode))
                {
                    return ImmutableArray<SymbolReference>.Empty;
                }

                CalculateContext(nameNode, _syntaxFacts, out var name, out var arity, out var inAttributeContext, out var hasIncompleteParentMember);

                // Find types in the search scope with the same name as what the user wrote.
                var symbols = await GetTypeSymbols(searchScope, name, nameNode, inAttributeContext).ConfigureAwait(false);

                // Only keep symbols which are accessible from the current location.
                var accessibleSymbols = symbols.WhereAsArray(
                    s => ArityAccessibilityAndAttributeContextAreCorrect(
                        s.Symbol, arity, inAttributeContext, hasIncompleteParentMember));

                // These types may be contained within namespaces, or they may be nested 
                // inside generic types.  Record these namespaces/types if it would be 
                // legal to add imports for them.

                var typesContainedDirectlyInNamespaces = accessibleSymbols.WhereAsArray(s => s.Symbol.ContainingSymbol is INamespaceSymbol);
                var typesContainedDirectlyInTypes = accessibleSymbols.WhereAsArray(s => s.Symbol.ContainingType != null);

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
                bool hasIncompleteParentMember)
            {
                return (arity == 0 || symbol.GetArity() == arity || hasIncompleteParentMember)
                       && symbol.IsAccessibleWithin(_semanticModel.Compilation.Assembly)
                       && (!inAttributeContext || symbol.IsAttribute());
            }

            /// <summary>
            /// Searches for namespaces that match the name the user has written.  Returns <see cref="SymbolReference"/>s
            /// to the <see cref="INamespaceSymbol"/>s those namespaces are contained in.
            /// </summary>
            private async Task<ImmutableArray<SymbolReference>> GetReferencesForMatchingNamespacesAsync(
                SearchScope searchScope)
            {
                searchScope.CancellationToken.ThrowIfCancellationRequested();
                if (_owner.CanAddImportForNamespace(_diagnostic, _node, out var nameNode))
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
                if (_owner.CanAddImportForMethod(_diagnostic, _syntaxFacts, _node, out var nameNode) &&
                    nameNode != null)
                {
                        // We have code like "Color.Black".  "Color" bound to a 'Color Color' property, and
                        // 'Black' did not bind.  We want to find a type called 'Color' that will actually
                        // allow 'Black' to bind.
                        var syntaxFacts = this._document.GetLanguageService<ISyntaxFactsService>();
                    if (syntaxFacts.IsNameOfMemberAccessExpression(nameNode))
                    {
                        var expression =
                            syntaxFacts.GetExpressionOfMemberAccessExpression(nameNode.Parent, allowImplicitTarget: true) ??
                            syntaxFacts.GetTargetOfMemberBinding(nameNode.Parent);
                        if (expression is TSimpleNameSyntax)
                        {
                            // Check if the expression before the dot binds to a property or field.
                            var symbol = this._semanticModel.GetSymbolInfo(expression, searchScope.CancellationToken).GetAnySymbol();
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

                                    return this.GetNamespaceSymbolReferences(searchScope, namespaceResults);
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
                if (!_owner.CanAddImportForMethod(_diagnostic, _syntaxFacts, _node, out var nameNode) &&
                    nameNode != null)
                {
                    var symbols = await GetSymbolsAsync(searchScope, nameNode).ConfigureAwait(false);
                    var methodSymbols = OfType<IMethodSymbol>(symbols);

                    var extensionMethodSymbols = GetViableExtensionMethods(
                        methodSymbols, nameNode.Parent, searchScope.CancellationToken);

                    var namespaceSymbols = extensionMethodSymbols.SelectAsArray(s => s.WithSymbol(s.Symbol.ContainingNamespace));
                    return GetNamespaceSymbolReferences(searchScope, namespaceSymbols);
                }

                return ImmutableArray<SymbolReference>.Empty;
            }

            private ImmutableArray<SymbolResult<IMethodSymbol>> GetViableExtensionMethods(
                ImmutableArray<SymbolResult<IMethodSymbol>> methodSymbols,
                SyntaxNode expression, CancellationToken cancellationToken)
            {
                return GetViableExtensionMethodsWorker(methodSymbols, cancellationToken).WhereAsArray(
                    s => _owner.IsViableExtensionMethod(s.Symbol, expression, _semanticModel, _syntaxFacts, cancellationToken));
            }
            private ImmutableArray<SymbolResult<IMethodSymbol>> GetViableExtensionMethods(
                ImmutableArray<SymbolResult<IMethodSymbol>> methodSymbols,
                ITypeSymbol typeSymbol, CancellationToken cancellationToken)
            {
                return GetViableExtensionMethodsWorker(methodSymbols, cancellationToken).WhereAsArray(
                    s => _owner.IsViableExtensionMethod(s.Symbol, typeSymbol));
            }
            private ImmutableArray<SymbolResult<IMethodSymbol>> GetViableExtensionMethodsWorker(
                ImmutableArray<SymbolResult<IMethodSymbol>> methodSymbols, CancellationToken cancellationToken)
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
                if (!_owner.CanAddImportForMethod(_diagnostic, _syntaxFacts, _node, out var nameNode))
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

                if (_owner.CanAddImportForQuery(_diagnostic, _node))
                {
                    var type = _owner.GetQueryClauseInfo(_semanticModel, _node, searchScope.CancellationToken);
                    if (type != null)
                    {
                        // find extension methods named "Select"
                        var symbols = await searchScope.FindDeclarationsAsync(
                            nameof(Enumerable.Select), nameNode: null, filter: SymbolFilter.Member).ConfigureAwait(false);

                        // Note: there is no "desiredName" when doing this.  We're not going to do any
                        // renames of the user code.  We're just looking for an extension method called 
                        // "Select", but that name has no bearing on the code in question that we're
                        // trying to fix up.
                        var methodSymbols = OfType<IMethodSymbol>(symbols).SelectAsArray(s => s.WithDesiredName(null));
                        var viableExtensionMethods = GetViableExtensionMethods(methodSymbols, type, searchScope.CancellationToken);
                        var namespaceSymbols = viableExtensionMethods.SelectAsArray(s => s.WithSymbol(s.Symbol.ContainingNamespace));

                        return GetNamespaceSymbolReferences(searchScope, namespaceSymbols);
                    }
                }

                return ImmutableArray<SymbolReference>.Empty;
            }

            internal async Task FindNugetOrReferenceAssemblyReferencesAsync(
                ArrayBuilder<Reference> allReferences, CancellationToken cancellationToken)
            {
                if (allReferences.Count > 0)
                {
                    // Only do this if none of the project or metadata searches produced 
                    // any results. We always consider source and local metadata to be 
                    // better than any NuGet/assembly-reference results.
                    return;
                }

                if (!_owner.CanAddImportForType(_diagnostic, _node, out var nameNode))
                {
                    return;
                }

                CalculateContext(nameNode, _syntaxFacts, out var name, out var arity, out var inAttributeContext, out var hasIncompleteParentMember);

                if (ExpressionBinds(nameNode, checkForExtensionMethods: false, cancellationToken: cancellationToken))
                {
                    return;
                }

                await FindNugetOrReferenceAssemblyTypeReferencesAsync(
                    allReferences, nameNode, name, arity, inAttributeContext, cancellationToken).ConfigureAwait(false);
            }

            private async Task FindNugetOrReferenceAssemblyTypeReferencesAsync(
                ArrayBuilder<Reference> allReferences, TSimpleNameSyntax nameNode,
                string name, int arity, bool inAttributeContext,
                CancellationToken cancellationToken)
            {
                if (arity == 0 && inAttributeContext)
                {
                    await FindNugetOrReferenceAssemblyTypeReferencesWorkerAsync(
                        allReferences, nameNode, name + AttributeSuffix, arity,
                        isAttributeSearch: true, cancellationToken: cancellationToken).ConfigureAwait(false);
                }

                await FindNugetOrReferenceAssemblyTypeReferencesWorkerAsync(
                    allReferences, nameNode, name, arity,
                    isAttributeSearch: false, cancellationToken: cancellationToken).ConfigureAwait(false);
            }

            private async Task FindNugetOrReferenceAssemblyTypeReferencesWorkerAsync(
                ArrayBuilder<Reference> allReferences, TSimpleNameSyntax nameNode,
                string name, int arity, bool isAttributeSearch, CancellationToken cancellationToken)
            {
                var workspaceServices = _document.Project.Solution.Workspace.Services;

                var symbolSearchService = _owner._symbolSearchService ?? workspaceServices.GetService<ISymbolSearchService>();
                var installerService = _owner._packageInstallerService ?? workspaceServices.GetService<IPackageInstallerService>();

                var language = _document.Project.Language;

                var options = workspaceServices.Workspace.Options;
                var searchReferenceAssemblies = options.GetOption(
                    SymbolSearchOptions.SuggestForTypesInReferenceAssemblies, language);
                var searchNugetPackages = options.GetOption(
                    SymbolSearchOptions.SuggestForTypesInNuGetPackages, language);

                if (symbolSearchService != null &&
                    searchReferenceAssemblies)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    await FindReferenceAssemblyTypeReferencesAsync(
                        symbolSearchService, allReferences, nameNode, name, arity, isAttributeSearch, cancellationToken).ConfigureAwait(false);
                }

                if (symbolSearchService != null &&
                    installerService != null &&
                    searchNugetPackages && 
                    installerService.IsEnabled)
                {
                    foreach (var packageSource in installerService.PackageSources)
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        await FindNugetTypeReferencesAsync(
                            packageSource, symbolSearchService, installerService, allReferences,
                            nameNode, name, arity, isAttributeSearch, cancellationToken).ConfigureAwait(false);
                    }
                }
            }

            private async Task FindReferenceAssemblyTypeReferencesAsync(
                ISymbolSearchService searchService,
                ArrayBuilder<Reference> allReferences,
                TSimpleNameSyntax nameNode,
                string name,
                int arity,
                bool isAttributeSearch,
                CancellationToken cancellationToken)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var results = await searchService.FindReferenceAssembliesWithTypeAsync(
                    name, arity, cancellationToken).ConfigureAwait(false);
                if (results.IsDefault)
                {
                    return;
                }

                var project = _document.Project;
                var projectId = project.Id;
                var workspace = project.Solution.Workspace;

                foreach (var result in results)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    await HandleReferenceAssemblyReferenceAsync(
                        allReferences, nameNode, project,
                        isAttributeSearch, result, weight: allReferences.Count,
                        cancellationToken: cancellationToken).ConfigureAwait(false);
                }
            }

            private async Task FindNugetTypeReferencesAsync(
                PackageSource source,
                ISymbolSearchService searchService,
                IPackageInstallerService installerService,
                ArrayBuilder<Reference> allReferences,
                TSimpleNameSyntax nameNode,
                string name,
                int arity,
                bool isAttributeSearch,
                CancellationToken cancellationToken)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var results = await searchService.FindPackagesWithTypeAsync(
                    source.Name, name, arity, cancellationToken).ConfigureAwait(false);
                if (results.IsDefault)
                {
                    return;
                }

                var project = _document.Project;
                var projectId = project.Id;
                var workspace = project.Solution.Workspace;

                foreach (var result in results)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    await HandleNugetReferenceAsync(
                        source.Source, installerService, allReferences, nameNode,
                        project, isAttributeSearch, result, 
                        weight: allReferences.Count).ConfigureAwait(false);
                }
            }

            private async Task HandleReferenceAssemblyReferenceAsync(
                ArrayBuilder<Reference> allReferences,
                TSimpleNameSyntax nameNode,
                Project project,
                bool isAttributeSearch,
                ReferenceAssemblyWithTypeResult result,
                int weight,
                CancellationToken cancellationToken)
            {
                foreach (var reference in project.MetadataReferences)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var compilation = await project.GetCompilationAsync(cancellationToken).ConfigureAwait(false);
                    var assemblySymbol = compilation.GetAssemblyOrModuleSymbol(reference) as IAssemblySymbol;
                    if (assemblySymbol?.Name == result.AssemblyName)
                    {
                        // Project already has a reference to an assembly with this name.
                        return;
                    }
                }

                var desiredName = GetDesiredName(isAttributeSearch, result.TypeName);
                allReferences.Add(new AssemblyReference(
                    _owner, new SearchResult(desiredName, nameNode, result.ContainingNamespaceNames, weight), result));
            }

            private Task HandleNugetReferenceAsync(
                string source,
                IPackageInstallerService installerService,
                ArrayBuilder<Reference> allReferences,
                TSimpleNameSyntax nameNode,
                Project project,
                bool isAttributeSearch,
                PackageWithTypeResult result,
                int weight)
            {
                if (!installerService.IsInstalled(project.Solution.Workspace, project.Id, result.PackageName))
                {
                    var desiredName = GetDesiredName(isAttributeSearch, result.TypeName);
                    allReferences.Add(new PackageReference(_owner, installerService,
                        new SearchResult(desiredName, nameNode, result.ContainingNamespaceNames, weight), 
                        source, result.PackageName, result.Version));
                }

                return SpecializedTasks.EmptyTask;
            }

            private static string GetDesiredName(bool isAttributeSearch, string typeName)
            {
                var desiredName = typeName;
                if (isAttributeSearch)
                {
                    desiredName = desiredName.GetWithoutAttributeSuffix(isCaseSensitive: false);
                }

                return desiredName;
            }

            private async Task<ImmutableArray<SymbolResult<ITypeSymbol>>> GetTypeSymbols(
                SearchScope searchScope,
                string name,
                TSimpleNameSyntax nameNode,
                bool inAttributeContext)
            {
                if (searchScope.CancellationToken.IsCancellationRequested)
                {
                    return ImmutableArray<SymbolResult<ITypeSymbol>>.Empty;
                }

                if (ExpressionBinds(nameNode, checkForExtensionMethods: false, cancellationToken: searchScope.CancellationToken))
                {
                    return ImmutableArray<SymbolResult<ITypeSymbol>>.Empty;
                }

                var symbols = await searchScope.FindDeclarationsAsync(name, nameNode, SymbolFilter.Type).ConfigureAwait(false);

                // also lookup type symbols with the "Attribute" suffix.
                if (inAttributeContext)
                {
                    var attributeSymbols = await searchScope.FindDeclarationsAsync(name + AttributeSuffix, nameNode, SymbolFilter.Type).ConfigureAwait(false);

                    symbols = symbols.AddRange(
                        attributeSymbols.Select(r => r.WithDesiredName(r.DesiredName.GetWithoutAttributeSuffix(isCaseSensitive: false))));
                }

                return OfType<ITypeSymbol>(symbols);
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

            private Task<ImmutableArray<SymbolResult<ISymbol>>> GetSymbolsAsync(SearchScope searchScope, TSimpleNameSyntax nameNode)
            {
                searchScope.CancellationToken.ThrowIfCancellationRequested();

                // See if the name binds.  If it does, there's nothing further we need to do.
                if (ExpressionBinds(nameNode, checkForExtensionMethods: true, cancellationToken: searchScope.CancellationToken))
                {
                    return SpecializedTasks.EmptyImmutableArray<SymbolResult<ISymbol>>();
                }

                _syntaxFacts.GetNameAndArityOfSimpleName(nameNode, out var name, out var arity);
                if (name == null)
                {
                    return SpecializedTasks.EmptyImmutableArray<SymbolResult<ISymbol>>();
                }

                return searchScope.FindDeclarationsAsync(name, nameNode, SymbolFilter.Member);
            }

            private ImmutableArray<SymbolResult<T>> OfType<T>(ImmutableArray<SymbolResult<ISymbol>> symbols) where T : ISymbol
            {
                return symbols.WhereAsArray(s => s.Symbol is T)
                              .SelectAsArray(s => s.WithSymbol((T)s.Symbol));
            }
        }
    }
}
