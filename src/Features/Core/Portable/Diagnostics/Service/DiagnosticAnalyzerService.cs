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
using Microsoft.CodeAnalysis.PooledObjects;
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
    [Import(AllowDefault = true)] IAsynchronousOperationListenerProvider? listenerProvider) : IWorkspaceServiceFactory
{
    public IWorkspaceService CreateService(HostWorkspaceServices workspaceServices)
    {
        return new DiagnosticAnalyzerService(
            globalOptions,
            diagnosticsRefresher,
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
    private readonly DiagnosticAnalyzerInfoCache _analyzerInfoCache = new();
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
        IAsynchronousOperationListenerProvider? listenerProvider,
        Workspace workspace)
    {
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

    public TestAccessor GetTestAccessor()
        => new(this);

    public readonly struct TestAccessor(DiagnosticAnalyzerService service)
    {
        public ImmutableArray<DiagnosticAnalyzer> GetAnalyzers(Project project)
            => service.GetProjectAnalyzers_OnlyCallInProcess(project);

        public Task<DiagnosticAnalysisResultMap<DiagnosticAnalyzer, DiagnosticAnalysisResult>> AnalyzeProjectInProcessAsync(
            Project project, CompilationWithAnalyzers compilationWithAnalyzers, bool logPerformanceInfo, bool getTelemetryInfo, CancellationToken cancellationToken)
            => service.AnalyzeInProcessAsync(documentAnalysisScope: null, project, compilationWithAnalyzers, logPerformanceInfo, getTelemetryInfo, cancellationToken);

        public async Task<ImmutableArray<DiagnosticAnalyzer>> GetDeprioritizedAnalyzersAsync(Project project)
        {
            using var _ = ArrayBuilder<DiagnosticAnalyzer>.GetInstance(out var builder);

            foreach (var analyzer in service.GetProjectAnalyzers_OnlyCallInProcess(project))
            {
                if (await service.IsDeprioritizedAnalyzerAsync(project, analyzer, CancellationToken.None).ConfigureAwait(false))
                    builder.Add(analyzer);
            }

            return builder.ToImmutableAndClear();
        }

        public async Task<ImmutableHashSet<string>> GetDeprioritizedDiagnosticIdsAsync(Project project)
        {
            var builder = ImmutableHashSet.CreateBuilder<string>();

            await service.PopulateDeprioritizedDiagnosticIdMapAsync(project, CancellationToken.None).ConfigureAwait(false);

            foreach (var analyzer in service.GetProjectAnalyzers_OnlyCallInProcess(project))
            {
                Contract.ThrowIfFalse(DiagnosticAnalyzerService.s_analyzerToDeprioritizedDiagnosticIds.TryGetValue(analyzer, out var set));
                if (set != null)
                    builder.UnionWith(set);
            }

            return builder.ToImmutableHashSet();

        }
    }
}
