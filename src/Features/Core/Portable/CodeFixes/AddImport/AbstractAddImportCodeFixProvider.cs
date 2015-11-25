// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Options;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CodeFixes.AddImport
{
    internal struct SymbolReference : IComparable<SymbolReference>
    {
        public readonly INamespaceOrTypeSymbol Symbol;
        public readonly ProjectId ProjectId;

        public SymbolReference(INamespaceOrTypeSymbol symbol, ProjectId projectId)
        {
            Symbol = symbol;
            ProjectId = projectId;
        }

        public int CompareTo(SymbolReference other)
        {
           return INamespaceOrTypeSymbolExtensions.CompareNamespaceOrTypeSymbols(this.Symbol, other.Symbol);
        }
    }

    internal abstract partial class AbstractAddImportCodeFixProvider : CodeFixProvider, IEqualityComparer<PortableExecutableReference>
    {
        private abstract class SearchScope
        {
            protected readonly bool ignoreCase;
            protected readonly CancellationToken cancellationToken;

            protected SearchScope(bool ignoreCase, CancellationToken cancellationToken)
            {
                this.ignoreCase = ignoreCase;
                this.cancellationToken = cancellationToken;
            }

            public abstract Task<IEnumerable<ISymbol>> FindDeclarationsAsync(string name, SymbolFilter filter);
            public abstract SymbolReference CreateReference(INamespaceOrTypeSymbol symbol);
        }

        private class ProjectSearchScope : SearchScope
        {
            private readonly bool includeDirectReferences;
            private readonly Project project;

            public ProjectSearchScope(Project project, bool includeDirectReferences, bool ignoreCase, CancellationToken cancellationToken)
                : base(ignoreCase, cancellationToken)
            {
                this.project = project;
                this.includeDirectReferences = includeDirectReferences;
            }

            public override Task<IEnumerable<ISymbol>> FindDeclarationsAsync(string name, SymbolFilter filter)
            {
                return SymbolFinder.FindDeclarationsAsync(
                    project, name, ignoreCase, filter, includeDirectReferences, cancellationToken);
            }

            public override SymbolReference CreateReference(INamespaceOrTypeSymbol symbol)
            {
                return new SymbolReference(symbol, project.Id);
            }
        }

        private const int MaxResults = 8;

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

                        var getter = new ProposedImportGetter(this, document, semanticModel, diagnostic, node, cancellationToken);
                        AddRange(allSymbolReferences, await getter.DoAsync(project, includeDirectReferences: true).ConfigureAwait(false));

                        await FindResultsInUnreferencedProjects(project, allSymbolReferences, getter, cancellationToken).ConfigureAwait(false);

#if false
                        // If we didn't find enough hits searching in the project and all other 
                        // projects, then check if any known metadata reference might help
                        if (allSymbolReferences.Count < MaxResults)
                        {
                            var viableMetadataReference = GetViablePortableExecutionReferences(project);
                            AddRange(allSymbolReferences, await getter.DoAsync().ConfigureAwait(false));
                        }
#endif

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

        private async Task<Solution> AddImportAndReferenceAsync(
            SyntaxNode node, SymbolReference reference, Document document, bool placeSystemNamespaceFirst, CancellationToken c)
        {
            // Defer to the language to add the actual import/using.
            var newDocument = await this.AddImportAsync(node, reference.Symbol, document, placeSystemNamespaceFirst, c).ConfigureAwait(false);

            if (reference.ProjectId == document.Project.Id)
            {
                return newDocument.Project.Solution;
            }

            // If this reference came from searching another project, then add a project reference
            // as well.
            var newProject = newDocument.Project;
            newProject = newProject.AddProjectReference(new ProjectReference(reference.ProjectId));

            return newProject.Solution;
        }

        private async Task FindResultsInUnreferencedProjects(
            Project project, List<SymbolReference> allSymbolReferences, ProposedImportGetter getter, CancellationToken cancellationToken)
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
                AddRange(allSymbolReferences, await getter.DoAsync(unreferencedProject, includeDirectReferences: false).ConfigureAwait(false));
                if (allSymbolReferences.Count >= MaxResults)
                {
                    return;
                }
            }
        }

        private ImmutableArray<PortableExecutableReference> GetViablePortableExecutionReferences(Project project)
        {
            var references = project.Solution.Projects.SelectMany(p => p.MetadataReferences)
                                             .OfType<PortableExecutableReference>()
                                             .Distinct(this)
                                             .ToSet(this);

            references.RemoveAll(project.MetadataReferences.OfType<PortableExecutableReference>());

            return references.ToImmutableArray();
        }

        bool IEqualityComparer<PortableExecutableReference>.Equals(PortableExecutableReference x, PortableExecutableReference y)
        {
            return StringComparer.OrdinalIgnoreCase.Equals(x.FilePath, y.FilePath);
        }

        int IEqualityComparer<PortableExecutableReference>.GetHashCode(PortableExecutableReference obj)
        {
            return StringComparer.OrdinalIgnoreCase.GetHashCode(obj.FilePath);
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

        private class ProposedImportGetter 
        {
            private readonly CancellationToken cancellationToken;
            private readonly Diagnostic diagnostic;
            private readonly Document document;
            private readonly SemanticModel semanticModel;

            private readonly INamedTypeSymbol containingType;
            private readonly ISymbol containingTypeOrAssembly;
            private readonly ISet<INamespaceSymbol> namespacesInScope;
            private readonly ISyntaxFactsService syntaxFacts;
            private readonly AbstractAddImportCodeFixProvider owner;

            private SyntaxNode node;

            public ProposedImportGetter(
                AbstractAddImportCodeFixProvider owner,
                Document document, SemanticModel semanticModel, Diagnostic diagnostic, SyntaxNode node, CancellationToken cancellationToken)
            {
                this.owner = owner;
                this.document = document;
                this.semanticModel = semanticModel;
                this.diagnostic = diagnostic;
                this.node = node;
                this.cancellationToken = cancellationToken;

                containingType = semanticModel.GetEnclosingNamedType(node.SpanStart, cancellationToken);
                containingTypeOrAssembly = containingType ?? (ISymbol)semanticModel.Compilation.Assembly;
                namespacesInScope = owner.GetNamespacesInScope(semanticModel, node, cancellationToken);
                syntaxFacts = document.Project.LanguageServices.GetService<ISyntaxFactsService>();
            }

            internal async Task<List<SymbolReference>> DoAsync(Project project, bool includeDirectReferences)
            {
                var searchScope = new ProjectSearchScope(project, includeDirectReferences, owner.IgnoreCase, cancellationToken);

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
                cancellationToken.ThrowIfCancellationRequested();

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
                if (!owner.CanAddImportForType(diagnostic, ref node))
                {
                    return null;
                }

                string name;
                int arity;
                bool inAttributeContext, hasIncompleteParentMember;
                CalculateContext(node, syntaxFacts, out name, out arity, out inAttributeContext, out hasIncompleteParentMember);

                var symbols = await GetTypeSymbols(searchScope, name, inAttributeContext).ConfigureAwait(false);
                if (symbols == null)
                {
                    return null;
                }

                return GetNamespacesForMatchingTypesAsync(searchScope, arity, inAttributeContext, hasIncompleteParentMember, symbols);
            }

            private async Task<IList<SymbolReference>> GetMatchingTypesAsync(SearchScope searchScope)
            {
                if (!owner.CanAddImportForType(diagnostic, ref node))
                {
                    return null;
                }

                string name;
                int arity;
                bool inAttributeContext, hasIncompleteParentMember;
                CalculateContext(node, syntaxFacts, out name, out arity, out inAttributeContext, out hasIncompleteParentMember);

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
                if (cancellationToken.IsCancellationRequested)
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
                cancellationToken.ThrowIfCancellationRequested();
                var symbolInfo = semanticModel.GetSymbolInfo(node, cancellationToken);
                if (symbolInfo.CandidateReason == CandidateReason.OverloadResolutionFailure && !checkForExtensionMethods)
                {
                    return true;
                }

                return symbolInfo.Symbol != null;
            }

            private async Task<IList<SymbolReference>> GetNamespacesForMatchingNamespacesAsync(SearchScope searchScope)
            {
                if (!owner.CanAddImportForNamespace(diagnostic, ref node))
                {
                    return null;
                }

                string name;
                int arity;
                syntaxFacts.GetNameAndArityOfSimpleName(node, out name, out arity);

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
                if (!owner.CanAddImportForMethod(diagnostic, syntaxFacts, ref node))
                {
                    return null;
                }

                var expression = node.Parent;

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
                if (!owner.CanAddImportForMethod(diagnostic, syntaxFacts, ref node))
                {
                    return null;
                }

                var expression = node.Parent;

                var symbols = await GetSymbolsAsync(searchScope).ConfigureAwait(false);

                if (symbols != null)
                {
                    return FilterForFieldsAndProperties(searchScope, expression, symbols);
                }

                return null;
            }

            private async Task<IList<SymbolReference>> GetNamespacesForQueryPatternsAsync(SearchScope searchScope)
            {
                if (!owner.CanAddImportForQuery(diagnostic, ref node))
                {
                    return null;
                }

                ITypeSymbol type = owner.GetQueryClauseInfo(semanticModel, node, cancellationToken);
                if (type == null)
                {
                    return null;
                }

                // find extension methods named "Select"
                var symbols = await searchScope.FindDeclarationsAsync("Select", SymbolFilter.Member).ConfigureAwait(false);

                var extensionMethodSymbols = symbols
                    .OfType<IMethodSymbol>()
                    .Where(s => s.IsExtensionMethod && owner.IsViableExtensionMethod(type, s))
                    .ToList();

                return GetProposedNamespaces(
                    searchScope, extensionMethodSymbols.Select(s => s.ContainingNamespace));
            }

            private List<SymbolReference> GetMatchingTypes(
                SearchScope searchScope, string name, int arity, bool inAttributeContext, IEnumerable<ITypeSymbol> symbols, bool hasIncompleteParentMember)
            {
                var accessibleTypeSymbols = symbols
                    .Where(s => ArityAccessibilityAndAttributeContextAreCorrect(
                                    semanticModel, s, arity,
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
                                    semanticModel, s, arity,
                                    inAttributeContext, hasIncompleteParentMember))
                    .ToList();

                return GetProposedNamespaces(searchScope, accessibleTypeSymbols.Select(s => s.ContainingNamespace));
            }

            private List<SymbolReference> GetProposedNamespaces(SearchScope scope, IEnumerable<INamespaceSymbol> namespaces)
            {
                // We only want to offer to add a using if we don't already have one.
                return
                    namespaces.Where(n => !n.IsGlobalNamespace)
                              .Select(n => semanticModel.Compilation.GetCompilationNamespace(n) ?? n)
                              .Where(n => n != null && !namespacesInScope.Contains(n))
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
                cancellationToken.ThrowIfCancellationRequested();

                // See if the name binds.  If it does, there's nothing further we need to do.
                if (ExpressionBinds(checkForExtensionMethods: true))
                {
                    return SpecializedTasks.EmptyEnumerable<ISymbol>();
                }

                string name;
                int arity;
                syntaxFacts.GetNameAndArityOfSimpleName(node, out name, out arity);
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
                                     method.IsAccessibleWithin(semanticModel.Compilation.Assembly) == true &&
                                     owner.IsViableExtensionMethod(method, expression, semanticModel, syntaxFacts, cancellationToken))
                    .ToList();

                return GetProposedNamespaces(
                    searchScope, extensionMethodSymbols.Select(s => s.ContainingNamespace));
            }

            private IList<SymbolReference> FilterForFieldsAndProperties(
                SearchScope searchScope, SyntaxNode expression, IEnumerable<ISymbol> symbols)
            {
                var propertySymbols = symbols
                    .OfType<IPropertySymbol>()
                    .Where(property => property.IsAccessibleWithin(semanticModel.Compilation.Assembly) == true &&
                                       owner.IsViableProperty(property, expression, semanticModel, syntaxFacts, cancellationToken))
                    .ToList();

                var fieldSymbols = symbols
                    .OfType<IFieldSymbol>()
                    .Where(field => field.IsAccessibleWithin(semanticModel.Compilation.Assembly) == true &&
                                    owner.IsViableField(field, expression, semanticModel, syntaxFacts, cancellationToken))
                    .ToList();

                return GetProposedNamespaces(
                    searchScope, propertySymbols.Select(s => s.ContainingNamespace).Concat(fieldSymbols.Select(s => s.ContainingNamespace)));
            }

            private async Task<IEnumerable<IMethodSymbol>> GetAddMethodsAsync(
                SearchScope searchScope, SyntaxNode expression)
            {
                string name;
                int arity;
                syntaxFacts.GetNameAndArityOfSimpleName(node, out name, out arity);
                if (name != null)
                {
                    return SpecializedCollections.EmptyEnumerable<IMethodSymbol>();
                }

                if (owner.IsAddMethodContext(node, semanticModel))
                {
                    var symbols = await searchScope.FindDeclarationsAsync("Add", SymbolFilter.Member).ConfigureAwait(false);
                    return symbols
                        .OfType<IMethodSymbol>()
                        .Where(method => method.IsExtensionMethod &&
                                         method.IsAccessibleWithin(semanticModel.Compilation.Assembly) == true &&
                                         owner.IsViableExtensionMethod(method, expression, semanticModel, syntaxFacts, cancellationToken));
                }

                return SpecializedCollections.EmptyEnumerable<IMethodSymbol>();
            }
        }
    }
}