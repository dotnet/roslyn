// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.SolutionCrawler;

namespace Microsoft.CodeAnalysis.ExternalAccess.UnitTesting.Api
{
    internal sealed class UnitTestingIncrementalAnalyzerProvider : IIncrementalAnalyzerProvider
    {
        private readonly IUnitTestingIncrementalAnalyzerProviderImplementation _incrementalAnalyzerProvider;
        private readonly Workspace _workspace;

        private IIncrementalAnalyzer? _lazyAnalyzer;

        internal UnitTestingIncrementalAnalyzerProvider(Workspace workspace, IUnitTestingIncrementalAnalyzerProviderImplementation incrementalAnalyzerProvider)
        {
            _workspace = workspace;
            _incrementalAnalyzerProvider = incrementalAnalyzerProvider;
        }

        // NOTE: We're currently expecting the analyzer to be singleton, so that
        //       analyzers returned when calling this method twice would pass a reference equality check.
        //       One instance should be created by SolutionCrawler, another one by us, when calling the
        //       UnitTestingSolutionCrawlerServiceAccessor.Reanalyze method.
        IIncrementalAnalyzer IIncrementalAnalyzerProvider.CreateIncrementalAnalyzer(Workspace workspace)
            => _lazyAnalyzer ??= new UnitTestingIncrementalAnalyzer(_incrementalAnalyzerProvider.CreateIncrementalAnalyzer());

        public void Reanalyze()
        {
            var solutionCrawlerService = _workspace.Services.GetService<ISolutionCrawlerService>();
            if (solutionCrawlerService != null)
            {
                var analyzer = ((IIncrementalAnalyzerProvider)this).CreateIncrementalAnalyzer(_workspace)!;
                solutionCrawlerService.Reanalyze(_workspace, analyzer, projectIds: null, documentIds: null, highPriority: false);
            }
        }

        public static UnitTestingIncrementalAnalyzerProvider? TryRegister(Workspace workspace, string analyzerName, IUnitTestingIncrementalAnalyzerProviderImplementation provider)
        {
            var solutionCrawlerRegistrationService = workspace.Services.GetService<ISolutionCrawlerRegistrationService>();
            if (solutionCrawlerRegistrationService == null)
            {
                return null;
            }

            var analyzerProvider = new UnitTestingIncrementalAnalyzerProvider(workspace, provider);

            var metadata = new IncrementalAnalyzerProviderMetadata(
                analyzerName,
                highPriorityForActiveFile: false,
                new[] { workspace.Kind });

            solutionCrawlerRegistrationService.AddAnalyzerProvider(analyzerProvider, metadata);
            return analyzerProvider;
        }
    }
}
