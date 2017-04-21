// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Remote;
using Microsoft.CodeAnalysis.Serialization;
using Xunit;

namespace Roslyn.VisualStudio.Next.UnitTests.Remote
{
    internal static class TestUtils
    {
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
            SolutionStateChecksums solutionChecksums;
            Assert.True(solution.State.TryGetStateChecksums(out solutionChecksums));

            solutionChecksums.Find(solution.State, Flatten(solutionChecksums), map, CancellationToken.None);

            foreach (var project in solution.Projects)
            {
                AppendAssetMap(project, map);
            }
        }

        private static void AppendAssetMap(Project project, Dictionary<Checksum, object> map)
        {
            ProjectStateChecksums projectChecksums;
            if (!project.State.TryGetStateChecksums(out projectChecksums))
            {
                Assert.False(RemoteSupportedLanguages.IsSupported(project.Language));
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
            DocumentStateChecksums documentChecksums;
            Assert.True(document.State.TryGetStateChecksums(out documentChecksums));

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
                var checksum = child as Checksum;
                if (checksum != null)
                {
                    set.Add(checksum);
                }

                var collection = child as ChecksumCollection;
                if (collection != null)
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
