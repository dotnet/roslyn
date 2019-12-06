// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.ExternalAccess.UnitTesting.Api;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.SolutionCrawler;

namespace Microsoft.CodeAnalysis.ExternalAccess.UnitTesting
{
    internal sealed class UnitTestingSolutionCrawlerServiceAccessor : IUnitTestingSolutionCrawlerServiceAccessor
    {
        private readonly ISolutionCrawlerService _implementation;

        [Obsolete(MefConstruction.FactoryMethodMessage, error: true)]
        public UnitTestingSolutionCrawlerServiceAccessor(ISolutionCrawlerService implementation)
            => _implementation = implementation;

        public void Reanalyze(Workspace workspace, IUnitTestingIncrementalAnalyzerImplementation analyzer, IEnumerable<ProjectId> projectIds = null, IEnumerable<DocumentId> documentIds = null, bool highPriority = false)
            => _implementation.Reanalyze(workspace, new UnitTestingIncrementalAnalyzer(analyzer), projectIds, documentIds, highPriority);
    }
}
