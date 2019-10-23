using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.FindUsages;
using Microsoft.VisualStudio.Language.Intellisense.SymbolSearch;

namespace Microsoft.VisualStudio.LanguageServices.SymbolSearch
{
    class SymbolSearchContext : FindUsagesContext
    {
        private readonly RoslynSymbolSource SymbolSource;
        private readonly IStreamingSymbolSearchSink SymbolSink;
        private readonly CancellationToken CancellationToken;

        public SymbolSearchContext(RoslynSymbolSource symbolSource, IStreamingSymbolSearchSink sink)
        {
            this.SymbolSource = symbolSource;
            this.SymbolSink = sink;
            this.CancellationToken = sink.CancellationToken;
        }

        public override async Task OnDefinitionFoundAsync(DefinitionItem definition)
        {
            if (definition.SourceSpans.Length == 1)
            {
                var result = await RoslynSymbolResult.MakeAsync(this.SymbolSource, definition, definition.SourceSpans[0], CancellationToken)
                    .ConfigureAwait(false);
                this.SymbolSink.Add(result);
            }
            else if (definition.SourceSpans.Length == 0)
            {
                // TODO: that's interesting. investigate this!
                System.Diagnostics.Debugger.Break();
            }
            else
            {
                var builder = ImmutableArray.CreateBuilder<SymbolSearchResult>(definition.SourceSpans.Length);
                foreach (var sourceSpan in definition.SourceSpans)
                {
                    var result = await RoslynSymbolResult.MakeAsync(this.SymbolSource, definition, sourceSpan, CancellationToken)
                        .ConfigureAwait(false);
                    builder.Add(result);
                }
                this.SymbolSink.AddRange(builder.ToImmutable());
            }
            return;
        }

        public override async Task OnReferenceFoundAsync(SourceReferenceItem reference)
        {
            var result = await RoslynSymbolResult.MakeAsync(this.SymbolSource, reference, reference.SourceSpan, CancellationToken)
                .ConfigureAwait(false);
            this.SymbolSink.Add(result);
            return;
        }
    }
}
