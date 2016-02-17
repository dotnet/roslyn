// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Host;
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

                        // Caches so we don't produce the same data multiple times while searching 
                        // all over the solution.
                        var projectToAssembly = new ConcurrentDictionary<Project, AsyncLazy<IAssemblySymbol>>(concurrencyLevel: 2, capacity: project.Solution.ProjectIds.Count);
                        var referenceToCompilation = new ConcurrentDictionary<PortableExecutableReference, Compilation>(concurrencyLevel: 2, capacity: project.Solution.Projects.Sum(p => p.MetadataReferences.Count));

                        // Look for exact matches first:
                        await FindResults(projectToAssembly, referenceToCompilation, project, allSymbolReferences, finder, exact: true, cancellationToken: cancellationToken).ConfigureAwait(false);
                        if (allSymbolReferences.Count == 0)
                        {
                            // No exact matches found.  Fall back to fuzzy searching.
                            // Only bother doing this for host workspaces.  We don't want this for 
                            // things like the Interactive workspace as this will cause us to 
                            // create expensive bktrees which we won't even be able to save for 
                            // future use.
                            if (document.Project.Solution.Workspace.Kind == WorkspaceKind.Host)
                            {
                                await FindResults(projectToAssembly, referenceToCompilation, project, allSymbolReferences, finder, exact: false, cancellationToken: cancellationToken).ConfigureAwait(false);
                            }
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
                                var action = new MyCodeAction(description, reference.GetGlyph(document),
                                    c => this.AddImportAndReferenceAsync(node, reference, document, placeSystemNamespaceFirst, c),
                                    reference.GetIsApplicableCheck(document.Project));
                                context.RegisterCodeFix(action, diagnostic);
                            }
                        }
                    }
                }
            }
        }

        private async Task FindResults(
            ConcurrentDictionary<Project, AsyncLazy<IAssemblySymbol>> projectToAssembly,
            ConcurrentDictionary<PortableExecutableReference, Compilation> referenceToCompilation,
            Project project, List<SymbolReference> allSymbolReferences, SymbolReferenceFinder finder, bool exact, CancellationToken cancellationToken)
        {
            // First search the current project to see if any symbols (source or metadata) match the 
            // search string.
            await FindResultsInAllProjectSymbolsAsync(project, allSymbolReferences, finder, exact).ConfigureAwait(false);

            // Now search unreferenced projects, and see if they have any source symbols that match
            // the search string.
            await FindResultsInUnreferencedProjectSourceSymbolsAsync(projectToAssembly, project, allSymbolReferences, finder, exact, cancellationToken).ConfigureAwait(false);

            // Finally, check and see if we have any metadata symbols that match the search string.
            await FindResultsInUnreferencedMetadataSymbolsAsync(referenceToCompilation, project, allSymbolReferences, finder, exact, cancellationToken).ConfigureAwait(false);
        }

        private async Task FindResultsInAllProjectSymbolsAsync(
            Project project, List<SymbolReference> allSymbolReferences, SymbolReferenceFinder finder, bool exact)
        {
            var references = await finder.FindInAllProjectSymbolsAsync(project, exact).ConfigureAwait(false);
            AddRange(allSymbolReferences, references);
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

        private async Task FindResultsInUnreferencedProjectSourceSymbolsAsync(
            ConcurrentDictionary<Project, AsyncLazy<IAssemblySymbol>> projectToAssembly,
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
                AddRange(allSymbolReferences, await finder.FindInSourceProjectSymbolsAsync(projectToAssembly, unreferencedProject, exact: exact).ConfigureAwait(false));
                if (allSymbolReferences.Count >= MaxResults)
                {
                    return;
                }
            }
        }

        private async Task FindResultsInUnreferencedMetadataSymbolsAsync(
            ConcurrentDictionary<PortableExecutableReference, Compilation> referenceToCompilation,
            Project project, List<SymbolReference> allSymbolReferences, SymbolReferenceFinder finder, bool exact,
            CancellationToken cancellationToken)
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

            var newReferences =
                project.Solution.Projects.Where(p => p != project)
                                         .SelectMany(p => p.MetadataReferences.OfType<PortableExecutableReference>())
                                         .Distinct(comparer: this)
                                         .Where(r => !seenReferences.Contains(r));

            // Search all metadata references in parallel.
            var findTasks = new HashSet<Task<List<SymbolReference>>>();

            foreach (var reference in newReferences)
            {
                var compilation = referenceToCompilation.GetOrAdd(reference, r => CreateCompilation(project, r));

                // Ignore netmodules.  First, they're incredibly esoteric and barely used.
                // Second, the SymbolFinder api doesn't even support searching them. 
                var assembly = compilation.GetAssemblyOrModuleSymbol(reference) as IAssemblySymbol;
                if (assembly != null)
                {
                    findTasks.Add(finder.FindInMetadataAsync(project.Solution, assembly, reference, exact));
                }
            }

            while (findTasks.Count > 0)
            {
                // Keep on looping through the 'find' tasks, processing each when they finish.
                cancellationToken.ThrowIfCancellationRequested();
                var doneTask = await Task.WhenAny(findTasks).ConfigureAwait(false);

                // One of the tasks finished.  Remove it from the list we're waiting on.
                findTasks.Remove(doneTask);

                // Add its results to the final result set we're keeping.
                AddRange(allSymbolReferences, await doneTask.ConfigureAwait(false));

                // If we've got enough, no need to keep searching. 
                // Note: We do not cancel the existing tasks that are still executing.  These tasks will
                // cause our indices to be created if necessary.  And that's good for future searches which
                // we will invariably perform.
                if (allSymbolReferences.Count >= MaxResults)
                {
                    break;
                }
            }
        }

        /// <summary>
        /// Called when when we want to search a metadata reference.  We create a dummy compilation
        /// containing just that reference and we search that.  That way we can get actual symbols
        /// returned.
        /// 
        /// We don't want to use the project that the reference is actually associated with as 
        /// getting the compilation for that project may be extremely expensive.  For example,
        /// in a large solution it may cause us to build an enormous amount of skeleton assemblies.
        /// </summary>
        private Compilation CreateCompilation(Project project, PortableExecutableReference reference)
        {
            var compilationService = project.LanguageServices.GetService<ICompilationFactoryService>();
            var compilation = compilationService.CreateCompilation("TempAssembly", compilationService.GetDefaultCompilationOptions());
            return compilation.WithReferences(reference);
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

        protected bool IsViableExtensionMethod(IMethodSymbol method, ITypeSymbol receiver)
        {
            if (receiver == null || method == null)
            {
                return false;
            }

            // It's possible that the 'method' we're looking at is from a different language than
            // the language we're currently in.  For example, we might find the extension method
            // in an unreferenced VB project while we're in C#.  However, in order to 'reduce'
            // the extension method, the compiler requires both the method and receiver to be 
            // from the same language.
            //
            // So, if they're not from the same language, we simply can't proceed.  Now in this 
            // case we decide that the method is not viable.  But we could, in the future, decide
            // to just always consider such methods viable.

            if (receiver.Language != method.Language)
            {
                return false;
            }

            return method.ReduceExtensionMethod(receiver) != null;
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
            private readonly Glyph? _glyph;
            private readonly Func<Workspace, bool> _isApplicable;

            public MyCodeAction(
                string title,
                Glyph? glyph,
                Func<CancellationToken, Task<Solution>> createChangedSolution,
                Func<Workspace, bool> isApplicable = null) :
                base(title, createChangedSolution, equivalenceKey: title)
            {
                _glyph = glyph;
                _isApplicable = isApplicable;
            }

            internal override int? Glyph => _glyph.HasValue ? (int)_glyph.Value : (int?)null;

            internal override bool PerformFinalApplicabilityCheck => _isApplicable != null;

            internal override bool IsApplicable(Workspace workspace)
            {
                return _isApplicable == null ? true : _isApplicable(workspace);
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