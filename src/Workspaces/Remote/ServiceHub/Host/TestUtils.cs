// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Options;
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
        public static void RemoveChecksums(this Dictionary<Checksum, object> map, ChecksumWithChildren checksums)
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
            Solution incrementalSolutionBuilt)
        {
#if DEBUG
            var sb = new StringBuilder();
            var allChecksumsFromRequest = await GetAllChildrenChecksumsAsync(checksumFromRequest).ConfigureAwait(false);

            var assetMapFromNewSolution = await solutionFromScratch.GetAssetMapAsync(CancellationToken.None).ConfigureAwait(false);
            var assetMapFromIncrementalSolution = await incrementalSolutionBuilt.GetAssetMapAsync(CancellationToken.None).ConfigureAwait(false);

            // check 4 things
            // 1. first see if we create new solution from scratch, it works as expected (indicating a bug in incremental update)
            var mismatch1 = assetMapFromNewSolution.Where(p => !allChecksumsFromRequest.Contains(p.Key)).ToList();
            AppendMismatch(mismatch1, "assets only in new solutoin but not in the request", sb);

            // 2. second check what items is mismatching for incremental solution
            var mismatch2 = assetMapFromIncrementalSolution.Where(p => !allChecksumsFromRequest.Contains(p.Key)).ToList();
            AppendMismatch(mismatch2, "assets only in the incremental solution but not in the request", sb);

            // 3. check whether solution created from scratch and incremental one have any mismatch
            var mismatch3 = assetMapFromNewSolution.Where(p => !assetMapFromIncrementalSolution.ContainsKey(p.Key)).ToList();
            AppendMismatch(mismatch3, "assets only in new solution but not in incremental solution", sb);

            var mismatch4 = assetMapFromIncrementalSolution.Where(p => !assetMapFromNewSolution.ContainsKey(p.Key)).ToList();
            AppendMismatch(mismatch4, "assets only in incremental solution but not in new solution", sb);

            // 4. see what item is missing from request
            var mismatch5 = await GetAssetFromAssetServiceAsync(allChecksumsFromRequest.Except(assetMapFromNewSolution.Keys)).ConfigureAwait(false);
            AppendMismatch(mismatch5, "assets only in the request but not in new solution", sb);

            var mismatch6 = await GetAssetFromAssetServiceAsync(allChecksumsFromRequest.Except(assetMapFromIncrementalSolution.Keys)).ConfigureAwait(false);
            AppendMismatch(mismatch6, "assets only in the request but not in incremental solution", sb);

            AppendOptionSets();

            var result = sb.ToString();
            if (result.Length > 0)
            {
                Logger.Log(FunctionId.SolutionCreator_AssetDifferences, result);
                Debug.Fail("Differences detected in solution checksum: " + result);
            }

            return;

            void AppendOptionSets()
            {
                var seenChecksums = new HashSet<Checksum>();
                foreach (var list in new[] { mismatch1, mismatch2, mismatch3, mismatch4, mismatch5, mismatch6 })
                {
                    foreach (var (checksum, val) in list)
                    {
                        if (seenChecksums.Add(checksum) && val is SerializableOptionSet optionSet)
                        {
                            sb.AppendLine($"Checksum: {checksum}");
                            sb.AppendLine("Options:");
                            sb.AppendLine(optionSet.GetDebugString());
                            sb.AppendLine();
                        }
                    }
                }
            }

            static void AppendMismatch(List<KeyValuePair<Checksum, object>> items, string title, StringBuilder stringBuilder)
            {
                if (items.Count == 0)
                {
                    return;
                }

                stringBuilder.AppendLine(title);
                foreach (var kv in items)
                {
                    stringBuilder.AppendLine($"{kv.Key.ToString()}, {kv.Value.ToString()}");
                }

                stringBuilder.AppendLine();
            }

            async Task<List<KeyValuePair<Checksum, object>>> GetAssetFromAssetServiceAsync(IEnumerable<Checksum> checksums)
            {
                var items = new List<KeyValuePair<Checksum, object>>();

                foreach (var checksum in checksums)
                {
                    items.Add(new KeyValuePair<Checksum, object>(checksum, await assetService.GetAssetAsync<object>(checksum, CancellationToken.None).ConfigureAwait(false)));
                }

                return items;
            }

            async Task<HashSet<Checksum>> GetAllChildrenChecksumsAsync(Checksum solutionChecksum)
            {
                var set = new HashSet<Checksum>();

                var solutionChecksums = await assetService.GetAssetAsync<SolutionStateChecksums>(solutionChecksum, CancellationToken.None).ConfigureAwait(false);
                set.AppendChecksums(solutionChecksums);

                foreach (var projectChecksum in solutionChecksums.Projects)
                {
                    var projectChecksums = await assetService.GetAssetAsync<ProjectStateChecksums>(projectChecksum, CancellationToken.None).ConfigureAwait(false);
                    set.AppendChecksums(projectChecksums);

                    foreach (var documentChecksum in projectChecksums.Documents.Concat(projectChecksums.AdditionalDocuments).Concat(projectChecksums.AnalyzerConfigDocuments))
                    {
                        var documentChecksums = await assetService.GetAssetAsync<DocumentStateChecksums>(documentChecksum, CancellationToken.None).ConfigureAwait(false);
                        set.AppendChecksums(documentChecksums);
                    }
                }

                return set;
            }
#else

            // have this to avoid error on async
            await Task.CompletedTask.ConfigureAwait(false);
#endif
        }

        /// <summary>
        /// create checksum to correspoing object map from solution
        /// this map should contain every parts of solution that can be used to re-create the solution back
        /// </summary>
        public static async Task<Dictionary<Checksum, object>> GetAssetMapAsync(this Solution solution, CancellationToken cancellationToken)
        {
            var map = new Dictionary<Checksum, object>();
            await solution.AppendAssetMapAsync(map, cancellationToken).ConfigureAwait(false);
            return map;
        }

        /// <summary>
        /// create checksum to correspoing object map from project
        /// this map should contain every parts of project that can be used to re-create the project back
        /// </summary>
        public static async Task<Dictionary<Checksum, object>> GetAssetMapAsync(this Project project, CancellationToken cancellationToken)
        {
            var map = new Dictionary<Checksum, object>();

            await project.AppendAssetMapAsync(map, cancellationToken).ConfigureAwait(false);
            return map;
        }

        public static Task AppendAssetMapAsync(this Solution solution, Dictionary<Checksum, object> map, CancellationToken cancellationToken)
            => AppendAssetMapAsync(solution, map, projectId: null, cancellationToken);

        public static async Task AppendAssetMapAsync(
            this Solution solution, Dictionary<Checksum, object> map, ProjectId? projectId, CancellationToken cancellationToken)
        {
            if (projectId == null)
            {
                var solutionChecksums = await solution.State.GetStateChecksumsAsync(cancellationToken).ConfigureAwait(false);
                await solutionChecksums.FindAsync(solution.State, Flatten(solutionChecksums), map, cancellationToken).ConfigureAwait(false);

                foreach (var project in solution.Projects)
                    await project.AppendAssetMapAsync(map, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                var solutionChecksums = await solution.State.GetStateChecksumsAsync(projectId, cancellationToken).ConfigureAwait(false);
                await solutionChecksums.FindAsync(solution.State, Flatten(solutionChecksums), map, cancellationToken).ConfigureAwait(false);

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

            var projectChecksums = await project.State.GetStateChecksumsAsync(cancellationToken).ConfigureAwait(false);
            await projectChecksums.FindAsync(project.State, Flatten(projectChecksums), map, cancellationToken).ConfigureAwait(false);

            foreach (var document in project.Documents.Concat(project.AdditionalDocuments).Concat(project.AnalyzerConfigDocuments))
            {
                var documentChecksums = await document.State.GetStateChecksumsAsync(cancellationToken).ConfigureAwait(false);
                await documentChecksums.FindAsync(document.State, Flatten(documentChecksums), map, cancellationToken).ConfigureAwait(false);
            }
        }

        private static HashSet<Checksum> Flatten(ChecksumWithChildren checksums)
        {
            var set = new HashSet<Checksum>();
            set.AppendChecksums(checksums);

            return set;
        }

        public static void AppendChecksums(this HashSet<Checksum> set, ChecksumWithChildren checksums)
        {
            set.Add(checksums.Checksum);

            foreach (var child in checksums.Children)
            {
                if (child is Checksum checksum)
                {
                    if (checksum != Checksum.Null)
                        set.Add(checksum);
                }

                if (child is ChecksumCollection collection)
                {
                    set.AppendChecksums(collection);
                }
            }
        }
    }
}
