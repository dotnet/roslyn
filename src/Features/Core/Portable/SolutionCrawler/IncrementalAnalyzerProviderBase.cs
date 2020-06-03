// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

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
            => new AggregateIncrementalAnalyzer(workspace, this, _providers);
    }
}
