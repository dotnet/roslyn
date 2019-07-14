// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Remote.Shared;
using Microsoft.CodeAnalysis.Serialization;

namespace Microsoft.CodeAnalysis.Remote.DebugUtil
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
            AssetService assetService,
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

            var result = sb.ToString();
            if (result.Length > 0)
            {
                Logger.Log(FunctionId.SolutionCreator_AssetDifferences, result);
                Debug.Fail("Differences detected in solution checksum: " + result);
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
    }
}
