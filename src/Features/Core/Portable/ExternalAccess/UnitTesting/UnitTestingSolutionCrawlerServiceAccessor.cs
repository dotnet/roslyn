// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.ExternalAccess.UnitTesting.Api;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.SolutionCrawler;

namespace Microsoft.CodeAnalysis.ExternalAccess.UnitTesting
{
    internal sealed class UnitTestingSolutionCrawlerServiceAccessor : IUnitTestingSolutionCrawlerServiceAccessor
    {
        private readonly ISolutionCrawlerRegistrationService _registrationService;
        private readonly ISolutionCrawlerService _solutionCrawlerService;

        private UnitTestingIncrementalAnalyzerProvider _analyzerProvider;

        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public UnitTestingSolutionCrawlerServiceAccessor(
            ISolutionCrawlerRegistrationService registrationService,
            ISolutionCrawlerService solutionCrawlerService)
        {
            _registrationService = registrationService;
            _solutionCrawlerService = solutionCrawlerService;
        }

        public void AddAnalyzerProvider(IUnitTestingIncrementalAnalyzerProviderImplementation provider, UnitTestingIncrementalAnalyzerProviderMetadataWrapper metadata)
        {
            if (_analyzerProvider != null)
            {
                // NOTE: We should call this method just once.
                throw new ArgumentException(nameof(provider));
            }

            _analyzerProvider = new UnitTestingIncrementalAnalyzerProvider(provider);
            _registrationService.AddAnalyzerProvider(_analyzerProvider, metadata.UnderlyingObject);
        }

        public void Reanalyze(Workspace workspace, IEnumerable<ProjectId> projectIds = null, IEnumerable<DocumentId> documentIds = null, bool highPriority = false)
        {
            _solutionCrawlerService.Reanalyze(workspace, _analyzerProvider.CreateIncrementalAnalyzer(workspace), projectIds, documentIds, highPriority);
        }

        public void Register(Workspace workspace)
            => _registrationService.Register(workspace);
    }
}
