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
        protected abstract bool IsViableExtensionMethod(IMethodSymbol method, SyntaxNode expression, SemanticModel semanticModel, ISyntaxFactsService syntaxFacts, CancellationToken cancellationToken);

        protected abstract Task<Document> AddImportAsync(SyntaxNode contextNode, INamespaceOrTypeSymbol symbol, Document document, bool specialCaseSystem, CancellationToken cancellationToken);
        protected abstract Task<Document> AddImportAsync(SyntaxNode contextNode, IReadOnlyList<string> nameSpaceParts, Document document, bool specialCaseSystem, CancellationToken cancellationToken);

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
                        var allSymbolReferences = await FindResultsAsync(document, semanticModel, diagnostic, node, cancellationToken).ConfigureAwait(false);

                        // Nothing found at all. No need to proceed.
                        if (allSymbolReferences == null || allSymbolReferences.Count == 0)
                        {
                            return;
                        }

                        foreach (var reference in allSymbolReferences)
                        {
                            cancellationToken.ThrowIfCancellationRequested();

                            var description = reference.GetDescription(semanticModel, node);// this.GetDescription(reference.SearchResult.Symbol, semanticModel, node);
                            if (description != null)
                            {
                                var action = new MyCodeAction2(description, 
                                    c => reference.GetOperationsAsync(document, node, placeSystemNamespaceFirst, c));
                                context.RegisterCodeFix(action, diagnostic);
                            }
                        }
                    }
                }
            }
        }

        private async Task<IReadOnlyList<Reference>> FindResultsAsync(
            Document document, SemanticModel semanticModel, Diagnostic diagnostic, SyntaxNode node, CancellationToken cancellationToken)
        {
            var finder = new SymbolReferenceFinder(this, document, semanticModel, diagnostic, node, cancellationToken);

            // Look for exact matches first:
            var exactReferences = await FindResultsAsync(document.Project, finder, exact: true, cancellationToken: cancellationToken).ConfigureAwait(false);
            if (exactReferences?.Count > 0)
            {
                return exactReferences;
            }

            // No exact matches found.  Fall back to fuzzy searching.
            var fuzzyReferences = await FindResultsAsync(document.Project, finder, exact: false, cancellationToken: cancellationToken).ConfigureAwait(false);
            if (fuzzyReferences?.Count > 0)
            {
                return fuzzyReferences;
            }

            return await finder.FindNugetReferencesAsync().ConfigureAwait(false);
        }

        private async Task<List<Reference>> FindResultsAsync(
            Project project, SymbolReferenceFinder finder, bool exact, CancellationToken cancellationToken)
        {
            var allSymbolReferences = new List<Reference>();
            await FindResultsInCurrentProject(project, allSymbolReferences, finder, exact).ConfigureAwait(false);
            await FindResultsInUnreferencedProjects(project, allSymbolReferences, finder, exact, cancellationToken).ConfigureAwait(false);
            await FindResultsInUnreferencedMetadataReferences(project, allSymbolReferences, finder, exact, cancellationToken).ConfigureAwait(false);
            return allSymbolReferences;
        }

        private async Task FindResultsInCurrentProject(
            Project project, List<Reference> allSymbolReferences, SymbolReferenceFinder finder, bool exact)
        {
            AddRange(allSymbolReferences, await finder.FindInProjectAsync(project, includeDirectReferences: true, exact: exact).ConfigureAwait(false));
        }

        private async Task FindResultsInUnreferencedProjects(
            Project project, List<Reference> allSymbolReferences, SymbolReferenceFinder finder, bool exact, CancellationToken cancellationToken)
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
            Project project, List<Reference> allSymbolReferences, SymbolReferenceFinder finder, bool exact, CancellationToken cancellationToken)
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
            List<Reference> allSymbolReferences,
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

        private void AddRange(List<Reference> allSymbolReferences, IReadOnlyList<Reference> proposedReferences)
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
            var symbol = reference.SymbolResult.Symbol;
            return symbol.IsNamespace ? !((INamespaceSymbol)symbol).IsGlobalNamespace : true;
        }

        private static bool NotNull(SymbolReference reference)
        {
            return reference.SymbolResult.Symbol != null;
        }

        private class MyCodeAction2 : CodeAction
        {
            private readonly string _title;
            private readonly Func<CancellationToken, Task<ImmutableArray<CodeActionOperation>>> _getOperations;

            public override string Title => _title;
            public override string EquivalenceKey => _title;

            public MyCodeAction2(string title, Func<CancellationToken, Task<ImmutableArray<CodeActionOperation>>> getOperations)
            {
                _title = title;
                _getOperations = getOperations;
            }

            internal override Task<ImmutableArray<CodeActionOperation>> GetOperationsCoreAsync(CancellationToken cancellationToken)
            {
                return _getOperations(cancellationToken);
            }
        }

        private class MyCodeAction : CodeAction.SolutionChangeAction
        {
            public MyCodeAction(string title, Func<CancellationToken, Task<Solution>> createChangedSolution) :
                base(title, createChangedSolution, equivalenceKey: title)
            {
            }
        }
    }
}