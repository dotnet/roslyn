// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Serialization;

namespace Microsoft.CodeAnalysis.Remote.Shared
{
    internal static class Extensions
    {
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

        public static async Task AppendAssetMapAsync(this Solution solution, Dictionary<Checksum, object> map, CancellationToken cancellationToken)
        {
            var solutionChecksums = await solution.State.GetStateChecksumsAsync(cancellationToken).ConfigureAwait(false);

            solutionChecksums.Find(solution.State, Flatten(solutionChecksums), map, cancellationToken);

            foreach (var project in solution.Projects)
            {
                await project.AppendAssetMapAsync(map, cancellationToken).ConfigureAwait(false);
            }
        }

        private static async Task AppendAssetMapAsync(this Project project, Dictionary<Checksum, object> map, CancellationToken cancellationToken)
        {
            if (!RemoteSupportedLanguages.IsSupported(project.Language))
            {
                return;
            }

            var projectChecksums = await project.State.GetStateChecksumsAsync(cancellationToken).ConfigureAwait(false);

            projectChecksums.Find(project.State, Flatten(projectChecksums), map, cancellationToken);

            foreach (var document in project.Documents.Concat(project.AdditionalDocuments).Concat(project.AnalyzerConfigDocuments))
            {
                await document.AppendAssetMapAsync(map, cancellationToken).ConfigureAwait(false);
            }
        }

        private static async Task AppendAssetMapAsync(this TextDocument document, Dictionary<Checksum, object> map, CancellationToken cancellationToken)
        {
            var documentChecksums = await document.State.GetStateChecksumsAsync(cancellationToken).ConfigureAwait(false);

            documentChecksums.Find(document.State, Flatten(documentChecksums), map, CancellationToken.None);

            map[documentChecksums.Text] = await document.State.GetTextAsync(cancellationToken).ConfigureAwait(false);
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
