// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Host;

namespace Microsoft.CodeAnalysis.SolutionCrawler
{
    /// <summary>
    /// Register a solution crawler for a particular workspace
    /// </summary>
    internal interface ISolutionCrawlerRegistrationService : IWorkspaceService
    {
        void Register(Workspace workspace);

        /// <summary>
        /// Unregisters solution crawler for given <paramref name="workspace"/>.
        /// No-op if never registered or already unregistered.
        /// </summary>
        void Unregister(Workspace workspace, bool blockingShutdown = false);

        void AddAnalyzerProvider(IIncrementalAnalyzerProvider provider, IncrementalAnalyzerProviderMetadata metadata);
    }
}
