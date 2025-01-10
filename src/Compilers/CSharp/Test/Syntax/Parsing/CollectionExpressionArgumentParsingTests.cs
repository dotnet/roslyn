// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.Parsing
{
    public sealed class CollectionExpressionArgumentParsingTests : ParsingTests
    {
        public CollectionExpressionArgumentParsingTests(ITestOutputHelper output) : base(output) { }

        public static readonly TheoryData<LanguageVersion> LanguageVersions = new([LanguageVersion.CSharp13, LanguageVersion.Preview, LanguageVersionFacts.CSharpNext]);

        // PROTOTYPE: Update parser to generate a  specific CollectionExpressionSyntax.Arguments field.
        // PROTOTYPE: Test [args].
        // PROTOTYPE: Test [args()].
        // PROTOTYPE: Test [args(), args].
        // PROTOTYPE: Test [args(...),].
        // PROTOTYPE: Test [args(x), args(y)].
        // PROTOTYPE: Test [.. args()].
        // PROTOTYPE: Test [x:args()].
        // PROTOTYPE: Test [args():y].

        [Theory]
        [MemberData(nameof(LanguageVersions))]
        public void Arguments_01(LanguageVersion languageVersion)
        {
            UsingExpression("[args()]",
                TestOptions.Regular.WithLanguageVersion(languageVersion));

            N(SyntaxKind.CollectionExpression);
            {
                N(SyntaxKind.OpenBracketToken);
                N(SyntaxKind.ExpressionElement);
                {
                    N(SyntaxKind.InvocationExpression);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "args");
                        }
                        N(SyntaxKind.ArgumentList);
                        {
                            N(SyntaxKind.OpenParenToken);
                            N(SyntaxKind.CloseParenToken);
                        }
                    }
                }
                N(SyntaxKind.CloseBracketToken);
            }
            EOF();
        }
    }
}
