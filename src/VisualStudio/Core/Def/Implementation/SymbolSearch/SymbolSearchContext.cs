using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.FindUsages;
using Microsoft.VisualStudio.Language.Intellisense.SymbolSearch;

namespace Microsoft.VisualStudio.LanguageServices.SymbolSearch
{
    class SymbolSearchContext : FindUsagesContext
    {
        private RoslynSymbolSource SymbolSource;
        private IStreamingSymbolSearchSink SymbolSink;

        public SymbolSearchContext(RoslynSymbolSource symbolSource, IStreamingSymbolSearchSink sink)
        {
            this.SymbolSource = symbolSource;
            this.SymbolSink = sink;
        }

        public override async Task OnDefinitionFoundAsync(DefinitionItem definition)
        {
            return;

            var token = default(CancellationToken);
            if (definition.SourceSpans.Length == 1)
            {
                var result = await RoslynSymbolResult.MakeAsync(this.SymbolSource, definition, definition.SourceSpans[0], token)
                    .ConfigureAwait(false);
                this.SymbolSink.Add(result);
            }
            else if (definition.SourceSpans.Length == 0)
            {
                // that's interesting. let's investigate this!
            }
            else
            {
                var builder = ImmutableArray.CreateBuilder<SymbolSearchResult>(definition.SourceSpans.Length);
                foreach (var sourceSpan in definition.SourceSpans)
                {
                    var result = await RoslynSymbolResult.MakeAsync(this.SymbolSource, definition, sourceSpan, token)
                        .ConfigureAwait(false);
                    builder.Add(result);
                }
                this.SymbolSink.AddRange(builder.ToImmutable());
            }
            return;
        }

        public override async Task OnReferenceFoundAsync(SourceReferenceItem reference)
        {
            var token = default(CancellationToken);
            var result = await RoslynSymbolResult.MakeAsync(this.SymbolSource, reference, reference.SourceSpan, token)
                .ConfigureAwait(false);
            this.SymbolSink.Add(result);
            return;
        }
    }
}
