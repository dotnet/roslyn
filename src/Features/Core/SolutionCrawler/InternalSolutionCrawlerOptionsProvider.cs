using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Options.Providers;

namespace Microsoft.CodeAnalysis.SolutionCrawler
{
    [ExportOptionProvider, Shared]
    internal class InternalSolutionCrawlerOptionsProvider : IOptionProvider
    {
        private readonly IEnumerable<IOption> _options = new List<IOption>
            {
                InternalSolutionCrawlerOptions.SolutionCrawler,
                InternalSolutionCrawlerOptions.ActiveFileWorkerBackOffTimeSpanInMS,
                InternalSolutionCrawlerOptions.AllFilesWorkerBackOffTimeSpanInMS,
                InternalSolutionCrawlerOptions.EntireProjectWorkerBackOffTimeSpanInMS,
                InternalSolutionCrawlerOptions.SemanticChangeBackOffTimeSpanInMS,
                InternalSolutionCrawlerOptions.ProjectPropagationBackOffTimeSpanInMS,
                InternalSolutionCrawlerOptions.PreviewBackOffTimeSpanInMS
            }.ToImmutableArray();

        public IEnumerable<IOption> GetOptions()
        {
            return _options;
        }
    }
}
