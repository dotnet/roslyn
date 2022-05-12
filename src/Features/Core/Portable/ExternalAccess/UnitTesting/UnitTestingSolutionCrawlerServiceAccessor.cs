// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.ExternalAccess.UnitTesting.Api;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.SolutionCrawler;

namespace Microsoft.CodeAnalysis.ExternalAccess.UnitTesting
{
    [Obsolete]
    internal sealed class UnitTestingSolutionCrawlerServiceAccessor : IUnitTestingSolutionCrawlerServiceAccessor
    {
        private readonly ISolutionCrawlerRegistrationService _registrationService;
        private readonly ISolutionCrawlerService _solutionCrawlerService;
        private readonly IGlobalOptionService _globalOptions;

        private UnitTestingIncrementalAnalyzerProvider _analyzerProvider;

        [Obsolete(MefConstruction.FactoryMethodMessage, error: true)]
        public UnitTestingSolutionCrawlerServiceAccessor(
            ISolutionCrawlerRegistrationService registrationService,
            ISolutionCrawlerService solutionCrawlerService,
            IGlobalOptionService globalOptions)
        {
            _registrationService = registrationService;
            _solutionCrawlerService = solutionCrawlerService;
            _globalOptions = globalOptions;
        }

        public void AddAnalyzerProvider(IUnitTestingIncrementalAnalyzerProviderImplementation provider, UnitTestingIncrementalAnalyzerProviderMetadataWrapper metadata)
        {
            if (_analyzerProvider != null)
            {
                // NOTE: We expect the analyzer to be a singleton, therefore this method should be called just once.
                throw new InvalidOperationException();
            }

            _analyzerProvider = new UnitTestingIncrementalAnalyzerProvider(provider);
            _registrationService.AddAnalyzerProvider(_analyzerProvider, metadata.UnderlyingObject);
        }

        // NOTE: For the Reanalyze method to work correctly, the analyzer passed into the Reanalyze method,
        //       must be the same as created when we call the AddAnalyzerProvider method.
        //       As such the analyzer provider instance caches a single instance of the analyzer.
        public void Reanalyze(Workspace workspace, IEnumerable<ProjectId> projectIds = null, IEnumerable<DocumentId> documentIds = null, bool highPriority = false)
        {
            // NOTE: this method must be called after AddAnalyzerProvider was called previously.
            if (_analyzerProvider == null)
            {
                throw new InvalidOperationException();
            }

            _solutionCrawlerService.Reanalyze(workspace, _analyzerProvider.CreateIncrementalAnalyzer(workspace), projectIds, documentIds, highPriority);
        }

        public void Register(Workspace workspace)
        {
            if (_globalOptions.GetOption(SolutionCrawlerRegistrationService.EnableSolutionCrawler))
            {
                _registrationService.Register(workspace);
            }
        }
    }
}
