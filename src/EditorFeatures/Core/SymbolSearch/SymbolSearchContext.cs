using System;
using System.CodeDom;
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
        private RoslynSymbolSource SymbolSource;
        private IStreamingSymbolSearchSink SymbolSink;

        public SymbolSearchContext(RoslynSymbolSource symbolSource, IStreamingSymbolSearchSink sink)
        {
            this.SymbolSource = symbolSource;
            this.SymbolSink = sink;
        }

        public override Task OnDefinitionFoundAsync(DefinitionItem definition)
        {
            var result = new RoslynSymbolResult(this.SymbolSource, definition);
            this.SymbolSink.Add(result);
            return Task.CompletedTask;
        }

        public override Task OnReferenceFoundAsync(SourceReferenceItem reference)
        {
            var result = new RoslynSymbolResult(this.SymbolSource, reference);
            this.SymbolSink.Add(result);
            return Task.CompletedTask;
        }
    }
}
