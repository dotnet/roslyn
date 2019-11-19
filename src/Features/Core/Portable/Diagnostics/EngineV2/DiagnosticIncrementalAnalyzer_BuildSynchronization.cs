// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Workspaces.Diagnostics;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Diagnostics.EngineV2
{
    internal partial class DiagnosticIncrementalAnalyzer
    {
        public async Task SynchronizeWithBuildAsync(Workspace workspace, ImmutableDictionary<ProjectId, ImmutableArray<DiagnosticData>> map)
        {
            using (Logger.LogBlock(FunctionId.DiagnosticIncrementalAnalyzer_SynchronizeWithBuildAsync, (w, m) => LogSynchronizeWithBuild(w, m), workspace, map, CancellationToken.None))
            {
                DebugVerifyDiagnosticLocations(map);

                if (!PreferBuildErrors(workspace))
                {
                    // prefer live errors over build errors
                    return;
                }

                var solution = workspace.CurrentSolution;
                foreach (var projectEntry in map)
                {
                    var project = solution.GetProject(projectEntry.Key);
                    if (project == null)
                    {
                        continue;
                    }

                    // REVIEW: is build diagnostic contains suppressed diagnostics?
                    var stateSets = _stateManager.CreateBuildOnlyProjectStateSet(project);
                    var result = await CreateProjectAnalysisDataAsync(project, stateSets, projectEntry.Value).ConfigureAwait(false);

                    foreach (var stateSet in stateSets)
                    {
                        var state = stateSet.GetProjectState(project.Id);
                        await state.SaveAsync(project, result.GetResult(stateSet.Analyzer)).ConfigureAwait(false);
                    }

                    // REVIEW: this won't handle active files. might need to tweak it later.
                    RaiseProjectDiagnosticsIfNeeded(project, stateSets, result.OldResult, result.Result);
                }

                // if we have updated errors, refresh open files
                if (map.Count > 0 && PreferLiveErrorsOnOpenedFiles(workspace))
                {
                    // enqueue re-analysis of open documents.
                    AnalyzerService.Reanalyze(workspace, documentIds: workspace.GetOpenDocumentIds(), highPriority: true);
                }
            }
        }

        [Conditional("DEBUG")]
        private void DebugVerifyDiagnosticLocations(ImmutableDictionary<ProjectId, ImmutableArray<DiagnosticData>> map)
        {
            foreach (var diagnostic in map.Values.SelectMany(v => v))
            {
                // errors from build shouldn't have any span set.
                // this is debug check since it gets data from us only not from third party unlike one in compiler
                // that checks span for third party reported diagnostics
                Debug.Assert(!diagnostic.HasTextSpan);
            }
        }

        private async Task<ProjectAnalysisData> CreateProjectAnalysisDataAsync(Project project, ImmutableArray<StateSet> stateSets, ImmutableArray<DiagnosticData> diagnostics)
        {
            // we always load data since we don't know right version.
            var avoidLoadingData = false;
            var oldAnalysisData = await ProjectAnalysisData.CreateAsync(project, stateSets, avoidLoadingData, CancellationToken.None).ConfigureAwait(false);
            var newResult = CreateAnalysisResults(project, stateSets, oldAnalysisData, diagnostics);

            return new ProjectAnalysisData(project.Id, VersionStamp.Default, oldAnalysisData.Result, newResult);
        }

        private ImmutableDictionary<DiagnosticAnalyzer, DiagnosticAnalysisResult> CreateAnalysisResults(
            Project project, ImmutableArray<StateSet> stateSets, ProjectAnalysisData oldAnalysisData, ImmutableArray<DiagnosticData> diagnostics)
        {
            using var poolObject = SharedPools.Default<HashSet<string>>().GetPooledObject();

            var lookup = diagnostics.ToLookup(d => d.Id);

            var builder = ImmutableDictionary.CreateBuilder<DiagnosticAnalyzer, DiagnosticAnalysisResult>();
            foreach (var stateSet in stateSets)
            {
                var descriptors = HostAnalyzerManager.GetDiagnosticDescriptors(stateSet.Analyzer);
                var liveDiagnostics = MergeDiagnostics(ConvertToLiveDiagnostics(lookup, descriptors, poolObject.Object), GetDiagnostics(oldAnalysisData.GetResult(stateSet.Analyzer)));

                var result = DiagnosticAnalysisResult.CreateFromBuild(project, liveDiagnostics);

                builder.Add(stateSet.Analyzer, result);
            }

            return builder.ToImmutable();
        }

        private ImmutableArray<DiagnosticData> GetDiagnostics(DiagnosticAnalysisResult result)
        {
            // PERF: don't allocation anything if not needed
            if (result.IsAggregatedForm || result.IsEmpty)
            {
                return ImmutableArray<DiagnosticData>.Empty;
            }

            return result.SyntaxLocals.Values.SelectMany(v => v).Concat(
                   result.SemanticLocals.Values.SelectMany(v => v)).Concat(
                   result.NonLocals.Values.SelectMany(v => v)).Concat(
                   result.Others).ToImmutableArray();
        }

        private static bool PreferBuildErrors(Workspace workspace)
        {
            return workspace.Options.GetOption(InternalDiagnosticsOptions.PreferBuildErrorsOverLiveErrors);
        }

        private static bool PreferLiveErrorsOnOpenedFiles(Workspace workspace)
        {
            return workspace.Options.GetOption(InternalDiagnosticsOptions.PreferLiveErrorsOnOpenedFiles);
        }

        private ImmutableArray<DiagnosticData> MergeDiagnostics(ImmutableArray<DiagnosticData> newDiagnostics, ImmutableArray<DiagnosticData> existingDiagnostics)
        {
            ImmutableArray<DiagnosticData>.Builder builder = null;

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

        private ImmutableArray<DiagnosticData> ConvertToLiveDiagnostics(
            ILookup<string, DiagnosticData> lookup, ImmutableArray<DiagnosticDescriptor> descriptors, HashSet<string> seen)
        {
            if (lookup == null)
            {
                return ImmutableArray<DiagnosticData>.Empty;
            }

            ImmutableArray<DiagnosticData>.Builder builder = null;
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

        private static string LogSynchronizeWithBuild(Workspace workspace, ImmutableDictionary<ProjectId, ImmutableArray<DiagnosticData>> map)
        {
            using var pooledObject = SharedPools.Default<StringBuilder>().GetPooledObject();
            var sb = pooledObject.Object;
            sb.Append($"PreferBuildError:{PreferBuildErrors(workspace)}, PreferLiveOnOpenFiles:{PreferLiveErrorsOnOpenedFiles(workspace)}");

            if (map.Count > 0)
            {
                foreach (var kv in map)
                {
                    sb.AppendLine($"{kv.Key}, Count: {kv.Value.Length}");

                    foreach (var diagnostic in kv.Value)
                    {
                        sb.AppendLine($"    {diagnostic.ToString()}");
                    }
                }
            }

            return sb.ToString();
        }
    }
}
