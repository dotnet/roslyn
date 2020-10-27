// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
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
            var max = Math.Min(expectedTokens.Count, actualTokens.Count);
            for (var i = 0; i < max; i++)
            {
                var expectedToken = expectedTokens[i].ToString();
                var actualToken = actualTokens[i].ToString();
                if (!string.Equals(expectedToken, actualToken))
                {
                    string actualAll = "";
                    string expectedAll = "";
                    for (var j = i - 3; j <= i + 5; j++)
                    {
                        if (j >= 0 && j < max)
                        {
                            if (j == i)
                            {
                                actualAll += "^" + actualTokens[j].ToString() + "^ ";
                                expectedAll += "^" + expectedTokens[j].ToString() + "^ ";
                            }
                            else
                            {
                                actualAll += actualTokens[j].ToString() + " ";
                                expectedAll += expectedTokens[j].ToString() + " ";
                            }
                        }
                    }

                    AssertEx.Fail($"Unexpected token.  Actual '{actualAll}' Expected '{expectedAll}'\r\nActual:\r\n{actual}");
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

        internal static SyntaxNode GetSyntaxRoot(string expectedText, string language, ParseOptions options = null)
        {
            if (language == LanguageNames.CSharp)
            {
                return CS.SyntaxFactory.ParseCompilationUnit(expectedText, options: (CS.CSharpParseOptions)options);
            }
            else
            {
                return VB.SyntaxFactory.ParseCompilationUnit(expectedText, options: (VB.VisualBasicParseOptions)options);
            }
        }
    }
}
