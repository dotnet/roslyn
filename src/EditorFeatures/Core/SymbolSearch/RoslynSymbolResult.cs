using Microsoft.CodeAnalysis.FindUsages;
using Microsoft.VisualStudio.Language.Intellisense.SymbolSearch;
using Microsoft.VisualStudio.Text;

namespace Microsoft.CodeAnalysis.Editor.SymbolSearch
{
    internal class RoslynSymbolResult : SymbolSearchResult, IResultInLocalFile
    {
        private DefinitionItem definition;

        public RoslynSymbolResult(DefinitionItem definition)
        {
            this.definition = definition;
        }

        public IPersistentSpan PersistentSpan => throw new System.NotImplementedException();

        public override ISymbolOriginDefinition Origin { get => throw new System.NotImplementedException(); protected set => throw new System.NotImplementedException(); }
        public override ISymbolSource Owner { get => throw new System.NotImplementedException(); protected set => throw new System.NotImplementedException(); }
        public override string Name { get => throw new System.NotImplementedException(); protected set => throw new System.NotImplementedException(); }
    }
}
