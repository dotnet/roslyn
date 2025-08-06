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
using Microsoft.CodeAnalysis.Remote;
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

    private static readonly ImmutableArray<string> s_csharpLanguageArray = [LanguageNames.CSharp];
    private static readonly ImmutableArray<string> s_visualBasicLanguageArray = [LanguageNames.VisualBasic];
    private static readonly ImmutableArray<string> s_csharpAndVisualBasicLanguageArray = [.. s_csharpLanguageArray, .. s_visualBasicLanguageArray];

    public IAsynchronousOperationListener Listener { get; }
    private IGlobalOptionService GlobalOptions { get; }

    private readonly IDiagnosticsRefresher _diagnosticsRefresher;
    private readonly DiagnosticIncrementalAnalyzer _incrementalAnalyzer;
    private readonly DiagnosticAnalyzerInfoCache _analyzerInfoCache;

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

    public async Task<ImmutableArray<DiagnosticDescriptor>> GetDiagnosticDescriptorsAsync(
        Solution solution, AnalyzerReference analyzerReference, string language, CancellationToken cancellationToken)
    {
        // Attempt to compute this OOP.
        var client = await RemoteHostClient.TryGetClientAsync(solution.Services, cancellationToken).ConfigureAwait(false);
        if (client is not null &&
            analyzerReference is AnalyzerFileReference analyzerFileReference)
        {
            var descriptors = await client.TryInvokeAsync<IRemoteDiagnosticAnalyzerService, ImmutableArray<DiagnosticDescriptorData>>(
                solution,
                (service, solution, cancellationToken) => service.GetDiagnosticDescriptorsAsync(solution, analyzerFileReference.FullPath, cancellationToken),
                cancellationToken).ConfigureAwait(false);
            if (!descriptors.HasValue)
                return [];

            return descriptors.Value.SelectAsArray(d => d.ToDiagnosticDescriptor());
        }

        // Otherwise, fallback to computing in proc.
        return analyzerReference
            .GetAnalyzers(language)
            .SelectManyAsArray(this._analyzerInfoCache.GetDiagnosticDescriptors);
    }

    public Task<ImmutableDictionary<ImmutableArray<string>, ImmutableArray<DiagnosticDescriptor>>> GetDiagnosticDescriptorsAsync(
        Solution solution, AnalyzerReference analyzerReference, CancellationToken cancellationToken)
    {
        var mapBuilder = ImmutableDictionary.CreateBuilder<ImmutableArray<string>, ImmutableArray<DiagnosticDescriptor>>();

        var csharpAnalyzers = analyzerReference.GetAnalyzers(LanguageNames.CSharp);
        var visualBasicAnalyzers = analyzerReference.GetAnalyzers(LanguageNames.VisualBasic);

        var dotnetAnalyzers = csharpAnalyzers.Intersect(visualBasicAnalyzers, DiagnosticAnalyzerComparer.Instance).ToImmutableArray();
        csharpAnalyzers = [.. csharpAnalyzers.Except(dotnetAnalyzers, DiagnosticAnalyzerComparer.Instance)];
        visualBasicAnalyzers = [.. visualBasicAnalyzers.Except(dotnetAnalyzers, DiagnosticAnalyzerComparer.Instance)];

        mapBuilder.Add(s_csharpLanguageArray, GetDiagnosticDescriptors(csharpAnalyzers));
        mapBuilder.Add(s_visualBasicLanguageArray, GetDiagnosticDescriptors(visualBasicAnalyzers));
        mapBuilder.Add(s_csharpAndVisualBasicLanguageArray, GetDiagnosticDescriptors(dotnetAnalyzers));

        return Task.FromResult(mapBuilder.ToImmutable());

        ImmutableArray<DiagnosticDescriptor> GetDiagnosticDescriptors(ImmutableArray<DiagnosticAnalyzer> analyzers)
            => analyzers.SelectManyAsArray(this._analyzerInfoCache.GetDiagnosticDescriptors);
    }

    public Task<ImmutableDictionary<string, DiagnosticDescriptor>> TryGetDiagnosticDescriptorsAsync(
        Solution solution, ImmutableArray<string> diagnosticIds, CancellationToken cancellationToken)
    {
        var builder = ImmutableDictionary.CreateBuilder<string, DiagnosticDescriptor>();
        foreach (var diagnosticId in diagnosticIds)
        {
            if (this._analyzerInfoCache.TryGetDescriptorForDiagnosticId(diagnosticId, out var descriptor))
                builder[diagnosticId] = descriptor;
        }

        return Task.FromResult(builder.ToImmutable());
    }

    public Task<ImmutableDictionary<string, ImmutableArray<DiagnosticDescriptor>>> GetDiagnosticDescriptorsPerReferenceAsync(Solution solution, CancellationToken cancellationToken)
        => Task.FromResult(solution.SolutionState.Analyzers.GetDiagnosticDescriptorsPerReference(this._analyzerInfoCache));

    public Task<ImmutableDictionary<string, ImmutableArray<DiagnosticDescriptor>>> GetDiagnosticDescriptorsPerReferenceAsync(Project project, CancellationToken cancellationToken)
        => Task.FromResult(project.Solution.SolutionState.Analyzers.GetDiagnosticDescriptorsPerReference(this._analyzerInfoCache, project));

    private sealed class DiagnosticAnalyzerComparer : IEqualityComparer<DiagnosticAnalyzer>
    {
        public static readonly DiagnosticAnalyzerComparer Instance = new();

        public bool Equals(DiagnosticAnalyzer? x, DiagnosticAnalyzer? y)
        {
            if (x is null && y is null)
                return true;

            if (x is null || y is null)
                return false;

            return x.GetAnalyzerIdAndVersion().GetHashCode() == y.GetAnalyzerIdAndVersion().GetHashCode();
        }

        public int GetHashCode(DiagnosticAnalyzer obj)
            => obj.GetAnalyzerIdAndVersion().GetHashCode();
    }

    public TestAccessor GetTestAccessor()
        => new(this);

    public readonly struct TestAccessor(DiagnosticAnalyzerService service)
    {
        public Task<ImmutableArray<DiagnosticAnalyzer>> GetAnalyzersAsync(Project project, CancellationToken cancellationToken)
            => service._incrementalAnalyzer.GetAnalyzersForTestingPurposesOnlyAsync(project, cancellationToken);
    }
}
