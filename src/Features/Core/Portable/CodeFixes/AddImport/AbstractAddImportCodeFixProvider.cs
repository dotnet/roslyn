// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeGeneration;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Options;
using Roslyn.Utilities;
using static Roslyn.Utilities.PortableShim;

namespace Microsoft.CodeAnalysis.CodeFixes.AddImport
{
    internal abstract partial class AbstractAddImportCodeFixProvider<TSimpleNameSyntax> : CodeFixProvider, IEqualityComparer<PortableExecutableReference>
        where TSimpleNameSyntax : SyntaxNode
    {
        private const int MaxResults = 3;

        protected abstract bool CanAddImport(SyntaxNode node, CancellationToken cancellationToken);
        protected abstract bool CanAddImportForMethod(Diagnostic diagnostic, ISyntaxFactsService syntaxFacts, SyntaxNode node, out TSimpleNameSyntax nameNode);
        protected abstract bool CanAddImportForNamespace(Diagnostic diagnostic, SyntaxNode node, out TSimpleNameSyntax nameNode);
        protected abstract bool CanAddImportForQuery(Diagnostic diagnostic, SyntaxNode node);
        protected abstract bool CanAddImportForType(Diagnostic diagnostic, SyntaxNode node, out TSimpleNameSyntax nameNode);

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

                        // Look for exact matches first:
                        await FindResults(project, allSymbolReferences, finder, exact: true, cancellationToken: cancellationToken).ConfigureAwait(false);
                        if (allSymbolReferences.Count == 0)
                        {
                            // No exact matches found.  Fall back to fuzzy searching.
                            await FindResults(project, allSymbolReferences, finder, exact: false, cancellationToken: cancellationToken).ConfigureAwait(false);
                        }

                        // Nothing found at all. No need to proceed.
                        if (allSymbolReferences.Count == 0)
                        {
                            return;
                        }

                        cancellationToken.ThrowIfCancellationRequested();

                        foreach (var reference in allSymbolReferences)
                        {
                            var description = this.GetDescription(reference.SearchResult.Symbol, semanticModel, node);
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

        private async Task FindResults(Project project, List<SymbolReference> allSymbolReferences, SymbolReferenceFinder finder, bool exact, CancellationToken cancellationToken)
        {
            await FindResultsInCurrentProject(project, allSymbolReferences, finder, exact).ConfigureAwait(false);
            await FindResultsInUnreferencedProjects(project, allSymbolReferences, finder, exact, cancellationToken).ConfigureAwait(false);
            await FindResultsInUnreferencedMetadataReferences(project, allSymbolReferences, finder, exact, cancellationToken).ConfigureAwait(false);
        }

        private async Task FindResultsInCurrentProject(
            Project project, List<SymbolReference> allSymbolReferences, SymbolReferenceFinder finder, bool exact)
        {
            AddRange(allSymbolReferences, await finder.FindInProjectAsync(project, includeDirectReferences: true, exact: exact).ConfigureAwait(false));
        }

        private async Task<Solution> AddImportAndReferenceAsync(
            SyntaxNode contextNode, SymbolReference reference, Document document,
            bool placeSystemNamespaceFirst, CancellationToken cancellationToken)
        {
            ReplaceNameNode(reference, ref contextNode, ref document, cancellationToken);

            // Defer to the language to add the actual import/using.
            var newDocument = await this.AddImportAsync(contextNode,
                reference.SearchResult.Symbol, document,
                placeSystemNamespaceFirst, cancellationToken).ConfigureAwait(false);

            return reference.UpdateSolution(newDocument);
        }

        private static void ReplaceNameNode(
            SymbolReference reference, ref SyntaxNode contextNode, ref Document document, CancellationToken cancellationToken)
        {
            var desiredName = reference.SearchResult.DesiredName;
            if (!string.IsNullOrEmpty(reference.SearchResult.DesiredName))
            {
                var nameNode = reference.SearchResult.NameNode;

                if (nameNode != null)
                {
                    var identifier = nameNode.GetFirstToken();
                    if (identifier.ValueText != desiredName)
                    {
                        var generator = SyntaxGenerator.GetGenerator(document);
                        var newIdentifier = generator.IdentifierName(desiredName).GetFirstToken().WithTriviaFrom(identifier);
                        var annotation = new SyntaxAnnotation();

                        var root = contextNode.SyntaxTree.GetRoot(cancellationToken);
                        root = root.ReplaceToken(identifier, newIdentifier.WithAdditionalAnnotations(annotation));
                        document = document.WithSyntaxRoot(root);
                        contextNode = root.GetAnnotatedTokens(annotation).First().Parent;
                    }
                }
            }
        }

        private async Task FindResultsInUnreferencedProjects(
            Project project, List<SymbolReference> allSymbolReferences, SymbolReferenceFinder finder, bool exact, CancellationToken cancellationToken)
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
                AddRange(allSymbolReferences, await finder.FindInProjectAsync(unreferencedProject, includeDirectReferences: false, exact: exact).ConfigureAwait(false));
                if (allSymbolReferences.Count >= MaxResults)
                {
                    return;
                }
            }
        }

        private async Task FindResultsInUnreferencedMetadataReferences(
            Project project, List<SymbolReference> allSymbolReferences, SymbolReferenceFinder finder, bool exact, CancellationToken cancellationToken)
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
                    otherProject, allSymbolReferences, finder, seenReferences, exact, cancellationToken).ConfigureAwait(false);
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
            bool exact,
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
                        AddRange(allSymbolReferences, await finder.FindInMetadataAsync(otherProject.Solution, assembly, reference, exact).ConfigureAwait(false));
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

        private static void CalculateContext(
            TSimpleNameSyntax nameNode, ISyntaxFactsService syntaxFacts, out string name, out int arity,
            out bool inAttributeContext, out bool hasIncompleteParentMember)
        {
            // Has to be a simple identifier or generic name.
            syntaxFacts.GetNameAndArityOfSimpleName(nameNode, out name, out arity);

            inAttributeContext = syntaxFacts.IsAttributeName(nameNode);
            hasIncompleteParentMember = syntaxFacts.HasIncompleteParentMember(nameNode);
        }

        private static bool NotGlobalNamespace(SymbolReference reference)
        {
            var symbol = reference.SearchResult.Symbol;
            return symbol.IsNamespace ? !((INamespaceSymbol)symbol).IsGlobalNamespace : true;
        }

        private static bool NotNull(SymbolReference reference)
        {
            return reference.SearchResult.Symbol != null;
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

            internal Task<List<SymbolReference>> FindInProjectAsync(Project project, bool includeDirectReferences, bool exact)
            {
                var searchScope = new ProjectSearchScope(project, includeDirectReferences, exact, _cancellationToken);
                return DoAsync(searchScope);
            }

            internal Task<List<SymbolReference>> FindInMetadataAsync(
                Solution solution, IAssemblySymbol assembly, PortableExecutableReference metadataReference, bool exact)
            {
                var searchScope = new MetadataSearchScope(solution, assembly, metadataReference, exact, _cancellationToken);
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
                    .Where(s => s.Symbol.IsExtensionMethod && _owner.IsViableExtensionMethod(type, s.Symbol))
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

        private struct SearchResult<T> where T : ISymbol
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

            public SearchResult(string desiredName, TSimpleNameSyntax nameNode, T symbol, double weight)
            {
                DesiredName = desiredName;
                Symbol = symbol;
                Weight = weight;
                NameNode = nameNode;
            }

            public SearchResult<T2> WithSymbol<T2>(T2 symbol) where T2 : ISymbol
            {
                return new SearchResult<T2>(DesiredName, NameNode, symbol, this.Weight);
            }

            internal SearchResult<T> WithDesiredName(string desiredName)
            {
                return new SearchResult<T>(desiredName, NameNode, Symbol, Weight);
            }
        }

        private struct SearchResult
        {
            public static SearchResult<T> Create<T>(string desiredName, TSimpleNameSyntax nameNode, T symbol, double weight) where T : ISymbol
            {
                return new SearchResult<T>(desiredName, nameNode, symbol, weight);
            }
        }
    }
}