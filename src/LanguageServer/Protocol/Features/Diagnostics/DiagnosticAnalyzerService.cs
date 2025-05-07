// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.SolutionCrawler;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Threading;

namespace Microsoft.CodeAnalysis.Diagnostics;

[Export(typeof(IDiagnosticAnalyzerService)), Shared]
internal partial class DiagnosticAnalyzerService : IDiagnosticAnalyzerService
{
    private static readonly Option2<bool> s_crashOnAnalyzerException = new("dotnet_crash_on_analyzer_exception", defaultValue: false);

    public DiagnosticAnalyzerInfoCache AnalyzerInfoCache { get; private set; }

    public IAsynchronousOperationListener Listener { get; }
    private IGlobalOptionService GlobalOptions { get; }

    private readonly ConditionalWeakTable<Workspace, DiagnosticIncrementalAnalyzer> _map = new();
    private readonly ConditionalWeakTable<Workspace, DiagnosticIncrementalAnalyzer>.CreateValueCallback _createIncrementalAnalyzer;
    private readonly IDiagnosticsRefresher _diagnosticsRefresher;

    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public DiagnosticAnalyzerService(
        IAsynchronousOperationListenerProvider listenerProvider,
        DiagnosticAnalyzerInfoCache.SharedGlobalCache globalCache,
        IGlobalOptionService globalOptions,
        IDiagnosticsRefresher diagnosticsRefresher)
    {
        AnalyzerInfoCache = globalCache.AnalyzerInfoCache;
        Listener = listenerProvider.GetListener(FeatureAttribute.DiagnosticService);
        GlobalOptions = globalOptions;
        _diagnosticsRefresher = diagnosticsRefresher;
        _createIncrementalAnalyzer = CreateIncrementalAnalyzerCallback;

        globalOptions.AddOptionChangedHandler(this, (_, _, e) =>
        {
            if (e.HasOption(IsGlobalOptionAffectingDiagnostics))
            {
                RequestDiagnosticRefresh();
            }
        });
    }

    public static Task<VersionStamp> GetDiagnosticVersionAsync(Project project, CancellationToken cancellationToken)
        => project.GetDependentVersionAsync(cancellationToken);

    public bool CrashOnAnalyzerException
        => GlobalOptions.GetOption(s_crashOnAnalyzerException);

    public static bool IsGlobalOptionAffectingDiagnostics(IOption2 option)
        => option == NamingStyleOptions.NamingPreferences ||
           option.Definition.Group.Parent == CodeStyleOptionGroups.CodeStyle ||
           option == SolutionCrawlerOptionsStorage.BackgroundAnalysisScopeOption ||
           option == SolutionCrawlerOptionsStorage.SolutionBackgroundAnalysisScopeOption ||
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
        bool isExplicit,
        CancellationToken cancellationToken)
    {
        var analyzer = CreateIncrementalAnalyzer(document.Project.Solution.Workspace);

        // always make sure that analyzer is called on background thread.
        await TaskScheduler.Default;
        priorityProvider ??= new DefaultCodeActionRequestPriorityProvider();

        return await analyzer.GetDiagnosticsForSpanAsync(
            document, range, shouldIncludeDiagnostic, priorityProvider, diagnosticKinds, isExplicit, cancellationToken).ConfigureAwait(false);
    }

    public async Task<ImmutableArray<DiagnosticData>> ForceAnalyzeProjectAsync(Project project, CancellationToken cancellationToken)
    {
        var analyzer = CreateIncrementalAnalyzer(project.Solution.Workspace);
        return await analyzer.ForceAnalyzeProjectAsync(project, cancellationToken).ConfigureAwait(false);
    }

    public Task<ImmutableArray<DiagnosticData>> GetDiagnosticsForIdsAsync(
        Project project, DocumentId? documentId, ImmutableHashSet<string>? diagnosticIds, Func<DiagnosticAnalyzer, bool>? shouldIncludeAnalyzer, bool includeLocalDocumentDiagnostics, bool includeNonLocalDocumentDiagnostics, CancellationToken cancellationToken)
    {
        var analyzer = CreateIncrementalAnalyzer(project.Solution.Workspace);
        return analyzer.GetDiagnosticsForIdsAsync(project, documentId, diagnosticIds, shouldIncludeAnalyzer, includeLocalDocumentDiagnostics, includeNonLocalDocumentDiagnostics, cancellationToken);
    }

    public Task<ImmutableArray<DiagnosticData>> GetProjectDiagnosticsForIdsAsync(
        Project project, ImmutableHashSet<string>? diagnosticIds,
        Func<DiagnosticAnalyzer, bool>? shouldIncludeAnalyzer,
        bool includeNonLocalDocumentDiagnostics, CancellationToken cancellationToken)
    {
        var analyzer = CreateIncrementalAnalyzer(project.Solution.Workspace);
        return analyzer.GetProjectDiagnosticsForIdsAsync(project, diagnosticIds, shouldIncludeAnalyzer, includeNonLocalDocumentDiagnostics, cancellationToken);
    }

    public TestAccessor GetTestAccessor()
        => new(this);

    public readonly struct TestAccessor(DiagnosticAnalyzerService service)
    {
        public Task<ImmutableArray<DiagnosticAnalyzer>> GetAnalyzersAsync(Project project, CancellationToken cancellationToken)
        {
            return service.CreateIncrementalAnalyzer(project.Solution.Workspace).GetAnalyzersForTestingPurposesOnlyAsync(project, cancellationToken);
        }
    }
}
