// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Packaging;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Remote;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.SymbolSearch;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;
using Microsoft.CodeAnalysis.CodeGeneration;

namespace Microsoft.CodeAnalysis.AddImport
{
    internal abstract partial class AbstractAddImportFeatureService<TSimpleNameSyntax>
        : IAddImportFeatureService, IEqualityComparer<PortableExecutableReference>
        where TSimpleNameSyntax : SyntaxNode
    {
        protected abstract bool CanAddImport(SyntaxNode node, bool allowInHiddenRegions, CancellationToken cancellationToken);
        protected abstract bool CanAddImportForMethod(string diagnosticId, ISyntaxFacts syntaxFacts, SyntaxNode node, out TSimpleNameSyntax nameNode);
        protected abstract bool CanAddImportForNamespace(string diagnosticId, SyntaxNode node, out TSimpleNameSyntax nameNode);
        protected abstract bool CanAddImportForDeconstruct(string diagnosticId, SyntaxNode node);
        protected abstract bool CanAddImportForGetAwaiter(string diagnosticId, ISyntaxFacts syntaxFactsService, SyntaxNode node);
        protected abstract bool CanAddImportForGetEnumerator(string diagnosticId, ISyntaxFacts syntaxFactsService, SyntaxNode node);
        protected abstract bool CanAddImportForGetAsyncEnumerator(string diagnosticId, ISyntaxFacts syntaxFactsService, SyntaxNode node);
        protected abstract bool CanAddImportForQuery(string diagnosticId, SyntaxNode node);
        protected abstract bool CanAddImportForType(string diagnosticId, SyntaxNode node, out TSimpleNameSyntax nameNode);

        protected abstract ISet<INamespaceSymbol> GetImportNamespacesInScope(SemanticModel semanticModel, SyntaxNode node, CancellationToken cancellationToken);
        protected abstract ITypeSymbol GetDeconstructInfo(SemanticModel semanticModel, SyntaxNode node, CancellationToken cancellationToken);
        protected abstract ITypeSymbol GetQueryClauseInfo(SemanticModel semanticModel, SyntaxNode node, CancellationToken cancellationToken);
        protected abstract bool IsViableExtensionMethod(IMethodSymbol method, SyntaxNode expression, SemanticModel semanticModel, ISyntaxFacts syntaxFacts, CancellationToken cancellationToken);

        protected abstract Task<Document> AddImportAsync(SyntaxNode contextNode, INamespaceOrTypeSymbol symbol, Document document, AddImportPlacementOptions options, CancellationToken cancellationToken);
        protected abstract Task<Document> AddImportAsync(SyntaxNode contextNode, IReadOnlyList<string> nameSpaceParts, Document document, AddImportPlacementOptions options, CancellationToken cancellationToken);

        protected abstract bool IsAddMethodContext(SyntaxNode node, SemanticModel semanticModel);

        protected abstract string GetDescription(IReadOnlyList<string> nameParts);
        protected abstract (string description, bool hasExistingImport) GetDescription(Document document, AddImportPlacementOptions options, INamespaceOrTypeSymbol symbol, SemanticModel semanticModel, SyntaxNode root, CancellationToken cancellationToken);

        public async Task<ImmutableArray<AddImportFixData>> GetFixesAsync(
            Document document, TextSpan span, string diagnosticId, int maxResults,
            ISymbolSearchService symbolSearchService, AddImportOptions options,
            ImmutableArray<PackageSource> packageSources, CancellationToken cancellationToken)
        {
            var client = await RemoteHostClient.TryGetClientAsync(document.Project, cancellationToken).ConfigureAwait(false);
            if (client != null)
            {
                var result = await client.TryInvokeAsync<IRemoteMissingImportDiscoveryService, ImmutableArray<AddImportFixData>>(
                    document.Project.Solution,
                    (service, solutionInfo, callbackId, cancellationToken) =>
                        service.GetFixesAsync(solutionInfo, callbackId, document.Id, span, diagnosticId, maxResults, options, packageSources, cancellationToken),
                    callbackTarget: symbolSearchService,
                    cancellationToken).ConfigureAwait(false);

                return result.HasValue ? result.Value : ImmutableArray<AddImportFixData>.Empty;
            }

            return await GetFixesInCurrentProcessAsync(
                document, span, diagnosticId, maxResults,
                symbolSearchService, options,
                packageSources, cancellationToken).ConfigureAwait(false);
        }

        private async Task<ImmutableArray<AddImportFixData>> GetFixesInCurrentProcessAsync(
            Document document, TextSpan span, string diagnosticId, int maxResults,
            ISymbolSearchService symbolSearchService, AddImportOptions options,
            ImmutableArray<PackageSource> packageSources, CancellationToken cancellationToken)
        {
            var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var node = root.FindToken(span.Start, findInsideTrivia: true)
                           .GetAncestor(n => n.Span.Contains(span) && n != root);

            using var _ = ArrayBuilder<AddImportFixData>.GetInstance(out var result);
            if (node != null)
            {

                using (Logger.LogBlock(FunctionId.Refactoring_AddImport, cancellationToken))
                {
                    if (!cancellationToken.IsCancellationRequested)
                    {
                        if (CanAddImport(node, options.Placement.AllowInHiddenRegions, cancellationToken))
                        {
                            var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);
                            var allSymbolReferences = await FindResultsAsync(
                                document, semanticModel, diagnosticId, node, maxResults, symbolSearchService,
                                options, packageSources, cancellationToken).ConfigureAwait(false);

                            // Nothing found at all. No need to proceed.
                            foreach (var reference in allSymbolReferences)
                            {
                                cancellationToken.ThrowIfCancellationRequested();

                                var fixData = await reference.TryGetFixDataAsync(document, node, options.Placement, cancellationToken).ConfigureAwait(false);
                                result.AddIfNotNull(fixData);
                            }
                        }
                    }
                }
            }

            return result.ToImmutable();
        }

        private async Task<ImmutableArray<Reference>> FindResultsAsync(
            Document document, SemanticModel semanticModel, string diagnosticId, SyntaxNode node, int maxResults, ISymbolSearchService symbolSearchService,
            AddImportOptions options, ImmutableArray<PackageSource> packageSources, CancellationToken cancellationToken)
        {
            // Caches so we don't produce the same data multiple times while searching 
            // all over the solution.
            var project = document.Project;
            var projectToAssembly = new ConcurrentDictionary<Project, AsyncLazy<IAssemblySymbol>>(concurrencyLevel: 2, capacity: project.Solution.ProjectIds.Count);
            var referenceToCompilation = new ConcurrentDictionary<PortableExecutableReference, Compilation>(concurrencyLevel: 2, capacity: project.Solution.Projects.Sum(p => p.MetadataReferences.Count));

            var finder = new SymbolReferenceFinder(
                this, document, semanticModel, diagnosticId, node, symbolSearchService,
                options, packageSources, cancellationToken);

            // Look for exact matches first:
            var exactReferences = await FindResultsAsync(projectToAssembly, referenceToCompilation, project, maxResults, finder, exact: true, cancellationToken: cancellationToken).ConfigureAwait(false);
            if (exactReferences.Length > 0)
            {
                return exactReferences;
            }

            // No exact matches found.  Fall back to fuzzy searching.
            // Only bother doing this for host workspaces.  We don't want this for 
            // things like the Interactive workspace as this will cause us to 
            // create expensive bk-trees which we won't even be able to save for 
            // future use.
            if (!IsHostOrRemoteWorkspace(project))
            {
                return ImmutableArray<Reference>.Empty;
            }

            var fuzzyReferences = await FindResultsAsync(projectToAssembly, referenceToCompilation, project, maxResults, finder, exact: false, cancellationToken: cancellationToken).ConfigureAwait(false);
            return fuzzyReferences;
        }

        private static bool IsHostOrRemoteWorkspace(Project project)
        {
            return project.Solution.Workspace.Kind is WorkspaceKind.Host or
                   WorkspaceKind.RemoteWorkspace or
                   WorkspaceKind.RemoteTemporaryWorkspace;
        }

        private async Task<ImmutableArray<Reference>> FindResultsAsync(
            ConcurrentDictionary<Project, AsyncLazy<IAssemblySymbol>> projectToAssembly,
            ConcurrentDictionary<PortableExecutableReference, Compilation> referenceToCompilation,
            Project project, int maxResults, SymbolReferenceFinder finder, bool exact, CancellationToken cancellationToken)
        {
            using var _ = ArrayBuilder<Reference>.GetInstance(out var allReferences);

            // First search the current project to see if any symbols (source or metadata) match the 
            // search string.
            await FindResultsInAllSymbolsInStartingProjectAsync(
                allReferences, maxResults, finder, exact, cancellationToken).ConfigureAwait(false);

            // Only bother doing this for host workspaces.  We don't want this for 
            // things like the Interactive workspace as we can't even add project
            // references to the interactive window.  We could consider adding metadata
            // references with #r in the future.
            if (IsHostOrRemoteWorkspace(project))
            {
                // Now search unreferenced projects, and see if they have any source symbols that match
                // the search string.
                await FindResultsInUnreferencedProjectSourceSymbolsAsync(projectToAssembly, project, allReferences, maxResults, finder, exact, cancellationToken).ConfigureAwait(false);

                // Finally, check and see if we have any metadata symbols that match the search string.
                await FindResultsInUnreferencedMetadataSymbolsAsync(referenceToCompilation, project, allReferences, maxResults, finder, exact, cancellationToken).ConfigureAwait(false);

                // We only support searching NuGet in an exact manner currently. 
                if (exact)
                {
                    await finder.FindNugetOrReferenceAssemblyReferencesAsync(allReferences, cancellationToken).ConfigureAwait(false);
                }
            }

            return allReferences.ToImmutable();
        }

        private static async Task FindResultsInAllSymbolsInStartingProjectAsync(
            ArrayBuilder<Reference> allSymbolReferences, int maxResults, SymbolReferenceFinder finder,
            bool exact, CancellationToken cancellationToken)
        {
            var references = await finder.FindInAllSymbolsInStartingProjectAsync(exact, cancellationToken).ConfigureAwait(false);
            AddRange(allSymbolReferences, references, maxResults);
        }

        private static async Task FindResultsInUnreferencedProjectSourceSymbolsAsync(
            ConcurrentDictionary<Project, AsyncLazy<IAssemblySymbol>> projectToAssembly,
            Project project, ArrayBuilder<Reference> allSymbolReferences, int maxResults,
            SymbolReferenceFinder finder, bool exact, CancellationToken cancellationToken)
        {
            // If we didn't find enough hits searching just in the project, then check 
            // in any unreferenced projects.
            if (allSymbolReferences.Count >= maxResults)
            {
                return;
            }

            var viableUnreferencedProjects = GetViableUnreferencedProjects(project);

            // Search all unreferenced projects in parallel.
            var findTasks = new HashSet<Task<ImmutableArray<SymbolReference>>>();

            // Create another cancellation token so we can both search all projects in parallel,
            // but also stop any searches once we get enough results.
            using var nestedTokenSource = new CancellationTokenSource();
            using var linkedTokenSource = CancellationTokenSource.CreateLinkedTokenSource(nestedTokenSource.Token, cancellationToken);

            foreach (var unreferencedProject in viableUnreferencedProjects)
            {
                // Search in this unreferenced project.  But don't search in any of its'
                // direct references.  i.e. we don't want to search in its metadata references
                // or in the projects it references itself. We'll be searching those entities
                // individually.
                findTasks.Add(finder.FindInSourceSymbolsInProjectAsync(
                    projectToAssembly, unreferencedProject, exact, linkedTokenSource.Token));
            }

            await WaitForTasksAsync(allSymbolReferences, maxResults, findTasks, nestedTokenSource, cancellationToken).ConfigureAwait(false);
        }

        private async Task FindResultsInUnreferencedMetadataSymbolsAsync(
            ConcurrentDictionary<PortableExecutableReference, Compilation> referenceToCompilation,
            Project project, ArrayBuilder<Reference> allSymbolReferences, int maxResults, SymbolReferenceFinder finder,
            bool exact, CancellationToken cancellationToken)
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
            using var nestedTokenSource = new CancellationTokenSource();
            using var linkedTokenSource = CancellationTokenSource.CreateLinkedTokenSource(nestedTokenSource.Token, cancellationToken);

            foreach (var (referenceProjectId, reference) in newReferences)
            {
                var compilation = referenceToCompilation.GetOrAdd(
                    reference, r => CreateCompilation(project, r));

                // Ignore netmodules.  First, they're incredibly esoteric and barely used.
                // Second, the SymbolFinder API doesn't even support searching them. 
                if (compilation.GetAssemblyOrModuleSymbol(reference) is IAssemblySymbol assembly)
                {
                    findTasks.Add(finder.FindInMetadataSymbolsAsync(
                        assembly, referenceProjectId, reference, exact, linkedTokenSource.Token));
                }
            }

            await WaitForTasksAsync(allSymbolReferences, maxResults, findTasks, nestedTokenSource, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Returns the set of PEReferences in the solution that are not currently being referenced
        /// by this project.  The set returned will be tuples containing the PEReference, and the project-id
        /// for the project we found the pe-reference in.
        /// </summary>
        private static ImmutableArray<(ProjectId, PortableExecutableReference)> GetUnreferencedMetadataReferences(
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

        private static async Task WaitForTasksAsync(
            ArrayBuilder<Reference> allSymbolReferences,
            int maxResults,
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
                    AddRange(allSymbolReferences, await doneTask.ConfigureAwait(false), maxResults);

                    // Once we get enough, just stop.
                    if (allSymbolReferences.Count >= maxResults)
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
        /// We ignore references that are in a directory that contains the names
        /// "Packages", "packs", "NuGetFallbackFolder", or "NuGetPackages"
        /// These directories are most likely the ones produced by NuGet, and we don't want
        /// to offer to add .dll reference manually for dlls that are part of NuGet packages.
        /// 
        /// Note that this is only a heuristic (though a good one), and we should remove this
        /// when we can get an API from NuGet that tells us if a reference is actually provided
        /// by a nuget packages.
        /// Tracking issue: https://github.com/dotnet/project-system/issues/5275
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
        private static bool IsInPackagesDirectory(PortableExecutableReference reference)
        {
            return ContainsPathComponent(reference, "packages")
                || ContainsPathComponent(reference, "packs")
                || ContainsPathComponent(reference, "NuGetFallbackFolder")
                || ContainsPathComponent(reference, "NuGetPackages");

            static bool ContainsPathComponent(PortableExecutableReference reference, string pathComponent)
            {
                return PathUtilities.ContainsPathComponent(reference.FilePath, pathComponent, ignoreCase: true);
            }
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
        private static Compilation CreateCompilation(Project project, PortableExecutableReference reference)
        {
            var compilationService = project.LanguageServices.GetRequiredService<ICompilationFactoryService>();
            var compilation = compilationService.CreateCompilation("TempAssembly", compilationService.GetDefaultCompilationOptions());
            return compilation.WithReferences(reference);
        }

        bool IEqualityComparer<PortableExecutableReference>.Equals(PortableExecutableReference? x, PortableExecutableReference? y)
        {
            if (x == y)
                return true;

            var path1 = x?.FilePath ?? x?.Display;
            var path2 = y?.FilePath ?? y?.Display;
            if (path1 == null || path2 == null)
                return false;

            return StringComparer.OrdinalIgnoreCase.Equals(path1, path2);
        }

        int IEqualityComparer<PortableExecutableReference>.GetHashCode(PortableExecutableReference obj)
        {
            var path = obj.FilePath ?? obj.Display;
            return path == null ? 0 : StringComparer.OrdinalIgnoreCase.GetHashCode(path);
        }

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
                solution.GetRequiredProject(id)));

            // We also aren't interested in any projects we're already directly referencing.
            viableProjects.RemoveAll(project.ProjectReferences.Select(r => solution.GetRequiredProject(r.ProjectId)));
            return viableProjects;
        }

        private static void AddRange<TReference>(ArrayBuilder<Reference> allSymbolReferences, ImmutableArray<TReference> proposedReferences, int maxResults)
            where TReference : Reference
        {
            allSymbolReferences.AddRange(proposedReferences.Take(maxResults - allSymbolReferences.Count));
        }

        protected static bool IsViableExtensionMethod(IMethodSymbol method, ITypeSymbol receiver)
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
            => reference.SymbolResult.Symbol != null;

        public async Task<ImmutableArray<(Diagnostic Diagnostic, ImmutableArray<AddImportFixData> Fixes)>> GetFixesForDiagnosticsAsync(
            Document document, TextSpan span, ImmutableArray<Diagnostic> diagnostics, int maxResultsPerDiagnostic,
            ISymbolSearchService symbolSearchService, AddImportOptions options,
            ImmutableArray<PackageSource> packageSources, CancellationToken cancellationToken)
        {
            // We might have multiple different diagnostics covering the same span.  Have to
            // process them all as we might produce different fixes for each diagnostic.

            var fixesForDiagnosticBuilder = ArrayBuilder<(Diagnostic, ImmutableArray<AddImportFixData>)>.GetInstance();

            foreach (var diagnostic in diagnostics)
            {
                var fixes = await GetFixesAsync(
                    document, span, diagnostic.Id, maxResultsPerDiagnostic,
                    symbolSearchService, options,
                    packageSources, cancellationToken).ConfigureAwait(false);

                fixesForDiagnosticBuilder.Add((diagnostic, fixes));
            }

            return fixesForDiagnosticBuilder.ToImmutableAndFree();
        }

        public async Task<ImmutableArray<AddImportFixData>> GetUniqueFixesAsync(
            Document document, TextSpan span, ImmutableArray<string> diagnosticIds,
            ISymbolSearchService symbolSearchService, AddImportOptions options,
            ImmutableArray<PackageSource> packageSources, CancellationToken cancellationToken)
        {
            var client = await RemoteHostClient.TryGetClientAsync(document.Project, cancellationToken).ConfigureAwait(false);
            if (client != null)
            {
                var result = await client.TryInvokeAsync<IRemoteMissingImportDiscoveryService, ImmutableArray<AddImportFixData>>(
                    document.Project.Solution,
                    (service, solutionInfo, callbackId, cancellationToken) =>
                        service.GetUniqueFixesAsync(solutionInfo, callbackId, document.Id, span, diagnosticIds, options, packageSources, cancellationToken),
                    callbackTarget: symbolSearchService,
                    cancellationToken).ConfigureAwait(false);

                return result.HasValue ? result.Value : ImmutableArray<AddImportFixData>.Empty;
            }

            return await GetUniqueFixesAsyncInCurrentProcessAsync(
                document, span, diagnosticIds,
                symbolSearchService, options,
                packageSources, cancellationToken).ConfigureAwait(false);
        }

        private async Task<ImmutableArray<AddImportFixData>> GetUniqueFixesAsyncInCurrentProcessAsync(
            Document document,
            TextSpan span,
            ImmutableArray<string> diagnosticIds,
            ISymbolSearchService symbolSearchService,
            AddImportOptions options,
            ImmutableArray<PackageSource> packageSources,
            CancellationToken cancellationToken)
        {
            var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);

            // Get the diagnostics that indicate a missing import.
            var diagnostics = semanticModel.GetDiagnostics(span, cancellationToken)
               .Where(diagnostic => diagnosticIds.Contains(diagnostic.Id))
               .ToImmutableArray();

            var getFixesForDiagnosticsTasks = diagnostics
                .GroupBy(diagnostic => diagnostic.Location.SourceSpan)
                .Select(diagnosticsForSourceSpan => GetFixesForDiagnosticsAsync(
                        document, diagnosticsForSourceSpan.Key, diagnosticsForSourceSpan.AsImmutable(),
                        maxResultsPerDiagnostic: 2, symbolSearchService, options, packageSources, cancellationToken));

            using var _ = ArrayBuilder<AddImportFixData>.GetInstance(out var fixes);
            foreach (var getFixesForDiagnosticsTask in getFixesForDiagnosticsTasks)
            {
                var fixesForDiagnostics = await getFixesForDiagnosticsTask.ConfigureAwait(false);

                foreach (var fixesForDiagnostic in fixesForDiagnostics)
                {
                    // When there is more than one potential fix for a missing import diagnostic,
                    // which is possible when the same class name is present in multiple namespaces,
                    // we do not want to choose for the user and be wrong. We will not attempt to
                    // fix this diagnostic and instead leave it for the user to resolve since they
                    // will have more context for determining the proper fix.
                    if (fixesForDiagnostic.Fixes.Length == 1)
                        fixes.Add(fixesForDiagnostic.Fixes[0]);
                }
            }

            return fixes.ToImmutable();
        }

        public ImmutableArray<CodeAction> GetCodeActionsForFixes(
            Document document, ImmutableArray<AddImportFixData> fixes,
            IPackageInstallerService? installerService, int maxResults)
        {
            var codeActionsBuilder = ArrayBuilder<CodeAction>.GetInstance();

            foreach (var fix in fixes)
            {
                var codeAction = TryCreateCodeAction(document, fix, installerService);

                codeActionsBuilder.AddIfNotNull(codeAction);

                if (codeActionsBuilder.Count >= maxResults)
                {
                    break;
                }
            }

            return codeActionsBuilder.ToImmutableAndFree();
        }

        private static CodeAction? TryCreateCodeAction(Document document, AddImportFixData fixData, IPackageInstallerService? installerService)
            => fixData.Kind switch
            {
                AddImportFixKind.ProjectSymbol => new ProjectSymbolReferenceCodeAction(document, fixData),
                AddImportFixKind.MetadataSymbol => new MetadataSymbolReferenceCodeAction(document, fixData),
                AddImportFixKind.ReferenceAssemblySymbol => new AssemblyReferenceCodeAction(document, fixData),
                AddImportFixKind.PackageSymbol => installerService?.IsInstalled(document.Project.Solution.Workspace, document.Project.Id, fixData.PackageName) == false
                    ? new ParentInstallPackageCodeAction(document, fixData, installerService)
                    : null,
                _ => throw ExceptionUtilities.Unreachable,
            };

        private static ITypeSymbol? GetAwaitInfo(SemanticModel semanticModel, ISyntaxFacts syntaxFactsService, SyntaxNode node)
        {
            var awaitExpression = FirstAwaitExpressionAncestor(syntaxFactsService, node);
            if (awaitExpression is null)
                return null;

            Debug.Assert(syntaxFactsService.IsAwaitExpression(awaitExpression));
            var innerExpression = syntaxFactsService.GetExpressionOfAwaitExpression(awaitExpression);

            return semanticModel.GetTypeInfo(innerExpression).Type;
        }

        private static ITypeSymbol? GetCollectionExpressionType(SemanticModel semanticModel, ISyntaxFacts syntaxFactsService, SyntaxNode node)
        {
            var collectionExpression = FirstForeachCollectionExpressionAncestor(syntaxFactsService, node);

            if (collectionExpression is null)
            {
                return null;
            }

            return semanticModel.GetTypeInfo(collectionExpression).Type;
        }

        protected static bool AncestorOrSelfIsAwaitExpression(ISyntaxFacts syntaxFactsService, SyntaxNode node)
            => FirstAwaitExpressionAncestor(syntaxFactsService, node) != null;

        private static SyntaxNode? FirstAwaitExpressionAncestor(ISyntaxFacts syntaxFactsService, SyntaxNode node)
            => node.FirstAncestorOrSelf<SyntaxNode, ISyntaxFacts>((n, syntaxFactsService) => syntaxFactsService.IsAwaitExpression(n), syntaxFactsService);

        private static SyntaxNode? FirstForeachCollectionExpressionAncestor(ISyntaxFacts syntaxFactsService, SyntaxNode node)
            => node.FirstAncestorOrSelf<SyntaxNode, ISyntaxFacts>((n, syntaxFactsService) => syntaxFactsService.IsExpressionOfForeach(n), syntaxFactsService);
    }
}
