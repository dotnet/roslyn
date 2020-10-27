﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.SolutionCrawler;
using Microsoft.CodeAnalysis.Workspaces.Diagnostics;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Diagnostics.EngineV2
{
    internal partial class DiagnosticIncrementalAnalyzer
    {
        public async Task SynchronizeWithBuildAsync(
            ImmutableDictionary<ProjectId,
            ImmutableArray<DiagnosticData>> buildDiagnostics,
            TaskQueue postBuildAndErrorListRefreshTaskQueue,
            bool onBuildCompleted,
            CancellationToken cancellationToken)
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

                    var stateSets = _stateManager.CreateBuildOnlyProjectStateSet(project);
                    var newResult = CreateAnalysisResults(project, stateSets, diagnostics);

                    // PERF: Save the diagnostics into in-memory cache on the main thread.
                    //       Saving them into persistent storage is expensive, so we invoke that operation on a separate task queue
                    //       to ensure faster error list refresh.
                    foreach (var stateSet in stateSets)
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        var state = stateSet.GetOrCreateProjectState(project.Id);
                        var result = GetResultOrEmpty(newResult, stateSet.Analyzer, project.Id, VersionStamp.Default);

                        // Save into in-memory cache.
                        await state.SaveInMemoryCacheAsync(project, result).ConfigureAwait(false);

                        // Save into persistent storage on a separate post-build and post error list refresh task queue.
                        _ = postBuildAndErrorListRefreshTaskQueue.ScheduleTask(
                                nameof(SynchronizeWithBuildAsync),
                                () => state.SaveAsync(PersistentStorageService, project, result),
                                cancellationToken);
                    }

                    // Raise diagnostic updated events after the new diagnostics have been stored into the in-memory cache.
                    if (diagnostics.IsEmpty)
                    {
                        ClearAllDiagnostics(stateSets, projectId);
                    }
                    else
                    {
                        RaiseProjectDiagnosticsIfNeeded(project, stateSets, newResult);
                    }
                }

                // Refresh live diagnostics after solution build completes.
                if (onBuildCompleted && PreferLiveErrorsOnOpenedFiles(options))
                {
                    // Enqueue re-analysis of active document with high-priority right away.
                    if (_documentTrackingService?.GetActiveDocument(solution) is { } activeDocument)
                    {
                        AnalyzerService.Reanalyze(Workspace, documentIds: ImmutableArray.Create(activeDocument.Id), highPriority: true);
                    }

                    // Enqueue remaining re-analysis with normal priority on a separate task queue
                    // that will execute at the end of all the post build and error list refresh tasks.
                    _ = postBuildAndErrorListRefreshTaskQueue.ScheduleTask(nameof(SynchronizeWithBuildAsync), () =>
                    {
                        // Enqueue re-analysis of open documents.
                        AnalyzerService.Reanalyze(Workspace, documentIds: Workspace.GetOpenDocumentIds());

                        // Enqueue re-analysis of projects, if required.
                        foreach (var projectsByLanguage in solution.Projects.GroupBy(p => p.Language))
                        {
                            if (SolutionCrawlerOptions.GetBackgroundAnalysisScope(Workspace.Options, projectsByLanguage.Key) == BackgroundAnalysisScope.FullSolution)
                            {
                                AnalyzerService.Reanalyze(Workspace, projectsByLanguage.Select(p => p.Id));
                            }
                        }
                    }, cancellationToken);
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
            Project project, ImmutableArray<StateSet> stateSets, ImmutableArray<DiagnosticData> diagnostics)
        {
            using var poolObject = SharedPools.Default<HashSet<string>>().GetPooledObject();

            var lookup = diagnostics.ToLookup(d => d.Id);

            var builder = ImmutableDictionary.CreateBuilder<DiagnosticAnalyzer, DiagnosticAnalysisResult>();
            using var _ = PooledHashSet<DocumentId>.GetInstance(out var existingDocumentsInStateSet);
            foreach (var stateSet in stateSets)
            {
                var descriptors = DiagnosticAnalyzerInfoCache.GetDiagnosticDescriptors(stateSet.Analyzer);
                var liveDiagnostics = ConvertToLiveDiagnostics(lookup, descriptors, poolObject.Object);

                // Ensure that all documents with diagnostics in the previous state set are added to the result.
                existingDocumentsInStateSet.Clear();
                stateSet.CollectDocumentsWithDiagnostics(project.Id, existingDocumentsInStateSet);

                builder.Add(stateSet.Analyzer, DiagnosticAnalysisResult.CreateFromBuild(project, liveDiagnostics, existingDocumentsInStateSet));
            }

            return builder.ToImmutable();
        }

        private static bool PreferBuildErrors(OptionSet options)
            => options.GetOption(InternalDiagnosticsOptions.PreferBuildErrorsOverLiveErrors);

        private static bool PreferLiveErrorsOnOpenedFiles(OptionSet options)
            => options.GetOption(InternalDiagnosticsOptions.PreferLiveErrorsOnOpenedFiles);

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
