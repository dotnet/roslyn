// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.CodeAnalysis.ExternalAccess.UnitTesting.Api;
using Microsoft.CodeAnalysis.SolutionCrawler;

namespace Microsoft.CodeAnalysis.ExternalAccess.UnitTesting
{
    [Obsolete]
    internal class UnitTestingIncrementalAnalyzerProvider : IIncrementalAnalyzerProvider
    {
        private readonly IUnitTestingIncrementalAnalyzerProviderImplementation _incrementalAnalyzerProvider;
        private IIncrementalAnalyzer _analyzer;

        public UnitTestingIncrementalAnalyzerProvider(IUnitTestingIncrementalAnalyzerProviderImplementation incrementalAnalyzerProvider)
            => _incrementalAnalyzerProvider = incrementalAnalyzerProvider;

        public IIncrementalAnalyzer CreateIncrementalAnalyzer(Workspace workspace)
        {
            // NOTE: We're currently expecting the analyzer to be singleton, so that
            //       analyzers returned when calling this method twice would pass a reference equality check.
            //       One instance should be created by SolutionCrawler, another one by us, when calling the
            //       UnitTestingSolutionCrawlerServiceAccessor.Reanalyze method.
            if (_analyzer == null)
            {
                _analyzer = new UnitTestingIncrementalAnalyzer(_incrementalAnalyzerProvider.CreateIncrementalAnalyzer());
            }

            return _analyzer;
        }
    }
}
