// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.LexicalAndXml
{
    public class RawStringLiteralLexingTests
    {
        [Theory]
        [InlineData("\"\"\"{|CS9101:|}", SyntaxKind.SingleLineRawStringLiteralToken, "")]
        [InlineData("\"\"\" {|CS9101:|}", SyntaxKind.SingleLineRawStringLiteralToken, "")]
        [InlineData("\"\"\"{|CS9101:\n|}", SyntaxKind.SingleLineRawStringLiteralToken, "")]
        public void TestSingleToken(string markup, SyntaxKind expectedKind, string expectedValue)
        {
            MarkupTestFile.GetSpans(markup, out var input, out IDictionary<string, ImmutableArray<TextSpan>> spans);

            Assert.True(spans.Count == 0 || spans.Count == 1);
            if (spans.Count == 1)
                Assert.True(spans.Single().Value.Length == 1);

            var token = SyntaxFactory.ParseToken(input);
            var literal = SyntaxFactory.LiteralExpression(SyntaxKind.MultiLineRawStringLiteralExpression, token);
            token = literal.Token;

            Assert.Equal(expectedKind, token.Kind());
            Assert.Equal(input.Length, token.FullWidth);
            Assert.NotNull(token.Value);
            Assert.NotNull(token.ValueText);
            Assert.Equal(expectedValue, token.ValueText);

            if (spans.Count == 0)
            {
                Assert.Empty(token.GetDiagnostics());
            }
            else
            {
                var diagnostics = token.GetDiagnostics();
                Assert.True(diagnostics.Count() == 1);

                var actualDiagnostic = diagnostics.Single();
                var expectedDiagnostic = spans.Single();

                Assert.Equal(expectedDiagnostic.Key, actualDiagnostic.Id);
                Assert.Equal(expectedDiagnostic.Value.Single(), actualDiagnostic.Location.SourceSpan);
            }
        }
    }
}
