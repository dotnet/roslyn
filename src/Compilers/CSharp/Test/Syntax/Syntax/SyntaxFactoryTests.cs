// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Globalization;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;
using Xunit;
using InternalSyntax = Microsoft.CodeAnalysis.CSharp.Syntax.InternalSyntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public class SyntaxFactoryTests : CSharpTestBase
    {
        [Fact, WorkItem(33713, "https://github.com/dotnet/roslyn/issues/33713")]
        public void AlternateVerbatimString()
        {
            var token = SyntaxFactory.Token(SyntaxKind.InterpolatedVerbatimStringStartToken);
            Assert.Equal("$@\"", token.Text);
            Assert.Equal("$@\"", token.ValueText);
        }

        [Fact]
        public void SyntaxTree()
        {
            var text = SyntaxFactory.SyntaxTree(SyntaxFactory.CompilationUnit(), encoding: null).GetText();
            Assert.Null(text.Encoding);
            Assert.Equal(SourceHashAlgorithm.Sha1, text.ChecksumAlgorithm);
        }

        [Fact]
        public void SyntaxTreeFromNode()
        {
            var text = SyntaxFactory.CompilationUnit().SyntaxTree.GetText();
            Assert.Null(text.Encoding);
            Assert.Equal(SourceHashAlgorithm.Sha1, text.ChecksumAlgorithm);
        }

        [Fact]
        public void TestConstructNamespaceWithNameOnly()
        {
            var n = SyntaxFactory.NamespaceDeclaration(name: SyntaxFactory.IdentifierName(SyntaxFactory.Identifier("goo")));
            Assert.NotNull(n);
            Assert.Equal(0, n.Errors().Length);
            Assert.Equal(0, n.Externs.Count);
            Assert.Equal(0, n.Usings.Count);
            Assert.False(n.NamespaceKeyword.IsMissing);
            Assert.Equal(9, n.NamespaceKeyword.Width);
            Assert.False(n.OpenBraceToken.IsMissing);
            Assert.Equal(SyntaxKind.OpenBraceToken, n.OpenBraceToken.Kind());
            Assert.Equal(1, n.OpenBraceToken.Width);
            Assert.Equal(0, n.Members.Count);
            Assert.False(n.CloseBraceToken.IsMissing);
            Assert.Equal(SyntaxKind.CloseBraceToken, n.CloseBraceToken.Kind());
            Assert.Equal(1, n.CloseBraceToken.Width);
            Assert.Equal(SyntaxKind.None, n.SemicolonToken.Kind());
        }

        [Fact]
        public void TestConstructClassWithKindAndNameOnly()
        {
            var c = SyntaxFactory.ClassDeclaration(identifier: SyntaxFactory.Identifier("goo"));
            Assert.NotNull(c);
            Assert.Equal(0, c.AttributeLists.Count);
            Assert.Equal(0, c.Modifiers.Count);
            Assert.Equal(5, c.Keyword.Width);
            Assert.Equal(SyntaxKind.ClassKeyword, c.Keyword.Kind());
            Assert.Equal(0, c.ConstraintClauses.Count);
            Assert.False(c.OpenBraceToken.IsMissing);
            Assert.Equal(SyntaxKind.OpenBraceToken, c.OpenBraceToken.Kind());
            Assert.Equal(1, c.OpenBraceToken.Width);
            Assert.Equal(0, c.Members.Count);
            Assert.False(c.CloseBraceToken.IsMissing);
            Assert.Equal(SyntaxKind.CloseBraceToken, c.CloseBraceToken.Kind());
            Assert.Equal(1, c.CloseBraceToken.Width);
            Assert.Equal(SyntaxKind.None, c.SemicolonToken.Kind());
        }

        [WorkItem(528399, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/528399")]
        [Fact()]
        public void PassExpressionToSyntaxToken()
        {
            // Verify that the Token factory method does validation for cases when the argument is not a valid token.
            Assert.Throws<ArgumentException>(() => SyntaxFactory.Token(SyntaxKind.NumericLiteralExpression));
        }

        [WorkItem(546101, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546101")]
        [Fact]
        public void TestConstructPragmaChecksumDirective()
        {
            Func<string, SyntaxToken> makeStringLiteral = value =>
                SyntaxFactory.ParseToken(string.Format("\"{0}\"", value)).WithLeadingTrivia(SyntaxFactory.ElasticMarker).WithTrailingTrivia(SyntaxFactory.ElasticMarker);
            var t = SyntaxFactory.PragmaChecksumDirectiveTrivia(makeStringLiteral("file"), makeStringLiteral("guid"), makeStringLiteral("bytes"), true);
            Assert.Equal(SyntaxKind.PragmaChecksumDirectiveTrivia, t.Kind());
            Assert.Equal("#pragmachecksum\"file\"\"guid\"\"bytes\"", t.ToString());
            Assert.Equal("#pragma checksum \"file\" \"guid\" \"bytes\"\r\n", t.NormalizeWhitespace().ToFullString());
        }

        [Fact]
        public void TestFreeFormTokenFactory_NonTokenKind()
        {
            Type exceptionType;
            try
            {
                SyntaxFactory.Token(SyntaxKind.IdentifierName);
                AssertEx.Fail("Should have thrown - can't create an IdentifierName token");
                return;
            }
            catch (Exception e)
            {
                exceptionType = e.GetType();
            }

            // Should throw the same exception as the simpler API (which follows a different code path).
            Assert.Throws(exceptionType, () => SyntaxFactory.Token(default(SyntaxTriviaList), SyntaxKind.IdentifierName, "text", "valueText", default(SyntaxTriviaList)));
        }

        [Fact]
        public void TestFreeFormTokenFactory_SpecialTokenKinds()
        {
            // Factory method won't do the right thing for these SyntaxKinds - throws instead.
            Assert.Throws<ArgumentException>(() => SyntaxFactory.Token(default(SyntaxTriviaList), SyntaxKind.IdentifierToken, "text", "valueText", default(SyntaxTriviaList)));
            Assert.Throws<ArgumentException>(() => SyntaxFactory.Token(default(SyntaxTriviaList), SyntaxKind.CharacterLiteralToken, "text", "valueText", default(SyntaxTriviaList)));
            Assert.Throws<ArgumentException>(() => SyntaxFactory.Token(default(SyntaxTriviaList), SyntaxKind.NumericLiteralToken, "text", "valueText", default(SyntaxTriviaList)));

            // Ensure that when they throw, the appropriate message is used
            using (new EnsureEnglishUICulture())
            {
                try
                {
                    SyntaxFactory.Token(default(SyntaxTriviaList), SyntaxKind.IdentifierToken, "text", "valueText", default(SyntaxTriviaList));
                    AssertEx.Fail("Should have thrown");
                    return;
                }
                catch (ArgumentException e)
                {
                    Assert.Equal($"Use Microsoft.CodeAnalysis.CSharp.SyntaxFactory.Identifier or Microsoft.CodeAnalysis.CSharp.SyntaxFactory.VerbatimIdentifier to create identifier tokens.{Environment.NewLine}Parameter name: kind", e.Message);
                    Assert.Contains(typeof(SyntaxFactory).ToString(), e.Message); // Make sure the class/namespace aren't updated without also updating the exception message
                }

                try
                {
                    SyntaxFactory.Token(default(SyntaxTriviaList), SyntaxKind.CharacterLiteralToken, "text", "valueText", default(SyntaxTriviaList));
                    AssertEx.Fail("Should have thrown");
                    return;
                }
                catch (ArgumentException e)
                {
                    Assert.Equal($"Use Microsoft.CodeAnalysis.CSharp.SyntaxFactory.Literal to create character literal tokens.{Environment.NewLine}Parameter name: kind", e.Message);
                    Assert.Contains(typeof(SyntaxFactory).ToString(), e.Message); // Make sure the class/namespace aren't updated without also updating the exception message
                }

                try
                {
                    SyntaxFactory.Token(default(SyntaxTriviaList), SyntaxKind.NumericLiteralToken, "text", "valueText", default(SyntaxTriviaList));
                    AssertEx.Fail("Should have thrown");
                    return;
                }
                catch (ArgumentException e)
                {
                    Assert.Equal($"Use Microsoft.CodeAnalysis.CSharp.SyntaxFactory.Literal to create numeric literal tokens.{Environment.NewLine}Parameter name: kind", e.Message);
                    Assert.Contains(typeof(SyntaxFactory).ToString(), e.Message); // Make sure the class/namespace aren't updated without also updating the exception message
                }
            }

            // Make sure that the appropriate methods work as suggested in the exception messages, and don't throw
            SyntaxFactory.Identifier("text");
            SyntaxFactory.Literal('c'); //character literal
            SyntaxFactory.Literal(123); //numeric literal
        }

        [Fact]
        public void TestFreeFormTokenFactory_DefaultText()
        {
            for (SyntaxKind kind = InternalSyntax.SyntaxToken.FirstTokenWithWellKnownText; kind <= InternalSyntax.SyntaxToken.LastTokenWithWellKnownText; kind++)
            {
                if (!SyntaxFacts.IsAnyToken(kind)) continue;

                var defaultText = SyntaxFacts.GetText(kind);
                var actualRed = SyntaxFactory.Token(SyntaxTriviaList.Create(SyntaxFactory.ElasticMarker), kind, defaultText, defaultText, SyntaxTriviaList.Create(SyntaxFactory.ElasticMarker));
                var actualGreen = actualRed.Node;

                var expectedGreen = InternalSyntax.SyntaxFactory.Token(InternalSyntax.SyntaxFactory.ElasticZeroSpace, kind, InternalSyntax.SyntaxFactory.ElasticZeroSpace);

                Assert.Same(expectedGreen, actualGreen); // Don't create a new token if we don't have to.
            }
        }

        [Fact]
        public void TestFreeFormTokenFactory_CustomText()
        {
            for (SyntaxKind kind = InternalSyntax.SyntaxToken.FirstTokenWithWellKnownText; kind <= InternalSyntax.SyntaxToken.LastTokenWithWellKnownText; kind++)
            {
                if (!SyntaxFacts.IsAnyToken(kind)) continue;

                var defaultText = SyntaxFacts.GetText(kind);
                var text = ToXmlEntities(defaultText);
                var valueText = defaultText;

                var token = SyntaxFactory.Token(SyntaxTriviaList.Create(SyntaxFactory.ElasticMarker), kind, text, valueText, SyntaxTriviaList.Create(SyntaxFactory.ElasticMarker));

                Assert.Equal(kind, token.Kind());
                Assert.Equal(text, token.Text);
                Assert.Equal(valueText, token.ValueText);

                if (string.IsNullOrEmpty(valueText))
                {
                    Assert.IsType<InternalSyntax.SyntaxToken.SyntaxTokenWithTrivia>(token.Node);
                }
                else
                {
                    Assert.IsType<InternalSyntax.SyntaxToken.SyntaxTokenWithValueAndTrivia<string>>(token.Node);
                }
            }
        }

        [Fact]
        public void TestSeparatedListFactory_DefaultSeparators()
        {
            var null1 = SyntaxFactory.SeparatedList((ParameterSyntax[])null);

            Assert.Equal(0, null1.Count);
            Assert.Equal(0, null1.SeparatorCount);
            Assert.Equal("", null1.ToString());

            var null2 = SyntaxFactory.SeparatedList((System.Collections.Generic.IEnumerable<VariableDeclaratorSyntax>)null);

            Assert.Equal(0, null2.Count);
            Assert.Equal(0, null2.SeparatorCount);
            Assert.Equal("", null2.ToString());

            var empty1 = SyntaxFactory.SeparatedList(new TypeArgumentListSyntax[] { });

            Assert.Equal(0, empty1.Count);
            Assert.Equal(0, empty1.SeparatorCount);
            Assert.Equal("", empty1.ToString());

            var empty2 = SyntaxFactory.SeparatedList(System.Linq.Enumerable.Empty<TypeParameterSyntax>());

            Assert.Equal(0, empty2.Count);
            Assert.Equal(0, empty2.SeparatorCount);
            Assert.Equal("", empty2.ToString());

            var singleton1 = SyntaxFactory.SeparatedList(new[] { SyntaxFactory.IdentifierName("a") });

            Assert.Equal(1, singleton1.Count);
            Assert.Equal(0, singleton1.SeparatorCount);
            Assert.Equal("a", singleton1.ToString());

            var singleton2 = SyntaxFactory.SeparatedList((System.Collections.Generic.IEnumerable<ExpressionSyntax>)new[] { SyntaxFactory.IdentifierName("x") });

            Assert.Equal(1, singleton2.Count);
            Assert.Equal(0, singleton2.SeparatorCount);
            Assert.Equal("x", singleton2.ToString());

            var list1 = SyntaxFactory.SeparatedList(new[] { SyntaxFactory.IdentifierName("a"), SyntaxFactory.IdentifierName("b"), SyntaxFactory.IdentifierName("c") });

            Assert.Equal(3, list1.Count);
            Assert.Equal(2, list1.SeparatorCount);
            Assert.Equal("a,b,c", list1.ToString());

            var builder = new System.Collections.Generic.List<ArgumentSyntax>();
            builder.Add(SyntaxFactory.Argument(SyntaxFactory.IdentifierName("x")));
            builder.Add(SyntaxFactory.Argument(SyntaxFactory.IdentifierName("y")));
            builder.Add(SyntaxFactory.Argument(SyntaxFactory.IdentifierName("z")));

            var list2 = SyntaxFactory.SeparatedList<ArgumentSyntax>(builder);

            Assert.Equal(3, list2.Count);
            Assert.Equal(2, list2.SeparatorCount);
            Assert.Equal("x,y,z", list2.ToString());
        }

        [Fact]
        [WorkItem(33564, "https://github.com/dotnet/roslyn/issues/33564")]
        [WorkItem(720708, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/720708")]
        public void TestLiteralDefaultStringValues()
        {
            // string
            CheckLiteralToString("A", @"""A""");
            CheckLiteralToString("\r", @"""\r""");
            CheckLiteralToString("\u0007", @"""\a""");
            CheckLiteralToString("\u000c", @"""\f""");
            CheckLiteralToString("\u001f", @"""\u001f""");

            // char
            CheckLiteralToString('A', @"'A'");
            CheckLiteralToString('\r', @"'\r'");
            CheckLiteralToString('\u0007', @"'\a'");
            CheckLiteralToString('\u000c', @"'\f'");
            CheckLiteralToString('\u001f', @"'\u001f'");

            // byte
            CheckLiteralToString(byte.MinValue, @"0");
            CheckLiteralToString(byte.MaxValue, @"255");

            // sbyte
            CheckLiteralToString((sbyte)0, @"0");
            CheckLiteralToString(sbyte.MinValue, @"-128");
            CheckLiteralToString(sbyte.MaxValue, @"127");

            // ushort
            CheckLiteralToString(ushort.MinValue, @"0");
            CheckLiteralToString(ushort.MaxValue, @"65535");

            // short
            CheckLiteralToString((short)0, @"0");
            CheckLiteralToString(short.MinValue, @"-32768");
            CheckLiteralToString(short.MaxValue, @"32767");

            // uint
            CheckLiteralToString(uint.MinValue, @"0U");
            CheckLiteralToString(uint.MaxValue, @"4294967295U");

            // int
            CheckLiteralToString((int)0, @"0");
            CheckLiteralToString(int.MinValue, @"-2147483648");
            CheckLiteralToString(int.MaxValue, @"2147483647");

            // ulong
            CheckLiteralToString(ulong.MinValue, @"0UL");
            CheckLiteralToString(ulong.MaxValue, @"18446744073709551615UL");

            // long
            CheckLiteralToString((long)0, @"0L");
            CheckLiteralToString(long.MinValue, @"-9223372036854775808L");
            CheckLiteralToString(long.MaxValue, @"9223372036854775807L");


            // float
            CheckLiteralToString(0F, @"0F");
            CheckLiteralToString(0.012345F, @"0.012345F");
            CheckLiteralToString(float.MaxValue, @"3.40282347E+38F");

            // double
            CheckLiteralToString(0D, @"0");
            CheckLiteralToString(0.012345D, @"0.012345");
            CheckLiteralToString(double.MaxValue, @"1.7976931348623157E+308");

            // decimal
            CheckLiteralToString(0M, @"0M");
            CheckLiteralToString(0.012345M, @"0.012345M");
            CheckLiteralToString(decimal.MaxValue, @"79228162514264337593543950335M");
        }

        [Fact]
        [WorkItem(33564, "https://github.com/dotnet/roslyn/issues/33564")]
        [WorkItem(849836, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/849836")]
        public void TestLiteralToStringDifferentCulture()
        {
            var culture = CultureInfo.CurrentCulture;
            CultureInfo.CurrentCulture = new CultureInfo("de-DE", useUserOverride: false);

            // If we are using the current culture to format the string then
            // decimal values should render as , instead of .
            TestLiteralDefaultStringValues();
            var literal = SyntaxFactory.Literal(3.14);
            Assert.Equal("3.14", literal.ValueText);

            CultureInfo.CurrentCulture = culture;
        }

        [WorkItem(9484, "https://github.com/dotnet/roslyn/issues/9484")]
        [Fact]
        public void TestEscapeLineSeparator()
        {
            var literal = SyntaxFactory.Literal("\u2028");
            Assert.Equal("\"\\u2028\"", literal.Text);
        }

        [WorkItem(20693, "https://github.com/dotnet/roslyn/issues/20693")]
        [Fact]
        public void TestEscapeSurrogate()
        {
            var literal = SyntaxFactory.Literal('\uDBFF');
            Assert.Equal("'\\udbff'", literal.Text);
        }

        private static void CheckLiteralToString(dynamic value, string expected)
        {
            var literal = SyntaxFactory.Literal(value);
            Assert.Equal(expected, literal.ToString());
        }

        private static string ToXmlEntities(string str)
        {
            var builder = new StringBuilder();

            foreach (char ch in str)
            {
                builder.AppendFormat("&#{0};", (int)ch);
            }

            return builder.ToString();
        }

        [Fact]
        [WorkItem(17067, "https://github.com/dotnet/roslyn/issues/17067")]
        public void GetTokenDiagnosticsWithoutSyntaxTree_WithDiagnostics()
        {
            var tokens = SyntaxFactory.ParseTokens("1l").ToList();
            Assert.Equal(2, tokens.Count); // { "1l", "EOF" }

            var literal = tokens.First();
            Assert.Equal("1l", literal.Text);
            Assert.Equal(Location.None, literal.GetLocation());

            literal.GetDiagnostics().Verify(
                // warning CS0078: The 'l' suffix is easily confused with the digit '1' -- use 'L' for clarity
                Diagnostic(ErrorCode.WRN_LowercaseEllSuffix));
        }

        [Fact]
        [WorkItem(17067, "https://github.com/dotnet/roslyn/issues/17067")]
        public void GetTokenDiagnosticsWithoutSyntaxTree_WithoutDiagnostics()
        {
            var tokens = SyntaxFactory.ParseTokens("1L").ToList();
            Assert.Equal(2, tokens.Count); // { "1L", "EOF" }

            var literal = tokens.First();
            Assert.Equal("1L", literal.Text);
            Assert.Equal(Location.None, literal.GetLocation());

            literal.GetDiagnostics().Verify();
        }

        [Fact]
        [WorkItem(17067, "https://github.com/dotnet/roslyn/issues/17067")]
        public void GetTokenDiagnosticsWithSyntaxTree_WithDiagnostics()
        {
            var expression = (LiteralExpressionSyntax)SyntaxFactory.ParseExpression("1l");
            Assert.Equal("1l", expression.Token.Text);
            Assert.NotNull(expression.Token.SyntaxTree);

            var expectedLocation = Location.Create(expression.Token.SyntaxTree, TextSpan.FromBounds(0, 2));
            Assert.Equal(expectedLocation, expression.Token.GetLocation());

            expression.Token.GetDiagnostics().Verify(
                // (1,2): warning CS0078: The 'l' suffix is easily confused with the digit '1' -- use 'L' for clarity
                // 1l
                Diagnostic(ErrorCode.WRN_LowercaseEllSuffix, "l").WithLocation(1, 2));
        }

        [Fact]
        [WorkItem(17067, "https://github.com/dotnet/roslyn/issues/17067")]
        public void GetTokenDiagnosticsWithSyntaxTree_WithoutDiagnostics()
        {
            var expression = (LiteralExpressionSyntax)SyntaxFactory.ParseExpression("1L");
            Assert.Equal("1L", expression.Token.Text);
            Assert.NotNull(expression.Token.SyntaxTree);

            var expectedLocation = Location.Create(expression.Token.SyntaxTree, TextSpan.FromBounds(0, 2));
            Assert.Equal(expectedLocation, expression.Token.GetLocation());

            expression.Token.GetDiagnostics().Verify();
        }

        [Fact]
        [WorkItem(17067, "https://github.com/dotnet/roslyn/issues/17067")]
        public void GetDiagnosticsFromNullToken()
        {
            var token = new SyntaxToken(null);
            Assert.Equal(Location.None, token.GetLocation());
            token.GetDiagnostics().Verify();
        }

        [Fact]
        [WorkItem(21231, "https://github.com/dotnet/roslyn/issues/21231")]
        public void TestSpacingOnNullableIntType()
        {
            var syntaxNode =
                SyntaxFactory.CompilationUnit()
                .WithMembers(
                    SyntaxFactory.SingletonList<MemberDeclarationSyntax>(
                        SyntaxFactory.ClassDeclaration("C")
                        .WithMembers(
                            SyntaxFactory.SingletonList<MemberDeclarationSyntax>(
                                SyntaxFactory.PropertyDeclaration(
                                    SyntaxFactory.NullableType(
                                        SyntaxFactory.PredefinedType(
                                            SyntaxFactory.Token(SyntaxKind.IntKeyword))),
                                    SyntaxFactory.Identifier("P"))
                                    .WithAccessorList(
                                        SyntaxFactory.AccessorList())))))
                .NormalizeWhitespace();

            // no space between int and ?
            Assert.Equal("class C\r\n{\r\n    int? P\r\n    {\r\n    }\r\n}", syntaxNode.ToFullString());
        }

        [Fact]
        [WorkItem(21231, "https://github.com/dotnet/roslyn/issues/21231")]
        public void TestSpacingOnNullableDatetimeType()
        {
            var syntaxNode =
                SyntaxFactory.CompilationUnit()
                .WithMembers(
                    SyntaxFactory.SingletonList<MemberDeclarationSyntax>(
                        SyntaxFactory.ClassDeclaration("C")
                        .WithMembers(
                            SyntaxFactory.SingletonList<MemberDeclarationSyntax>(
                                SyntaxFactory.PropertyDeclaration(
                                    SyntaxFactory.NullableType(
                                        SyntaxFactory.ParseTypeName("DateTime")),
                                    SyntaxFactory.Identifier("P"))
                                    .WithAccessorList(
                                        SyntaxFactory.AccessorList())))))
                .NormalizeWhitespace();

            // no space between DateTime and ?
            Assert.Equal("class C\r\n{\r\n    DateTime? P\r\n    {\r\n    }\r\n}", syntaxNode.ToFullString());
        }

        [Fact]
        [WorkItem(21231, "https://github.com/dotnet/roslyn/issues/21231")]
        public void TestSpacingOnTernary()
        {
            var syntaxNode = SyntaxFactory.ParseExpression("x is int? y: z").NormalizeWhitespace();

            // space between int and ?
            Assert.Equal("x is int ? y : z", syntaxNode.ToFullString());

            var syntaxNode2 = SyntaxFactory.ParseExpression("x is DateTime? y: z").NormalizeWhitespace();

            // space between DateTime and ?
            Assert.Equal("x is DateTime ? y : z", syntaxNode2.ToFullString());
        }

        [Fact]
        [WorkItem(21231, "https://github.com/dotnet/roslyn/issues/21231")]
        public void TestSpacingOnCoalescing()
        {
            var syntaxNode = SyntaxFactory.ParseExpression("x is int??y").NormalizeWhitespace();
            Assert.Equal("x is int ?? y", syntaxNode.ToFullString());

            var syntaxNode2 = SyntaxFactory.ParseExpression("x is DateTime??y").NormalizeWhitespace();
            Assert.Equal("x is DateTime ?? y", syntaxNode2.ToFullString());

            var syntaxNode3 = SyntaxFactory.ParseExpression("x is object??y").NormalizeWhitespace();
            Assert.Equal("x is object ?? y", syntaxNode3.ToFullString());
        }

        [Fact]
        [WorkItem(37467, "https://github.com/dotnet/roslyn/issues/37467")]
        public void TestUnnecessarySemicolon()
        {
            var syntaxNode = SyntaxFactory.MethodDeclaration(
                attributeLists: default,
                modifiers: default,
                returnType: SyntaxFactory.ParseTypeName("int[]"),
                explicitInterfaceSpecifier: default,
                identifier: SyntaxFactory.Identifier("M"),
                typeParameterList: default,
                parameterList: SyntaxFactory.ParseParameterList("()"),
                constraintClauses: default,
                body: (BlockSyntax)SyntaxFactory.ParseStatement("{}"),
                semicolonToken: SyntaxFactory.Token(SyntaxKind.SemicolonToken)
                );
            Assert.Equal("int[]M(){};", syntaxNode.ToFullString());
        }
    }
}
