// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
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
    using System.Collections.Immutable;
    using SymbolReference = ValueTuple<INamespaceOrTypeSymbol, Project>;

    internal abstract partial class AbstractAddImportCodeFixProvider : CodeFixProvider, IEqualityComparer<PortableExecutableReference>
    {
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
                            var description = this.GetDescription(reference.Item1, semanticModel, node);
                            if (description != null)
                            {
                                if (reference.Item2.Id != project.Id)
                                {
                                    description = string.Format(FeaturesResources._0_reference_1, description, reference.Item2.Name);
                                }

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
            var newDocument = await this.AddImportAsync(node, reference.Item1, document, placeSystemNamespaceFirst, c).ConfigureAwait(false);

            // If this reference came from searching another project
            if (reference.Item2.Id == document.Project.Id)
            {
                return newDocument.Project.Solution;
            }

            // Also need to add a project reference.
            var newProject = newDocument.Project;
            newProject = newProject.AddProjectReference(new ProjectReference(reference.Item2.Id));

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
                foreach (var reference in proposedReferences)
                {
                    if (allSymbolReferences.Count >= MaxResults)
                    {
                        return;
                    }

                    allSymbolReferences.Add(reference);
                }
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
            var symbol = reference.Item1;
            return symbol.IsNamespace ? !((INamespaceSymbol)symbol).IsGlobalNamespace : true;
        }

        private static bool NotNull(SymbolReference reference)
        {
            return reference.Item1 != null;
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
                // Spin off tasks to do all our searching.
                var tasks = new[]
                {
                    this.GetNamespacesForMatchingTypesAsync(project, includeDirectReferences),
                    this.GetMatchingTypesAsync(project, includeDirectReferences),
                    this.GetNamespacesForMatchingNamespacesAsync(project, includeDirectReferences),
                    this.GetNamespacesForMatchingExtensionMethodsAsync(project, includeDirectReferences),
                    this.GetNamespacesForMatchingFieldsAndPropertiesAsync(project, includeDirectReferences),
                    this.GetNamespacesForQueryPatternsAsync(project, includeDirectReferences)
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
                    .OrderBy(CompareReferences)
                    .ToList();
            }

            private int CompareReferences(SymbolReference x, SymbolReference y)
            {
                return INamespaceOrTypeSymbolExtensions.CompareNamespaceOrTypeSymbols(x.Item1, y.Item1);
            }

            private async Task<IList<SymbolReference>> GetNamespacesForMatchingTypesAsync(
                Project project,
                bool includeDirectReferences)
            {
                if (!owner.CanAddImportForType(diagnostic, ref node))
                {
                    return null;
                }

                string name;
                int arity;
                bool inAttributeContext, hasIncompleteParentMember;
                CalculateContext(node, syntaxFacts, out name, out arity, out inAttributeContext, out hasIncompleteParentMember);

                var symbols = await GetTypeSymbols(project, includeDirectReferences, name, inAttributeContext).ConfigureAwait(false);
                if (symbols == null)
                {
                    return null;
                }

                return GetNamespacesForMatchingTypesAsync(project, arity, inAttributeContext, hasIncompleteParentMember, symbols);
            }

            private async Task<IList<SymbolReference>> GetMatchingTypesAsync(
                Project project,
                bool includeDirectReferences)
            {
                if (!owner.CanAddImportForType(diagnostic, ref node))
                {
                    return null;
                }

                string name;
                int arity;
                bool inAttributeContext, hasIncompleteParentMember;
                CalculateContext(node, syntaxFacts, out name, out arity, out inAttributeContext, out hasIncompleteParentMember);

                var symbols = await GetTypeSymbols(project, includeDirectReferences, name, inAttributeContext).ConfigureAwait(false);
                if (symbols == null)
                {
                    return null;
                }

                return GetMatchingTypes(project, name, arity, inAttributeContext, symbols, hasIncompleteParentMember);
            }

            private async Task<IEnumerable<ITypeSymbol>> GetTypeSymbols(
                Project project,
                bool includeDirectReferences,
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

                var symbols = await SymbolFinder.FindDeclarationsAsync(
                    project, name, owner.IgnoreCase, SymbolFilter.Type, includeDirectReferences, cancellationToken).ConfigureAwait(false);

                // also lookup type symbols with the "Attribute" suffix.
                if (inAttributeContext)
                {
                    symbols = symbols.Concat(
                        await SymbolFinder.FindDeclarationsAsync(project, name + "Attribute", owner.IgnoreCase, SymbolFilter.Type, cancellationToken).ConfigureAwait(false));
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

            private async Task<IList<SymbolReference>> GetNamespacesForMatchingNamespacesAsync(
                Project project,
                bool includeDirectReferences)
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

                var symbols = await SymbolFinder.FindDeclarationsAsync(
                    project, name, owner.IgnoreCase, SymbolFilter.Namespace, includeDirectReferences, cancellationToken).ConfigureAwait(false);

                return GetProposedNamespaces(
                    project, symbols.OfType<INamespaceSymbol>().Select(n => n.ContainingNamespace));
            }

            private async Task<IList<SymbolReference>> GetNamespacesForMatchingExtensionMethodsAsync(
                Project project,
                bool includeDirectReferences)
            {
                if (!owner.CanAddImportForMethod(diagnostic, syntaxFacts, ref node))
                {
                    return null;
                }

                var expression = node.Parent;

                var extensionMethods = SpecializedCollections.EmptyEnumerable<SymbolReference>();
                var symbols = await GetSymbolsAsync(project, includeDirectReferences).ConfigureAwait(false);
                if (symbols != null)
                {
                    extensionMethods = FilterForExtensionMethods(project, expression, symbols);
                }

                var addMethods = SpecializedCollections.EmptyEnumerable<SymbolReference>();
                var methodSymbols = await GetAddMethodsAsync(project, includeDirectReferences, expression).ConfigureAwait(false);
                if (methodSymbols != null)
                {
                    addMethods = GetProposedNamespaces(
                        project, methodSymbols.Select(s => s.ContainingNamespace));
                }

                return extensionMethods.Concat(addMethods).ToList();
            }

            private async Task<IList<SymbolReference>> GetNamespacesForMatchingFieldsAndPropertiesAsync(
                Project project, bool includeDirectReferences)
            {
                if (!owner.CanAddImportForMethod(diagnostic, syntaxFacts, ref node))
                {
                    return null;
                }

                var expression = node.Parent;

                var symbols = await GetSymbolsAsync(project, includeDirectReferences).ConfigureAwait(false);

                if (symbols != null)
                {
                    return FilterForFieldsAndProperties(project, expression, symbols);
                }

                return null;
            }

            private async Task<IList<SymbolReference>> GetNamespacesForQueryPatternsAsync(
                Project project,
                bool includeDirectReferences)
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
                var symbols = await SymbolFinder.FindDeclarationsAsync(
                    project, "Select", owner.IgnoreCase, SymbolFilter.Member, includeDirectReferences, cancellationToken).ConfigureAwait(false);

                var extensionMethodSymbols = symbols
                    .OfType<IMethodSymbol>()
                    .Where(s => s.IsExtensionMethod && owner.IsViableExtensionMethod(type, s))
                    .ToList();

                return GetProposedNamespaces(
                    project, extensionMethodSymbols.Select(s => s.ContainingNamespace));
            }

            private List<SymbolReference> GetMatchingTypes(Project project, string name, int arity, bool inAttributeContext, IEnumerable<ITypeSymbol> symbols, bool hasIncompleteParentMember)
            {
                var accessibleTypeSymbols = symbols
                    .Where(s => ArityAccessibilityAndAttributeContextAreCorrect(
                                    semanticModel, s, arity,
                                    inAttributeContext, hasIncompleteParentMember))
                    .ToList();

                return GetProposedTypes(project, name, accessibleTypeSymbols);
            }

            private List<SymbolReference> GetNamespacesForMatchingTypesAsync(
                Project project, int arity, bool inAttributeContext, bool hasIncompleteParentMember, IEnumerable<ITypeSymbol> symbols)
            {
                var accessibleTypeSymbols = symbols
                    .Where(s => s.ContainingSymbol is INamespaceSymbol
                                && ArityAccessibilityAndAttributeContextAreCorrect(
                                    semanticModel, s, arity,
                                    inAttributeContext, hasIncompleteParentMember))
                    .ToList();

                return GetProposedNamespaces(project, accessibleTypeSymbols.Select(s => s.ContainingNamespace));
            }

            private List<SymbolReference> GetProposedNamespaces(Project project, IEnumerable<INamespaceSymbol> namespaces)
            {
                // We only want to offer to add a using if we don't already have one.
                return
                    namespaces.Where(n => !n.IsGlobalNamespace)
                              .Select(n => semanticModel.Compilation.GetCompilationNamespace(n) ?? n)
                              .Where(n => n != null && !namespacesInScope.Contains(n))
                              .Select(n => new SymbolReference(n, project))
                              .ToList();
            }

            private List<SymbolReference> GetProposedTypes(Project project, string name, List<ITypeSymbol> accessibleTypeSymbols)
            {
                List<SymbolReference> result = null;
                if (accessibleTypeSymbols != null)
                {
                    foreach (var typeSymbol in accessibleTypeSymbols)
                    {
                        if (typeSymbol?.ContainingType != null)
                        {
                            result = result ?? new List<SymbolReference>();
                            result.Add(new SymbolReference(typeSymbol.ContainingType, project));
                        }
                    }
                }

                return result;
            }

            private Task<IEnumerable<ISymbol>> GetSymbolsAsync(
                Project project,
                bool includeDirectReferences)
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

                return SymbolFinder.FindDeclarationsAsync(
                    project, name, owner.IgnoreCase, SymbolFilter.Member, includeDirectReferences, cancellationToken);
            }

            private IEnumerable<SymbolReference> FilterForExtensionMethods(
                Project project, SyntaxNode expression, IEnumerable<ISymbol> symbols)
            {
                var extensionMethodSymbols = symbols
                    .OfType<IMethodSymbol>()
                    .Where(method => method.IsExtensionMethod &&
                                     method.IsAccessibleWithin(semanticModel.Compilation.Assembly) == true &&
                                     owner.IsViableExtensionMethod(method, expression, semanticModel, syntaxFacts, cancellationToken))
                    .ToList();

                return GetProposedNamespaces(
                    project, extensionMethodSymbols.Select(s => s.ContainingNamespace));
            }

            private IList<SymbolReference> FilterForFieldsAndProperties(Project project, SyntaxNode expression, IEnumerable<ISymbol> symbols)
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
                    project, propertySymbols.Select(s => s.ContainingNamespace).Concat(fieldSymbols.Select(s => s.ContainingNamespace)));
            }

            private async Task<IEnumerable<IMethodSymbol>> GetAddMethodsAsync(
                Project project,
                bool includeDirectReferences, 
                SyntaxNode expression)
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
                    var symbols = await SymbolFinder.FindDeclarationsAsync(
                        project, "Add", owner.IgnoreCase, SymbolFilter.Member, includeDirectReferences, cancellationToken).ConfigureAwait(false);
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