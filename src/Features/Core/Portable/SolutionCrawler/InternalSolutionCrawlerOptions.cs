// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.SolutionCrawler
{
    internal static class InternalSolutionCrawlerOptions
    {
        private const string LocalRegistryPath = @"Roslyn\Internal\SolutionCrawler\";

        public static readonly Option2<bool> SolutionCrawler = new Option2<bool>(nameof(InternalSolutionCrawlerOptions), "Solution Crawler", defaultValue: true,
            storageLocations: new LocalUserProfileStorageLocation(LocalRegistryPath + "Solution Crawler"));

        public static readonly Option2<bool> DirectDependencyPropagationOnly = new Option2<bool>(nameof(InternalSolutionCrawlerOptions), "Project propagation only on direct dependency", defaultValue: true,
            storageLocations: new LocalUserProfileStorageLocation(LocalRegistryPath + "Project propagation only on direct dependency"));

        public static readonly Option2<int> ActiveFileWorkerBackOffTimeSpanInMS = new Option2<int>(nameof(InternalSolutionCrawlerOptions), "Active file worker backoff timespan in ms", defaultValue: 100,
            storageLocations: new LocalUserProfileStorageLocation(LocalRegistryPath + "Active file worker backoff timespan in ms"));

        public static readonly Option2<int> AllFilesWorkerBackOffTimeSpanInMS = new Option2<int>(nameof(InternalSolutionCrawlerOptions), "All files worker backoff timespan in ms", defaultValue: 1500,
            storageLocations: new LocalUserProfileStorageLocation(LocalRegistryPath + "All files worker backoff timespan in ms"));

        public static readonly Option2<int> EntireProjectWorkerBackOffTimeSpanInMS = new Option2<int>(nameof(InternalSolutionCrawlerOptions), "Entire project analysis worker backoff timespan in ms", defaultValue: 5000,
            storageLocations: new LocalUserProfileStorageLocation(LocalRegistryPath + "Entire project analysis worker backoff timespan in ms"));

        public static readonly Option2<int> SemanticChangeBackOffTimeSpanInMS = new Option2<int>(nameof(InternalSolutionCrawlerOptions), "Semantic change backoff timespan in ms", defaultValue: 100,
            storageLocations: new LocalUserProfileStorageLocation(LocalRegistryPath + "Semantic change backoff timespan in ms"));

        public static readonly Option2<int> ProjectPropagationBackOffTimeSpanInMS = new Option2<int>(nameof(InternalSolutionCrawlerOptions), "Project propagation backoff timespan in ms", defaultValue: 500,
            storageLocations: new LocalUserProfileStorageLocation(LocalRegistryPath + "Project propagation backoff timespan in ms"));

        public static readonly Option2<int> PreviewBackOffTimeSpanInMS = new Option2<int>(nameof(InternalSolutionCrawlerOptions), "Preview backoff timespan in ms", defaultValue: 500,
            storageLocations: new LocalUserProfileStorageLocation(LocalRegistryPath + "Preview backoff timespan in ms"));
    }
}
