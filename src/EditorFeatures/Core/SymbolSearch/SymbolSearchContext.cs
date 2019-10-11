using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.FindUsages;
using Microsoft.VisualStudio.Language.Intellisense.SymbolSearch;

namespace Microsoft.CodeAnalysis.Editor.SymbolSearch
{
    class SymbolSearchContext : FindUsagesContext
    {
        private IStreamingSymbolSearchSink SymbolSink;

        public SymbolSearchContext(IStreamingSymbolSearchSink sink)
        {
            SymbolSink = sink;
        }

        public override Task OnDefinitionFoundAsync(DefinitionItem definition)
        {
            var result = new RoslynSymbolResult(definition);
            SymbolSink.Add(result);
            return Task.CompletedTask;
        }

        public override Task OnReferenceFoundAsync(SourceReferenceItem reference)
        {
            var result = new RoslynSymbolResult(reference);
            SymbolSink.Add(result);
            return Task.CompletedTask;
        }
    }
}
