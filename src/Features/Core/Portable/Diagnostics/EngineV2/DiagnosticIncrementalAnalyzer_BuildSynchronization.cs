// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Workspaces.Diagnostics;

#if NETSTANDARD2_0
using Roslyn.Utilities;
#endif

namespace Microsoft.CodeAnalysis.Diagnostics.EngineV2
{
    internal partial class DiagnosticIncrementalAnalyzer
    {
        public async Task InitializeSynchronizationStateWithBuildAsync(Solution solution, CancellationToken cancellationToken)
        {
            foreach (var project in solution.Projects)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var stateSets = _stateManager.CreateBuildOnlyProjectStateSet(project);
                _ = await ProjectAnalysisData.CreateAsync(PersistentStorageService, project, stateSets, avoidLoadingData: false, cancellationToken).ConfigureAwait(false);
            }
        }

        public async Task SynchronizeWithBuildAsync(ImmutableDictionary<ProjectId, ImmutableArray<DiagnosticData>> buildDiagnostics, bool onBuildCompleted, CancellationToken cancellationToken)
        {
            var options = Workspace.Options;

            using (Logger.LogBlock(FunctionId.DiagnosticIncrementalAnalyzer_SynchronizeWithBuildAsync, LogSynchronizeWithBuild, options, buildDiagnostics, cancellationToken))
            {
                DebugVerifyDiagnosticLocations(buildDiagnostics);

                if (!PreferBuildErrors(options))
                {
                    // Prefer live errors over build errors
                    return;
                }

                var solution = Workspace.CurrentSolution;

                foreach (var (projectId, diagnostics) in buildDiagnostics)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var project = solution.GetProject(projectId);
                    if (project == null)
                    {
                        continue;
                    }

                    // REVIEW: do build diagnostics include suppressed diagnostics?
                    var stateSets = _stateManager.CreateBuildOnlyProjectStateSet(project);

                    // We load data since we don't know right version.
                    var oldAnalysisData = await ProjectAnalysisData.CreateAsync(PersistentStorageService, project, stateSets, avoidLoadingData: false, cancellationToken).ConfigureAwait(false);
                    var newResult = CreateAnalysisResults(project, stateSets, oldAnalysisData, diagnostics);

                    foreach (var stateSet in stateSets)
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        var state = stateSet.GetOrCreateProjectState(project.Id);
                        var result = GetResultOrEmpty(newResult, stateSet.Analyzer, project.Id, VersionStamp.Default);
                        await state.SaveAsync(PersistentStorageService, project, result).ConfigureAwait(false);
                    }

                    // REVIEW: this won't handle active files. might need to tweak it later.
                    RaiseProjectDiagnosticsIfNeeded(project, stateSets, oldAnalysisData.Result, newResult);
                }

                // Refresh diagnostics for open files after solution build completes.
                if (onBuildCompleted && PreferLiveErrorsOnOpenedFiles(options))
                {
                    // Enqueue re-analysis of active document.
                    if (_documentTrackingService?.GetActiveDocument(solution) is { } activeDocument)
                    {
                        AnalyzerService.Reanalyze(Workspace, documentIds: ImmutableArray.Create(activeDocument.Id), highPriority: true);
                    }

                    // Enqueue re-analysis of open documents.
                    AnalyzerService.Reanalyze(Workspace, documentIds: Workspace.GetOpenDocumentIds(), highPriority: true);
                }
            }
        }

        [Conditional("DEBUG")]
        private static void DebugVerifyDiagnosticLocations(ImmutableDictionary<ProjectId, ImmutableArray<DiagnosticData>> buildDiagnostics)
        {
            foreach (var diagnostic in buildDiagnostics.Values.SelectMany(v => v))
            {
                // errors from build shouldn't have any span set.
                // this is debug check since it gets data from us only not from third party unlike one in compiler
                // that checks span for third party reported diagnostics
                Debug.Assert(!diagnostic.HasTextSpan);
            }
        }

        private ImmutableDictionary<DiagnosticAnalyzer, DiagnosticAnalysisResult> CreateAnalysisResults(
            Project project, ImmutableArray<StateSet> stateSets, ProjectAnalysisData oldAnalysisData, ImmutableArray<DiagnosticData> diagnostics)
        {
            using var poolObject = SharedPools.Default<HashSet<string>>().GetPooledObject();

            var lookup = diagnostics.ToLookup(d => d.Id);

            var builder = ImmutableDictionary.CreateBuilder<DiagnosticAnalyzer, DiagnosticAnalysisResult>();
            foreach (var stateSet in stateSets)
            {
                var descriptors = DiagnosticAnalyzerInfoCache.GetDiagnosticDescriptors(stateSet.Analyzer);

                var liveDiagnostics = MergeDiagnostics(
                    ConvertToLiveDiagnostics(lookup, descriptors, poolObject.Object),
                    oldAnalysisData.GetResult(stateSet.Analyzer).GetAllDiagnostics());

                builder.Add(stateSet.Analyzer, DiagnosticAnalysisResult.CreateFromBuild(project, liveDiagnostics));
            }

            return builder.ToImmutable();
        }

        private static bool PreferBuildErrors(OptionSet options)
            => options.GetOption(InternalDiagnosticsOptions.PreferBuildErrorsOverLiveErrors);

        private static bool PreferLiveErrorsOnOpenedFiles(OptionSet options)
            => options.GetOption(InternalDiagnosticsOptions.PreferLiveErrorsOnOpenedFiles);

        private static ImmutableArray<DiagnosticData> MergeDiagnostics(ImmutableArray<DiagnosticData> newDiagnostics, ImmutableArray<DiagnosticData> existingDiagnostics)
        {
            ImmutableArray<DiagnosticData>.Builder? builder = null;

            if (newDiagnostics.Length > 0)
            {
                builder = ImmutableArray.CreateBuilder<DiagnosticData>();
                builder.AddRange(newDiagnostics);
            }

            if (existingDiagnostics.Length > 0)
            {
                // retain hidden live diagnostics since it won't be comes from build.
                builder ??= ImmutableArray.CreateBuilder<DiagnosticData>();
                builder.AddRange(existingDiagnostics.Where(d => d.Severity == DiagnosticSeverity.Hidden));
            }

            return builder == null ? ImmutableArray<DiagnosticData>.Empty : builder.ToImmutable();
        }

        private static ImmutableArray<DiagnosticData> ConvertToLiveDiagnostics(
            ILookup<string, DiagnosticData> lookup, ImmutableArray<DiagnosticDescriptor> descriptors, HashSet<string> seen)
        {
            if (lookup == null)
            {
                return ImmutableArray<DiagnosticData>.Empty;
            }

            ImmutableArray<DiagnosticData>.Builder? builder = null;
            foreach (var descriptor in descriptors)
            {
                // make sure we don't report same id to multiple different analyzers
                if (!seen.Add(descriptor.Id))
                {
                    // TODO: once we have information where diagnostic came from, we probably don't need this.
                    continue;
                }

                var items = lookup[descriptor.Id];
                if (items == null)
                {
                    continue;
                }

                builder ??= ImmutableArray.CreateBuilder<DiagnosticData>();
                builder.AddRange(items.Select(d => CreateLiveDiagnostic(descriptor, d)));
            }

            return builder == null ? ImmutableArray<DiagnosticData>.Empty : builder.ToImmutable();
        }

        private static DiagnosticData CreateLiveDiagnostic(DiagnosticDescriptor descriptor, DiagnosticData diagnostic)
        {
            return new DiagnosticData(
                descriptor.Id,
                descriptor.Category,
                diagnostic.Message,
                descriptor.GetBingHelpMessage(),
                diagnostic.Severity,
                descriptor.DefaultSeverity,
                descriptor.IsEnabledByDefault,
                diagnostic.WarningLevel,
                descriptor.CustomTags.ToImmutableArray(),
                diagnostic.Properties,
                diagnostic.ProjectId,
                diagnostic.DataLocation,
                diagnostic.AdditionalLocations,
                diagnostic.Language,
                descriptor.Title.ToString(CultureInfo.CurrentUICulture),
                descriptor.Description.ToString(CultureInfo.CurrentUICulture),
                descriptor.HelpLinkUri,
                isSuppressed: diagnostic.IsSuppressed);
        }

        private static string LogSynchronizeWithBuild(OptionSet options, ImmutableDictionary<ProjectId, ImmutableArray<DiagnosticData>> map)
        {
            using var pooledObject = SharedPools.Default<StringBuilder>().GetPooledObject();
            var sb = pooledObject.Object;
            sb.Append($"PreferBuildError:{PreferBuildErrors(options)}, PreferLiveOnOpenFiles:{PreferLiveErrorsOnOpenedFiles(options)}");

            if (map.Count > 0)
            {
                foreach (var (projectId, diagnostics) in map)
                {
                    sb.AppendLine($"{projectId}, Count: {diagnostics.Length}");

                    foreach (var diagnostic in diagnostics)
                    {
                        sb.AppendLine($"    {diagnostic.ToString()}");
                    }
                }
            }

            return sb.ToString();
        }
    }
}
