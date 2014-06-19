using System.Text;
using Microsoft.CodeAnalysis.CSharp.Semantics;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Syntax.InternalSyntax
{
    internal class LexerBaseCache
    {
        private readonly StringTable strings;

        internal LexerBaseCache()
        {
            this.strings = new StringTable();
        }

        protected LexerBaseCache(StringTable strings)
        {
            this.strings = strings;
        }

        internal string Intern(string text)
        {
            return this.strings.Add(text);
        }

        internal string Intern(StringBuilder text)
        {
            return this.strings.Add(text);
        }

        internal string Intern(char[] array, int start, int length)
        {
            return this.strings.Add(array, start, length);
        }
    }
}