// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Serialization;
using Xunit;

namespace Roslyn.VisualStudio.Next.UnitTests.Remote
{
    internal static class ChecksumUtils
    {
        public static Dictionary<Checksum, object> GetAssetMap(this Solution solution)
        {
            var map = new Dictionary<Checksum, object>();

            SolutionStateChecksums solutionChecksums;
            Assert.True(solution.State.TryGetStateChecksums(out solutionChecksums));

            solutionChecksums.Find(solution.State, Flatten(solutionChecksums), map, CancellationToken.None);

            foreach (var project in solution.Projects)
            {
                ProjectStateChecksums projectChecksums;
                Assert.True(project.State.TryGetStateChecksums(out projectChecksums));

                projectChecksums.Find(project.State, Flatten(projectChecksums), map, CancellationToken.None);

                foreach (var document in project.Documents)
                {
                    DocumentStateChecksums documentChecksums;
                    Assert.True(document.State.TryGetStateChecksums(out documentChecksums));

                    documentChecksums.Find(document.State, Flatten(documentChecksums), map, CancellationToken.None);

                    // fix up due to source text can't be obtained synchronously in product code
                    map[documentChecksums.Text] = document.State.GetTextSynchronously(CancellationToken.None);
                }
            }

            return map;
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
