// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.AddPackage;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.Packaging;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Remote;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.SymbolSearch;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.Threading;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.AddImport;

internal abstract partial class AbstractAddImportFeatureService<TSimpleNameSyntax>
    : IAddImportFeatureService, IEqualityComparer<PortableExecutableReference>
    where TSimpleNameSyntax : SyntaxNode
{
    /// <summary>
    /// Cache of information about whether a <see cref="PortableExecutableReference"/> is likely contained within a
    /// NuGet packages directory.
    /// </summary>
    private static readonly ConditionalWeakTable<PortableExecutableReference, StrongBox<bool>> s_isInPackagesDirectory = new();

    internal static void LogAddImportException(string message, Exception exception)
        => AddImportTrace.LogException(message, exception);

    internal static void LogAddImportMessage(string message)
        => AddImportTrace.LogMessage(message);

    protected abstract bool IsWithinImport(SyntaxNode node);
    protected abstract bool CanAddImport(SyntaxNode node, bool allowInHiddenRegions, CancellationToken cancellationToken);
    protected abstract bool CanAddImportForMember(string diagnosticId, ISyntaxFacts syntaxFacts, SyntaxNode node, out TSimpleNameSyntax nameNode);
    protected abstract bool CanAddImportForNamespace(string diagnosticId, SyntaxNode node, out TSimpleNameSyntax nameNode);
    protected abstract bool CanAddImportForDeconstruct(string diagnosticId, SyntaxNode node);
    protected abstract bool CanAddImportForGetAwaiter(string diagnosticId, ISyntaxFacts syntaxFactsService, SyntaxNode node);
    protected abstract bool CanAddImportForGetEnumerator(string diagnosticId, ISyntaxFacts syntaxFactsService, SyntaxNode node);
    protected abstract bool CanAddImportForGetAsyncEnumerator(string diagnosticId, ISyntaxFacts syntaxFactsService, SyntaxNode node);
    protected abstract bool CanAddImportForQuery(string diagnosticId, SyntaxNode node);
    protected abstract bool CanAddImportForTypeOrNamespace(string diagnosticId, SyntaxNode node, out TSimpleNameSyntax nameNode);

    protected abstract ISet<INamespaceSymbol> GetImportNamespacesInScope(SemanticModel semanticModel, SyntaxNode node, CancellationToken cancellationToken);
    protected abstract ITypeSymbol GetDeconstructInfo(SemanticModel semanticModel, SyntaxNode node, CancellationToken cancellationToken);
    protected abstract ITypeSymbol GetQueryClauseInfo(SemanticModel semanticModel, SyntaxNode node, CancellationToken cancellationToken);

    protected abstract Task<Document> AddImportAsync(SyntaxNode contextNode, INamespaceOrTypeSymbol symbol, Document document, AddImportPlacementOptions options, CancellationToken cancellationToken);
    protected abstract Task<Document> AddImportAsync(SyntaxNode contextNode, IReadOnlyList<string> nameSpaceParts, Document document, AddImportPlacementOptions options, CancellationToken cancellationToken);

    protected abstract bool IsAddMethodContext(
        SyntaxNode node, SemanticModel semanticModel, [NotNullWhen(true)] out SyntaxNode? objectCreationExpression);

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
            AddImportTrace.LogMessage(AddImportTrace.CreateRemoteCallMessage(
                phase: "LocalRequest",
                documentName: document.FilePath ?? document.Name,
                projectName: document.Project.Name,
                language: document.Project.Language,
                span: span,
                diagnosticId: diagnosticId,
                maxResults: maxResults,
                options: options,
                packageSourceCount: packageSources.Length,
                remoteClientAvailable: true));

            var result = await client.TryInvokeAsync<IRemoteMissingImportDiscoveryService, ImmutableArray<AddImportFixData>>(
                document.Project.Solution,
                (service, solutionInfo, callbackId, cancellationToken) =>
                    service.GetFixesAsync(solutionInfo, callbackId, document.Id, span, diagnosticId, maxResults, options, packageSources, cancellationToken),
                callbackTarget: symbolSearchService,
                cancellationToken).ConfigureAwait(false);

            AddImportTrace.LogMessage(AddImportTrace.CreateRemoteCallMessage(
                phase: "LocalResponse",
                documentName: document.FilePath ?? document.Name,
                projectName: document.Project.Name,
                language: document.Project.Language,
                span: span,
                diagnosticId: diagnosticId,
                maxResults: maxResults,
                options: options,
                packageSourceCount: packageSources.Length,
                resultCount: result.HasValue ? result.Value.Length : null,
                remoteClientAvailable: true,
                extra: result.HasValue ? $"Fixes=[{AddImportTrace.CreateFixSummary(result.Value)}]" : null));

            if (!result.HasValue)
            {
                AddImportTrace.LogMessage($"AddImport LocalResponseFailure: Document='{document.FilePath ?? document.Name}' returned no remote result.");
            }

            return result.HasValue ? result.Value : [];
        }

        AddImportTrace.LogMessage(AddImportTrace.CreateRemoteCallMessage(
            phase: "LocalInProcRequest",
            documentName: document.FilePath ?? document.Name,
            projectName: document.Project.Name,
            language: document.Project.Language,
            span: span,
            diagnosticId: diagnosticId,
            maxResults: maxResults,
            options: options,
            packageSourceCount: packageSources.Length,
            remoteClientAvailable: false));

        var localResult = await GetFixesInCurrentProcessAsync(
            document, span, diagnosticId, maxResults,
            symbolSearchService, options,
            packageSources, cancellationToken).ConfigureAwait(false);

        AddImportTrace.LogMessage(AddImportTrace.CreateRemoteCallMessage(
            phase: "LocalInProcResponse",
            documentName: document.FilePath ?? document.Name,
            projectName: document.Project.Name,
            language: document.Project.Language,
            span: span,
            diagnosticId: diagnosticId,
            maxResults: maxResults,
            options: options,
            packageSourceCount: packageSources.Length,
            resultCount: localResult.Length,
            remoteClientAvailable: false,
            extra: $"Fixes=[{AddImportTrace.CreateFixSummary(localResult)}]"));

        return localResult;
    }

    private async Task<ImmutableArray<AddImportFixData>> GetFixesInCurrentProcessAsync(
        Document document, TextSpan span, string diagnosticId, int maxResults,
        ISymbolSearchService symbolSearchService, AddImportOptions options,
        ImmutableArray<PackageSource> packageSources, CancellationToken cancellationToken)
    {
        var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        var node = root.FindToken(span.Start, findInsideTrivia: true)
                       .GetAncestor(n => n.Span.Contains(span) && n != root);

        AddImportTrace.LogMessage($"AddImport CurrentProcessNode: Document='{document.FilePath ?? document.Name}', Project='{document.Project.Name}', Language='{document.Project.Language}', DiagnosticId='{diagnosticId}', Span='{span.Start}..{span.End}', RootKind='{root.RawKind}', Node={FormatNode(node)}");

        using var _ = ArrayBuilder<AddImportFixData>.GetInstance(out var result);
        if (node != null)
        {
            using (Logger.LogBlock(FunctionId.Refactoring_AddImport, cancellationToken))
            {
                if (!cancellationToken.IsCancellationRequested)
                {
                    var canAddImport = CanAddImport(node, options.CleanupOptions.AddImportOptions.AllowInHiddenRegions, cancellationToken);
                    AddImportTrace.LogMessage($"AddImport CurrentProcessCanAddImport: Document='{document.FilePath ?? document.Name}', DiagnosticId='{diagnosticId}', Node={FormatNode(node)}, AllowInHiddenRegions={options.CleanupOptions.AddImportOptions.AllowInHiddenRegions}, CanAddImport={canAddImport}");

                    if (canAddImport)
                    {
                        var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);
                        ImmutableArray<Reference> allSymbolReferences;
                        try
                        {
                            allSymbolReferences = await FindResultsAsync(
                                document, semanticModel, diagnosticId, node, maxResults, symbolSearchService,
                                options, packageSources, cancellationToken).ConfigureAwait(false);
                        }
                        catch (Exception ex) when (ex is not OperationCanceledException)
                        {
                            LogAddImportException(
                                $"CurrentProcessSearchFailure: Document='{document.FilePath ?? document.Name}', Project='{document.Project.Name}', Language='{document.Project.Language}', DiagnosticId='{diagnosticId}', Span='{span.Start}..{span.End}', Node={FormatNode(node)}",
                                ex);
                            throw;
                        }

                        AddImportTrace.LogMessage($"AddImport CurrentProcessReferences: Document='{document.FilePath ?? document.Name}', DiagnosticId='{diagnosticId}', ReferenceCount={allSymbolReferences.Length}, References=[{FormatReferences(allSymbolReferences)}]");

                        foreach (var reference in allSymbolReferences)
                        {
                            cancellationToken.ThrowIfCancellationRequested();

                            var fixData = await reference.TryGetFixDataAsync(
                                document, node, options.CleanupDocument, options.CleanupOptions, cancellationToken).ConfigureAwait(false);
                            AddImportTrace.LogMessage($"AddImport CurrentProcessFixData: Document='{document.FilePath ?? document.Name}', DiagnosticId='{diagnosticId}', Reference={FormatReference(reference)}, Fix={FormatFixData(fixData)}");
                            result.AddIfNotNull(fixData);

                            if (result.Count > maxResults)
                            {
                                AddImportTrace.LogMessage($"AddImport CurrentProcessMaxResultsReached: Document='{document.FilePath ?? document.Name}', DiagnosticId='{diagnosticId}', ResultCount={result.Count}, MaxResults={maxResults}");
                                break;
                            }
                        }

                        GC.KeepAlive(semanticModel);
                    }
                    else
                    {
                        AddImportTrace.LogMessage($"AddImport CurrentProcessSkipped: Document='{document.FilePath ?? document.Name}', DiagnosticId='{diagnosticId}', Reason='CanAddImport returned false'");
                    }
                }
                else
                {
                    AddImportTrace.LogMessage($"AddImport CurrentProcessSkipped: Document='{document.FilePath ?? document.Name}', DiagnosticId='{diagnosticId}', Reason='Cancellation requested before processing'");
                }
            }
        }
        else
        {
            AddImportTrace.LogMessage($"AddImport CurrentProcessSkipped: Document='{document.FilePath ?? document.Name}', DiagnosticId='{diagnosticId}', Reason='No syntax node found for span'");
        }

        AddImportTrace.LogMessage($"AddImport CurrentProcessResult: Document='{document.FilePath ?? document.Name}', DiagnosticId='{diagnosticId}', ResultCount={result.Count}, Fixes=[{FormatFixDataList(result)}]");
        return result.ToImmutableAndClear();
    }

    private static string FormatNode(SyntaxNode? node)
    {
        if (node is null)
        {
            return "<null>";
        }

        return $"Type='{node.GetType().FullName}', RawKind={node.RawKind}, Span='{node.SpanStart}..{node.Span.End}', Text='{TrimForLog(node.ToString())}'";
    }

    private static string FormatReferences(ImmutableArray<Reference> references)
        => references.IsEmpty
            ? "<empty>"
            : string.Join("; ", references.Select(static (reference, index) => $"{index}: {FormatReference(reference)}"));

    private static string FormatReference(Reference reference)
    {
        var nameParts = reference.SearchResult.NameParts is null
            ? "<null>"
            : string.Join(".", reference.SearchResult.NameParts);

        return $"Type='{reference.GetType().FullName}', NameParts='{nameParts}', DesiredName='{reference.SearchResult.DesiredName ?? "<null>"}', SourceName='{reference.SearchResult.NameNode?.GetFirstToken().ValueText ?? "<null>"}', Weight={reference.SearchResult.Weight}";
    }

    private static string FormatFixData(AddImportFixData? fixData)
        => fixData is null
            ? "<null>"
            : AddImportTrace.CreateFixSummary(ImmutableArray.Create(fixData));

    private static string FormatFixDataList(ArrayBuilder<AddImportFixData> fixes)
        => fixes.Count == 0
            ? "<empty>"
            : string.Join("; ", fixes.Select(static (fix, index) => $"{index}: {FormatFixData(fix)}"));

    private static string FormatDiagnostics(ImmutableArray<Diagnostic> diagnostics)
        => diagnostics.IsEmpty
            ? "<empty>"
            : string.Join("; ", diagnostics.Select(static (diagnostic, index) => $"{index}: Id='{diagnostic.Id}', Span='{diagnostic.Location.SourceSpan.Start}..{diagnostic.Location.SourceSpan.End}', Severity='{diagnostic.Severity}', Message='{TrimForLog(diagnostic.GetMessage())}'"));

    private static string FormatDiagnosticIds(ImmutableArray<string> diagnosticIds)
        => diagnosticIds.IsEmpty
            ? "<empty>"
            : string.Join(",", diagnosticIds);

    private static string TrimForLog(string value)
    {
        const int maxLength = 160;
        value = value.Replace('\r', ' ').Replace('\n', ' ');
        return value.Length <= maxLength
            ? value
            : value[..maxLength] + "...";
    }

    private async Task<ImmutableArray<Reference>> FindResultsAsync(
        Document document,
        SemanticModel semanticModel,
        string diagnosticId,
        SyntaxNode node,
        int maxResults,
        ISymbolSearchService symbolSearchService,
        AddImportOptions options,
        ImmutableArray<PackageSource> packageSources,
        CancellationToken cancellationToken)
    {
        // Caches so we don't produce the same data multiple times while searching 
        // all over the solution.
        var project = document.Project;
        var projectToAssembly = new ConcurrentDictionary<Project, AsyncLazy<IAssemblySymbol?>>(concurrencyLevel: 2, capacity: project.Solution.ProjectIds.Count);
        var referenceToCompilation = new ConcurrentDictionary<PortableExecutableReference, Compilation>(concurrencyLevel: 2, capacity: project.Solution.Projects.Sum(p => p.MetadataReferences.Count));

        var finder = new SymbolReferenceFinder(
            this, document, semanticModel, diagnosticId, node, symbolSearchService, options, packageSources, cancellationToken);

        // Look for exact matches first:
        var exactReferences = await FindResultsAsync(projectToAssembly, referenceToCompilation, project, maxResults, finder, exact: true, cancellationToken).ConfigureAwait(false);
        AddImportTrace.LogMessage($"AddImport SearchExactComplete: Document='{document.FilePath ?? document.Name}', Project='{project.Name}', DiagnosticId='{diagnosticId}', ReferenceCount={exactReferences.Length}, References=[{FormatReferences(exactReferences)}]");
        if (exactReferences.Length > 0)
            return exactReferences;

        // No exact matches found.  Fall back to fuzzy searching. Only bother doing this for host workspaces.  We don't
        // want this for things like the Interactive workspace as this will cause us to create expensive bk-trees which
        // we won't even be able to save for future use.
        if (!IsHostOrRemoteWorkspace(project))
        {
            AddImportTrace.LogMessage($"AddImport SearchFuzzySkipped: Document='{document.FilePath ?? document.Name}', Project='{project.Name}', DiagnosticId='{diagnosticId}', WorkspaceKind='{project.Solution.WorkspaceKind}', Reason='Not host or remote workspace'");
            return [];
        }

        var fuzzyReferences = await FindResultsAsync(projectToAssembly, referenceToCompilation, project, maxResults, finder, exact: false, cancellationToken).ConfigureAwait(false);
        AddImportTrace.LogMessage($"AddImport SearchFuzzyComplete: Document='{document.FilePath ?? document.Name}', Project='{project.Name}', DiagnosticId='{diagnosticId}', ReferenceCount={fuzzyReferences.Length}, References=[{FormatReferences(fuzzyReferences)}]");
        return fuzzyReferences;
    }

    private static bool IsHostOrRemoteWorkspace(Project project)
        => project.Solution.WorkspaceKind is WorkspaceKind.Host or WorkspaceKind.RemoteWorkspace;

    private async Task<ImmutableArray<Reference>> FindResultsAsync(
        ConcurrentDictionary<Project, AsyncLazy<IAssemblySymbol?>> projectToAssembly,
        ConcurrentDictionary<PortableExecutableReference, Compilation> referenceToCompilation,
        Project project,
        int maxResults,
        SymbolReferenceFinder finder,
        bool exact,
        CancellationToken cancellationToken)
    {
        var allReferences = new ConcurrentQueue<Reference>();

        // First search the current project to see if any symbols (source or metadata) match the search string.
        var searchOptions = finder.Options.SearchOptions;
        AddImportTrace.LogMessage($"AddImport SearchStart: Project='{project.Name}', WorkspaceKind='{project.Solution.WorkspaceKind}', Exact={exact}, MaxResults={maxResults}, SearchReferencedProjectSymbols={searchOptions.SearchReferencedProjectSymbols}, SearchUnreferencedProjectSourceSymbols={searchOptions.SearchUnreferencedProjectSourceSymbols}, SearchUnreferencedMetadataSymbols={searchOptions.SearchUnreferencedMetadataSymbols}, SearchNuGetPackages={searchOptions.SearchNuGetPackages}, SearchReferenceAssemblies={searchOptions.SearchReferenceAssemblies}");
        if (searchOptions.SearchReferencedProjectSymbols)
        {
            await LogSearchStageAsync(
                stage: "StartingProjectSymbols",
                project,
                allReferences,
                exact,
                () => FindResultsInAllSymbolsInStartingProjectAsync(allReferences, finder, exact, cancellationToken)).ConfigureAwait(false);
            AddImportTrace.LogMessage($"AddImport SearchStageComplete: Project='{project.Name}', Stage='StartingProjectSymbols', Exact={exact}, TotalReferenceCount={allReferences.Count}");
        }
        else
        {
            AddImportTrace.LogMessage($"AddImport SearchStageSkipped: Project='{project.Name}', Stage='StartingProjectSymbols', Exact={exact}, Reason='Search option disabled'");
        }

        // Only bother doing this for host workspaces.  We don't want this for things like the Interactive workspace as
        // we can't even add project references to the interactive window.  We could consider adding metadata references
        // with #r in the future.
        if (IsHostOrRemoteWorkspace(project))
        {
            // Now search unreferenced projects, and see if they have any source symbols that match the search string.
            if (searchOptions.SearchUnreferencedProjectSourceSymbols)
            {
                await LogSearchStageAsync(
                    stage: "UnreferencedProjectSourceSymbols",
                    project,
                    allReferences,
                    exact,
                    () => FindResultsInUnreferencedProjectSourceSymbolsAsync(projectToAssembly, project, allReferences, maxResults, finder, exact, cancellationToken)).ConfigureAwait(false);
                AddImportTrace.LogMessage($"AddImport SearchStageComplete: Project='{project.Name}', Stage='UnreferencedProjectSourceSymbols', Exact={exact}, TotalReferenceCount={allReferences.Count}");
            }
            else
            {
                AddImportTrace.LogMessage($"AddImport SearchStageSkipped: Project='{project.Name}', Stage='UnreferencedProjectSourceSymbols', Exact={exact}, Reason='Search option disabled'");
            }

            // Next, check and see if we have any metadata symbols that match the search string.
            if (searchOptions.SearchUnreferencedMetadataSymbols)
            {
                await LogSearchStageAsync(
                    stage: "UnreferencedMetadataSymbols",
                    project,
                    allReferences,
                    exact,
                    () => FindResultsInUnreferencedMetadataSymbolsAsync(referenceToCompilation, project, allReferences, maxResults, finder, exact, cancellationToken)).ConfigureAwait(false);
                AddImportTrace.LogMessage($"AddImport SearchStageComplete: Project='{project.Name}', Stage='UnreferencedMetadataSymbols', Exact={exact}, TotalReferenceCount={allReferences.Count}");
            }
            else
            {
                AddImportTrace.LogMessage($"AddImport SearchStageSkipped: Project='{project.Name}', Stage='UnreferencedMetadataSymbols', Exact={exact}, Reason='Search option disabled'");
            }

            // Finally, search for nuget or reference assembly symbols that match the search string.
            if (searchOptions.SearchNuGetPackages || searchOptions.SearchReferenceAssemblies)
            {
                await LogSearchStageAsync(
                    stage: "NuGetOrReferenceAssemblies",
                    project,
                    allReferences,
                    exact,
                    () => finder.FindNugetOrReferenceAssemblyReferencesAsync(allReferences, exact, cancellationToken)).ConfigureAwait(false);
                AddImportTrace.LogMessage($"AddImport SearchStageComplete: Project='{project.Name}', Stage='NuGetOrReferenceAssemblies', Exact={exact}, TotalReferenceCount={allReferences.Count}");
            }
            else
            {
                AddImportTrace.LogMessage($"AddImport SearchStageSkipped: Project='{project.Name}', Stage='NuGetOrReferenceAssemblies', Exact={exact}, Reason='Search options disabled'");
            }
        }
        else
        {
            AddImportTrace.LogMessage($"AddImport SearchStageSkipped: Project='{project.Name}', Stage='HostOnlySearches', Exact={exact}, WorkspaceKind='{project.Solution.WorkspaceKind}', Reason='Not host or remote workspace'");
        }

        var result = allReferences.ToImmutableArray();
        AddImportTrace.LogMessage($"AddImport SearchComplete: Project='{project.Name}', Exact={exact}, ReferenceCount={result.Length}, References=[{FormatReferences(result)}]");
        return result;
    }

    private static async Task LogSearchStageAsync(
        string stage,
        Project project,
        ConcurrentQueue<Reference> allReferences,
        bool exact,
        Func<Task> action)
    {
        LogAddImportMessage($"SearchStageStart: Project='{project.Name}', WorkspaceKind='{project.Solution.WorkspaceKind}', Stage='{stage}', Exact={exact}, ReferenceCountBefore={allReferences.Count}");
        try
        {
            await action().ConfigureAwait(false);
            LogAddImportMessage($"SearchStageComplete: Project='{project.Name}', WorkspaceKind='{project.Solution.WorkspaceKind}', Stage='{stage}', Exact={exact}, ReferenceCountAfter={allReferences.Count}");
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            LogAddImportException(
                $"SearchStageFailure: Project='{project.Name}', WorkspaceKind='{project.Solution.WorkspaceKind}', Stage='{stage}', Exact={exact}, ReferenceCount={allReferences.Count}",
                ex);
            throw;
        }
    }

    private static async Task FindResultsInAllSymbolsInStartingProjectAsync(
        ConcurrentQueue<Reference> allSymbolReferences, SymbolReferenceFinder finder, bool exact, CancellationToken cancellationToken)
    {
        AddRange(
            allSymbolReferences,
            await finder.FindInAllSymbolsInStartingProjectAsync(exact, cancellationToken).ConfigureAwait(false));
    }

    private static async Task FindResultsInUnreferencedProjectSourceSymbolsAsync(
        ConcurrentDictionary<Project, AsyncLazy<IAssemblySymbol?>> projectToAssembly,
        Project project, ConcurrentQueue<Reference> allSymbolReferences, int maxResults,
        SymbolReferenceFinder finder, bool exact, CancellationToken cancellationToken)
    {
        // If we didn't find enough hits searching just in the project, then check 
        // in any unreferenced projects.
        if (allSymbolReferences.Count >= maxResults)
        {
            AddImportTrace.LogMessage($"AddImport SearchUnreferencedProjectsSkipped: Project='{project.Name}', Exact={exact}, ExistingReferenceCount={allSymbolReferences.Count}, MaxResults={maxResults}, Reason='Already at max results'");
            return;
        }

        var viableUnreferencedProjects = GetViableUnreferencedProjects(project);
        AddImportTrace.LogMessage($"AddImport SearchUnreferencedProjectsStart: Project='{project.Name}', Exact={exact}, ViableProjectCount={viableUnreferencedProjects.Count}, ExistingReferenceCount={allSymbolReferences.Count}, MaxResults={maxResults}");

        // Create another cancellation token so we can both search all projects in parallel,
        // but also stop any searches once we get enough results.
        using var linkedTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        try
        {
            // Defer to the ProducerConsumer.  We're search the unreferenced projects in parallel. As we get results, we'll
            // add them to the 'allSymbolReferences' queue.  If we get enough results, we'll cancel all the other work.
            await ProducerConsumer<ImmutableArray<SymbolReference>>.RunParallelAsync(
                source: viableUnreferencedProjects,
                produceItems: static async (project, onItemsFound, args, cancellationToken) =>
                {
                    var (projectToAssembly, allSymbolReferences, maxResults, finder, exact, linkedTokenSource) = args;
                    // Search in this unreferenced project.  But don't search in any of its' direct references.  i.e. we
                    // don't want to search in its metadata references or in the projects it references itself. We'll be
                    // searching those entities individually.
                    var references = await finder.FindInSourceSymbolsInProjectAsync(
                        projectToAssembly, project, exact, cancellationToken).ConfigureAwait(false);
                    onItemsFound(references);
                },
                consumeItems: static (symbolReferencesEnumerable, args, cancellationToken) =>
                    ProcessReferencesAsync(args.allSymbolReferences, args.maxResults, symbolReferencesEnumerable, args.linkedTokenSource),
                args: (projectToAssembly, allSymbolReferences, maxResults, finder, exact, linkedTokenSource),
                linkedTokenSource.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException ex) when (ex.CancellationToken == linkedTokenSource.Token)
        {
            // We'll get cancellation exceptions on our linked token source once we exceed the max results. We don't
            // want that cancellation to bubble up.  Just because we've found enough results doesn't mean we should
            // abort the entire operation.
            AddImportTrace.LogMessage($"AddImport SearchUnreferencedProjectsCanceledAfterEnoughResults: Project='{project.Name}', Exact={exact}, ReferenceCount={allSymbolReferences.Count}, MaxResults={maxResults}");
        }

        AddImportTrace.LogMessage($"AddImport SearchUnreferencedProjectsComplete: Project='{project.Name}', Exact={exact}, ReferenceCount={allSymbolReferences.Count}, MaxResults={maxResults}");
    }

    private async Task FindResultsInUnreferencedMetadataSymbolsAsync(
        ConcurrentDictionary<PortableExecutableReference, Compilation> referenceToCompilation,
        Project project, ConcurrentQueue<Reference> allSymbolReferences, int maxResults, SymbolReferenceFinder finder,
        bool exact, CancellationToken cancellationToken)
    {
        // Only do this if none of the project searches produced any results. We may have a 
        // lot of metadata to search through, and it would be good to avoid that if we can.
        if (!allSymbolReferences.IsEmpty)
        {
            AddImportTrace.LogMessage($"AddImport SearchUnreferencedMetadataSkipped: Project='{project.Name}', Exact={exact}, ExistingReferenceCount={allSymbolReferences.Count}, Reason='Existing project/source references found'");
            return;
        }

        // Keep track of the references we've seen (so that we don't process them multiple times
        // across many sibling projects).  Prepopulate it with our own metadata references since
        // we know we don't need to search in that.
        var seenReferences = new HashSet<PortableExecutableReference>(comparer: this);
        seenReferences.AddAll(project.MetadataReferences.OfType<PortableExecutableReference>());

        var newReferences = GetUnreferencedMetadataReferences(project, seenReferences);
        AddImportTrace.LogMessage($"AddImport SearchUnreferencedMetadataStart: Project='{project.Name}', Exact={exact}, MetadataReferenceCount={newReferences.Length}, MaxResults={maxResults}");

        // Create another cancellation token so we can both search all projects in parallel,
        // but also stop any searches once we get enough results.
        using var linkedTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        try
        {
            // Defer to the ProducerConsumer.  We're search the metadata references in parallel. As we get results, we'll
            // add them to the 'allSymbolReferences' queue.  If we get enough results, we'll cancel all the other work.
            await ProducerConsumer<ImmutableArray<SymbolReference>>.RunParallelAsync(
                source: newReferences,
                produceItems: static async (tuple, onItemsFound, args, cancellationToken) =>
                {
                    var (referenceProject, reference) = tuple;
                    var (referenceToCompilation, project, allSymbolReferences, maxResults, finder, exact, newReferences, linkedTokenSource) = args;

                    var compilation = referenceToCompilation.GetOrAdd(reference, r => CreateCompilation(project, r));

                    // Ignore netmodules.  First, they're incredibly esoteric and barely used.
                    // Second, the SymbolFinder API doesn't even support searching them. 
                    if (compilation.GetAssemblyOrModuleSymbol(reference) is not IAssemblySymbol assembly)
                        return;

                    var references = await finder.FindInMetadataSymbolsAsync(
                        assembly, referenceProject, reference, exact, cancellationToken).ConfigureAwait(false);
                    onItemsFound(references);
                },
                consumeItems: static (symbolReferencesEnumerable, args, cancellationToken) =>
                    ProcessReferencesAsync(args.allSymbolReferences, args.maxResults, symbolReferencesEnumerable, args.linkedTokenSource),
                args: (referenceToCompilation, project, allSymbolReferences, maxResults, finder, exact, newReferences, linkedTokenSource),
                linkedTokenSource.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException ex) when (ex.CancellationToken == linkedTokenSource.Token)
        {
            // We'll get cancellation exceptions on our linked token source once we exceed the max results. We don't
            // want that cancellation to bubble up.  Just because we've found enough results doesn't mean we should
            // abort the entire operation.
            AddImportTrace.LogMessage($"AddImport SearchUnreferencedMetadataCanceledAfterEnoughResults: Project='{project.Name}', Exact={exact}, ReferenceCount={allSymbolReferences.Count}, MaxResults={maxResults}");
        }

        AddImportTrace.LogMessage($"AddImport SearchUnreferencedMetadataComplete: Project='{project.Name}', Exact={exact}, ReferenceCount={allSymbolReferences.Count}, MaxResults={maxResults}");
    }

    /// <summary>
    /// Returns the set of PEReferences in the solution that are not currently being referenced
    /// by this project.  The set returned will be tuples containing the PEReference, and the project-id
    /// for the project we found the pe-reference in.
    /// </summary>
    private static ImmutableArray<(Project, PortableExecutableReference)> GetUnreferencedMetadataReferences(
        Project project, HashSet<PortableExecutableReference> seenReferences)
    {
        using var _ = ArrayBuilder<(Project, PortableExecutableReference)>.GetInstance(out var result);

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
                    result.Add((p, peReference));
                }
            }
        }

        return result.ToImmutableAndClear();
    }

    private static async Task ProcessReferencesAsync(
        ConcurrentQueue<Reference> allSymbolReferences,
        int maxResults,
        IAsyncEnumerable<ImmutableArray<SymbolReference>> reader,
        CancellationTokenSource linkedTokenSource)
    {
        await foreach (var symbolReferences in reader.ConfigureAwait(false))
        {
            linkedTokenSource.Token.ThrowIfCancellationRequested();
            AddRange(allSymbolReferences, symbolReferences);

            // If we've gone over the max amount of items we're looking for, attempt to cancel all existing work that is
            // still searching.
            if (allSymbolReferences.Count >= maxResults)
            {
                try
                {
                    linkedTokenSource.Cancel();
                }
                catch (ObjectDisposedException)
                {
                }
            }
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
        return s_isInPackagesDirectory.GetValue(
            reference,
            static reference => new StrongBox<bool>(ComputeIsInPackagesDirectory(reference))).Value;

        static bool ComputeIsInPackagesDirectory(PortableExecutableReference reference)
        {
            return ContainsPathComponent(reference, "packages")
                || ContainsPathComponent(reference, "packs")
                || ContainsPathComponent(reference, "NuGetFallbackFolder")
                || ContainsPathComponent(reference, "NuGetPackages");
        }

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
        var compilationService = project.Services.GetRequiredService<ICompilationFactoryService>();
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
        var viableProjects = new HashSet<Project>(solution.Projects.Where(p => p.SupportsCompilation));

        // Clearly we can't reference ourselves.
        viableProjects.Remove(project);

        // We can't reference any project that transitively depends on us.  Doing so would
        // cause a circular reference between projects.
        var dependencyGraph = solution.GetProjectDependencyGraph();
        var projectsThatTransitivelyDependOnThisProject = dependencyGraph.GetProjectsThatTransitivelyDependOnThisProject(project.Id);

        viableProjects.RemoveAll(projectsThatTransitivelyDependOnThisProject.Select(solution.GetRequiredProject));

        // We also aren't interested in any projects we're already directly referencing.
        viableProjects.RemoveAll(project.ProjectReferences.Select(r => solution.GetRequiredProject(r.ProjectId)));
        return viableProjects;
    }

    private static void AddRange(ConcurrentQueue<Reference> allSymbolReferences, ImmutableArray<SymbolReference> proposedReferences)
    {
        foreach (var reference in proposedReferences)
            allSymbolReferences.Enqueue(reference);
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

        AddImportTrace.LogMessage($"AddImport DiagnosticsRequest: Document='{document.FilePath ?? document.Name}', Project='{document.Project.Name}', Language='{document.Project.Language}', Span='{span.Start}..{span.End}', DiagnosticCount={diagnostics.Length}, MaxResultsPerDiagnostic={maxResultsPerDiagnostic}, Diagnostics=[{FormatDiagnostics(diagnostics)}]");

        var result = new FixedSizeArrayBuilder<(Diagnostic, ImmutableArray<AddImportFixData>)>(diagnostics.Length);

        foreach (var diagnostic in diagnostics)
        {
            var fixes = await GetFixesAsync(
                document, span, diagnostic.Id, maxResultsPerDiagnostic,
                symbolSearchService, options,
                packageSources, cancellationToken).ConfigureAwait(false);

            AddImportTrace.LogMessage($"AddImport DiagnosticsResponseItem: Document='{document.FilePath ?? document.Name}', DiagnosticId='{diagnostic.Id}', DiagnosticSpan='{diagnostic.Location.SourceSpan.Start}..{diagnostic.Location.SourceSpan.End}', FixCount={fixes.Length}, Fixes=[{AddImportTrace.CreateFixSummary(fixes)}]");
            result.Add((diagnostic, fixes));
        }

        var finalResult = result.MoveToImmutable();
        AddImportTrace.LogMessage($"AddImport DiagnosticsResponse: Document='{document.FilePath ?? document.Name}', ResultCount={finalResult.Length}, FixCounts=[{string.Join("; ", finalResult.Select(static (item, index) => $"{index}: DiagnosticId='{item.Item1.Id}', FixCount={item.Item2.Length}"))}]");
        return finalResult;
    }

    public async Task<ImmutableArray<AddImportFixData>> GetUniqueFixesAsync(
        Document document, TextSpan span, ImmutableArray<string> diagnosticIds,
        ISymbolSearchService symbolSearchService, AddImportOptions options,
        ImmutableArray<PackageSource> packageSources, CancellationToken cancellationToken)
    {
        var client = await RemoteHostClient.TryGetClientAsync(document.Project, cancellationToken).ConfigureAwait(false);
        if (client != null)
        {
            AddImportTrace.LogMessage(AddImportTrace.CreateRemoteCallMessage(
                phase: "UniqueRemoteRequest",
                documentName: document.FilePath ?? document.Name,
                projectName: document.Project.Name,
                language: document.Project.Language,
                span: span,
                diagnosticId: FormatDiagnosticIds(diagnosticIds),
                maxResults: 0,
                options: options,
                packageSourceCount: packageSources.Length,
                remoteClientAvailable: true));

            var result = await client.TryInvokeAsync<IRemoteMissingImportDiscoveryService, ImmutableArray<AddImportFixData>>(
                document.Project.Solution,
                (service, solutionInfo, callbackId, cancellationToken) =>
                    service.GetUniqueFixesAsync(solutionInfo, callbackId, document.Id, span, diagnosticIds, options, packageSources, cancellationToken),
                callbackTarget: symbolSearchService,
                cancellationToken).ConfigureAwait(false);

            AddImportTrace.LogMessage(AddImportTrace.CreateRemoteCallMessage(
                phase: "UniqueRemoteResponse",
                documentName: document.FilePath ?? document.Name,
                projectName: document.Project.Name,
                language: document.Project.Language,
                span: span,
                diagnosticId: FormatDiagnosticIds(diagnosticIds),
                maxResults: 0,
                options: options,
                packageSourceCount: packageSources.Length,
                resultCount: result.HasValue ? result.Value.Length : null,
                remoteClientAvailable: true,
                extra: result.HasValue ? $"Fixes=[{AddImportTrace.CreateFixSummary(result.Value)}]" : null));

            return result.HasValue ? result.Value : [];
        }

        AddImportTrace.LogMessage(AddImportTrace.CreateRemoteCallMessage(
            phase: "UniqueInProcRequest",
            documentName: document.FilePath ?? document.Name,
            projectName: document.Project.Name,
            language: document.Project.Language,
            span: span,
            diagnosticId: FormatDiagnosticIds(diagnosticIds),
            maxResults: 0,
            options: options,
            packageSourceCount: packageSources.Length,
            remoteClientAvailable: false));

        var localResult = await GetUniqueFixesAsyncInCurrentProcessAsync(
            document, span, diagnosticIds,
            symbolSearchService, options,
            packageSources, cancellationToken).ConfigureAwait(false);

        AddImportTrace.LogMessage(AddImportTrace.CreateRemoteCallMessage(
            phase: "UniqueInProcResponse",
            documentName: document.FilePath ?? document.Name,
            projectName: document.Project.Name,
            language: document.Project.Language,
            span: span,
            diagnosticId: FormatDiagnosticIds(diagnosticIds),
            maxResults: 0,
            options: options,
            packageSourceCount: packageSources.Length,
            resultCount: localResult.Length,
            remoteClientAvailable: false,
            extra: $"Fixes=[{AddImportTrace.CreateFixSummary(localResult)}]"));

        return localResult;
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
           .WhereAsArray(diagnostic => diagnosticIds.Contains(diagnostic.Id));

        AddImportTrace.LogMessage($"AddImport UniqueCurrentProcessDiagnostics: Document='{document.FilePath ?? document.Name}', Project='{document.Project.Name}', Span='{span.Start}..{span.End}', RequestedDiagnosticIds='{FormatDiagnosticIds(diagnosticIds)}', MatchingDiagnosticCount={diagnostics.Length}, Diagnostics=[{FormatDiagnostics(diagnostics)}]");

        var groupedDiagnostics = diagnostics
            .GroupBy(diagnostic => diagnostic.Location.SourceSpan)
            .ToArray();

        AddImportTrace.LogMessage($"AddImport UniqueCurrentProcessGroups: Document='{document.FilePath ?? document.Name}', GroupCount={groupedDiagnostics.Length}, Groups=[{string.Join("; ", groupedDiagnostics.Select(static (group, index) => $"{index}: Span='{group.Key.Start}..{group.Key.End}', DiagnosticCount={group.Count()}"))}]");

        var getFixesForDiagnosticsTasks = groupedDiagnostics
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
                AddImportTrace.LogMessage($"AddImport UniqueCurrentProcessCandidate: Document='{document.FilePath ?? document.Name}', DiagnosticId='{fixesForDiagnostic.Diagnostic.Id}', DiagnosticSpan='{fixesForDiagnostic.Diagnostic.Location.SourceSpan.Start}..{fixesForDiagnostic.Diagnostic.Location.SourceSpan.End}', CandidateFixCount={fixesForDiagnostic.Fixes.Length}, CandidateFixes=[{AddImportTrace.CreateFixSummary(fixesForDiagnostic.Fixes)}], WillAdd={fixesForDiagnostic.Fixes.Length == 1}");
                if (fixesForDiagnostic.Fixes.Length == 1)
                    fixes.Add(fixesForDiagnostic.Fixes[0]);
            }
        }

        AddImportTrace.LogMessage($"AddImport UniqueCurrentProcessResult: Document='{document.FilePath ?? document.Name}', ResultCount={fixes.Count}, Fixes=[{FormatFixDataList(fixes)}]");
        return fixes.ToImmutableAndClear();
    }

    public ImmutableArray<CodeAction> GetCodeActionsForFixes(
        Document document, ImmutableArray<AddImportFixData> fixes,
        IPackageInstallerService? installerService, int maxResults)
    {
        using var _ = ArrayBuilder<CodeAction>.GetInstance(out var result);

        foreach (var fix in fixes)
        {
            result.AddIfNotNull(TryCreateCodeAction(document, fix, installerService));
            if (result.Count >= maxResults)
                break;
        }

        return result.ToImmutableAndClear();
    }

    private static CodeAction? TryCreateCodeAction(Document document, AddImportFixData fixData, IPackageInstallerService? installerService)
        => fixData.Kind switch
        {
            AddImportFixKind.ProjectSymbol => new ProjectSymbolReferenceCodeAction(document, fixData),
            AddImportFixKind.MetadataSymbol => new MetadataSymbolReferenceCodeAction(document, fixData),
            AddImportFixKind.ReferenceAssemblySymbol => new AssemblyReferenceCodeAction(document, fixData),
            AddImportFixKind.PackageSymbol => ParentInstallPackageCodeAction.TryCreateCodeAction(
                document, new InstallPackageData(fixData.PackageSource, fixData.PackageName, fixData.PackageVersionOpt, fixData.TextChanges), installerService),
            _ => throw ExceptionUtilities.Unreachable(),
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
