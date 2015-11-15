// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.VisualBasic.Symbols;
using Microsoft.CodeAnalysis.VisualBasic.Syntax;
using Xunit;
using CS = Microsoft.CodeAnalysis.CSharp;
using VB = Microsoft.CodeAnalysis.VisualBasic;

namespace Roslyn.Test.Utilities
{
    public static class TokenUtilities
    {
        public static void AssertTokensEqual(
            string expected, string actual, string language)
        {
            var expectedTokens = GetTokens(expected, language);
            var actualTokens = GetTokens(actual, language);

            for (var i = 0; i < Math.Min(expectedTokens.Count, actualTokens.Count); i++)
            {
                var expectedToken = expectedTokens[i].ToString();
                var actualToken = actualTokens[i].ToString();
                if (!String.Equals(expectedToken, actualToken))
                {
                    var prev = (i - 1 > -1) ? actualTokens[i - 1].ToString() : "^";
                    var next = (i + 1 < actualTokens.Count) ? actualTokens[i + 1].ToString() : "$";
                    AssertEx.Fail($"Unexpected token at index {i} near \"{prev} {actualToken} {next}\". Expected '{expectedToken}', Actual '{actualToken}'");
                }
            }

            if (expectedTokens.Count != actualTokens.Count)
            {
                var expectedDisplay = string.Join(" ", expectedTokens.Select(t => t.ToString()));
                var actualDisplay = string.Join(" ", actualTokens.Select(t => t.ToString()));
                AssertEx.Fail(@"Wrong token count. Expected '{0}', Actual '{1}', Expected Text: '{2}', Actual Text: '{3}'",
                    expectedTokens.Count, actualTokens.Count, expectedDisplay, actualDisplay);
            }
        }

        private static bool SkipVisualBasicToken(SyntaxToken token)
        {
            return token.RawKind == (int)VB.SyntaxKind.StatementTerminatorToken;
        }

        private static bool SkipCSharpToken(SyntaxToken token)
        {
            return token.RawKind == (int)CS.SyntaxKind.OmittedArraySizeExpressionToken;
        }

        public static IList<SyntaxToken> GetTokens(string text, string language)
        {
            if (language == LanguageNames.CSharp)
            {
                return CS.SyntaxFactory.ParseTokens(text).Select(t => (SyntaxToken)t).Where(t => !SkipCSharpToken(t)).ToList();
            }
            else
            {
                return VB.SyntaxFactory.ParseTokens(text).Select(t => (SyntaxToken)t).Where(t => !SkipVisualBasicToken(t)).ToList();
            }
        }

        public static IList<SyntaxToken> GetTokens(SyntaxNode node)
        {
            if (node.Language == LanguageNames.CSharp)
            {
                return node.DescendantTokens().Where(t => !SkipCSharpToken(t)).ToList();
            }
            else
            {
                return node.DescendantTokens().Where(t => !SkipVisualBasicToken(t)).ToList();
            }
        }

        internal static SyntaxNode GetSyntaxRoot(string expectedText, string language)
        {
            if (language == LanguageNames.CSharp)
            {
                return CS.SyntaxFactory.ParseCompilationUnit(expectedText);
            }
            else
            {
                return VB.SyntaxFactory.ParseCompilationUnit(expectedText);
            }
        }
    }
}
