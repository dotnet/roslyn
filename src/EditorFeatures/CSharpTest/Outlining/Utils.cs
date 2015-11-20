using System;
using Microsoft.CodeAnalysis.CSharp;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Outlining
{
    internal static class Utils
    {
        public static SyntaxTree ParseCode(string code)
        {
            return SyntaxFactory.ParseSyntaxTree(code);
        }

        public static string StringFromLines(params string[] lines)
        {
            return string.Join(Environment.NewLine, lines);
        }

        public static SyntaxTree ParseLines(params string[] lines)
        {
            var code = StringFromLines(lines);
            return ParseCode(code);
        }
    }
}
