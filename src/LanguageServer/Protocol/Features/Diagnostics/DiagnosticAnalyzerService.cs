// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.SolutionCrawler;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Threading;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Diagnostics
{
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

        private static readonly ConditionalWeakTable<Project, Roslyn.Utilities.AsyncLazy<Checksum>> s_projectToDiagnosticChecksum = new();

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
               option == s_crashOnAnalyzerException;

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

        public Task<Checksum> GetDiagnosticChecksumAsync(Project project, CancellationToken cancellationToken)
            => StaticGetDiagnosticChecksumAsync(project, cancellationToken);

        private static Task<Checksum> StaticGetDiagnosticChecksumAsync(Project project, CancellationToken cancellationToken)
        {
            var lazyChecksum = s_projectToDiagnosticChecksum.GetValue(
                project,
                static project => AsyncLazy.Create(
                    static (project, cancellationToken) => ComputeDiagnosticChecksumAsync(project, cancellationToken),
                    project));

            return lazyChecksum.GetValueAsync(cancellationToken);

            static async Task<Checksum> ComputeDiagnosticChecksumAsync(Project project, CancellationToken cancellationToken)
            {
                var solution = project.Solution;

                using var _ = ArrayBuilder<Checksum>.GetInstance(out var tempChecksumArray);

                // Mix in the SG information for this project.  That way if it changes, we will have a different
                // checksum (since semantics could have changed because of this).
                if (solution.CompilationState.SourceGeneratorExecutionVersionMap.Map.TryGetValue(project.Id, out var executionVersion))
                    tempChecksumArray.Add(executionVersion.Checksum);

                // Get the checksum for the project itself.  Note: this will normally be cached.  As such, even if we
                // have a different Project instance (due to a change in an unrelated project), this will be fast to
                // compute and return.
                var projectChecksum = await project.State.GetChecksumAsync(cancellationToken).ConfigureAwait(false);
                tempChecksumArray.Add(projectChecksum);

                // Calculate a checksum this project and for each dependent project that could affect semantics for this
                // project. We order the projects so that we are resilient to the underlying in-memory graph structure
                // changing this arbitrarily.  We do not want that to cause us to change our semantic version.. Note: we
                // use the project filepath+name as a unique way to reference a project.  This matches the logic in our
                // persistence-service implementation as to how information is associated with a project.
                var transitiveDependencies = solution.SolutionState.GetProjectDependencyGraph().GetProjectsThatThisProjectTransitivelyDependsOn(project.Id);
                var orderedProjectIds = transitiveDependencies.OrderBy(id =>
                {
                    var depProject = solution.SolutionState.GetRequiredProjectState(id);
                    return (depProject.FilePath, depProject.Name);
                });

                foreach (var projectId in orderedProjectIds)
                {
                    // Note that these checksums should only actually be calculated once, if the project is unchanged
                    // the same checksum will be returned.
                    tempChecksumArray.Add(await StaticGetDiagnosticChecksumAsync(
                        solution.GetRequiredProject(projectId), cancellationToken).ConfigureAwait(false));
                }

                return Checksum.Create(tempChecksumArray);
            }
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
}
