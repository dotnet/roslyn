// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.SolutionCrawler
{
    internal static class InternalSolutionCrawlerOptions
    {
        private const string LocalRegistryPath = @"Roslyn\Internal\SolutionCrawler\";

        public static readonly Option2<bool> SolutionCrawler = new(nameof(InternalSolutionCrawlerOptions), "Solution Crawler", defaultValue: true,
            storageLocation: new LocalUserProfileStorageLocation(LocalRegistryPath + "Solution Crawler"));

        public static readonly Option2<bool> DirectDependencyPropagationOnly = new(nameof(InternalSolutionCrawlerOptions), "Project propagation only on direct dependency", defaultValue: true,
            storageLocation: new LocalUserProfileStorageLocation(LocalRegistryPath + "Project propagation only on direct dependency"));

        public static readonly TimeSpan ActiveFileWorkerBackOffTimeSpan = TimeSpan.FromMilliseconds(100);
        public static readonly TimeSpan AllFilesWorkerBackOffTimeSpan = TimeSpan.FromMilliseconds(1500);
        public static readonly TimeSpan EntireProjectWorkerBackOffTimeSpan = TimeSpan.FromMilliseconds(5000);
        public static readonly TimeSpan SemanticChangeBackOffTimeSpan = TimeSpan.FromMilliseconds(100);
        public static readonly TimeSpan ProjectPropagationBackOffTimeSpan = TimeSpan.FromMilliseconds(500);
        public static readonly TimeSpan PreviewBackOffTimeSpan = TimeSpan.FromMilliseconds(500);
    }
}
