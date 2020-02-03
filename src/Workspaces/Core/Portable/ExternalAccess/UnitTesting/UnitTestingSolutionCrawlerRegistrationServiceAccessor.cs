// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.CodeAnalysis.ExternalAccess.UnitTesting.Api;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.SolutionCrawler;

namespace Microsoft.CodeAnalysis.ExternalAccess.UnitTesting
{
    internal sealed class UnitTestingSolutionCrawlerRegistrationServiceAccessor
        : IUnitTestingSolutionCrawlerRegistrationServiceAccessor
    {
        private readonly ISolutionCrawlerRegistrationService _implementation;

        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public UnitTestingSolutionCrawlerRegistrationServiceAccessor(ISolutionCrawlerRegistrationService implementation)
            => _implementation = implementation;

        public void AddAnalyzerProvider(IUnitTestingIncrementalAnalyzerProviderImplementation provider, UnitTestingIncrementalAnalyzerProviderMetadataWrapper metadata)
            => _implementation.AddAnalyzerProvider(new UnitTestingIncrementalAnalyzerProvider(provider), metadata.UnderlyingObject);

        public void Register(Workspace workspace)
            => _implementation.Register(workspace);
    }
}
