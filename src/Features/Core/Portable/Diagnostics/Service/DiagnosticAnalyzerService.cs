// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.SolutionCrawler;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.Threading;

namespace Microsoft.CodeAnalysis.Diagnostics;

[ExportWorkspaceServiceFactory(typeof(IDiagnosticAnalyzerService)), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class DiagnosticAnalyzerServiceFactory(
    IGlobalOptionService globalOptions,
    IDiagnosticsRefresher diagnosticsRefresher,
    DiagnosticAnalyzerInfoCache.SharedGlobalCache globalCache,
    [Import(AllowDefault = true)] IAsynchronousOperationListenerProvider? listenerProvider) : IWorkspaceServiceFactory
{
    public IWorkspaceService CreateService(HostWorkspaceServices workspaceServices)
    {
        return new DiagnosticAnalyzerService(
            globalOptions,
            diagnosticsRefresher,
            globalCache,
            listenerProvider,
            workspaceServices.Workspace);
    }
}

/// <summary>
/// Only implementation of <see cref="IDiagnosticAnalyzerService"/>.  Note: all methods in this class
/// should attempt to run in OOP as soon as possible.  This is not always easy, especially if the apis
/// involve working with in-memory data structures that are not serializable.  In those cases, we should
/// do all that work in-proc, and then send the results to OOP for further processing.  Examples of this
/// are apis that take in a delegate callback to determine which analyzers to actually execute.
/// </summary>
internal sealed partial class DiagnosticAnalyzerService
{
    private static readonly Option2<bool> s_crashOnAnalyzerException = new("dotnet_crash_on_analyzer_exception", defaultValue: false);

    private static readonly ImmutableArray<string> s_csharpLanguageArray = [LanguageNames.CSharp];
    private static readonly ImmutableArray<string> s_visualBasicLanguageArray = [LanguageNames.VisualBasic];
    private static readonly ImmutableArray<string> s_csharpAndVisualBasicLanguageArray = [.. s_csharpLanguageArray, .. s_visualBasicLanguageArray];

    public IAsynchronousOperationListener Listener { get; }
    private IGlobalOptionService GlobalOptions { get; }

    private readonly IDiagnosticsRefresher _diagnosticsRefresher;
    private readonly DiagnosticIncrementalAnalyzer _incrementalAnalyzer;
    private readonly DiagnosticAnalyzerInfoCache _analyzerInfoCache;
    private readonly StateManager _stateManager;

    public DiagnosticAnalyzerService(
        IGlobalOptionService globalOptions,
        IDiagnosticsRefresher diagnosticsRefresher,
        DiagnosticAnalyzerInfoCache.SharedGlobalCache globalCache,
        IAsynchronousOperationListenerProvider? listenerProvider,
        Workspace workspace)
    {
        _analyzerInfoCache = globalCache.AnalyzerInfoCache;
        Listener = listenerProvider?.GetListener(FeatureAttribute.DiagnosticService) ?? AsynchronousOperationListenerProvider.NullListener;
        GlobalOptions = globalOptions;
        _diagnosticsRefresher = diagnosticsRefresher;
        _incrementalAnalyzer = new DiagnosticIncrementalAnalyzer(this, _analyzerInfoCache, this.GlobalOptions);
        _stateManager = new StateManager(_analyzerInfoCache);

        globalOptions.AddOptionChangedHandler(this, (_, _, e) =>
        {
            if (e.HasOption(IsGlobalOptionAffectingDiagnostics))
            {
                RequestDiagnosticRefresh();
            }
        });

        // When the workspace changes what context a document is in (when a user picks a different tfm to view the
        // document in), kick off a refresh so that diagnostics properly update in the task list and editor.
        workspace.RegisterDocumentActiveContextChangedHandler(args => RequestDiagnosticRefresh());
    }

    public static Task<VersionStamp> GetDiagnosticVersionAsync(Project project, CancellationToken cancellationToken)
        => project.GetDependentVersionAsync(cancellationToken);

    public bool CrashOnAnalyzerException
        => GlobalOptions.GetOption(s_crashOnAnalyzerException);

    public static bool IsGlobalOptionAffectingDiagnostics(IOption2 option)
        => option == NamingStyleOptions.NamingPreferences ||
           option.Definition.Group.Parent == CodeStyleOptionGroups.CodeStyle ||
           option == SolutionCrawlerOptionsStorage.BackgroundAnalysisScopeOption ||
           option == SolutionCrawlerOptionsStorage.CompilerDiagnosticsScopeOption ||
           option == s_crashOnAnalyzerException ||
           // Fading is controlled by reporting diagnostics for the faded region.  So if a fading option changes we
           // want to recompute and rereport up to date diagnostics.
           option == FadingOptions.FadeOutUnusedImports ||
           option == FadingOptions.FadeOutUnusedMembers ||
           option == FadingOptions.FadeOutUnreachableCode;

    public void RequestDiagnosticRefresh()
        => _diagnosticsRefresher.RequestWorkspaceRefresh();

    public async Task<ImmutableArray<DiagnosticData>> GetDiagnosticsForSpanAsync(
        TextDocument document,
        TextSpan? range,
        Func<string, bool>? shouldIncludeDiagnostic,
        ICodeActionRequestPriorityProvider priorityProvider,
        DiagnosticKind diagnosticKinds,
        CancellationToken cancellationToken)
    {
        // always make sure that analyzer is called on background thread.
        await Task.Yield().ConfigureAwait(false);
        priorityProvider ??= new DefaultCodeActionRequestPriorityProvider();

        return await _incrementalAnalyzer.GetDiagnosticsForSpanAsync(
            document, range, shouldIncludeDiagnostic, priorityProvider, diagnosticKinds, cancellationToken).ConfigureAwait(false);
    }

    internal Task<ImmutableArray<DiagnosticAnalyzer>> GetProjectAnalyzersAsync(
        Project project, CancellationToken cancellationToken)
    {
        return _stateManager.GetOrCreateAnalyzersAsync(
            project.Solution.SolutionState, project.State, cancellationToken);
    }

    private async Task<ImmutableArray<DiagnosticAnalyzer>> GetDiagnosticAnalyzersAsync(
        Project project,
        ImmutableHashSet<string>? diagnosticIds,
        Func<DiagnosticAnalyzer, bool>? shouldIncludeAnalyzer,
        CancellationToken cancellationToken)
    {
        var analyzersForProject = await GetProjectAnalyzersAsync(project, cancellationToken).ConfigureAwait(false);
        var analyzers = analyzersForProject.WhereAsArray(a => ShouldIncludeAnalyzer(project, a));

        return analyzers;

        bool ShouldIncludeAnalyzer(Project project, DiagnosticAnalyzer analyzer)
        {
            if (!DocumentAnalysisExecutor.IsAnalyzerEnabledForProject(analyzer, project, this.GlobalOptions))
                return false;

            if (shouldIncludeAnalyzer != null && !shouldIncludeAnalyzer(analyzer))
                return false;

            if (diagnosticIds != null && _analyzerInfoCache.GetDiagnosticDescriptors(analyzer).All(d => !diagnosticIds.Contains(d.Id)))
                return false;

            return true;
        }
    }

    public async Task<ImmutableArray<DiagnosticData>> GetDiagnosticsForIdsAsync(
        Project project, DocumentId? documentId, ImmutableHashSet<string>? diagnosticIds, Func<DiagnosticAnalyzer, bool>? shouldIncludeAnalyzer, bool includeLocalDocumentDiagnostics, bool includeNonLocalDocumentDiagnostics, CancellationToken cancellationToken)
    {
        var analyzers = await GetDiagnosticAnalyzersAsync(
            project, diagnosticIds, shouldIncludeAnalyzer, cancellationToken).ConfigureAwait(false);

        return await ProduceProjectDiagnosticsAsync(
            project, analyzers, diagnosticIds,
            // Ensure we compute and return diagnostics for both the normal docs and the additional docs in this
            // project if no specific document id was requested.
            documentId != null ? [documentId] : [.. project.DocumentIds, .. project.AdditionalDocumentIds],
            includeLocalDocumentDiagnostics,
            includeNonLocalDocumentDiagnostics,
            // return diagnostics specific to one project or document
            includeProjectNonLocalResult: documentId == null,
            cancellationToken).ConfigureAwait(false);
    }

    public async Task<ImmutableArray<DiagnosticData>> GetProjectDiagnosticsForIdsAsync(
        Project project, ImmutableHashSet<string>? diagnosticIds,
        Func<DiagnosticAnalyzer, bool>? shouldIncludeAnalyzer,
        bool includeNonLocalDocumentDiagnostics, CancellationToken cancellationToken)
    {
        var analyzers = await GetDiagnosticAnalyzersAsync(
            project, diagnosticIds, shouldIncludeAnalyzer, cancellationToken).ConfigureAwait(false);

        return await ProduceProjectDiagnosticsAsync(
            project, analyzers, diagnosticIds,
            documentIds: [],
            includeLocalDocumentDiagnostics: false,
            includeNonLocalDocumentDiagnostics: includeNonLocalDocumentDiagnostics,
            includeProjectNonLocalResult: true,
            cancellationToken).ConfigureAwait(false);
    }

    public TestAccessor GetTestAccessor()
        => new(this);

    public readonly struct TestAccessor(DiagnosticAnalyzerService service)
    {
        public Task<ImmutableArray<DiagnosticAnalyzer>> GetAnalyzersAsync(Project project, CancellationToken cancellationToken)
            => service._incrementalAnalyzer.GetAnalyzersForTestingPurposesOnlyAsync(project, cancellationToken);
    }
}
