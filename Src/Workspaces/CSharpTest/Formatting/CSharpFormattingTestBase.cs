using System.Collections.Generic;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.UnitTests.Formatting;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.Formatting
{
    public class CSharpFormattingTestBase : FormattingTestBase
    {
        protected override SyntaxNode ParseCompilation(string text)
        {
            return SyntaxFactory.ParseCompilationUnit(text);
        }

        protected void AssertFormat(
            string expected,
            string code,
            bool debugMode = false,
            Dictionary<OptionKey, object> changedOptionSet = null,
            bool testWithTransformation = true)
        {
            AssertFormat(expected, code, SpecializedCollections.SingletonEnumerable(new TextSpan(0, code.Length)), debugMode, changedOptionSet, testWithTransformation);
        }

        protected void AssertFormat(
            string expected,
            string code,
            IEnumerable<TextSpan> spans,
            bool debugMode = false,
            Dictionary<OptionKey, object> changedOptionSet = null,
            bool testWithTransformation = true)
        {
            AssertFormat(expected, code, spans, LanguageNames.CSharp, debugMode, changedOptionSet, testWithTransformation);
        }
    }
}
