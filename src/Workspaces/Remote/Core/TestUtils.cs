// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Threading;
using Microsoft.CodeAnalysis.Serialization;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Remote.DebugUtil
{
    internal static class TestUtils
    {
        public static void RemoveChecksums(this Dictionary<Checksum, object> map, ChecksumWithChildren checksums)
        {
            map.Remove(checksums.Checksum);

            foreach (var child in checksums.Children)
            {
                if (child is Checksum checksum)
                {
                    map.Remove(checksum);
                }

                if (child is ChecksumCollection collection)
                {
                    foreach (var item in collection)
                    {
                        map.Remove(item);
                    }
                }
            }
        }

        public static Dictionary<Checksum, object> GetAssetMap(this Solution solution)
        {
            var map = new Dictionary<Checksum, object>();

            AppendAssetMap(solution, map);

            return map;
        }

        public static Dictionary<Checksum, object> GetAssetMap(this Project project)
        {
            var map = new Dictionary<Checksum, object>();

            AppendAssetMap(project, map);

            return map;
        }

        public static void AppendAssetMap(this Solution solution, Dictionary<Checksum, object> map)
        {
            Contract.ThrowIfFalse(solution.State.TryGetStateChecksums(out var solutionChecksums));

            solutionChecksums.Find(solution.State, Flatten(solutionChecksums), map, CancellationToken.None);

            foreach (var project in solution.Projects)
            {
                AppendAssetMap(project, map);
            }
        }

        private static void AppendAssetMap(Project project, Dictionary<Checksum, object> map)
        {
            if (!project.State.TryGetStateChecksums(out var projectChecksums))
            {
                Contract.Requires(!RemoteSupportedLanguages.IsSupported(project.Language));
                return;
            }

            projectChecksums.Find(project.State, Flatten(projectChecksums), map, CancellationToken.None);

            foreach (var document in project.Documents)
            {
                AppendAssetMap(document, map);
            }

            foreach (var document in project.AdditionalDocuments)
            {
                AppendAssetMap(document, map);
            }
        }

        private static void AppendAssetMap(TextDocument document, Dictionary<Checksum, object> map)
        {
            Contract.ThrowIfFalse(document.State.TryGetStateChecksums(out var documentChecksums));

            documentChecksums.Find(document.State, Flatten(documentChecksums), map, CancellationToken.None);

            // fix up due to source text can't be obtained synchronously in product code
            map[documentChecksums.Text] = document.State.GetTextSynchronously(CancellationToken.None);
        }

        private static HashSet<Checksum> Flatten(ChecksumWithChildren checksums)
        {
            var set = new HashSet<Checksum>();
            set.Add(checksums.Checksum);

            foreach (var child in checksums.Children)
            {
                if (child is Checksum checksum)
                {
                    set.Add(checksum);
                }

                if (child is ChecksumCollection collection)
                {
                    foreach (var item in collection)
                    {
                        set.Add(item);
                    }
                }
            }

            return set;
        }
    }
}
