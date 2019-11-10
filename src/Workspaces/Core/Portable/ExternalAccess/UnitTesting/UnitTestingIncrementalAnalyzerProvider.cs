// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.ExternalAccess.UnitTesting.Api;
using Microsoft.CodeAnalysis.SolutionCrawler;

namespace Microsoft.CodeAnalysis.ExternalAccess.UnitTesting
{
    internal class UnitTestingIncrementalAnalyzerProvider : IIncrementalAnalyzerProvider
    {
        private readonly IUnitTestingIncrementalAnalyzerProviderImplementation _incrementalAnalyzerProvider;

        public UnitTestingIncrementalAnalyzerProvider(IUnitTestingIncrementalAnalyzerProviderImplementation incrementalAnalyzerProvider)
            => _incrementalAnalyzerProvider = incrementalAnalyzerProvider;

        public IIncrementalAnalyzer CreateIncrementalAnalyzer(Workspace workspace)
            => new UnitTestingIncrementalAnalyzer(_incrementalAnalyzerProvider.CreateIncrementalAnalyzer(workspace));
    }
}
