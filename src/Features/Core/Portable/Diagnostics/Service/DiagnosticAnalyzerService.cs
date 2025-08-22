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
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.SolutionCrawler;
using Microsoft.CodeAnalysis.Workspaces.Diagnostics;

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
    // Shared with Compiler
    public const string AnalyzerExceptionDiagnosticId = "AD0001";

    private static readonly Option2<bool> s_crashOnAnalyzerException = new("dotnet_crash_on_analyzer_exception", defaultValue: false);

    private readonly IAsynchronousOperationListener _listener;
    private readonly IGlobalOptionService _globalOptions;

    private readonly IDiagnosticsRefresher _diagnosticsRefresher;
    private readonly DiagnosticAnalyzerInfoCache _analyzerInfoCache;
    private readonly DiagnosticAnalyzerTelemetry _telemetry = new();
    private readonly IncrementalMemberEditAnalyzer _incrementalMemberEditAnalyzer = new();

    /// <summary>
    /// Analyzers supplied by the host (IDE). These are built-in to the IDE, the compiler, or from an installed IDE extension (VSIX). 
    /// Maps language name to the analyzers and their state.
    /// </summary>
    private ImmutableDictionary<HostAnalyzerInfoKey, HostAnalyzerInfo> _hostAnalyzerStateMap = ImmutableDictionary<HostAnalyzerInfoKey, HostAnalyzerInfo>.Empty;

    /// <summary>
    /// Analyzers referenced by the project via a PackageReference. Updates are protected by _projectAnalyzerStateMapGuard.
    /// ImmutableDictionary used to present a safe, non-immutable view to users.
    /// </summary>
    private ImmutableDictionary<(ProjectId projectId, IReadOnlyList<AnalyzerReference> analyzerReferences), ProjectAnalyzerInfo> _projectAnalyzerStateMap = ImmutableDictionary<(ProjectId projectId, IReadOnlyList<AnalyzerReference> analyzerReferences), ProjectAnalyzerInfo>.Empty;

    public DiagnosticAnalyzerService(
        IGlobalOptionService globalOptions,
        IDiagnosticsRefresher diagnosticsRefresher,
        DiagnosticAnalyzerInfoCache.SharedGlobalCache globalCache,
        IAsynchronousOperationListenerProvider? listenerProvider,
        Workspace workspace)
    {
        _analyzerInfoCache = globalCache.AnalyzerInfoCache;
        _listener = listenerProvider?.GetListener(FeatureAttribute.DiagnosticService) ?? AsynchronousOperationListenerProvider.NullListener;
        _globalOptions = globalOptions;
        _diagnosticsRefresher = diagnosticsRefresher;

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
        => _globalOptions.GetOption(s_crashOnAnalyzerException);

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

    private ImmutableArray<DiagnosticAnalyzer> GetDiagnosticAnalyzers(
        Project project,
        ImmutableHashSet<string>? diagnosticIds,
        Func<DiagnosticAnalyzer, bool>? shouldIncludeAnalyzer)
    {
        var analyzersForProject = GetProjectAnalyzers(project);
        var analyzers = analyzersForProject.WhereAsArray(a => ShouldIncludeAnalyzer(project, a));

        return analyzers;

        bool ShouldIncludeAnalyzer(Project project, DiagnosticAnalyzer analyzer)
        {
            if (!DocumentAnalysisExecutor.IsAnalyzerEnabledForProject(analyzer, project, this._globalOptions))
                return false;

            if (shouldIncludeAnalyzer != null && !shouldIncludeAnalyzer(analyzer))
                return false;

            if (diagnosticIds != null && _analyzerInfoCache.GetDiagnosticDescriptors(analyzer).All(d => !diagnosticIds.Contains(d.Id)))
                return false;

            return true;
        }
    }

    public Task<ImmutableArray<DiagnosticData>> GetDiagnosticsForIdsAsync(
        Project project, DocumentId? documentId, ImmutableHashSet<string>? diagnosticIds, Func<DiagnosticAnalyzer, bool>? shouldIncludeAnalyzer, bool includeLocalDocumentDiagnostics, bool includeNonLocalDocumentDiagnostics, CancellationToken cancellationToken)
    {
        var analyzers = GetDiagnosticAnalyzers(project, diagnosticIds, shouldIncludeAnalyzer);

        return ProduceProjectDiagnosticsAsync(
            project, analyzers, diagnosticIds,
            // Ensure we compute and return diagnostics for both the normal docs and the additional docs in this
            // project if no specific document id was requested.
            documentId != null ? [documentId] : [.. project.DocumentIds, .. project.AdditionalDocumentIds],
            includeLocalDocumentDiagnostics,
            includeNonLocalDocumentDiagnostics,
            // return diagnostics specific to one project or document
            includeProjectNonLocalResult: documentId == null,
            cancellationToken);
    }

    public Task<ImmutableArray<DiagnosticData>> GetProjectDiagnosticsForIdsAsync(
        Project project, ImmutableHashSet<string>? diagnosticIds,
        Func<DiagnosticAnalyzer, bool>? shouldIncludeAnalyzer,
        bool includeNonLocalDocumentDiagnostics, CancellationToken cancellationToken)
    {
        var analyzers = GetDiagnosticAnalyzers(project, diagnosticIds, shouldIncludeAnalyzer);

        return ProduceProjectDiagnosticsAsync(
            project, analyzers, diagnosticIds,
            documentIds: [],
            includeLocalDocumentDiagnostics: false,
            includeNonLocalDocumentDiagnostics: includeNonLocalDocumentDiagnostics,
            includeProjectNonLocalResult: true,
            cancellationToken);
    }

    public TestAccessor GetTestAccessor()
        => new(this);

    public readonly struct TestAccessor(DiagnosticAnalyzerService service)
    {
        public ImmutableArray<DiagnosticAnalyzer> GetAnalyzers(Project project)
            => service.GetProjectAnalyzers(project);

        public Task<DiagnosticAnalysisResultMap<DiagnosticAnalyzer, DiagnosticAnalysisResult>> AnalyzeProjectInProcessAsync(
            Project project, CompilationWithAnalyzersPair compilationWithAnalyzers, bool logPerformanceInfo, bool getTelemetryInfo, CancellationToken cancellationToken)
            => service.AnalyzeInProcessAsync(documentAnalysisScope: null, project, compilationWithAnalyzers, logPerformanceInfo, getTelemetryInfo, cancellationToken);
    }
}
