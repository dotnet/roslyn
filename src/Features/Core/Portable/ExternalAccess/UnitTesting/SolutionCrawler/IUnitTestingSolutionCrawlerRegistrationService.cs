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

        void AddAnalyzerProvider(IUnitTestingIncrementalAnalyzerProvider provider, UnitTestingIncrementalAnalyzerProviderMetadata metadata);

        bool HasRegisteredAnalyzerProviders { get; }
    }
}
