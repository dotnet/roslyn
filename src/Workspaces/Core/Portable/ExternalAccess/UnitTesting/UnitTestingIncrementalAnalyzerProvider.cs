// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.CodeAnalysis.ExternalAccess.UnitTesting.Api;
using Microsoft.CodeAnalysis.SolutionCrawler;

namespace Microsoft.CodeAnalysis.ExternalAccess.UnitTesting
{
    internal class UnitTestingIncrementalAnalyzerProvider : IIncrementalAnalyzerProvider
    {
        private readonly IUnitTestingIncrementalAnalyzerProviderImplementation _incrementalAnalyzerProvider;
        private IIncrementalAnalyzer _analyzer;
        private Workspace _workspace;

        public UnitTestingIncrementalAnalyzerProvider(IUnitTestingIncrementalAnalyzerProviderImplementation incrementalAnalyzerProvider)
            => _incrementalAnalyzerProvider = incrementalAnalyzerProvider;

        public IIncrementalAnalyzer CreateIncrementalAnalyzer(Workspace workspace)
        {
            if (_workspace != null && _workspace != workspace)
            {
                throw new ArgumentException(nameof(workspace));
            }

            if (_analyzer == null)
            {
                _analyzer = new UnitTestingIncrementalAnalyzer(_incrementalAnalyzerProvider.CreateIncrementalAnalyzer(workspace));
                _workspace = workspace;
            }

            return _analyzer;
        }
    }
}
