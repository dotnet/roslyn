// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;
using Xunit;
using InternalSyntax = Microsoft.CodeAnalysis.CSharp.Syntax.InternalSyntax;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public abstract class DocumentationCommentLexerTestBase : CSharpTestBase
    {
        /// <summary>
        /// Lexes the given text into a sequence of tokens and asserts that the actual
        /// tokens match the expected tokens.
        /// </summary>
        /// <remarks>
        /// Prints a baseline on failure.
        /// </remarks>
        internal void AssertTokens(string text, params TokenDescription[] expected)
        {
            var actual = GetTokens(text);

            var actualEnumerator = actual.GetEnumerator();
            var expectedEnumerator = expected.GetEnumerator();

            try
            {
                while (true)
                {
                    bool actualHadNext = actualEnumerator.MoveNext();
                    bool expectedHadNext = expectedEnumerator.MoveNext();

                    if (!actualHadNext && !expectedHadNext)
                    {
                        return;
                    }
                    else if (!actualHadNext)
                    {
                        Assert.True(expectedHadNext);
                        AssertEx.Fail("Unmatched expected: " + expectedEnumerator.Current);
                    }
                    else if (!expectedHadNext)
                    {
                        Assert.True(actualHadNext);
                        AssertEx.Fail("Unmatched actual: " + expectedEnumerator.Current);
                    }

                    var actualToken = actualEnumerator.Current;
                    var expectedToken = (TokenDescription)expectedEnumerator.Current;

                    Assert.Equal(expectedToken.Text, actualToken.Text); //This first, since it's easiest to debug.
                    Assert.Equal(expectedToken.ValueText, actualToken.ValueText);
                    Assert.Equal(expectedToken.Kind, actualToken.Kind);
                }
            }
            catch
            {
                var baseline = actual.Select(ToExpectedTokenString);
                Console.WriteLine(string.Join("," + Environment.NewLine, baseline));
                throw;
            }
        }

        /// <summary>
        /// Basic cleverness for cutting down the verbosity of expected token baselines
        /// (i.e. don't output a parameter if the default is correct).
        /// </summary>
        private static string ToExpectedTokenString(InternalSyntax.SyntaxToken token)
        {
            var canonicalText = SyntaxFacts.GetText(token.Kind);

            var builder = new StringBuilder();

            builder.AppendFormat("Token(SyntaxKind.{0}", token.Kind);

            if (token.Text != canonicalText)
            {
                builder.AppendFormat(", \"{0}\"", token.Text);

                if (token.ValueText != token.Text)
                {
                    builder.AppendFormat(", \"{0}\"", token.ValueText);

                    if (token.ContextualKind != token.Kind)
                    {
                        builder.AppendFormat(", SyntaxKind.{0}", token.ContextualKind);
                    }
                }
            }

            builder.Append(')');

            return builder.ToString();
        }

        /// <summary>
        /// Convenience method for constructing TokenDescriptions.  If the text fields can be populated based
        /// on the SyntaxKind, then they will be.
        /// </summary>
        /// <param name="kind">Mandatory.</param>
        /// <param name="text">Defaults to Syntax.GetText of (contextual) kind.</param>
        /// <param name="valueText">Defaults to the computed value of text.</param>
        /// <param name="contextualKind">Defaults to None.</param>
        internal static TokenDescription Token(SyntaxKind kind, string text = null, string valueText = null, SyntaxKind contextualKind = SyntaxKind.None)
        {
            string canonicalText = contextualKind == SyntaxKind.None
                ? SyntaxFacts.GetText(kind)
                : SyntaxFacts.GetText(contextualKind);
            return new TokenDescription
            {
                Kind = kind,
                ContextualKind = contextualKind,
                Text = text ?? canonicalText,
                ValueText = valueText ?? text ?? canonicalText
            };
        }

        internal class TokenDescription
        {
            public SyntaxKind Kind;
            public SyntaxKind ContextualKind;
            public string Text;
            public string ValueText;

            public override string ToString()
            {
                return Kind + " (" + ContextualKind + ") '" + ValueText + "' (really '" + Text + "')";
            }
        }

        /// <summary>
        /// Wrap the text in enough context to make the lexer happy and return a sequence of tokens.
        /// </summary>
        internal abstract IEnumerable<InternalSyntax.SyntaxToken> GetTokens(string text);
    }
}
