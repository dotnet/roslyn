// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Serialization;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

#if DEBUG
using System.Diagnostics;
using System.Text;
using Microsoft.CodeAnalysis.Internal.Log;
#endif

namespace Microsoft.CodeAnalysis.Remote
{
    internal static class TestUtils
    {
        public static void RemoveChecksums(this Dictionary<Checksum, object> map, ChecksumCollection checksums)
        {
            var set = new HashSet<Checksum>();
            set.AppendChecksums(checksums);

            RemoveChecksums(map, set);
        }

        public static void RemoveChecksums(this Dictionary<Checksum, object> map, IEnumerable<Checksum> checksums)
        {
            foreach (var checksum in checksums)
            {
                map.Remove(checksum);
            }
        }

        internal static async Task AssertChecksumsAsync(
            AssetProvider assetService,
            Checksum checksumFromRequest,
            Solution solutionFromScratch,
            Solution incrementalSolutionBuilt,
            ProjectId? projectConeId)
        {
#if DEBUG
            var sb = new StringBuilder();
            var allChecksumsFromRequest = await GetAllChildrenChecksumsAsync(checksumFromRequest).ConfigureAwait(false);

            var assetMapFromNewSolution = await solutionFromScratch.GetAssetMapAsync(projectConeId, CancellationToken.None).ConfigureAwait(false);
            var assetMapFromIncrementalSolution = await incrementalSolutionBuilt.GetAssetMapAsync(projectConeId, CancellationToken.None).ConfigureAwait(false);

            // check 4 things
            // 1. first see if we create new solution from scratch, it works as expected (indicating a bug in incremental update)
            var mismatch1 = assetMapFromNewSolution.Where(p => !allChecksumsFromRequest.Contains(p.Key)).ToList();
            AppendMismatch(mismatch1, "Assets only in new solution but not in the request", sb);

            // 2. second check what items is mismatching for incremental solution
            var mismatch2 = assetMapFromIncrementalSolution.Where(p => !allChecksumsFromRequest.Contains(p.Key)).ToList();
            AppendMismatch(mismatch2, "Assets only in the incremental solution but not in the request", sb);

            // 3. check whether solution created from scratch and incremental one have any mismatch
            var mismatch3 = assetMapFromNewSolution.Where(p => !assetMapFromIncrementalSolution.ContainsKey(p.Key)).ToList();
            AppendMismatch(mismatch3, "Assets only in new solution but not in incremental solution", sb);

            var mismatch4 = assetMapFromIncrementalSolution.Where(p => !assetMapFromNewSolution.ContainsKey(p.Key)).ToList();
            AppendMismatch(mismatch4, "Assets only in incremental solution but not in new solution", sb);

            // 4. see what item is missing from request
            var mismatch5 = await GetAssetFromAssetServiceAsync(allChecksumsFromRequest.Except(assetMapFromNewSolution.Keys)).ConfigureAwait(false);
            AppendMismatch(mismatch5, "Assets only in the request but not in new solution", sb);

            var mismatch6 = await GetAssetFromAssetServiceAsync(allChecksumsFromRequest.Except(assetMapFromIncrementalSolution.Keys)).ConfigureAwait(false);
            AppendMismatch(mismatch6, "Assets only in the request but not in incremental solution", sb);

            var result = sb.ToString();
            if (result.Length > 0)
            {
                Logger.Log(FunctionId.SolutionCreator_AssetDifferences, result);
                Debug.Fail($"Differences detected in solution checksum (ProjectId={projectConeId}):\r\n{result}");
            }

            return;

            static void AppendMismatch(List<KeyValuePair<Checksum, object>> items, string title, StringBuilder stringBuilder)
            {
                if (items.Count == 0)
                {
                    return;
                }

                stringBuilder.AppendLine(title);
                foreach (var kv in items)
                {
                    stringBuilder.AppendLine($"{kv.Key.ToString()}, {kv.Value?.ToString()}");
                }

                stringBuilder.AppendLine();
            }

            async Task<List<KeyValuePair<Checksum, object>>> GetAssetFromAssetServiceAsync(IEnumerable<Checksum> checksums)
            {
                var items = new List<KeyValuePair<Checksum, object>>();

                foreach (var checksum in checksums)
                {
                    items.Add(KeyValuePairUtil.Create(checksum, await assetService.GetAssetAsync<object>(
                        AssetPath.FullLookupForTesting, checksum, CancellationToken.None).ConfigureAwait(false)));
                }

                return items;
            }

            async Task<HashSet<Checksum>> GetAllChildrenChecksumsAsync(Checksum solutionChecksum)
            {
                var set = new HashSet<Checksum>();

                var solutionCompilationChecksums = await assetService.GetAssetAsync<SolutionCompilationStateChecksums>(
                    AssetPathKind.SolutionCompilationStateChecksums, solutionChecksum, CancellationToken.None).ConfigureAwait(false);
                var solutionChecksums = await assetService.GetAssetAsync<SolutionStateChecksums>(
                    AssetPathKind.SolutionStateChecksums, solutionCompilationChecksums.SolutionState, CancellationToken.None).ConfigureAwait(false);

                solutionCompilationChecksums.AddAllTo(set);
                solutionChecksums.AddAllTo(set);

                foreach (var (projectChecksum, projectId) in solutionChecksums.Projects)
                {
                    var projectChecksums = await assetService.GetAssetAsync<ProjectStateChecksums>(
                        assetPath: projectId, projectChecksum, CancellationToken.None).ConfigureAwait(false);
                    projectChecksums.AddAllTo(set);

                    projectChecksums.Documents.AddAllTo(set);
                    projectChecksums.AdditionalDocuments.AddAllTo(set);
                    projectChecksums.AnalyzerConfigDocuments.AddAllTo(set);
                }

                return set;
            }

#else

            // have this to avoid error on async
            await Task.CompletedTask.ConfigureAwait(false);
#endif
        }

        private static void AddAllTo(DocumentStateChecksums documentStateChecksums, HashSet<Checksum> checksums)
        {
            checksums.AddIfNotNullChecksum(documentStateChecksums.Checksum);
            checksums.AddIfNotNullChecksum(documentStateChecksums.Info);
            checksums.AddIfNotNullChecksum(documentStateChecksums.Text);
        }

        /// <summary>
        /// create checksum to corresponding object map from solution this map should contain every parts of solution
        /// that can be used to re-create the solution back
        /// </summary>
        public static async Task<Dictionary<Checksum, object>> GetAssetMapAsync(this Solution solution, ProjectId? projectConeId, CancellationToken cancellationToken)
        {
            var map = new Dictionary<Checksum, object>();
            await solution.AppendAssetMapAsync(map, projectConeId, cancellationToken).ConfigureAwait(false);
            return map;
        }

        /// <summary>
        /// create checksum to corresponding object map from project this map should contain every parts of project that
        /// can be used to re-create the project back
        /// </summary>
        public static async Task<Dictionary<Checksum, object>> GetAssetMapAsync(this Project project, CancellationToken cancellationToken)
        {
            var map = new Dictionary<Checksum, object>();

            await project.AppendAssetMapAsync(map, cancellationToken).ConfigureAwait(false);

            // don't include the root checksum itself.  it's not one of the assets of the actual project.
            var projectStateChecksums = await project.State.GetStateChecksumsAsync(cancellationToken).ConfigureAwait(false);
            map.Remove(projectStateChecksums.Checksum);

            return map;
        }

        public static Task AppendAssetMapAsync(this Solution solution, Dictionary<Checksum, object> map, CancellationToken cancellationToken)
            => AppendAssetMapAsync(solution, map, projectId: null, cancellationToken);

        public static async Task AppendAssetMapAsync(
            this Solution solution, Dictionary<Checksum, object> map, ProjectId? projectId, CancellationToken cancellationToken)
        {
            var callback = static (Checksum checksum, object asset, Dictionary<Checksum, object> map) => { map[checksum] = asset; };

            if (projectId == null)
            {
                var compilationChecksums = await solution.CompilationState.GetStateChecksumsAsync(cancellationToken).ConfigureAwait(false);
                await compilationChecksums.FindAsync(solution.CompilationState, projectCone: null, AssetPath.FullLookupForTesting, Flatten(compilationChecksums), onAssetFound: callback, arg: map, cancellationToken: cancellationToken).ConfigureAwait(false);

                foreach (var frozenSourceGeneratedDocumentState in solution.CompilationState.FrozenSourceGeneratedDocumentStates?.States.Values ?? [])
                {
                    var documentChecksums = await frozenSourceGeneratedDocumentState.GetStateChecksumsAsync(cancellationToken).ConfigureAwait(false);
                    await compilationChecksums.FindAsync(solution.CompilationState, projectCone: null, AssetPath.FullLookupForTesting, Flatten(documentChecksums), onAssetFound: callback, arg: map, cancellationToken: cancellationToken).ConfigureAwait(false);
                }

                var solutionChecksums = await solution.CompilationState.SolutionState.GetStateChecksumsAsync(cancellationToken).ConfigureAwait(false);
                Contract.ThrowIfTrue(solutionChecksums.ProjectCone != null);
                await solutionChecksums.FindAsync(solution.CompilationState.SolutionState, projectCone: null, AssetPath.FullLookupForTesting, Flatten(solutionChecksums), onAssetFound: callback, arg: map, cancellationToken: cancellationToken).ConfigureAwait(false);

                foreach (var project in solution.Projects)
                    await project.AppendAssetMapAsync(map, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                var (compilationChecksums, projectCone) = await solution.CompilationState.GetStateChecksumsAsync(projectId, cancellationToken).ConfigureAwait(false);
                await compilationChecksums.FindAsync(solution.CompilationState, projectCone, AssetPath.SolutionAndProjectForTesting(projectId), Flatten(compilationChecksums), onAssetFound: callback, arg: map, cancellationToken: cancellationToken).ConfigureAwait(false);

                var solutionChecksums = await solution.CompilationState.SolutionState.GetStateChecksumsAsync(projectId, cancellationToken).ConfigureAwait(false);
                Contract.ThrowIfFalse(projectCone.Equals(solutionChecksums.ProjectCone));
                await solutionChecksums.FindAsync(solution.CompilationState.SolutionState, projectCone, AssetPath.SolutionAndProjectForTesting(projectId), Flatten(solutionChecksums), onAssetFound: callback, arg: map, cancellationToken: cancellationToken).ConfigureAwait(false);

                var project = solution.GetRequiredProject(projectId);
                await project.AppendAssetMapAsync(map, cancellationToken).ConfigureAwait(false);
                foreach (var dep in solution.GetProjectDependencyGraph().GetProjectsThatThisProjectTransitivelyDependsOn(projectId))
                    await solution.GetRequiredProject(dep).AppendAssetMapAsync(map, cancellationToken).ConfigureAwait(false);
            }
        }

        private static async Task AppendAssetMapAsync(this Project project, Dictionary<Checksum, object> map, CancellationToken cancellationToken)
        {
            if (!RemoteSupportedLanguages.IsSupported(project.Language))
            {
                return;
            }

            var callback = static (Checksum checksum, object asset, Dictionary<Checksum, object> map) => { map[checksum] = asset; };

            var projectChecksums = await project.State.GetStateChecksumsAsync(cancellationToken).ConfigureAwait(false);
            await projectChecksums.FindAsync(project.State, AssetPath.FullLookupForTesting, Flatten(projectChecksums), onAssetFound: callback, arg: map, cancellationToken: cancellationToken).ConfigureAwait(false);

            foreach (var document in project.Documents.Concat(project.AdditionalDocuments).Concat(project.AnalyzerConfigDocuments))
            {
                var documentChecksums = await document.State.GetStateChecksumsAsync(cancellationToken).ConfigureAwait(false);
                await documentChecksums.FindAsync(AssetPathKind.Documents, document.State, Flatten(documentChecksums), onAssetFound: callback, arg: map, cancellationToken: cancellationToken).ConfigureAwait(false);
            }
        }

        private static HashSet<Checksum> Flatten(SolutionCompilationStateChecksums checksums)
        {
            var set = new HashSet<Checksum>();
            checksums.AddAllTo(set);
            return set;
        }

        private static HashSet<Checksum> Flatten(SolutionStateChecksums checksums)
        {
            var set = new HashSet<Checksum>();
            checksums.AddAllTo(set);
            return set;
        }

        private static HashSet<Checksum> Flatten(ProjectStateChecksums checksums)
        {
            var set = new HashSet<Checksum>();
            checksums.AddAllTo(set);
            return set;
        }

        private static HashSet<Checksum> Flatten(DocumentStateChecksums checksums)
        {
            var set = new HashSet<Checksum>();
            AddAllTo(checksums, set);
            return set;
        }

        public static void AppendChecksums(this HashSet<Checksum> set, ChecksumCollection checksums)
        {
            set.Add(checksums.Checksum);

            foreach (var child in checksums.Children)
            {
                if (child != Checksum.Null)
                    set.Add(child);
            }
        }
    }
}
