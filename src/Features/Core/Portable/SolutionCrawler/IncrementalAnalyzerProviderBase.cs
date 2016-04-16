// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.CodeAnalysis.SolutionCrawler
{
    internal class IncrementalAnalyzerProviderBase : IIncrementalAnalyzerProvider
    {
        private readonly List<Lazy<IPerLanguageIncrementalAnalyzerProvider, PerLanguageIncrementalAnalyzerProviderMetadata>> _providers;

        protected IncrementalAnalyzerProviderBase(
            string name, IEnumerable<Lazy<IPerLanguageIncrementalAnalyzerProvider, PerLanguageIncrementalAnalyzerProviderMetadata>> providers)
        {
            _providers = providers.Where(p => p.Metadata.Name == name).ToList();
        }

        public virtual IIncrementalAnalyzer CreateIncrementalAnalyzer(Workspace workspace)
        {
            return new AggregateIncrementalAnalyzer(workspace, this, _providers);
        }
    }
}
