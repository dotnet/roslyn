#if false
using System.Text;
using Roslyn.Services.Shared.Utilities;

namespace Roslyn.Services.Shared.Utilities
{
    internal class TextSymbolContentBuilder : SymbolContentBuilder
    {
        private readonly StringBuilder builder = new StringBuilder();

        public TextSymbolContentBuilder()
        {
            this.builder = new StringBuilder();
        }

        public string GetText()
        {
            return this.builder.ToString();
        }

        protected override void DefaultAdd(string text)
        {
            this.builder.Append(text);
        }
    }
}
#endif