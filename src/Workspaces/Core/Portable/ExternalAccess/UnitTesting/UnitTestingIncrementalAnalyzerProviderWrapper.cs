// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.ExternalAccess.UnitTesting.Api;
using Microsoft.CodeAnalysis.SolutionCrawler;

namespace Microsoft.CodeAnalysis.ExternalAccess.UnitTesting
{
    internal class UnitTestingIncrementalAnalyzerProviderWrapper : IIncrementalAnalyzerProvider
    {
        private readonly IUnitTestingIncrementalAnalyzerProvider _incrementalAnalyzerProvider;

        public UnitTestingIncrementalAnalyzerProviderWrapper(IUnitTestingIncrementalAnalyzerProvider incrementalAnalyzerProvider)
            => _incrementalAnalyzerProvider = incrementalAnalyzerProvider;

        public IIncrementalAnalyzer CreateIncrementalAnalyzer(Workspace workspace)
            => new UnitTestingIncrementalAnalyzerWrapper(_incrementalAnalyzerProvider.CreateIncrementalAnalyzer(workspace));
    }
}
