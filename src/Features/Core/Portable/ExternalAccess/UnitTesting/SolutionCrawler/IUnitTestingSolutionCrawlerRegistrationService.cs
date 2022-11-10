// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Host;

namespace Microsoft.CodeAnalysis.ExternalAccess.UnitTesting.SolutionCrawler
{
    /// <summary>
    /// Register a solution crawler for a particular workspace
    /// </summary>
    internal interface IUnitTestingSolutionCrawlerRegistrationService : IWorkspaceService
    {
        IUnitTestingWorkCoordinator Register(Solution solution);

#if false // Not used in unit testing crawling
        void Unregister(Workspace workspace, bool blockingShutdown = false);
#endif

        void AddAnalyzerProvider(IUnitTestingIncrementalAnalyzerProvider provider, UnitTestingIncrementalAnalyzerProviderMetadata metadata);

        bool HasRegisteredAnalyzerProviders { get; }
    }
}
