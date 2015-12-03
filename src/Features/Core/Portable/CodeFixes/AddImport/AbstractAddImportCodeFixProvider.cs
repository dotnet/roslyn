// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Options;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CodeFixes.AddImport
{
    internal abstract partial class AbstractAddImportCodeFixProvider : CodeFixProvider, IEqualityComparer<PortableExecutableReference>
    {
        private const int MaxResults = 3;

        protected abstract bool IgnoreCase { get; }

        protected abstract bool CanAddImport(SyntaxNode node, CancellationToken cancellationToken);
        protected abstract bool CanAddImportForMethod(Diagnostic diagnostic, ISyntaxFactsService syntaxFacts, ref SyntaxNode node);
        protected abstract bool CanAddImportForNamespace(Diagnostic diagnostic, ref SyntaxNode node);
        protected abstract bool CanAddImportForQuery(Diagnostic diagnostic, ref SyntaxNode node);
        protected abstract bool CanAddImportForType(Diagnostic diagnostic, ref SyntaxNode node);

        protected abstract ISet<INamespaceSymbol> GetNamespacesInScope(SemanticModel semanticModel, SyntaxNode node, CancellationToken cancellationToken);
        protected abstract ITypeSymbol GetQueryClauseInfo(SemanticModel semanticModel, SyntaxNode node, CancellationToken cancellationToken);
        protected abstract string GetDescription(INamespaceOrTypeSymbol symbol, SemanticModel semanticModel, SyntaxNode root);
        protected abstract Task<Document> AddImportAsync(SyntaxNode contextNode, INamespaceOrTypeSymbol symbol, Document document, bool specialCaseSystem, CancellationToken cancellationToken);
        protected abstract bool IsViableExtensionMethod(IMethodSymbol method, SyntaxNode expression, SemanticModel semanticModel, ISyntaxFactsService syntaxFacts, CancellationToken cancellationToken);
        internal abstract bool IsViableField(IFieldSymbol field, SyntaxNode expression, SemanticModel semanticModel, ISyntaxFactsService syntaxFacts, CancellationToken cancellationToken);
        internal abstract bool IsViableProperty(IPropertySymbol property, SyntaxNode expression, SemanticModel semanticModel, ISyntaxFactsService syntaxFacts, CancellationToken cancellationToken);
        internal abstract bool IsAddMethodContext(SyntaxNode node, SemanticModel semanticModel);

        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var document = context.Document;
            var span = context.Span;
            var diagnostics = context.Diagnostics;
            var cancellationToken = context.CancellationToken;

            var project = document.Project;
            var diagnostic = diagnostics.First();
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var ancestors = root.FindToken(span.Start, findInsideTrivia: true).GetAncestors<SyntaxNode>();
            if (!ancestors.Any())
            {
                return;
            }

            var node = ancestors.FirstOrDefault(n => n.Span.Contains(span) && n != root);
            if (node == null)
            {
                return;
            }

            var placeSystemNamespaceFirst = document.Project.Solution.Workspace.Options.GetOption(
                OrganizerOptions.PlaceSystemNamespaceFirst, document.Project.Language);

            using (Logger.LogBlock(FunctionId.Refactoring_AddImport, cancellationToken))
            {
                if (!cancellationToken.IsCancellationRequested)
                {
                    if (this.CanAddImport(node, cancellationToken))
                    {
                        var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);

                        var allSymbolReferences = new List<SymbolReference>();

                        var finder = new SymbolReferenceFinder(this, document, semanticModel, diagnostic, node, cancellationToken);

                        await FindResultsInCurrentProject(project, allSymbolReferences, finder).ConfigureAwait(false);
                        await FindResultsInUnreferencedProjects(project, allSymbolReferences, finder, cancellationToken).ConfigureAwait(false);
                        await FindResultsInUnreferencedMetadataReferences(project, allSymbolReferences, finder, cancellationToken).ConfigureAwait(false);

                        if (allSymbolReferences.Count == 0)
                        {
                            return;
                        }

                        cancellationToken.ThrowIfCancellationRequested();

                        foreach (var reference in allSymbolReferences)
                        {
                            var description = this.GetDescription(reference.Symbol, semanticModel, node);
                            if (description != null)
                            {
                                var action = new MyCodeAction(description, c =>
                                    this.AddImportAndReferenceAsync(node, reference, document, placeSystemNamespaceFirst, c));
                                context.RegisterCodeFix(action, diagnostic);
                            }
                        }
                    }
                }
            }
        }

        private async Task FindResultsInCurrentProject(Project project, List<SymbolReference> allSymbolReferences, SymbolReferenceFinder finder)
        {
            AddRange(allSymbolReferences, await finder.FindInProjectAsync(project, includeDirectReferences: true).ConfigureAwait(false));
        }

        private async Task<Solution> AddImportAndReferenceAsync(
            SyntaxNode node, SymbolReference reference, Document document, bool placeSystemNamespaceFirst, CancellationToken c)
        {
            // Defer to the language to add the actual import/using.
            var newDocument = await this.AddImportAsync(node, reference.Symbol, document, placeSystemNamespaceFirst, c).ConfigureAwait(false);

            return reference.UpdateSolution(newDocument);
        }

        private async Task FindResultsInUnreferencedProjects(
            Project project, List<SymbolReference> allSymbolReferences, SymbolReferenceFinder finder, CancellationToken cancellationToken)
        {
            // If we didn't find enough hits searching just in the project, then check 
            // in any unreferenced projects.
            if (allSymbolReferences.Count >= MaxResults)
            {
                return;
            }

            var viableUnreferencedProjects = GetViableUnreferencedProjects(project);
            foreach (var unreferencedProject in viableUnreferencedProjects)
            {
                // Search in this unreferenced project.  But don't search in any of its'
                // direct references.  i.e. we don't want to search in its metadata references
                // or in the projects it references itself. We'll be searching those entities
                // individually.
                AddRange(allSymbolReferences, await finder.FindInProjectAsync(unreferencedProject, includeDirectReferences: false).ConfigureAwait(false));
                if (allSymbolReferences.Count >= MaxResults)
                {
                    return;
                }
            }
        }

        private async Task FindResultsInUnreferencedMetadataReferences(Project project, List<SymbolReference> allSymbolReferences, SymbolReferenceFinder finder, CancellationToken cancellationToken)
        {
            if (allSymbolReferences.Count > 0)
            {
                // Only do this if none of the project searches produced any results.  We may have
                // a lot of metadata to search through, and it would be good to avoid that if we
                // can.
                return;
            }

            // Keep track of the references we've seen (so that we don't process them multiple times
            // across many sibling projects).  Prepopulate it with our own metadata references since
            // we know we don't need to search in that.
            var seenReferences = new HashSet<PortableExecutableReference>(comparer: this);
            seenReferences.AddAll(project.MetadataReferences.OfType<PortableExecutableReference>());

            // Check all the other projects in the system so see if they have a metadata reference
            // with a potential result.
            foreach (var otherProject in project.Solution.Projects)
            {
                if (otherProject == project)
                {
                    continue;
                }

                await FindResultsInMetadataReferences(
                    otherProject, allSymbolReferences, finder, seenReferences, cancellationToken).ConfigureAwait(false);
                if (allSymbolReferences.Count >= MaxResults)
                {
                    break;
                }
            }
        }

        private async Task FindResultsInMetadataReferences(
            Project otherProject,
            List<SymbolReference> allSymbolReferences,
            SymbolReferenceFinder finder,
            HashSet<PortableExecutableReference> seenReferences,
            CancellationToken cancellationToken)
        {
            // See if this project has a metadata reference we haven't already looked at.
            var newMetadataReferences = otherProject.MetadataReferences.OfType<PortableExecutableReference>();

            Compilation compilation = null;
            foreach (var reference in newMetadataReferences)
            {
                // Make sure we don't check the same metadata reference multiple times from 
                // different projects.
                if (seenReferences.Add(reference))
                {
                    // Defer making the compilation until necessary.
                    compilation = compilation ?? await otherProject.GetCompilationAsync(cancellationToken).ConfigureAwait(false);

                    // Ignore netmodules.  First, they're incredibly esoteric and barely used.
                    // Second, the SymbolFinder api doesn't even support searching them. 
                    var assembly = compilation.GetAssemblyOrModuleSymbol(reference) as IAssemblySymbol;
                    if (assembly != null)
                    {
                        AddRange(allSymbolReferences, await finder.FindInMetadataAsync(otherProject.Solution, assembly, reference).ConfigureAwait(false));
                    }
                }

                if (allSymbolReferences.Count >= MaxResults)
                {
                    break;
                }
            }
        }

        bool IEqualityComparer<PortableExecutableReference>.Equals(PortableExecutableReference x, PortableExecutableReference y)
        {
            return StringComparer.OrdinalIgnoreCase.Equals(
                x.FilePath ?? x.Display,
                y.FilePath ?? y.Display);
        }

        int IEqualityComparer<PortableExecutableReference>.GetHashCode(PortableExecutableReference obj)
        {
            return StringComparer.OrdinalIgnoreCase.GetHashCode(obj.FilePath ?? obj.Display);
        }

        private static HashSet<Project> GetViableUnreferencedProjects(Project project)
        {
            var solution = project.Solution;
            var viableProjects = new HashSet<Project>(solution.Projects);

            // Clearly we can't reference ourselves.
            viableProjects.Remove(project);

            // We can't reference any project that transitively depends on on us.  Doing so would
            // cause a circular reference between projects.
            var dependencyGraph = solution.GetProjectDependencyGraph();
            var projectsThatTransitivelyDependOnThisProject = dependencyGraph.GetProjectsThatTransitivelyDependOnThisProject(project.Id);

            viableProjects.RemoveAll(projectsThatTransitivelyDependOnThisProject.Select(id =>
                solution.GetProject(id)));

            // We also aren't interested in any projects we're already directly referencing.
            viableProjects.RemoveAll(project.ProjectReferences.Select(r => solution.GetProject(r.ProjectId)));
            return viableProjects;
        }

        private void AddRange(List<SymbolReference> allSymbolReferences, List<SymbolReference> proposedReferences)
        {
            if (proposedReferences != null)
            {
                allSymbolReferences.AddRange(proposedReferences.Take(MaxResults - allSymbolReferences.Count));
            }
        }

        private bool IsViableExtensionMethod(
            ITypeSymbol typeSymbol,
            IMethodSymbol method)
        {
            return typeSymbol != null && method.ReduceExtensionMethod(typeSymbol) != null;
        }

        private static bool ArityAccessibilityAndAttributeContextAreCorrect(
            SemanticModel semanticModel,
            ITypeSymbol symbol,
            int arity,
            bool inAttributeContext,
            bool hasIncompleteParentMember)
        {
            return (arity == 0 || symbol.GetArity() == arity || hasIncompleteParentMember)
                   && symbol.IsAccessibleWithin(semanticModel.Compilation.Assembly)
                   && (!inAttributeContext || symbol.IsAttribute());
        }

        private static void CalculateContext(SyntaxNode node, ISyntaxFactsService syntaxFacts, out string name, out int arity, out bool inAttributeContext, out bool hasIncompleteParentMember)
        {
            // Has to be a simple identifier or generic name.
            syntaxFacts.GetNameAndArityOfSimpleName(node, out name, out arity);

            inAttributeContext = syntaxFacts.IsAttributeName(node);
            hasIncompleteParentMember = syntaxFacts.HasIncompleteParentMember(node);
        }

        private static bool NotGlobalNamespace(SymbolReference reference)
        {
            var symbol = reference.Symbol;
            return symbol.IsNamespace ? !((INamespaceSymbol)symbol).IsGlobalNamespace : true;
        }

        private static bool NotNull(SymbolReference reference)
        {
            return reference.Symbol != null;
        }

        private class MyCodeAction : CodeAction.SolutionChangeAction
        {
            public MyCodeAction(string title, Func<CancellationToken, Task<Solution>> createChangedSolution) :
                base(title, createChangedSolution, equivalenceKey: title)
            {
            }
        }

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
            private readonly AbstractAddImportCodeFixProvider _owner;

            private SyntaxNode _node;

            public SymbolReferenceFinder(
                AbstractAddImportCodeFixProvider owner,
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

            internal Task<List<SymbolReference>> FindInProjectAsync(Project project, bool includeDirectReferences)
            {
                var searchScope = new ProjectSearchScope(project, includeDirectReferences, _owner.IgnoreCase, _cancellationToken);
                return DoAsync(searchScope);
            }

            internal Task<List<SymbolReference>> FindInMetadataAsync(Solution solution, IAssemblySymbol assembly, PortableExecutableReference metadataReference)
            {
                var searchScope = new MetadataSearchScope(solution, assembly, metadataReference, _owner.IgnoreCase, _cancellationToken);
                return DoAsync(searchScope);
            }

            private async Task<List<SymbolReference>> DoAsync(SearchScope searchScope)
            {
                // Spin off tasks to do all our searching.
                var tasks = new[]
                {
                    this.GetNamespacesForMatchingTypesAsync(searchScope),
                    this.GetMatchingTypesAsync(searchScope),
                    this.GetNamespacesForMatchingNamespacesAsync(searchScope),
                    this.GetNamespacesForMatchingExtensionMethodsAsync(searchScope),
                    this.GetNamespacesForMatchingFieldsAndPropertiesAsync(searchScope),
                    this.GetNamespacesForQueryPatternsAsync(searchScope)
                };

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
                if (!_owner.CanAddImportForType(_diagnostic, ref _node))
                {
                    return null;
                }

                string name;
                int arity;
                bool inAttributeContext, hasIncompleteParentMember;
                CalculateContext(_node, _syntaxFacts, out name, out arity, out inAttributeContext, out hasIncompleteParentMember);

                var symbols = await GetTypeSymbols(searchScope, name, inAttributeContext).ConfigureAwait(false);
                if (symbols == null)
                {
                    return null;
                }

                return GetNamespacesForMatchingTypesAsync(searchScope, arity, inAttributeContext, hasIncompleteParentMember, symbols);
            }

            private async Task<IList<SymbolReference>> GetMatchingTypesAsync(SearchScope searchScope)
            {
                if (!_owner.CanAddImportForType(_diagnostic, ref _node))
                {
                    return null;
                }

                string name;
                int arity;
                bool inAttributeContext, hasIncompleteParentMember;
                CalculateContext(_node, _syntaxFacts, out name, out arity, out inAttributeContext, out hasIncompleteParentMember);

                var symbols = await GetTypeSymbols(searchScope, name, inAttributeContext).ConfigureAwait(false);
                if (symbols == null)
                {
                    return null;
                }

                return GetMatchingTypes(searchScope, name, arity, inAttributeContext, symbols, hasIncompleteParentMember);
            }

            private async Task<IEnumerable<ITypeSymbol>> GetTypeSymbols(
                SearchScope searchScope,
                string name,
                bool inAttributeContext)
            {
                if (_cancellationToken.IsCancellationRequested)
                {
                    return null;
                }

                if (ExpressionBinds(checkForExtensionMethods: false))
                {
                    return null;
                }

                var symbols = await searchScope.FindDeclarationsAsync(name, SymbolFilter.Type).ConfigureAwait(false);

                // also lookup type symbols with the "Attribute" suffix.
                if (inAttributeContext)
                {
                    symbols = symbols.Concat(
                        await searchScope.FindDeclarationsAsync(name + "Attribute", SymbolFilter.Type).ConfigureAwait(false));
                }

                return symbols.OfType<ITypeSymbol>();
            }

            protected bool ExpressionBinds(bool checkForExtensionMethods)
            {
                // See if the name binds to something other then the error type. If it does, there's nothing further we need to do.
                // For extension methods, however, we will continue to search if there exists any better matched method.
                _cancellationToken.ThrowIfCancellationRequested();
                var symbolInfo = _semanticModel.GetSymbolInfo(_node, _cancellationToken);
                if (symbolInfo.CandidateReason == CandidateReason.OverloadResolutionFailure && !checkForExtensionMethods)
                {
                    return true;
                }

                return symbolInfo.Symbol != null;
            }

            private async Task<IList<SymbolReference>> GetNamespacesForMatchingNamespacesAsync(SearchScope searchScope)
            {
                if (!_owner.CanAddImportForNamespace(_diagnostic, ref _node))
                {
                    return null;
                }

                string name;
                int arity;
                _syntaxFacts.GetNameAndArityOfSimpleName(_node, out name, out arity);

                if (ExpressionBinds(checkForExtensionMethods: false))
                {
                    return null;
                }

                var symbols = await searchScope.FindDeclarationsAsync(name, SymbolFilter.Namespace).ConfigureAwait(false);

                return GetProposedNamespaces(
                    searchScope, symbols.OfType<INamespaceSymbol>().Select(n => n.ContainingNamespace));
            }

            private async Task<IList<SymbolReference>> GetNamespacesForMatchingExtensionMethodsAsync(SearchScope searchScope)
            {
                if (!_owner.CanAddImportForMethod(_diagnostic, _syntaxFacts, ref _node))
                {
                    return null;
                }

                var expression = _node.Parent;

                var extensionMethods = SpecializedCollections.EmptyEnumerable<SymbolReference>();
                var symbols = await GetSymbolsAsync(searchScope).ConfigureAwait(false);
                if (symbols != null)
                {
                    extensionMethods = FilterForExtensionMethods(searchScope, expression, symbols);
                }

                var addMethods = SpecializedCollections.EmptyEnumerable<SymbolReference>();
                var methodSymbols = await GetAddMethodsAsync(searchScope, expression).ConfigureAwait(false);
                if (methodSymbols != null)
                {
                    addMethods = GetProposedNamespaces(searchScope, methodSymbols.Select(s => s.ContainingNamespace));
                }

                return extensionMethods.Concat(addMethods).ToList();
            }

            private async Task<IList<SymbolReference>> GetNamespacesForMatchingFieldsAndPropertiesAsync(
                SearchScope searchScope)
            {
                if (!_owner.CanAddImportForMethod(_diagnostic, _syntaxFacts, ref _node))
                {
                    return null;
                }

                var expression = _node.Parent;

                var symbols = await GetSymbolsAsync(searchScope).ConfigureAwait(false);

                if (symbols != null)
                {
                    return FilterForFieldsAndProperties(searchScope, expression, symbols);
                }

                return null;
            }

            private async Task<IList<SymbolReference>> GetNamespacesForQueryPatternsAsync(SearchScope searchScope)
            {
                if (!_owner.CanAddImportForQuery(_diagnostic, ref _node))
                {
                    return null;
                }

                ITypeSymbol type = _owner.GetQueryClauseInfo(_semanticModel, _node, _cancellationToken);
                if (type == null)
                {
                    return null;
                }

                // find extension methods named "Select"
                var symbols = await searchScope.FindDeclarationsAsync("Select", SymbolFilter.Member).ConfigureAwait(false);

                var extensionMethodSymbols = symbols
                    .OfType<IMethodSymbol>()
                    .Where(s => s.IsExtensionMethod && _owner.IsViableExtensionMethod(type, s))
                    .ToList();

                return GetProposedNamespaces(
                    searchScope, extensionMethodSymbols.Select(s => s.ContainingNamespace));
            }

            private List<SymbolReference> GetMatchingTypes(
                SearchScope searchScope, string name, int arity, bool inAttributeContext, IEnumerable<ITypeSymbol> symbols, bool hasIncompleteParentMember)
            {
                var accessibleTypeSymbols = symbols
                    .Where(s => ArityAccessibilityAndAttributeContextAreCorrect(
                                    _semanticModel, s, arity,
                                    inAttributeContext, hasIncompleteParentMember))
                    .ToList();

                return GetProposedTypes(searchScope, name, accessibleTypeSymbols);
            }

            private List<SymbolReference> GetNamespacesForMatchingTypesAsync(
                SearchScope searchScope, int arity, bool inAttributeContext, bool hasIncompleteParentMember, IEnumerable<ITypeSymbol> symbols)
            {
                var accessibleTypeSymbols = symbols
                    .Where(s => s.ContainingSymbol is INamespaceSymbol
                                && ArityAccessibilityAndAttributeContextAreCorrect(
                                    _semanticModel, s, arity,
                                    inAttributeContext, hasIncompleteParentMember))
                    .ToList();

                return GetProposedNamespaces(searchScope, accessibleTypeSymbols.Select(s => s.ContainingNamespace));
            }

            private List<SymbolReference> GetProposedNamespaces(SearchScope scope, IEnumerable<INamespaceSymbol> namespaces)
            {
                // We only want to offer to add a using if we don't already have one.
                return
                    namespaces.Where(n => !n.IsGlobalNamespace)
                              .Select(n => _semanticModel.Compilation.GetCompilationNamespace(n) ?? n)
                              .Where(n => n != null && !_namespacesInScope.Contains(n))
                              .Select(n => scope.CreateReference(n))
                              .ToList();
            }

            private List<SymbolReference> GetProposedTypes(SearchScope searchScope, string name, List<ITypeSymbol> accessibleTypeSymbols)
            {
                List<SymbolReference> result = null;
                if (accessibleTypeSymbols != null)
                {
                    foreach (var typeSymbol in accessibleTypeSymbols)
                    {
                        if (typeSymbol?.ContainingType != null)
                        {
                            result = result ?? new List<SymbolReference>();
                            result.Add(searchScope.CreateReference(typeSymbol.ContainingType));
                        }
                    }
                }

                return result;
            }

            private Task<IEnumerable<ISymbol>> GetSymbolsAsync(SearchScope searchScope)
            {
                _cancellationToken.ThrowIfCancellationRequested();

                // See if the name binds.  If it does, there's nothing further we need to do.
                if (ExpressionBinds(checkForExtensionMethods: true))
                {
                    return SpecializedTasks.EmptyEnumerable<ISymbol>();
                }

                string name;
                int arity;
                _syntaxFacts.GetNameAndArityOfSimpleName(_node, out name, out arity);
                if (name == null)
                {
                    return SpecializedTasks.EmptyEnumerable<ISymbol>();
                }

                return searchScope.FindDeclarationsAsync(name, SymbolFilter.Member);
            }

            private IEnumerable<SymbolReference> FilterForExtensionMethods(
                SearchScope searchScope, SyntaxNode expression, IEnumerable<ISymbol> symbols)
            {
                var extensionMethodSymbols = symbols
                    .OfType<IMethodSymbol>()
                    .Where(method => method.IsExtensionMethod &&
                                     method.IsAccessibleWithin(_semanticModel.Compilation.Assembly) == true &&
                                     _owner.IsViableExtensionMethod(method, expression, _semanticModel, _syntaxFacts, _cancellationToken))
                    .ToList();

                return GetProposedNamespaces(
                    searchScope, extensionMethodSymbols.Select(s => s.ContainingNamespace));
            }

            private IList<SymbolReference> FilterForFieldsAndProperties(
                SearchScope searchScope, SyntaxNode expression, IEnumerable<ISymbol> symbols)
            {
                var propertySymbols = symbols
                    .OfType<IPropertySymbol>()
                    .Where(property => property.IsAccessibleWithin(_semanticModel.Compilation.Assembly) == true &&
                                       _owner.IsViableProperty(property, expression, _semanticModel, _syntaxFacts, _cancellationToken))
                    .ToList();

                var fieldSymbols = symbols
                    .OfType<IFieldSymbol>()
                    .Where(field => field.IsAccessibleWithin(_semanticModel.Compilation.Assembly) == true &&
                                    _owner.IsViableField(field, expression, _semanticModel, _syntaxFacts, _cancellationToken))
                    .ToList();

                return GetProposedNamespaces(
                    searchScope, propertySymbols.Select(s => s.ContainingNamespace).Concat(fieldSymbols.Select(s => s.ContainingNamespace)));
            }

            private async Task<IEnumerable<IMethodSymbol>> GetAddMethodsAsync(
                SearchScope searchScope, SyntaxNode expression)
            {
                string name;
                int arity;
                _syntaxFacts.GetNameAndArityOfSimpleName(_node, out name, out arity);
                if (name != null)
                {
                    return SpecializedCollections.EmptyEnumerable<IMethodSymbol>();
                }

                if (_owner.IsAddMethodContext(_node, _semanticModel))
                {
                    var symbols = await searchScope.FindDeclarationsAsync("Add", SymbolFilter.Member).ConfigureAwait(false);
                    return symbols
                        .OfType<IMethodSymbol>()
                        .Where(method => method.IsExtensionMethod &&
                                         method.IsAccessibleWithin(_semanticModel.Compilation.Assembly) == true &&
                                         _owner.IsViableExtensionMethod(method, expression, _semanticModel, _syntaxFacts, _cancellationToken));
                }

                return SpecializedCollections.EmptyEnumerable<IMethodSymbol>();
            }
        }
    }
}