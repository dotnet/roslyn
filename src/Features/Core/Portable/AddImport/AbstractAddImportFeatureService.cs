﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Packaging;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Remote;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.SymbolSearch;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.AddImport
{
    internal abstract partial class AbstractAddImportFeatureService<TSimpleNameSyntax>
        : IAddImportFeatureService, IEqualityComparer<PortableExecutableReference>
        where TSimpleNameSyntax : SyntaxNode
    {
        private const int MaxResults = 3;

        /// <summary>
        /// Values for these parameters can be provided (during testing) for mocking purposes.
        /// </summary> 
        protected AbstractAddImportFeatureService()
        {
        }

        protected abstract bool CanAddImport(SyntaxNode node, CancellationToken cancellationToken);
        protected abstract bool CanAddImportForMethod(string diagnosticId, ISyntaxFactsService syntaxFacts, SyntaxNode node, out TSimpleNameSyntax nameNode);
        protected abstract bool CanAddImportForNamespace(string diagnosticId, SyntaxNode node, out TSimpleNameSyntax nameNode);
        protected abstract bool CanAddImportForDeconstruct(string diagnosticId, SyntaxNode node);
        protected abstract bool CanAddImportForQuery(string diagnosticId, SyntaxNode node);
        protected abstract bool CanAddImportForType(string diagnosticId, SyntaxNode node, out TSimpleNameSyntax nameNode);

        protected abstract ISet<INamespaceSymbol> GetImportNamespacesInScope(SemanticModel semanticModel, SyntaxNode node, CancellationToken cancellationToken);
        protected abstract ITypeSymbol GetDeconstructInfo(SemanticModel semanticModel, SyntaxNode node, CancellationToken cancellationToken);
        protected abstract ITypeSymbol GetQueryClauseInfo(SemanticModel semanticModel, SyntaxNode node, CancellationToken cancellationToken);
        protected abstract bool IsViableExtensionMethod(IMethodSymbol method, SyntaxNode expression, SemanticModel semanticModel, ISyntaxFactsService syntaxFacts, CancellationToken cancellationToken);

        protected abstract Task<Document> AddImportAsync(SyntaxNode contextNode, INamespaceOrTypeSymbol symbol, Document document, bool specialCaseSystem, CancellationToken cancellationToken);
        protected abstract Task<Document> AddImportAsync(SyntaxNode contextNode, IReadOnlyList<string> nameSpaceParts, Document document, bool specialCaseSystem, CancellationToken cancellationToken);

        protected abstract bool IsAddMethodContext(SyntaxNode node, SemanticModel semanticModel);

        protected abstract string GetDescription(IReadOnlyList<string> nameParts);
        protected abstract (string description, bool hasExistingImport) GetDescription(Document document, INamespaceOrTypeSymbol symbol, SemanticModel semanticModel, SyntaxNode root, CancellationToken cancellationToken);

        public async Task<ImmutableArray<AddImportFixData>> GetFixesAsync(
            Document document, TextSpan span, string diagnosticId, bool placeSystemNamespaceFirst,
            ISymbolSearchService symbolSearchService, bool searchReferenceAssemblies,
            ImmutableArray<PackageSource> packageSources, CancellationToken cancellationToken)
        {
            if (RemoteSupportedLanguages.IsSupported(document.Project.Language))
            {
                var callbackTarget = new RemoteSymbolSearchService(symbolSearchService, cancellationToken);
                var result = await document.Project.Solution.TryRunCodeAnalysisRemoteAsync<ImmutableArray<AddImportFixData>>(
                    RemoteFeatureOptions.AddImportEnabled,
                    callbackTarget,
                    nameof(IRemoteAddImportFeatureService.GetFixesAsync),
                    new object[]
                    {
                    document.Id,
                    span,
                    diagnosticId,
                    placeSystemNamespaceFirst,
                    searchReferenceAssemblies,
                    packageSources
                    },
                    cancellationToken).ConfigureAwait(false);

                var documentOptions = await document.GetOptionsAsync(cancellationToken).ConfigureAwait(false);

                if (!result.IsDefault)
                {
                    return result;
                }
            }

            return await GetFixesInCurrentProcessAsync(
                document, span, diagnosticId, placeSystemNamespaceFirst,
                symbolSearchService, searchReferenceAssemblies,
                packageSources, cancellationToken).ConfigureAwait(false);
        }

        private async Task<ImmutableArray<AddImportFixData>> GetFixesInCurrentProcessAsync(
            Document document, TextSpan span, string diagnosticId, bool placeSystemNamespaceFirst,
            ISymbolSearchService symbolSearchService, bool searchReferenceAssemblies,
            ImmutableArray<PackageSource> packageSources, CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var node = root.FindToken(span.Start, findInsideTrivia: true)
                           .GetAncestor(n => n.Span.Contains(span) && n != root);

            var result = ArrayBuilder<AddImportFixData>.GetInstance();
            if (node != null)
            {

                using (Logger.LogBlock(FunctionId.Refactoring_AddImport, cancellationToken))
                {
                    if (!cancellationToken.IsCancellationRequested)
                    {
                        if (this.CanAddImport(node, cancellationToken))
                        {
                            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
                            var allSymbolReferences = await FindResultsAsync(
                                document, semanticModel, diagnosticId, node, symbolSearchService,
                                searchReferenceAssemblies, packageSources, cancellationToken).ConfigureAwait(false);

                            // Nothing found at all. No need to proceed.
                            foreach (var reference in allSymbolReferences)
                            {
                                cancellationToken.ThrowIfCancellationRequested();

                                var fixData = await reference.TryGetFixDataAsync(document, node, placeSystemNamespaceFirst, cancellationToken).ConfigureAwait(false);
                                result.AddIfNotNull(fixData);
                            }
                        }
                    }
                }
            }

            return result.ToImmutableAndFree();
        }

        private async Task<ImmutableArray<Reference>> FindResultsAsync(
            Document document, SemanticModel semanticModel, string diagnosticId, SyntaxNode node, ISymbolSearchService symbolSearchService,
            bool searchReferenceAssemblies, ImmutableArray<PackageSource> packageSources, CancellationToken cancellationToken)
        {
            // Caches so we don't produce the same data multiple times while searching 
            // all over the solution.
            var project = document.Project;
            var projectToAssembly = new ConcurrentDictionary<Project, AsyncLazy<IAssemblySymbol>>(concurrencyLevel: 2, capacity: project.Solution.ProjectIds.Count);
            var referenceToCompilation = new ConcurrentDictionary<PortableExecutableReference, Compilation>(concurrencyLevel: 2, capacity: project.Solution.Projects.Sum(p => p.MetadataReferences.Count));

            var finder = new SymbolReferenceFinder(
                this, document, semanticModel, diagnosticId, node, symbolSearchService,
                searchReferenceAssemblies, packageSources, cancellationToken);

            // Look for exact matches first:
            var exactReferences = await FindResultsAsync(projectToAssembly, referenceToCompilation, project, finder, exact: true, cancellationToken: cancellationToken).ConfigureAwait(false);
            if (exactReferences.Length > 0)
            {
                return exactReferences;
            }

            // No exact matches found.  Fall back to fuzzy searching.
            // Only bother doing this for host workspaces.  We don't want this for 
            // things like the Interactive workspace as this will cause us to 
            // create expensive bk-trees which we won't even be able to save for 
            // future use.
            if (!IsHostOrTestOrRemoteWorkspace(project))
            {
                return ImmutableArray<Reference>.Empty;
            }

            var fuzzyReferences = await FindResultsAsync(projectToAssembly, referenceToCompilation, project, finder, exact: false, cancellationToken: cancellationToken).ConfigureAwait(false);
            return fuzzyReferences;
        }

        private static bool IsHostOrTestOrRemoteWorkspace(Project project)
        {
            return project.Solution.Workspace.Kind == WorkspaceKind.Host ||
                   project.Solution.Workspace.Kind == WorkspaceKind.Test ||
                   project.Solution.Workspace.Kind == WorkspaceKind.RemoteWorkspace;
        }

        private async Task<ImmutableArray<Reference>> FindResultsAsync(
            ConcurrentDictionary<Project, AsyncLazy<IAssemblySymbol>> projectToAssembly,
            ConcurrentDictionary<PortableExecutableReference, Compilation> referenceToCompilation,
            Project project, SymbolReferenceFinder finder, bool exact, CancellationToken cancellationToken)
        {
            var allReferences = ArrayBuilder<Reference>.GetInstance();

            // First search the current project to see if any symbols (source or metadata) match the 
            // search string.
            await FindResultsInAllSymbolsInStartingProjectAsync(
                allReferences, finder, exact, cancellationToken).ConfigureAwait(false);

            // Only bother doing this for host workspaces.  We don't want this for 
            // things like the Interactive workspace as we can't even add project
            // references to the interactive window.  We could consider adding metadata
            // references with #r in the future.
            if (IsHostOrTestOrRemoteWorkspace(project))
            {
                // Now search unreferenced projects, and see if they have any source symbols that match
                // the search string.
                await FindResultsInUnreferencedProjectSourceSymbolsAsync(projectToAssembly, project, allReferences, finder, exact, cancellationToken).ConfigureAwait(false);

                // Finally, check and see if we have any metadata symbols that match the search string.
                await FindResultsInUnreferencedMetadataSymbolsAsync(referenceToCompilation, project, allReferences, finder, exact, cancellationToken).ConfigureAwait(false);

                // We only support searching NuGet in an exact manner currently. 
                if (exact)
                {
                    await finder.FindNugetOrReferenceAssemblyReferencesAsync(allReferences, cancellationToken).ConfigureAwait(false);
                }
            }

            return allReferences.ToImmutableAndFree();
        }

        private async Task FindResultsInAllSymbolsInStartingProjectAsync(
            ArrayBuilder<Reference> allSymbolReferences, SymbolReferenceFinder finder,
            bool exact, CancellationToken cancellationToken)
        {
            var references = await finder.FindInAllSymbolsInStartingProjectAsync(exact, cancellationToken).ConfigureAwait(false);
            AddRange(allSymbolReferences, references);
        }

        private async Task FindResultsInUnreferencedProjectSourceSymbolsAsync(
            ConcurrentDictionary<Project, AsyncLazy<IAssemblySymbol>> projectToAssembly,
            Project project, ArrayBuilder<Reference> allSymbolReferences, SymbolReferenceFinder finder, bool exact, CancellationToken cancellationToken)
        {
            // If we didn't find enough hits searching just in the project, then check 
            // in any unreferenced projects.
            if (allSymbolReferences.Count >= MaxResults)
            {
                return;
            }

            var viableUnreferencedProjects = GetViableUnreferencedProjects(project);

            // Search all unreferenced projects in parallel.
            var findTasks = new HashSet<Task<ImmutableArray<SymbolReference>>>();

            // Create another cancellation token so we can both search all projects in parallel,
            // but also stop any searches once we get enough results.
            using (var nestedTokenSource = new CancellationTokenSource())
            using (var linkedTokenSource = CancellationTokenSource.CreateLinkedTokenSource(nestedTokenSource.Token, cancellationToken))
            {
                foreach (var unreferencedProject in viableUnreferencedProjects)
                {
                    // Search in this unreferenced project.  But don't search in any of its'
                    // direct references.  i.e. we don't want to search in its metadata references
                    // or in the projects it references itself. We'll be searching those entities
                    // individually.
                    findTasks.Add(finder.FindInSourceSymbolsInProjectAsync(
                        projectToAssembly, unreferencedProject, exact, linkedTokenSource.Token));
                }

                await WaitForTasksAsync(allSymbolReferences, findTasks, nestedTokenSource, cancellationToken).ConfigureAwait(false);
            }
        }

        private async Task FindResultsInUnreferencedMetadataSymbolsAsync(
            ConcurrentDictionary<PortableExecutableReference, Compilation> referenceToCompilation,
            Project project, ArrayBuilder<Reference> allSymbolReferences, SymbolReferenceFinder finder, bool exact,
            CancellationToken cancellationToken)
        {
            if (allSymbolReferences.Count > 0)
            {
                // Only do this if none of the project searches produced any results. We may have a 
                // lot of metadata to search through, and it would be good to avoid that if we can.
                return;
            }

            // Keep track of the references we've seen (so that we don't process them multiple times
            // across many sibling projects).  Prepopulate it with our own metadata references since
            // we know we don't need to search in that.
            var seenReferences = new HashSet<PortableExecutableReference>(comparer: this);
            seenReferences.AddAll(project.MetadataReferences.OfType<PortableExecutableReference>());

            var newReferences = GetUnreferencedMetadataReferences(project, seenReferences);

            // Search all metadata references in parallel.
            var findTasks = new HashSet<Task<ImmutableArray<SymbolReference>>>();

            // Create another cancellation token so we can both search all projects in parallel,
            // but also stop any searches once we get enough results.
            using (var nestedTokenSource = new CancellationTokenSource())
            using (var linkedTokenSource = CancellationTokenSource.CreateLinkedTokenSource(nestedTokenSource.Token, cancellationToken))
            {
                foreach (var (referenceProjectId, reference) in newReferences)
                {
                    var compilation = referenceToCompilation.GetOrAdd(
                        reference, r => CreateCompilation(project, r));

                    // Ignore netmodules.  First, they're incredibly esoteric and barely used.
                    // Second, the SymbolFinder API doesn't even support searching them. 
                    var assembly = compilation.GetAssemblyOrModuleSymbol(reference) as IAssemblySymbol;
                    if (assembly != null)
                    {
                        findTasks.Add(finder.FindInMetadataSymbolsAsync(
                            assembly, referenceProjectId, reference, exact, linkedTokenSource.Token));
                    }
                }

                await WaitForTasksAsync(allSymbolReferences, findTasks, nestedTokenSource, cancellationToken).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Returns the set of PEReferences in the solution that are not currently being referenced
        /// by this project.  The set returned will be tuples containing the PEReference, and the project-id
        /// for the project we found the pe-reference in.
        /// </summary>
        private ImmutableArray<(ProjectId, PortableExecutableReference)> GetUnreferencedMetadataReferences(
            Project project, HashSet<PortableExecutableReference> seenReferences)
        {
            var result = ArrayBuilder<(ProjectId, PortableExecutableReference)>.GetInstance();

            var solution = project.Solution;
            foreach (var p in solution.Projects)
            {
                if (p == project)
                {
                    continue;
                }

                foreach (var reference in p.MetadataReferences)
                {
                    if (reference is PortableExecutableReference peReference &&
                        !IsInPackagesDirectory(peReference) &&
                        seenReferences.Add(peReference))
                    {
                        result.Add((p.Id, peReference));
                    }
                }
            }

            return result.ToImmutableAndFree();
        }

        private async Task WaitForTasksAsync(
            ArrayBuilder<Reference> allSymbolReferences,
            HashSet<Task<ImmutableArray<SymbolReference>>> findTasks,
            CancellationTokenSource nestedTokenSource,
            CancellationToken cancellationToken)
        {
            try
            {
                while (findTasks.Count > 0)
                {
                    // Keep on looping through the 'find' tasks, processing each when they finish.
                    cancellationToken.ThrowIfCancellationRequested();
                    var doneTask = await Task.WhenAny(findTasks).ConfigureAwait(false);

                    // One of the tasks finished.  Remove it from the list we're waiting on.
                    findTasks.Remove(doneTask);

                    // Add its results to the final result set we're keeping.
                    AddRange(allSymbolReferences, await doneTask.ConfigureAwait(false));

                    // Once we get enough, just stop.
                    if (allSymbolReferences.Count >= MaxResults)
                    {
                        return;
                    }
                }
            }
            finally
            {
                // Cancel any nested work that's still happening.
                nestedTokenSource.Cancel();
            }
        }

        /// <summary>
        /// We ignore references that are in a directory that contains the names "Packages".
        /// These directories are most likely the ones produced by NuGet, and we don't want
        /// to offer to add .dll reference manually for dlls that are part of NuGet packages.
        /// 
        /// Note that this is only a heuristic (though a good one), and we should remove this
        /// when we can get an API from NuGet that tells us if a reference is actually provided
        /// by a nuget packages.
        /// 
        /// This heuristic will do the right thing in practically all cases for all. It 
        /// prevents the very unpleasant experience of us offering to add a direct metadata 
        /// reference to something that should only be referenced as a nuget package.
        ///
        /// It does mean that if the following is true:
        /// You have a project that has a non-nuget metadata reference to something in a "packages"
        /// directory, and you are in another project that uses a type name that would have matched
        /// an accessible type from that dll. then we will not offer to add that .dll reference to
        /// that other project.
        /// 
        /// However, that would be an exceedingly uncommon case that is degraded.  Whereas we're 
        /// vastly improved in the common case. This is a totally acceptable and desirable outcome
        /// for such a heuristic.
        /// </summary>
        private bool IsInPackagesDirectory(PortableExecutableReference reference)
        {
            return PathUtilities.ContainsPathComponent(reference.FilePath, "packages", ignoreCase: true);
        }

        /// <summary>
        /// Called when we want to search a metadata reference.  We create a dummy compilation
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
            => StringComparer.OrdinalIgnoreCase.Equals(x.FilePath ?? x.Display, y.FilePath ?? y.Display);

        int IEqualityComparer<PortableExecutableReference>.GetHashCode(PortableExecutableReference obj)
            => StringComparer.OrdinalIgnoreCase.GetHashCode(obj.FilePath ?? obj.Display);

        private static HashSet<Project> GetViableUnreferencedProjects(Project project)
        {
            var solution = project.Solution;
            var viableProjects = new HashSet<Project>(solution.Projects);

            // Clearly we can't reference ourselves.
            viableProjects.Remove(project);

            // We can't reference any project that transitively depends on us.  Doing so would
            // cause a circular reference between projects.
            var dependencyGraph = solution.GetProjectDependencyGraph();
            var projectsThatTransitivelyDependOnThisProject = dependencyGraph.GetProjectsThatTransitivelyDependOnThisProject(project.Id);

            viableProjects.RemoveAll(projectsThatTransitivelyDependOnThisProject.Select(id =>
                solution.GetProject(id)));

            // We also aren't interested in any projects we're already directly referencing.
            viableProjects.RemoveAll(project.ProjectReferences.Select(r => solution.GetProject(r.ProjectId)));
            return viableProjects;
        }

        private void AddRange<TReference>(ArrayBuilder<Reference> allSymbolReferences, ImmutableArray<TReference> proposedReferences)
            where TReference : Reference
        {
            allSymbolReferences.AddRange(proposedReferences.Take(MaxResults - allSymbolReferences.Count));
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

        private static bool NotGlobalNamespace(SymbolReference reference)
        {
            var symbol = reference.SymbolResult.Symbol;
            return symbol.IsNamespace ? !((INamespaceSymbol)symbol).IsGlobalNamespace : true;
        }

        private static bool NotNull(SymbolReference reference)
        {
            return reference.SymbolResult.Symbol != null;
        }
    }
}
