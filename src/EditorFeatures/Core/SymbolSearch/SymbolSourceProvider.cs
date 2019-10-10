using System;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Language.Intellisense.SymbolSearch;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.CodeAnalysis.Editor.SymbolSearch
{
    [Export(typeof(ISymbolSourceProvider))]
    [ContentType(ContentTypeNames.RoslynContentType)]
    [Name("Roslyn symbol source")]
    class SymbolSourceProvider : ISymbolSourceProvider
    {
        ImmutableArray<ISymbolSource> ExportedSources;

        public ImmutableArray<ISymbolSource> GetOrCreate(ITextBuffer buffer)
        {
            if (ExportedSources == default)
            {
                ExportedSources = ImmutableArray.Create<ISymbolSource>(new SymbolSource(this));
            }
            return ExportedSources;
        }
    }
}
