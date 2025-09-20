// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
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

internal sealed partial class DiagnosticAnalyzerService : IDiagnosticAnalyzerService
{
    private static readonly Option2<bool> s_crashOnAnalyzerException = new("dotnet_crash_on_analyzer_exception", defaultValue: false);

    public DiagnosticAnalyzerInfoCache AnalyzerInfoCache { get; private set; }

    public IAsynchronousOperationListener Listener { get; }
    private IGlobalOptionService GlobalOptions { get; }

    private readonly IDiagnosticsRefresher _diagnosticsRefresher;
    private readonly DiagnosticIncrementalAnalyzer _incrementalAnalyzer;

    public DiagnosticAnalyzerService(
        IGlobalOptionService globalOptions,
        IDiagnosticsRefresher diagnosticsRefresher,
        DiagnosticAnalyzerInfoCache.SharedGlobalCache globalCache,
        IAsynchronousOperationListenerProvider? listenerProvider,
        Workspace workspace)
    {
        AnalyzerInfoCache = globalCache.AnalyzerInfoCache;
        Listener = listenerProvider?.GetListener(FeatureAttribute.DiagnosticService) ?? AsynchronousOperationListenerProvider.NullListener;
        GlobalOptions = globalOptions;
        _diagnosticsRefresher = diagnosticsRefresher;
        _incrementalAnalyzer = new DiagnosticIncrementalAnalyzer(this, AnalyzerInfoCache, this.GlobalOptions);

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

    public Task<ImmutableArray<DiagnosticData>> ForceAnalyzeProjectAsync(Project project, CancellationToken cancellationToken)
        => _incrementalAnalyzer.ForceAnalyzeProjectAsync(project, cancellationToken);

    public Task<ImmutableArray<DiagnosticData>> GetDiagnosticsForIdsAsync(
        Project project, DocumentId? documentId, ImmutableHashSet<string>? diagnosticIds, Func<DiagnosticAnalyzer, bool>? shouldIncludeAnalyzer, bool includeLocalDocumentDiagnostics, bool includeNonLocalDocumentDiagnostics, CancellationToken cancellationToken)
    {
        return _incrementalAnalyzer.GetDiagnosticsForIdsAsync(project, documentId, diagnosticIds, shouldIncludeAnalyzer, includeLocalDocumentDiagnostics, includeNonLocalDocumentDiagnostics, cancellationToken);
    }

    public Task<ImmutableArray<DiagnosticData>> GetProjectDiagnosticsForIdsAsync(
        Project project, ImmutableHashSet<string>? diagnosticIds,
        Func<DiagnosticAnalyzer, bool>? shouldIncludeAnalyzer,
        bool includeNonLocalDocumentDiagnostics, CancellationToken cancellationToken)
    {
        return _incrementalAnalyzer.GetProjectDiagnosticsForIdsAsync(project, diagnosticIds, shouldIncludeAnalyzer, includeNonLocalDocumentDiagnostics, cancellationToken);
    }

    public TestAccessor GetTestAccessor()
        => new(this);

    public readonly struct TestAccessor(DiagnosticAnalyzerService service)
    {
        public Task<ImmutableArray<DiagnosticAnalyzer>> GetAnalyzersAsync(Project project, CancellationToken cancellationToken)
            => service._incrementalAnalyzer.GetAnalyzersForTestingPurposesOnlyAsync(project, cancellationToken);
    }
}
