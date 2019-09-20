// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public class ExpressionParsingTests : ParsingTests
    {
        public ExpressionParsingTests(ITestOutputHelper output) : base(output) { }

        protected override SyntaxTree ParseTree(string text, CSharpParseOptions options)
        {
            return SyntaxFactory.ParseSyntaxTree(text, options: options);
        }

        private ExpressionSyntax ParseExpression(string text, ParseOptions options = null)
        {
            return SyntaxFactory.ParseExpression(text, options: options);
        }

        [Fact]
        public void TestEmptyString()
        {
            var text = string.Empty;
            var expr = this.ParseExpression(text);

            Assert.NotNull(expr);
            Assert.Equal(SyntaxKind.IdentifierName, expr.Kind());
            Assert.True(((IdentifierNameSyntax)expr).Identifier.IsMissing);
            Assert.Equal(text, expr.ToString());
            Assert.Equal(1, expr.Errors().Length);
            Assert.Equal((int)ErrorCode.ERR_ExpressionExpected, expr.Errors()[0].Code);
        }

        [Fact]
        public void TestInterpolatedVerbatimString()
        {
            UsingExpression(@"$@""hello""");
            N(SyntaxKind.InterpolatedStringExpression);
            {
                N(SyntaxKind.InterpolatedVerbatimStringStartToken);
                N(SyntaxKind.InterpolatedStringText);
                {
                    N(SyntaxKind.InterpolatedStringTextToken);
                }
                N(SyntaxKind.InterpolatedStringEndToken);
            }
            EOF();
        }

        [Fact]
        public void TestAltInterpolatedVerbatimString_CSharp73()
        {
            UsingExpression(@"@$""hello""", TestOptions.Regular7_3,
                // (1,1): error CS8401: To use '@$' instead of '$@' for an interpolated verbatim string, please use language version '8.0' or greater.
                // @$"hello"
                Diagnostic(ErrorCode.ERR_AltInterpolatedVerbatimStringsNotAvailable, @"@$""").WithArguments("8.0").WithLocation(1, 1)
                );

            N(SyntaxKind.InterpolatedStringExpression);
            {
                N(SyntaxKind.InterpolatedVerbatimStringStartToken);
                N(SyntaxKind.InterpolatedStringText);
                {
                    N(SyntaxKind.InterpolatedStringTextToken);
                }
                N(SyntaxKind.InterpolatedStringEndToken);
            }
            EOF();
        }

        [Fact]
        public void TestAltInterpolatedVerbatimString_CSharp8()
        {
            UsingExpression(@"@$""hello""", TestOptions.Regular8);
            N(SyntaxKind.InterpolatedStringExpression);
            {
                N(SyntaxKind.InterpolatedVerbatimStringStartToken);
                N(SyntaxKind.InterpolatedStringText);
                {
                    N(SyntaxKind.InterpolatedStringTextToken);
                }
                N(SyntaxKind.InterpolatedStringEndToken);
            }
            EOF();
        }

        [Fact]
        public void TestName()
        {
            var text = "goo";
            var expr = this.ParseExpression(text);

            Assert.NotNull(expr);
            Assert.Equal(SyntaxKind.IdentifierName, expr.Kind());
            Assert.False(((IdentifierNameSyntax)expr).Identifier.IsMissing);
            Assert.Equal(text, expr.ToString());
            Assert.Equal(0, expr.Errors().Length);
        }

        [Fact]
        public void TestParenthesizedExpression()
        {
            var text = "(goo)";
            var expr = this.ParseExpression(text);

            Assert.NotNull(expr);
            Assert.Equal(SyntaxKind.ParenthesizedExpression, expr.Kind());
            Assert.Equal(text, expr.ToString());
            Assert.Equal(0, expr.Errors().Length);
        }

        private void TestLiteralExpression(SyntaxKind kind)
        {
            var text = SyntaxFacts.GetText(kind);
            var expr = this.ParseExpression(text);

            Assert.NotNull(expr);
            var opKind = SyntaxFacts.GetLiteralExpression(kind);
            Assert.Equal(opKind, expr.Kind());
            Assert.Equal(0, expr.Errors().Length);
            var us = (LiteralExpressionSyntax)expr;
            Assert.NotEqual(default, us.Token);
            Assert.Equal(kind, us.Token.Kind());
        }

        [Fact]
        public void TestPrimaryExpressions()
        {
            TestLiteralExpression(SyntaxKind.NullKeyword);
            TestLiteralExpression(SyntaxKind.TrueKeyword);
            TestLiteralExpression(SyntaxKind.FalseKeyword);
            TestLiteralExpression(SyntaxKind.ArgListKeyword);
        }

        private void TestInstanceExpression(SyntaxKind kind)
        {
            var text = SyntaxFacts.GetText(kind);
            var expr = this.ParseExpression(text);

            Assert.NotNull(expr);
            var opKind = SyntaxFacts.GetInstanceExpression(kind);
            Assert.Equal(opKind, expr.Kind());
            Assert.Equal(0, expr.Errors().Length);
            SyntaxToken token;
            switch (expr.Kind())
            {
                case SyntaxKind.ThisExpression:
                    token = ((ThisExpressionSyntax)expr).Token;
                    Assert.NotEqual(default, token);
                    Assert.Equal(kind, token.Kind());
                    break;
                case SyntaxKind.BaseExpression:
                    token = ((BaseExpressionSyntax)expr).Token;
                    Assert.NotEqual(default, token);
                    Assert.Equal(kind, token.Kind());
                    break;
            }
        }

        [Fact]
        public void TestInstanceExpressions()
        {
            TestInstanceExpression(SyntaxKind.ThisKeyword);
            TestInstanceExpression(SyntaxKind.BaseKeyword);
        }

        [Fact]
        public void TestStringLiteralExpression()
        {
            var text = "\"stuff\"";
            var expr = this.ParseExpression(text);

            Assert.NotNull(expr);
            Assert.Equal(SyntaxKind.StringLiteralExpression, expr.Kind());
            Assert.Equal(0, expr.Errors().Length);
            var us = (LiteralExpressionSyntax)expr;
            Assert.NotEqual(default, us.Token);
            Assert.Equal(SyntaxKind.StringLiteralToken, us.Token.Kind());
        }

        [WorkItem(540379, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540379")]
        [Fact]
        public void TestVerbatimLiteralExpression()
        {
            var text = "@\"\"\"stuff\"\"\"";
            var expr = this.ParseExpression(text);

            Assert.NotNull(expr);
            Assert.Equal(SyntaxKind.StringLiteralExpression, expr.Kind());
            Assert.Equal(0, expr.Errors().Length);
            var us = (LiteralExpressionSyntax)expr;
            Assert.NotEqual(default, us.Token);
            Assert.Equal(SyntaxKind.StringLiteralToken, us.Token.Kind());
            Assert.Equal("\"stuff\"", us.Token.ValueText);
        }

        [Fact]
        public void TestCharacterLiteralExpression()
        {
            var text = "'c'";
            var expr = this.ParseExpression(text);

            Assert.NotNull(expr);
            Assert.Equal(SyntaxKind.CharacterLiteralExpression, expr.Kind());
            Assert.Equal(0, expr.Errors().Length);
            var us = (LiteralExpressionSyntax)expr;
            Assert.NotEqual(default, us.Token);
            Assert.Equal(SyntaxKind.CharacterLiteralToken, us.Token.Kind());
        }

        [Fact]
        public void TestNumericLiteralExpression()
        {
            var text = "0";
            var expr = this.ParseExpression(text);

            Assert.NotNull(expr);
            Assert.Equal(SyntaxKind.NumericLiteralExpression, expr.Kind());
            Assert.Equal(0, expr.Errors().Length);
            var us = (LiteralExpressionSyntax)expr;
            Assert.NotEqual(default, us.Token);
            Assert.Equal(SyntaxKind.NumericLiteralToken, us.Token.Kind());
        }

        private void TestPrefixUnary(SyntaxKind kind)
        {
            var text = SyntaxFacts.GetText(kind) + "a";
            var expr = this.ParseExpression(text);

            Assert.NotNull(expr);
            var opKind = SyntaxFacts.GetPrefixUnaryExpression(kind);
            Assert.Equal(opKind, expr.Kind());
            Assert.Equal(text, expr.ToString());
            Assert.Equal(0, expr.Errors().Length);
            var us = (PrefixUnaryExpressionSyntax)expr;
            Assert.NotEqual(default, us.OperatorToken);
            Assert.Equal(kind, us.OperatorToken.Kind());
            Assert.NotNull(us.Operand);
            Assert.Equal(SyntaxKind.IdentifierName, us.Operand.Kind());
            Assert.Equal("a", us.Operand.ToString());
        }

        [Fact]
        public void TestPrefixUnaryOperators()
        {
            TestPrefixUnary(SyntaxKind.PlusToken);
            TestPrefixUnary(SyntaxKind.MinusToken);
            TestPrefixUnary(SyntaxKind.TildeToken);
            TestPrefixUnary(SyntaxKind.ExclamationToken);
            TestPrefixUnary(SyntaxKind.PlusPlusToken);
            TestPrefixUnary(SyntaxKind.MinusMinusToken);
            TestPrefixUnary(SyntaxKind.AmpersandToken);
            TestPrefixUnary(SyntaxKind.AsteriskToken);
        }

        private void TestPostfixUnary(SyntaxKind kind, ParseOptions options = null)
        {
            var text = "a" + SyntaxFacts.GetText(kind);
            var expr = this.ParseExpression(text, options: options);

            Assert.NotNull(expr);
            var opKind = SyntaxFacts.GetPostfixUnaryExpression(kind);
            Assert.Equal(opKind, expr.Kind());
            Assert.Equal(text, expr.ToString());
            Assert.Equal(0, expr.Errors().Length);
            var us = (PostfixUnaryExpressionSyntax)expr;
            Assert.NotEqual(default, us.OperatorToken);
            Assert.Equal(kind, us.OperatorToken.Kind());
            Assert.NotNull(us.Operand);
            Assert.Equal(SyntaxKind.IdentifierName, us.Operand.Kind());
            Assert.Equal("a", us.Operand.ToString());
        }

        [Fact]
        public void TestPostfixUnaryOperators()
        {
            TestPostfixUnary(SyntaxKind.PlusPlusToken);
            TestPostfixUnary(SyntaxKind.MinusMinusToken);
            TestPostfixUnary(SyntaxKind.ExclamationToken, TestOptions.Regular8);
        }

        private void TestBinary(SyntaxKind kind)
        {
            var text = "(a) " + SyntaxFacts.GetText(kind) + " b";
            var expr = this.ParseExpression(text);

            Assert.NotNull(expr);
            var opKind = SyntaxFacts.GetBinaryExpression(kind);
            Assert.Equal(opKind, expr.Kind());
            Assert.Equal(text, expr.ToString());
            Assert.Equal(0, expr.Errors().Length);
            var b = (BinaryExpressionSyntax)expr;
            Assert.NotEqual(default, b.OperatorToken);
            Assert.Equal(kind, b.OperatorToken.Kind());
            Assert.NotNull(b.Left);
            Assert.NotNull(b.Right);
            Assert.Equal("(a)", b.Left.ToString());
            Assert.Equal("b", b.Right.ToString());
        }

        [Fact]
        public void TestBinaryOperators()
        {
            TestBinary(SyntaxKind.PlusToken);
            TestBinary(SyntaxKind.MinusToken);
            TestBinary(SyntaxKind.AsteriskToken);
            TestBinary(SyntaxKind.SlashToken);
            TestBinary(SyntaxKind.PercentToken);
            TestBinary(SyntaxKind.EqualsEqualsToken);
            TestBinary(SyntaxKind.ExclamationEqualsToken);
            TestBinary(SyntaxKind.LessThanToken);
            TestBinary(SyntaxKind.LessThanEqualsToken);
            TestBinary(SyntaxKind.LessThanLessThanToken);
            TestBinary(SyntaxKind.GreaterThanToken);
            TestBinary(SyntaxKind.GreaterThanEqualsToken);
            TestBinary(SyntaxKind.GreaterThanGreaterThanToken);
            TestBinary(SyntaxKind.AmpersandToken);
            TestBinary(SyntaxKind.AmpersandAmpersandToken);
            TestBinary(SyntaxKind.BarToken);
            TestBinary(SyntaxKind.BarBarToken);
            TestBinary(SyntaxKind.CaretToken);
            TestBinary(SyntaxKind.IsKeyword);
            TestBinary(SyntaxKind.AsKeyword);
            TestBinary(SyntaxKind.QuestionQuestionToken);
        }

        private void TestAssignment(SyntaxKind kind, ParseOptions options = null)
        {
            var text = "(a) " + SyntaxFacts.GetText(kind) + " b";
            var expr = this.ParseExpression(text, options);

            Assert.NotNull(expr);
            var opKind = SyntaxFacts.GetAssignmentExpression(kind);
            Assert.Equal(opKind, expr.Kind());
            Assert.Equal(text, expr.ToString());
            Assert.Equal(0, expr.Errors().Length);
            var a = (AssignmentExpressionSyntax)expr;
            Assert.NotEqual(default, a.OperatorToken);
            Assert.Equal(kind, a.OperatorToken.Kind());
            Assert.NotNull(a.Left);
            Assert.NotNull(a.Right);
            Assert.Equal("(a)", a.Left.ToString());
            Assert.Equal("b", a.Right.ToString());
        }

        [Fact]
        public void TestAssignmentOperators()
        {
            TestAssignment(SyntaxKind.PlusEqualsToken);
            TestAssignment(SyntaxKind.MinusEqualsToken);
            TestAssignment(SyntaxKind.AsteriskEqualsToken);
            TestAssignment(SyntaxKind.SlashEqualsToken);
            TestAssignment(SyntaxKind.PercentEqualsToken);
            TestAssignment(SyntaxKind.EqualsToken);
            TestAssignment(SyntaxKind.LessThanLessThanEqualsToken);
            TestAssignment(SyntaxKind.GreaterThanGreaterThanEqualsToken);
            TestAssignment(SyntaxKind.AmpersandEqualsToken);
            TestAssignment(SyntaxKind.BarEqualsToken);
            TestAssignment(SyntaxKind.CaretEqualsToken);
            TestAssignment(SyntaxKind.QuestionQuestionEqualsToken, options: TestOptions.Regular8);
        }

        private void TestMemberAccess(SyntaxKind kind)
        {
            var text = "(a)" + SyntaxFacts.GetText(kind) + " b";
            var expr = this.ParseExpression(text);

            Assert.NotNull(expr);
            Assert.Equal(text, expr.ToString());
            Assert.Equal(0, expr.Errors().Length);
            var e = (MemberAccessExpressionSyntax)expr;
            Assert.NotEqual(default, e.OperatorToken);
            Assert.Equal(kind, e.OperatorToken.Kind());
            Assert.NotNull(e.Expression);
            Assert.NotNull(e.Name);
            Assert.Equal("(a)", e.Expression.ToString());
            Assert.Equal("b", e.Name.ToString());
        }

        [Fact]
        public void TestMemberAccessTokens()
        {
            TestMemberAccess(SyntaxKind.DotToken);
            TestMemberAccess(SyntaxKind.MinusGreaterThanToken);
        }

        [Fact]
        public void TestConditionalAccessNotVersion5()
        {
            var text = "a.b?.c.d?[1]?.e()?.f";
            var expr = this.ParseExpression(text, options: CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp5));

            Assert.NotNull(expr);
            Assert.Equal(text, expr.ToString());
            Assert.Equal(1, expr.Errors().Length);

            var e = (ConditionalAccessExpressionSyntax)expr;
            Assert.Equal("a.b", e.Expression.ToString());
            Assert.Equal(".c.d?[1]?.e()?.f", e.WhenNotNull.ToString());
        }

        [Fact]
        public void TestConditionalAccess()
        {
            var text = "a.b?.c.d?[1]?.e()?.f";
            var expr = this.ParseExpression(text, options: CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp6));

            Assert.NotNull(expr);
            Assert.Equal(text, expr.ToString());
            Assert.Equal(0, expr.Errors().Length);

            var e = (ConditionalAccessExpressionSyntax)expr;
            Assert.Equal("a.b", e.Expression.ToString());
            var cons = e.WhenNotNull;
            Assert.Equal(".c.d?[1]?.e()?.f", cons.ToString());
            Assert.Equal(cons.Kind(), SyntaxKind.ConditionalAccessExpression);

            e = e.WhenNotNull as ConditionalAccessExpressionSyntax;
            Assert.Equal(".c.d", e.Expression.ToString());
            cons = e.WhenNotNull;
            Assert.Equal("[1]?.e()?.f", cons.ToString());
            Assert.Equal(cons.Kind(), SyntaxKind.ConditionalAccessExpression);

            e = e.WhenNotNull as ConditionalAccessExpressionSyntax;
            Assert.Equal("[1]", e.Expression.ToString());
            cons = e.WhenNotNull;
            Assert.Equal(".e()?.f", cons.ToString());
            Assert.Equal(cons.Kind(), SyntaxKind.ConditionalAccessExpression);

            e = e.WhenNotNull as ConditionalAccessExpressionSyntax;
            Assert.Equal(".e()", e.Expression.ToString());
            cons = e.WhenNotNull;
            Assert.Equal(".f", cons.ToString());
            Assert.Equal(cons.Kind(), SyntaxKind.MemberBindingExpression);
        }

        private void TestFunctionKeyword(SyntaxKind kind, SyntaxToken keyword)
        {
            Assert.NotEqual(default, keyword);
            Assert.Equal(kind, keyword.Kind());
        }

        private void TestParenthesizedArgument(SyntaxToken openParen, CSharpSyntaxNode arg, SyntaxToken closeParen)
        {
            Assert.NotEqual(default, openParen);
            Assert.False(openParen.IsMissing);
            Assert.NotEqual(default, closeParen);
            Assert.False(closeParen.IsMissing);
            Assert.Equal("a", arg.ToString());
        }

        private void TestSingleParamFunctionalOperator(SyntaxKind kind)
        {
            var text = SyntaxFacts.GetText(kind) + "(a)";
            var expr = this.ParseExpression(text);

            Assert.NotNull(expr);
            var opKind = SyntaxFacts.GetPrimaryFunction(kind);
            Assert.Equal(opKind, expr.Kind());
            Assert.Equal(text, expr.ToString());
            Assert.Equal(0, expr.Errors().Length);
            switch (opKind)
            {
                case SyntaxKind.MakeRefExpression:
                    var makeRefSyntax = (MakeRefExpressionSyntax)expr;
                    TestFunctionKeyword(kind, makeRefSyntax.Keyword);
                    TestParenthesizedArgument(makeRefSyntax.OpenParenToken, makeRefSyntax.Expression, makeRefSyntax.CloseParenToken);
                    break;

                case SyntaxKind.RefTypeExpression:
                    var refTypeSyntax = (RefTypeExpressionSyntax)expr;
                    TestFunctionKeyword(kind, refTypeSyntax.Keyword);
                    TestParenthesizedArgument(refTypeSyntax.OpenParenToken, refTypeSyntax.Expression, refTypeSyntax.CloseParenToken);
                    break;

                case SyntaxKind.CheckedExpression:
                case SyntaxKind.UncheckedExpression:
                    var checkedSyntax = (CheckedExpressionSyntax)expr;
                    TestFunctionKeyword(kind, checkedSyntax.Keyword);
                    TestParenthesizedArgument(checkedSyntax.OpenParenToken, checkedSyntax.Expression, checkedSyntax.CloseParenToken);
                    break;

                case SyntaxKind.TypeOfExpression:
                    var typeOfSyntax = (TypeOfExpressionSyntax)expr;
                    TestFunctionKeyword(kind, typeOfSyntax.Keyword);
                    TestParenthesizedArgument(typeOfSyntax.OpenParenToken, typeOfSyntax.Type, typeOfSyntax.CloseParenToken);
                    break;

                case SyntaxKind.SizeOfExpression:
                    var sizeOfSyntax = (SizeOfExpressionSyntax)expr;
                    TestFunctionKeyword(kind, sizeOfSyntax.Keyword);
                    TestParenthesizedArgument(sizeOfSyntax.OpenParenToken, sizeOfSyntax.Type, sizeOfSyntax.CloseParenToken);
                    break;

                case SyntaxKind.DefaultExpression:
                    var defaultSyntax = (DefaultExpressionSyntax)expr;
                    TestFunctionKeyword(kind, defaultSyntax.Keyword);
                    TestParenthesizedArgument(defaultSyntax.OpenParenToken, defaultSyntax.Type, defaultSyntax.CloseParenToken);
                    break;
            }
        }

        [Fact]
        public void TestFunctionOperators()
        {
            TestSingleParamFunctionalOperator(SyntaxKind.MakeRefKeyword);
            TestSingleParamFunctionalOperator(SyntaxKind.RefTypeKeyword);
            TestSingleParamFunctionalOperator(SyntaxKind.CheckedKeyword);
            TestSingleParamFunctionalOperator(SyntaxKind.UncheckedKeyword);
            TestSingleParamFunctionalOperator(SyntaxKind.SizeOfKeyword);
            TestSingleParamFunctionalOperator(SyntaxKind.TypeOfKeyword);
            TestSingleParamFunctionalOperator(SyntaxKind.DefaultKeyword);
        }

        [Fact]
        public void TestRefValue()
        {
            var text = SyntaxFacts.GetText(SyntaxKind.RefValueKeyword) + "(a, b)";
            var expr = this.ParseExpression(text);

            Assert.NotNull(expr);
            Assert.Equal(SyntaxKind.RefValueExpression, expr.Kind());
            Assert.Equal(text, expr.ToString());
            Assert.Equal(0, expr.Errors().Length);
            var fs = (RefValueExpressionSyntax)expr;
            Assert.NotEqual(default, fs.Keyword);
            Assert.Equal(SyntaxKind.RefValueKeyword, fs.Keyword.Kind());
            Assert.NotEqual(default, fs.OpenParenToken);
            Assert.False(fs.OpenParenToken.IsMissing);
            Assert.NotEqual(default, fs.CloseParenToken);
            Assert.False(fs.CloseParenToken.IsMissing);
            Assert.Equal("a", fs.Expression.ToString());
            Assert.Equal("b", fs.Type.ToString());
        }

        [Fact]
        public void TestConditional()
        {
            var text = "a ? b : c";
            var expr = this.ParseExpression(text);

            Assert.NotNull(expr);
            Assert.Equal(SyntaxKind.ConditionalExpression, expr.Kind());
            Assert.Equal(text, expr.ToString());
            Assert.Equal(0, expr.Errors().Length);
            var ts = (ConditionalExpressionSyntax)expr;
            Assert.NotEqual(default, ts.QuestionToken);
            Assert.NotEqual(default, ts.ColonToken);
            Assert.Equal(SyntaxKind.QuestionToken, ts.QuestionToken.Kind());
            Assert.Equal(SyntaxKind.ColonToken, ts.ColonToken.Kind());
            Assert.Equal("a", ts.Condition.ToString());
            Assert.Equal("b", ts.WhenTrue.ToString());
            Assert.Equal("c", ts.WhenFalse.ToString());
        }

        [Fact]
        public void TestConditional02()
        {
            // ensure that ?: has lower precedence than assignment.
            var text = "a ? b=c : d=e";
            var expr = this.ParseExpression(text);
            Assert.Equal(SyntaxKind.ConditionalExpression, expr.Kind());
            Assert.False(expr.HasErrors);
        }

        [Fact]
        public void TestCast()
        {
            var text = "(a) b";
            var expr = this.ParseExpression(text);

            Assert.NotNull(expr);
            Assert.Equal(SyntaxKind.CastExpression, expr.Kind());
            Assert.Equal(text, expr.ToString());
            Assert.Equal(0, expr.Errors().Length);
            var cs = (CastExpressionSyntax)expr;
            Assert.NotEqual(default, cs.OpenParenToken);
            Assert.NotEqual(default, cs.CloseParenToken);
            Assert.False(cs.OpenParenToken.IsMissing);
            Assert.False(cs.CloseParenToken.IsMissing);
            Assert.NotNull(cs.Type);
            Assert.NotNull(cs.Expression);
            Assert.Equal("a", cs.Type.ToString());
            Assert.Equal("b", cs.Expression.ToString());
        }

        [Fact]
        public void TestCall()
        {
            var text = "a(b)";
            var expr = this.ParseExpression(text);

            Assert.NotNull(expr);
            Assert.Equal(SyntaxKind.InvocationExpression, expr.Kind());
            Assert.Equal(text, expr.ToString());
            Assert.Equal(0, expr.Errors().Length);
            var cs = (InvocationExpressionSyntax)expr;
            Assert.NotEqual(default, cs.ArgumentList.OpenParenToken);
            Assert.NotEqual(default, cs.ArgumentList.CloseParenToken);
            Assert.False(cs.ArgumentList.OpenParenToken.IsMissing);
            Assert.False(cs.ArgumentList.CloseParenToken.IsMissing);
            Assert.NotNull(cs.Expression);
            Assert.Equal(1, cs.ArgumentList.Arguments.Count);
            Assert.Equal("a", cs.Expression.ToString());
            Assert.Equal("b", cs.ArgumentList.Arguments[0].ToString());
        }

        [Fact]
        public void TestCallWithRef()
        {
            var text = "a(ref b)";
            var expr = this.ParseExpression(text);

            Assert.NotNull(expr);
            Assert.Equal(SyntaxKind.InvocationExpression, expr.Kind());
            Assert.Equal(text, expr.ToString());
            Assert.Equal(0, expr.Errors().Length);
            var cs = (InvocationExpressionSyntax)expr;
            Assert.NotEqual(default, cs.ArgumentList.OpenParenToken);
            Assert.NotEqual(default, cs.ArgumentList.CloseParenToken);
            Assert.False(cs.ArgumentList.OpenParenToken.IsMissing);
            Assert.False(cs.ArgumentList.CloseParenToken.IsMissing);
            Assert.NotNull(cs.Expression);
            Assert.Equal(1, cs.ArgumentList.Arguments.Count);
            Assert.Equal("a", cs.Expression.ToString());
            Assert.Equal("ref b", cs.ArgumentList.Arguments[0].ToString());
            Assert.NotEqual(default, cs.ArgumentList.Arguments[0].RefOrOutKeyword);
            Assert.Equal(SyntaxKind.RefKeyword, cs.ArgumentList.Arguments[0].RefOrOutKeyword.Kind());
            Assert.NotNull(cs.ArgumentList.Arguments[0].Expression);
            Assert.Equal("b", cs.ArgumentList.Arguments[0].Expression.ToString());
        }

        [Fact]
        public void TestCallWithOut()
        {
            var text = "a(out b)";
            var expr = this.ParseExpression(text);

            Assert.NotNull(expr);
            Assert.Equal(SyntaxKind.InvocationExpression, expr.Kind());
            Assert.Equal(text, expr.ToString());
            Assert.Equal(0, expr.Errors().Length);
            var cs = (InvocationExpressionSyntax)expr;
            Assert.NotEqual(default, cs.ArgumentList.OpenParenToken);
            Assert.NotEqual(default, cs.ArgumentList.CloseParenToken);
            Assert.False(cs.ArgumentList.OpenParenToken.IsMissing);
            Assert.False(cs.ArgumentList.CloseParenToken.IsMissing);
            Assert.NotNull(cs.Expression);
            Assert.Equal(1, cs.ArgumentList.Arguments.Count);
            Assert.Equal("a", cs.Expression.ToString());
            Assert.Equal("out b", cs.ArgumentList.Arguments[0].ToString());
            Assert.NotEqual(default, cs.ArgumentList.Arguments[0].RefOrOutKeyword);
            Assert.Equal(SyntaxKind.OutKeyword, cs.ArgumentList.Arguments[0].RefOrOutKeyword.Kind());
            Assert.NotNull(cs.ArgumentList.Arguments[0].Expression);
            Assert.Equal("b", cs.ArgumentList.Arguments[0].Expression.ToString());
        }

        [Fact]
        public void TestCallWithNamedArgument()
        {
            var text = "a(B: b)";
            var expr = this.ParseExpression(text);

            Assert.NotNull(expr);
            Assert.Equal(SyntaxKind.InvocationExpression, expr.Kind());
            Assert.Equal(text, expr.ToString());
            Assert.Equal(0, expr.Errors().Length);
            var cs = (InvocationExpressionSyntax)expr;
            Assert.NotEqual(default, cs.ArgumentList.OpenParenToken);
            Assert.NotEqual(default, cs.ArgumentList.CloseParenToken);
            Assert.False(cs.ArgumentList.OpenParenToken.IsMissing);
            Assert.False(cs.ArgumentList.CloseParenToken.IsMissing);
            Assert.NotNull(cs.Expression);
            Assert.Equal(1, cs.ArgumentList.Arguments.Count);
            Assert.Equal("a", cs.Expression.ToString());
            Assert.Equal("B: b", cs.ArgumentList.Arguments[0].ToString());
            Assert.NotNull(cs.ArgumentList.Arguments[0].NameColon);
            Assert.Equal("B", cs.ArgumentList.Arguments[0].NameColon.Name.ToString());
            Assert.NotEqual(default, cs.ArgumentList.Arguments[0].NameColon.ColonToken);
            Assert.Equal("b", cs.ArgumentList.Arguments[0].Expression.ToString());
        }

        [Fact]
        public void TestIndex()
        {
            var text = "a[b]";
            var expr = this.ParseExpression(text);

            Assert.NotNull(expr);
            Assert.Equal(SyntaxKind.ElementAccessExpression, expr.Kind());
            Assert.Equal(text, expr.ToString());
            Assert.Equal(0, expr.Errors().Length);
            var ea = (ElementAccessExpressionSyntax)expr;
            Assert.NotEqual(default, ea.ArgumentList.OpenBracketToken);
            Assert.NotEqual(default, ea.ArgumentList.CloseBracketToken);
            Assert.False(ea.ArgumentList.OpenBracketToken.IsMissing);
            Assert.False(ea.ArgumentList.CloseBracketToken.IsMissing);
            Assert.NotNull(ea.Expression);
            Assert.Equal(1, ea.ArgumentList.Arguments.Count);
            Assert.Equal("a", ea.Expression.ToString());
            Assert.Equal("b", ea.ArgumentList.Arguments[0].ToString());
        }

        [Fact]
        public void TestIndexWithRef()
        {
            var text = "a[ref b]";
            var expr = this.ParseExpression(text);

            Assert.NotNull(expr);
            Assert.Equal(SyntaxKind.ElementAccessExpression, expr.Kind());
            Assert.Equal(text, expr.ToString());
            Assert.Equal(0, expr.Errors().Length);
            var ea = (ElementAccessExpressionSyntax)expr;
            Assert.NotEqual(default, ea.ArgumentList.OpenBracketToken);
            Assert.NotEqual(default, ea.ArgumentList.CloseBracketToken);
            Assert.False(ea.ArgumentList.OpenBracketToken.IsMissing);
            Assert.False(ea.ArgumentList.CloseBracketToken.IsMissing);
            Assert.NotNull(ea.Expression);
            Assert.Equal(1, ea.ArgumentList.Arguments.Count);
            Assert.Equal("a", ea.Expression.ToString());
            Assert.Equal("ref b", ea.ArgumentList.Arguments[0].ToString());
            Assert.NotEqual(default, ea.ArgumentList.Arguments[0].RefOrOutKeyword);
            Assert.Equal(SyntaxKind.RefKeyword, ea.ArgumentList.Arguments[0].RefOrOutKeyword.Kind());
            Assert.NotNull(ea.ArgumentList.Arguments[0].Expression);
            Assert.Equal("b", ea.ArgumentList.Arguments[0].Expression.ToString());
        }

        [Fact]
        public void TestIndexWithOut()
        {
            var text = "a[out b]";
            var expr = this.ParseExpression(text);

            Assert.NotNull(expr);
            Assert.Equal(SyntaxKind.ElementAccessExpression, expr.Kind());
            Assert.Equal(text, expr.ToString());
            Assert.Equal(0, expr.Errors().Length);
            var ea = (ElementAccessExpressionSyntax)expr;
            Assert.NotEqual(default, ea.ArgumentList.OpenBracketToken);
            Assert.NotEqual(default, ea.ArgumentList.CloseBracketToken);
            Assert.False(ea.ArgumentList.OpenBracketToken.IsMissing);
            Assert.False(ea.ArgumentList.CloseBracketToken.IsMissing);
            Assert.NotNull(ea.Expression);
            Assert.Equal(1, ea.ArgumentList.Arguments.Count);
            Assert.Equal("a", ea.Expression.ToString());
            Assert.Equal("out b", ea.ArgumentList.Arguments[0].ToString());
            Assert.NotEqual(default, ea.ArgumentList.Arguments[0].RefOrOutKeyword);
            Assert.Equal(SyntaxKind.OutKeyword, ea.ArgumentList.Arguments[0].RefOrOutKeyword.Kind());
            Assert.NotNull(ea.ArgumentList.Arguments[0].Expression);
            Assert.Equal("b", ea.ArgumentList.Arguments[0].Expression.ToString());
        }

        [Fact]
        public void TestIndexWithNamedArgument()
        {
            var text = "a[B: b]";
            var expr = this.ParseExpression(text);

            Assert.NotNull(expr);
            Assert.Equal(SyntaxKind.ElementAccessExpression, expr.Kind());
            Assert.Equal(text, expr.ToString());
            Assert.Equal(0, expr.Errors().Length);
            var ea = (ElementAccessExpressionSyntax)expr;
            Assert.NotEqual(default, ea.ArgumentList.OpenBracketToken);
            Assert.NotEqual(default, ea.ArgumentList.CloseBracketToken);
            Assert.False(ea.ArgumentList.OpenBracketToken.IsMissing);
            Assert.False(ea.ArgumentList.CloseBracketToken.IsMissing);
            Assert.NotNull(ea.Expression);
            Assert.Equal(1, ea.ArgumentList.Arguments.Count);
            Assert.Equal("a", ea.Expression.ToString());
            Assert.Equal("B: b", ea.ArgumentList.Arguments[0].ToString());
        }

        [Fact]
        public void TestNew()
        {
            var text = "new a()";
            var expr = this.ParseExpression(text);

            Assert.NotNull(expr);
            Assert.Equal(SyntaxKind.ObjectCreationExpression, expr.Kind());
            Assert.Equal(text, expr.ToString());
            Assert.Equal(0, expr.Errors().Length);
            var oc = (ObjectCreationExpressionSyntax)expr;
            Assert.NotNull(oc.ArgumentList);
            Assert.NotEqual(default, oc.ArgumentList.OpenParenToken);
            Assert.NotEqual(default, oc.ArgumentList.CloseParenToken);
            Assert.False(oc.ArgumentList.OpenParenToken.IsMissing);
            Assert.False(oc.ArgumentList.CloseParenToken.IsMissing);
            Assert.Equal(0, oc.ArgumentList.Arguments.Count);
            Assert.NotNull(oc.Type);
            Assert.Equal("a", oc.Type.ToString());
            Assert.Null(oc.Initializer);
        }

        [Fact]
        public void TestNewWithArgument()
        {
            var text = "new a(b)";
            var expr = this.ParseExpression(text);

            Assert.NotNull(expr);
            Assert.Equal(SyntaxKind.ObjectCreationExpression, expr.Kind());
            Assert.Equal(text, expr.ToString());
            Assert.Equal(0, expr.Errors().Length);
            var oc = (ObjectCreationExpressionSyntax)expr;
            Assert.NotNull(oc.ArgumentList);
            Assert.NotEqual(default, oc.ArgumentList.OpenParenToken);
            Assert.NotEqual(default, oc.ArgumentList.CloseParenToken);
            Assert.False(oc.ArgumentList.OpenParenToken.IsMissing);
            Assert.False(oc.ArgumentList.CloseParenToken.IsMissing);
            Assert.Equal(1, oc.ArgumentList.Arguments.Count);
            Assert.Equal("b", oc.ArgumentList.Arguments[0].ToString());
            Assert.NotNull(oc.Type);
            Assert.Equal("a", oc.Type.ToString());
            Assert.Null(oc.Initializer);
        }

        [Fact]
        public void TestNewWithNamedArgument()
        {
            var text = "new a(B: b)";
            var expr = this.ParseExpression(text);

            Assert.NotNull(expr);
            Assert.Equal(SyntaxKind.ObjectCreationExpression, expr.Kind());
            Assert.Equal(text, expr.ToString());
            Assert.Equal(0, expr.Errors().Length);
            var oc = (ObjectCreationExpressionSyntax)expr;
            Assert.NotNull(oc.ArgumentList);
            Assert.NotEqual(default, oc.ArgumentList.OpenParenToken);
            Assert.NotEqual(default, oc.ArgumentList.CloseParenToken);
            Assert.False(oc.ArgumentList.OpenParenToken.IsMissing);
            Assert.False(oc.ArgumentList.CloseParenToken.IsMissing);
            Assert.Equal(1, oc.ArgumentList.Arguments.Count);
            Assert.Equal("B: b", oc.ArgumentList.Arguments[0].ToString());
            Assert.NotNull(oc.Type);
            Assert.Equal("a", oc.Type.ToString());
            Assert.Null(oc.Initializer);
        }

        [Fact]
        public void TestNewWithEmptyInitializer()
        {
            var text = "new a() { }";
            var expr = this.ParseExpression(text);

            Assert.NotNull(expr);
            Assert.Equal(SyntaxKind.ObjectCreationExpression, expr.Kind());
            Assert.Equal(text, expr.ToString());
            Assert.Equal(0, expr.Errors().Length);
            var oc = (ObjectCreationExpressionSyntax)expr;
            Assert.NotNull(oc.ArgumentList);
            Assert.NotEqual(default, oc.ArgumentList.OpenParenToken);
            Assert.NotEqual(default, oc.ArgumentList.CloseParenToken);
            Assert.False(oc.ArgumentList.OpenParenToken.IsMissing);
            Assert.False(oc.ArgumentList.CloseParenToken.IsMissing);
            Assert.Equal(0, oc.ArgumentList.Arguments.Count);
            Assert.NotNull(oc.Type);
            Assert.Equal("a", oc.Type.ToString());

            Assert.NotNull(oc.Initializer);
            Assert.NotEqual(default, oc.Initializer.OpenBraceToken);
            Assert.NotEqual(default, oc.Initializer.CloseBraceToken);
            Assert.False(oc.Initializer.OpenBraceToken.IsMissing);
            Assert.False(oc.Initializer.CloseBraceToken.IsMissing);
            Assert.Equal(0, oc.Initializer.Expressions.Count);
        }

        [Fact]
        public void TestNewWithNoArgumentsAndEmptyInitializer()
        {
            var text = "new a { }";
            var expr = this.ParseExpression(text);

            Assert.NotNull(expr);
            Assert.Equal(SyntaxKind.ObjectCreationExpression, expr.Kind());
            Assert.Equal(text, expr.ToString());
            Assert.Equal(0, expr.Errors().Length);
            var oc = (ObjectCreationExpressionSyntax)expr;
            Assert.Null(oc.ArgumentList);
            Assert.NotNull(oc.Type);
            Assert.Equal("a", oc.Type.ToString());

            Assert.NotNull(oc.Initializer);
            Assert.NotEqual(default, oc.Initializer.OpenBraceToken);
            Assert.NotEqual(default, oc.Initializer.CloseBraceToken);
            Assert.False(oc.Initializer.OpenBraceToken.IsMissing);
            Assert.False(oc.Initializer.CloseBraceToken.IsMissing);
            Assert.Equal(0, oc.Initializer.Expressions.Count);
        }

        [Fact]
        public void TestNewWithNoArgumentsAndInitializer()
        {
            var text = "new a { b }";
            var expr = this.ParseExpression(text);

            Assert.NotNull(expr);
            Assert.Equal(SyntaxKind.ObjectCreationExpression, expr.Kind());
            Assert.Equal(text, expr.ToString());
            Assert.Equal(0, expr.Errors().Length);
            var oc = (ObjectCreationExpressionSyntax)expr;
            Assert.Null(oc.ArgumentList);
            Assert.NotNull(oc.Type);
            Assert.Equal("a", oc.Type.ToString());

            Assert.NotNull(oc.Initializer);
            Assert.NotEqual(default, oc.Initializer.OpenBraceToken);
            Assert.NotEqual(default, oc.Initializer.CloseBraceToken);
            Assert.False(oc.Initializer.OpenBraceToken.IsMissing);
            Assert.False(oc.Initializer.CloseBraceToken.IsMissing);
            Assert.Equal(1, oc.Initializer.Expressions.Count);
            Assert.Equal("b", oc.Initializer.Expressions[0].ToString());
        }

        [Fact]
        public void TestNewWithNoArgumentsAndInitializers()
        {
            var text = "new a { b, c, d }";
            var expr = this.ParseExpression(text);

            Assert.NotNull(expr);
            Assert.Equal(SyntaxKind.ObjectCreationExpression, expr.Kind());
            Assert.Equal(text, expr.ToString());
            Assert.Equal(0, expr.Errors().Length);
            var oc = (ObjectCreationExpressionSyntax)expr;
            Assert.Null(oc.ArgumentList);
            Assert.NotNull(oc.Type);
            Assert.Equal("a", oc.Type.ToString());

            Assert.NotNull(oc.Initializer);
            Assert.NotEqual(default, oc.Initializer.OpenBraceToken);
            Assert.NotEqual(default, oc.Initializer.CloseBraceToken);
            Assert.False(oc.Initializer.OpenBraceToken.IsMissing);
            Assert.False(oc.Initializer.CloseBraceToken.IsMissing);
            Assert.Equal(3, oc.Initializer.Expressions.Count);
            Assert.Equal("b", oc.Initializer.Expressions[0].ToString());
            Assert.Equal("c", oc.Initializer.Expressions[1].ToString());
            Assert.Equal("d", oc.Initializer.Expressions[2].ToString());
        }

        [Fact]
        public void TestNewWithNoArgumentsAndAssignmentInitializer()
        {
            var text = "new a { B = b }";
            var expr = this.ParseExpression(text);

            Assert.NotNull(expr);
            Assert.Equal(SyntaxKind.ObjectCreationExpression, expr.Kind());
            Assert.Equal(text, expr.ToString());
            Assert.Equal(0, expr.Errors().Length);
            var oc = (ObjectCreationExpressionSyntax)expr;
            Assert.Null(oc.ArgumentList);
            Assert.NotNull(oc.Type);
            Assert.Equal("a", oc.Type.ToString());

            Assert.NotNull(oc.Initializer);
            Assert.NotEqual(default, oc.Initializer.OpenBraceToken);
            Assert.NotEqual(default, oc.Initializer.CloseBraceToken);
            Assert.False(oc.Initializer.OpenBraceToken.IsMissing);
            Assert.False(oc.Initializer.CloseBraceToken.IsMissing);
            Assert.Equal(1, oc.Initializer.Expressions.Count);
            Assert.Equal("B = b", oc.Initializer.Expressions[0].ToString());
        }

        [Fact]
        public void TestNewWithNoArgumentsAndNestedAssignmentInitializer()
        {
            var text = "new a { B = { X = x } }";
            var expr = this.ParseExpression(text);

            Assert.NotNull(expr);
            Assert.Equal(SyntaxKind.ObjectCreationExpression, expr.Kind());
            Assert.Equal(text, expr.ToString());
            Assert.Equal(0, expr.Errors().Length);
            var oc = (ObjectCreationExpressionSyntax)expr;
            Assert.Null(oc.ArgumentList);
            Assert.NotNull(oc.Type);
            Assert.Equal("a", oc.Type.ToString());

            Assert.NotNull(oc.Initializer);
            Assert.NotEqual(default, oc.Initializer.OpenBraceToken);
            Assert.NotEqual(default, oc.Initializer.CloseBraceToken);
            Assert.False(oc.Initializer.OpenBraceToken.IsMissing);
            Assert.False(oc.Initializer.CloseBraceToken.IsMissing);
            Assert.Equal(1, oc.Initializer.Expressions.Count);
            Assert.Equal("B = { X = x }", oc.Initializer.Expressions[0].ToString());
            Assert.Equal(SyntaxKind.SimpleAssignmentExpression, oc.Initializer.Expressions[0].Kind());
            var b = (AssignmentExpressionSyntax)oc.Initializer.Expressions[0];
            Assert.Equal("B", b.Left.ToString());
            Assert.Equal(SyntaxKind.ObjectInitializerExpression, b.Right.Kind());
        }

        [Fact]
        public void TestNewArray()
        {
            var text = "new a[1]";
            var expr = this.ParseExpression(text);

            Assert.NotNull(expr);
            Assert.Equal(SyntaxKind.ArrayCreationExpression, expr.Kind());
            Assert.Equal(text, expr.ToString());
            Assert.Equal(0, expr.Errors().Length);
            var ac = (ArrayCreationExpressionSyntax)expr;
            Assert.NotNull(ac.Type);
            Assert.Equal("a[1]", ac.Type.ToString());
            Assert.Null(ac.Initializer);
        }

        [Fact]
        public void TestNewArrayWithInitializer()
        {
            var text = "new a[] {b}";
            var expr = this.ParseExpression(text);

            Assert.NotNull(expr);
            Assert.Equal(SyntaxKind.ArrayCreationExpression, expr.Kind());
            Assert.Equal(text, expr.ToString());
            Assert.Equal(0, expr.Errors().Length);
            var ac = (ArrayCreationExpressionSyntax)expr;
            Assert.NotNull(ac.Type);
            Assert.Equal("a[]", ac.Type.ToString());
            Assert.NotNull(ac.Initializer);
            Assert.NotEqual(default, ac.Initializer.OpenBraceToken);
            Assert.NotEqual(default, ac.Initializer.CloseBraceToken);
            Assert.False(ac.Initializer.OpenBraceToken.IsMissing);
            Assert.False(ac.Initializer.CloseBraceToken.IsMissing);
            Assert.Equal(1, ac.Initializer.Expressions.Count);
            Assert.Equal("b", ac.Initializer.Expressions[0].ToString());
        }

        [Fact]
        public void TestNewArrayWithInitializers()
        {
            var text = "new a[] {b, c, d}";
            var expr = this.ParseExpression(text);

            Assert.NotNull(expr);
            Assert.Equal(SyntaxKind.ArrayCreationExpression, expr.Kind());
            Assert.Equal(text, expr.ToString());
            Assert.Equal(0, expr.Errors().Length);
            var ac = (ArrayCreationExpressionSyntax)expr;
            Assert.NotNull(ac.Type);
            Assert.Equal("a[]", ac.Type.ToString());
            Assert.NotNull(ac.Initializer);
            Assert.NotEqual(default, ac.Initializer.OpenBraceToken);
            Assert.NotEqual(default, ac.Initializer.CloseBraceToken);
            Assert.False(ac.Initializer.OpenBraceToken.IsMissing);
            Assert.False(ac.Initializer.CloseBraceToken.IsMissing);
            Assert.Equal(3, ac.Initializer.Expressions.Count);
            Assert.Equal("b", ac.Initializer.Expressions[0].ToString());
            Assert.Equal("c", ac.Initializer.Expressions[1].ToString());
            Assert.Equal("d", ac.Initializer.Expressions[2].ToString());
        }

        [Fact]
        public void TestNewMultiDimensionalArrayWithInitializer()
        {
            var text = "new a[][,][,,] {b}";
            var expr = this.ParseExpression(text);

            Assert.NotNull(expr);
            Assert.Equal(SyntaxKind.ArrayCreationExpression, expr.Kind());
            Assert.Equal(text, expr.ToString());
            Assert.Equal(0, expr.Errors().Length);
            var ac = (ArrayCreationExpressionSyntax)expr;
            Assert.NotNull(ac.Type);
            Assert.Equal("a[][,][,,]", ac.Type.ToString());
            Assert.NotNull(ac.Initializer);
            Assert.NotEqual(default, ac.Initializer.OpenBraceToken);
            Assert.NotEqual(default, ac.Initializer.CloseBraceToken);
            Assert.False(ac.Initializer.OpenBraceToken.IsMissing);
            Assert.False(ac.Initializer.CloseBraceToken.IsMissing);
            Assert.Equal(1, ac.Initializer.Expressions.Count);
            Assert.Equal("b", ac.Initializer.Expressions[0].ToString());
        }

        [Fact]
        public void TestImplicitArrayCreation()
        {
            var text = "new [] {b}";
            var expr = this.ParseExpression(text);

            Assert.NotNull(expr);
            Assert.Equal(SyntaxKind.ImplicitArrayCreationExpression, expr.Kind());
            Assert.Equal(text, expr.ToString());
            Assert.Equal(0, expr.Errors().Length);
            var ac = (ImplicitArrayCreationExpressionSyntax)expr;
            Assert.NotNull(ac.Initializer);
            Assert.NotEqual(default, ac.Initializer.OpenBraceToken);
            Assert.NotEqual(default, ac.Initializer.CloseBraceToken);
            Assert.False(ac.Initializer.OpenBraceToken.IsMissing);
            Assert.False(ac.Initializer.CloseBraceToken.IsMissing);
            Assert.Equal(1, ac.Initializer.Expressions.Count);
            Assert.Equal("b", ac.Initializer.Expressions[0].ToString());
        }

        [Fact]
        public void TestAnonymousObjectCreation()
        {
            var text = "new {a, b}";
            var expr = this.ParseExpression(text);

            Assert.NotNull(expr);
            Assert.Equal(SyntaxKind.AnonymousObjectCreationExpression, expr.Kind());
            Assert.Equal(text, expr.ToString());
            Assert.Equal(0, expr.Errors().Length);
            var ac = (AnonymousObjectCreationExpressionSyntax)expr;
            Assert.NotEqual(default, ac.NewKeyword);
            Assert.NotEqual(default, ac.OpenBraceToken);
            Assert.NotEqual(default, ac.CloseBraceToken);
            Assert.False(ac.OpenBraceToken.IsMissing);
            Assert.False(ac.CloseBraceToken.IsMissing);
            Assert.Equal(2, ac.Initializers.Count);
            Assert.Equal("a", ac.Initializers[0].ToString());
            Assert.Equal("b", ac.Initializers[1].ToString());
        }

        [Fact]
        public void TestAnonymousMethod()
        {
            var text = "delegate (int a) { }";
            var expr = this.ParseExpression(text);

            Assert.NotNull(expr);
            Assert.Equal(SyntaxKind.AnonymousMethodExpression, expr.Kind());
            Assert.Equal(text, expr.ToString());
            Assert.Equal(0, expr.Errors().Length);
            var am = (AnonymousMethodExpressionSyntax)expr;

            Assert.NotEqual(default, am.DelegateKeyword);
            Assert.False(am.DelegateKeyword.IsMissing);

            Assert.NotNull(am.ParameterList);
            Assert.NotEqual(default, am.ParameterList.OpenParenToken);
            Assert.NotEqual(default, am.ParameterList.CloseParenToken);
            Assert.False(am.ParameterList.OpenParenToken.IsMissing);
            Assert.False(am.ParameterList.CloseParenToken.IsMissing);
            Assert.Equal(1, am.ParameterList.Parameters.Count);
            Assert.Equal("int a", am.ParameterList.Parameters[0].ToString());

            Assert.NotNull(am.Block);
            Assert.NotEqual(default, am.Block.OpenBraceToken);
            Assert.NotEqual(default, am.Block.CloseBraceToken);
            Assert.False(am.Block.OpenBraceToken.IsMissing);
            Assert.False(am.Block.CloseBraceToken.IsMissing);
            Assert.Equal(0, am.Block.Statements.Count);
        }

        [Fact]
        public void TestAnonymousMethodWithNoArguments()
        {
            var text = "delegate () { }";
            var expr = this.ParseExpression(text);

            Assert.NotNull(expr);
            Assert.Equal(SyntaxKind.AnonymousMethodExpression, expr.Kind());
            Assert.Equal(text, expr.ToString());
            Assert.Equal(0, expr.Errors().Length);
            var am = (AnonymousMethodExpressionSyntax)expr;

            Assert.NotEqual(default, am.DelegateKeyword);
            Assert.False(am.DelegateKeyword.IsMissing);

            Assert.NotNull(am.ParameterList);
            Assert.NotEqual(default, am.ParameterList.OpenParenToken);
            Assert.NotEqual(default, am.ParameterList.CloseParenToken);
            Assert.False(am.ParameterList.OpenParenToken.IsMissing);
            Assert.False(am.ParameterList.CloseParenToken.IsMissing);
            Assert.Equal(0, am.ParameterList.Parameters.Count);

            Assert.NotNull(am.Block);
            Assert.NotEqual(default, am.Block.OpenBraceToken);
            Assert.NotEqual(default, am.Block.CloseBraceToken);
            Assert.False(am.Block.OpenBraceToken.IsMissing);
            Assert.False(am.Block.CloseBraceToken.IsMissing);
            Assert.Equal(0, am.Block.Statements.Count);
        }

        [Fact]
        public void TestAnonymousMethodWithNoArgumentList()
        {
            var text = "delegate { }";
            var expr = this.ParseExpression(text);

            Assert.NotNull(expr);
            Assert.Equal(SyntaxKind.AnonymousMethodExpression, expr.Kind());
            Assert.Equal(text, expr.ToString());
            Assert.Equal(0, expr.Errors().Length);
            var am = (AnonymousMethodExpressionSyntax)expr;

            Assert.NotEqual(default, am.DelegateKeyword);
            Assert.False(am.DelegateKeyword.IsMissing);

            Assert.Null(am.ParameterList);

            Assert.NotNull(am.Block);
            Assert.NotEqual(default, am.Block.OpenBraceToken);
            Assert.NotEqual(default, am.Block.CloseBraceToken);
            Assert.False(am.Block.OpenBraceToken.IsMissing);
            Assert.False(am.Block.CloseBraceToken.IsMissing);
            Assert.Equal(0, am.Block.Statements.Count);
        }

        [Fact]
        public void TestSimpleLambda()
        {
            var text = "a => b";
            var expr = this.ParseExpression(text);

            Assert.NotNull(expr);
            Assert.Equal(SyntaxKind.SimpleLambdaExpression, expr.Kind());
            Assert.Equal(text, expr.ToString());
            Assert.Equal(0, expr.Errors().Length);
            var lambda = (SimpleLambdaExpressionSyntax)expr;
            Assert.NotEqual(default, lambda.Parameter.Identifier);
            Assert.False(lambda.Parameter.Identifier.IsMissing);
            Assert.Equal("a", lambda.Parameter.Identifier.ToString());
            Assert.NotNull(lambda.Body);
            Assert.Equal("b", lambda.Body.ToString());
        }

        [Fact]
        public void TestSimpleLambdaWithRefReturn()
        {
            var text = "a => ref b";
            var expr = this.ParseExpression(text);

            Assert.NotNull(expr);
            Assert.Equal(SyntaxKind.SimpleLambdaExpression, expr.Kind());
            Assert.Equal(text, expr.ToString());
            Assert.Equal(0, expr.Errors().Length);
            var lambda = (SimpleLambdaExpressionSyntax)expr;
            Assert.NotEqual(default, lambda.Parameter.Identifier);
            Assert.False(lambda.Parameter.Identifier.IsMissing);
            Assert.Equal("a", lambda.Parameter.Identifier.ToString());
            Assert.Equal(SyntaxKind.RefExpression, lambda.Body.Kind());
            Assert.Equal("ref b", lambda.Body.ToString());
        }

        [Fact]
        public void TestSimpleLambdaWithBlock()
        {
            var text = "a => { }";
            var expr = this.ParseExpression(text);

            Assert.NotNull(expr);
            Assert.Equal(SyntaxKind.SimpleLambdaExpression, expr.Kind());
            Assert.Equal(text, expr.ToString());
            Assert.Equal(0, expr.Errors().Length);
            var lambda = (SimpleLambdaExpressionSyntax)expr;
            Assert.NotEqual(default, lambda.Parameter.Identifier);
            Assert.False(lambda.Parameter.Identifier.IsMissing);
            Assert.Equal("a", lambda.Parameter.Identifier.ToString());
            Assert.NotNull(lambda.Body);
            Assert.Equal(SyntaxKind.Block, lambda.Body.Kind());
            var b = (BlockSyntax)lambda.Body;
            Assert.Equal("{ }", lambda.Body.ToString());
        }

        [Fact]
        public void TestLambdaWithNoParameters()
        {
            var text = "() => b";
            var expr = this.ParseExpression(text);

            Assert.NotNull(expr);
            Assert.Equal(SyntaxKind.ParenthesizedLambdaExpression, expr.Kind());
            Assert.Equal(text, expr.ToString());
            Assert.Equal(0, expr.Errors().Length);
            var lambda = (ParenthesizedLambdaExpressionSyntax)expr;
            Assert.NotEqual(default, lambda.ParameterList.OpenParenToken);
            Assert.NotEqual(default, lambda.ParameterList.CloseParenToken);
            Assert.False(lambda.ParameterList.OpenParenToken.IsMissing);
            Assert.False(lambda.ParameterList.CloseParenToken.IsMissing);
            Assert.Equal(0, lambda.ParameterList.Parameters.Count);
            Assert.NotNull(lambda.Body);
            Assert.Equal("b", lambda.Body.ToString());
        }

        [Fact]
        public void TestLambdaWithNoParametersAndRefReturn()
        {
            var text = "() => ref b";
            var expr = this.ParseExpression(text);

            Assert.NotNull(expr);
            Assert.Equal(SyntaxKind.ParenthesizedLambdaExpression, expr.Kind());
            Assert.Equal(text, expr.ToString());
            Assert.Equal(0, expr.Errors().Length);
            var lambda = (ParenthesizedLambdaExpressionSyntax)expr;
            Assert.NotEqual(default, lambda.ParameterList.OpenParenToken);
            Assert.NotEqual(default, lambda.ParameterList.CloseParenToken);
            Assert.False(lambda.ParameterList.OpenParenToken.IsMissing);
            Assert.False(lambda.ParameterList.CloseParenToken.IsMissing);
            Assert.Equal(0, lambda.ParameterList.Parameters.Count);
            Assert.Equal(SyntaxKind.RefExpression, lambda.Body.Kind());
            Assert.Equal("ref b", lambda.Body.ToString());
        }

        [Fact]
        public void TestLambdaWithNoParametersAndBlock()
        {
            var text = "() => { }";
            var expr = this.ParseExpression(text);

            Assert.NotNull(expr);
            Assert.Equal(SyntaxKind.ParenthesizedLambdaExpression, expr.Kind());
            Assert.Equal(text, expr.ToString());
            Assert.Equal(0, expr.Errors().Length);
            var lambda = (ParenthesizedLambdaExpressionSyntax)expr;
            Assert.NotEqual(default, lambda.ParameterList.OpenParenToken);
            Assert.NotEqual(default, lambda.ParameterList.CloseParenToken);
            Assert.False(lambda.ParameterList.OpenParenToken.IsMissing);
            Assert.False(lambda.ParameterList.CloseParenToken.IsMissing);
            Assert.Equal(0, lambda.ParameterList.Parameters.Count);
            Assert.NotNull(lambda.Body);
            Assert.Equal(SyntaxKind.Block, lambda.Body.Kind());
            var b = (BlockSyntax)lambda.Body;
            Assert.Equal("{ }", lambda.Body.ToString());
        }

        [Fact]
        public void TestLambdaWithOneParameter()
        {
            var text = "(a) => b";
            var expr = this.ParseExpression(text);

            Assert.NotNull(expr);
            Assert.Equal(SyntaxKind.ParenthesizedLambdaExpression, expr.Kind());
            Assert.Equal(text, expr.ToString());
            Assert.Equal(0, expr.Errors().Length);
            var lambda = (ParenthesizedLambdaExpressionSyntax)expr;
            Assert.NotEqual(default, lambda.ParameterList.OpenParenToken);
            Assert.NotEqual(default, lambda.ParameterList.CloseParenToken);
            Assert.False(lambda.ParameterList.OpenParenToken.IsMissing);
            Assert.False(lambda.ParameterList.CloseParenToken.IsMissing);
            Assert.Equal(1, lambda.ParameterList.Parameters.Count);
            Assert.Equal(SyntaxKind.Parameter, lambda.ParameterList.Parameters[0].Kind());
            var ps = (ParameterSyntax)lambda.ParameterList.Parameters[0];
            Assert.Null(ps.Type);
            Assert.Equal("a", ps.Identifier.ToString());
            Assert.NotNull(lambda.Body);
            Assert.Equal("b", lambda.Body.ToString());
        }

        [Fact]
        public void TestLambdaWithTwoParameters()
        {
            var text = "(a, a2) => b";
            var expr = this.ParseExpression(text);

            Assert.NotNull(expr);
            Assert.Equal(SyntaxKind.ParenthesizedLambdaExpression, expr.Kind());
            Assert.Equal(text, expr.ToString());
            Assert.Equal(0, expr.Errors().Length);
            var lambda = (ParenthesizedLambdaExpressionSyntax)expr;
            Assert.NotEqual(default, lambda.ParameterList.OpenParenToken);
            Assert.NotEqual(default, lambda.ParameterList.CloseParenToken);
            Assert.False(lambda.ParameterList.OpenParenToken.IsMissing);
            Assert.False(lambda.ParameterList.CloseParenToken.IsMissing);
            Assert.Equal(2, lambda.ParameterList.Parameters.Count);
            Assert.Equal(SyntaxKind.Parameter, lambda.ParameterList.Parameters[0].Kind());
            var ps = (ParameterSyntax)lambda.ParameterList.Parameters[0];
            Assert.Null(ps.Type);
            Assert.Equal("a", ps.Identifier.ToString());
            var ps2 = (ParameterSyntax)lambda.ParameterList.Parameters[1];
            Assert.Null(ps2.Type);
            Assert.Equal("a2", ps2.Identifier.ToString());
            Assert.NotNull(lambda.Body);
            Assert.Equal("b", lambda.Body.ToString());
        }

        [Fact]
        public void TestLambdaWithOneTypedParameter()
        {
            var text = "(T a) => b";
            var expr = this.ParseExpression(text);

            Assert.NotNull(expr);
            Assert.Equal(SyntaxKind.ParenthesizedLambdaExpression, expr.Kind());
            Assert.Equal(text, expr.ToString());
            Assert.Equal(0, expr.Errors().Length);
            var lambda = (ParenthesizedLambdaExpressionSyntax)expr;
            Assert.NotEqual(default, lambda.ParameterList.OpenParenToken);
            Assert.NotEqual(default, lambda.ParameterList.CloseParenToken);
            Assert.False(lambda.ParameterList.OpenParenToken.IsMissing);
            Assert.False(lambda.ParameterList.CloseParenToken.IsMissing);
            Assert.Equal(1, lambda.ParameterList.Parameters.Count);
            Assert.Equal(SyntaxKind.Parameter, lambda.ParameterList.Parameters[0].Kind());
            var ps = (ParameterSyntax)lambda.ParameterList.Parameters[0];
            Assert.NotNull(ps.Type);
            Assert.Equal("T", ps.Type.ToString());
            Assert.Equal("a", ps.Identifier.ToString());
            Assert.NotNull(lambda.Body);
            Assert.Equal("b", lambda.Body.ToString());
        }

        [Fact]
        public void TestLambdaWithOneRefParameter()
        {
            var text = "(ref T a) => b";
            var expr = this.ParseExpression(text);

            Assert.NotNull(expr);
            Assert.Equal(SyntaxKind.ParenthesizedLambdaExpression, expr.Kind());
            Assert.Equal(text, expr.ToString());
            Assert.Equal(0, expr.Errors().Length);
            var lambda = (ParenthesizedLambdaExpressionSyntax)expr;
            Assert.NotEqual(default, lambda.ParameterList.OpenParenToken);
            Assert.NotEqual(default, lambda.ParameterList.CloseParenToken);
            Assert.False(lambda.ParameterList.OpenParenToken.IsMissing);
            Assert.False(lambda.ParameterList.CloseParenToken.IsMissing);
            Assert.Equal(1, lambda.ParameterList.Parameters.Count);
            Assert.Equal(SyntaxKind.Parameter, lambda.ParameterList.Parameters[0].Kind());
            var ps = (ParameterSyntax)lambda.ParameterList.Parameters[0];
            Assert.NotNull(ps.Type);
            Assert.Equal("T", ps.Type.ToString());
            Assert.Equal("a", ps.Identifier.ToString());
            Assert.Equal(1, ps.Modifiers.Count);
            Assert.Equal(SyntaxKind.RefKeyword, ps.Modifiers[0].Kind());
            Assert.NotNull(lambda.Body);
            Assert.Equal("b", lambda.Body.ToString());
        }

        [Fact]
        public void TestTupleWithTwoArguments()
        {
            var text = "(a, a2)";
            var expr = this.ParseExpression(text, options: TestOptions.Regular);

            Assert.NotNull(expr);
            Assert.Equal(SyntaxKind.TupleExpression, expr.Kind());
            Assert.Equal(text, expr.ToString());
            Assert.Equal(0, expr.Errors().Length);
            var tuple = (TupleExpressionSyntax)expr;
            Assert.NotEqual(default, tuple.OpenParenToken);
            Assert.NotEqual(default, tuple.CloseParenToken);
            Assert.False(tuple.OpenParenToken.IsMissing);
            Assert.False(tuple.CloseParenToken.IsMissing);
            Assert.Equal(2, tuple.Arguments.Count);
            Assert.Equal(SyntaxKind.IdentifierName, tuple.Arguments[0].Expression.Kind());
            Assert.Null(tuple.Arguments[1].NameColon);
        }

        [Fact]
        public void TestTupleWithTwoNamedArguments()
        {
            var text = "(arg1: (a, a2), arg2: a2)";
            var expr = this.ParseExpression(text, options: TestOptions.Regular);

            Assert.NotNull(expr);
            Assert.Equal(SyntaxKind.TupleExpression, expr.Kind());
            Assert.Equal(text, expr.ToString());
            Assert.Equal(0, expr.Errors().Length);
            var tuple = (TupleExpressionSyntax)expr;
            Assert.NotEqual(default, tuple.OpenParenToken);
            Assert.NotEqual(default, tuple.CloseParenToken);
            Assert.False(tuple.OpenParenToken.IsMissing);
            Assert.False(tuple.CloseParenToken.IsMissing);
            Assert.Equal(2, tuple.Arguments.Count);
            Assert.Equal(SyntaxKind.TupleExpression, tuple.Arguments[0].Expression.Kind());
            Assert.NotNull(tuple.Arguments[0].NameColon.Name);
            Assert.Equal("arg2", tuple.Arguments[1].NameColon.Name.ToString());
        }

        [Fact]
        public void TestFromSelect()
        {
            var text = "from a in A select b";
            var expr = this.ParseExpression(text);

            Assert.NotNull(expr);
            Assert.Equal(SyntaxKind.QueryExpression, expr.Kind());
            Assert.Equal(text, expr.ToString());
            Assert.Equal(0, expr.Errors().Length);

            var qs = (QueryExpressionSyntax)expr;
            Assert.Equal(0, qs.Body.Clauses.Count);

            var fs = (FromClauseSyntax)qs.FromClause;
            Assert.NotEqual(default, fs.FromKeyword);
            Assert.Equal(SyntaxKind.FromKeyword, fs.FromKeyword.Kind());
            Assert.False(fs.FromKeyword.IsMissing);
            Assert.Null(fs.Type);
            Assert.Equal("a", fs.Identifier.ToString());
            Assert.NotEqual(default, fs.InKeyword);
            Assert.False(fs.InKeyword.IsMissing);
            Assert.Equal("A", fs.Expression.ToString());

            Assert.Equal(SyntaxKind.SelectClause, qs.Body.SelectOrGroup.Kind());
            var ss = (SelectClauseSyntax)qs.Body.SelectOrGroup;
            Assert.NotEqual(default, ss.SelectKeyword);
            Assert.Equal(SyntaxKind.SelectKeyword, ss.SelectKeyword.Kind());
            Assert.False(ss.SelectKeyword.IsMissing);
            Assert.Equal("b", ss.Expression.ToString());
            Assert.Null(qs.Body.Continuation);
        }

        [Fact]
        public void TestFromWithType()
        {
            var text = "from T a in A select b";
            var expr = this.ParseExpression(text);

            Assert.NotNull(expr);
            Assert.Equal(SyntaxKind.QueryExpression, expr.Kind());
            Assert.Equal(text, expr.ToString());
            Assert.Equal(0, expr.Errors().Length);

            var qs = (QueryExpressionSyntax)expr;
            Assert.Equal(0, qs.Body.Clauses.Count);

            Assert.Equal(SyntaxKind.FromClause, qs.FromClause.Kind());
            var fs = (FromClauseSyntax)qs.FromClause;
            Assert.NotEqual(default, fs.FromKeyword);
            Assert.False(fs.FromKeyword.IsMissing);
            Assert.NotNull(fs.Type);
            Assert.Equal("T", fs.Type.ToString());
            Assert.Equal("a", fs.Identifier.ToString());
            Assert.NotEqual(default, fs.InKeyword);
            Assert.False(fs.InKeyword.IsMissing);
            Assert.Equal("A", fs.Expression.ToString());

            Assert.Equal(SyntaxKind.SelectClause, qs.Body.SelectOrGroup.Kind());
            var ss = (SelectClauseSyntax)qs.Body.SelectOrGroup;
            Assert.NotEqual(default, ss.SelectKeyword);
            Assert.False(ss.SelectKeyword.IsMissing);
            Assert.Equal("b", ss.Expression.ToString());
            Assert.Null(qs.Body.Continuation);
        }

        [Fact]
        public void TestFromSelectIntoSelect()
        {
            var text = "from a in A select b into c select d";
            var expr = this.ParseExpression(text);

            Assert.NotNull(expr);
            Assert.Equal(SyntaxKind.QueryExpression, expr.Kind());
            Assert.Equal(text, expr.ToString());
            Assert.Equal(0, expr.Errors().Length);

            var qs = (QueryExpressionSyntax)expr;
            Assert.Equal(0, qs.Body.Clauses.Count);
            Assert.Equal(SyntaxKind.FromClause, qs.FromClause.Kind());
            Assert.Equal(SyntaxKind.SelectClause, qs.Body.SelectOrGroup.Kind());

            var fs = (FromClauseSyntax)qs.FromClause;
            Assert.NotEqual(default, fs.FromKeyword);
            Assert.False(fs.FromKeyword.IsMissing);
            Assert.Null(fs.Type);
            Assert.Equal("a", fs.Identifier.ToString());
            Assert.NotEqual(default, fs.InKeyword);
            Assert.False(fs.InKeyword.IsMissing);
            Assert.Equal("A", fs.Expression.ToString());

            Assert.Equal(SyntaxKind.SelectClause, qs.Body.SelectOrGroup.Kind());
            var ss = (SelectClauseSyntax)qs.Body.SelectOrGroup;
            Assert.NotEqual(default, ss.SelectKeyword);
            Assert.False(ss.SelectKeyword.IsMissing);
            Assert.Equal("b", ss.Expression.ToString());

            Assert.NotNull(qs.Body.Continuation);
            Assert.Equal(SyntaxKind.QueryContinuation, qs.Body.Continuation.Kind());
            Assert.NotEqual(default, qs.Body.Continuation.IntoKeyword);
            Assert.Equal(SyntaxKind.IntoKeyword, qs.Body.Continuation.IntoKeyword.Kind());
            Assert.False(qs.Body.Continuation.IntoKeyword.IsMissing);
            Assert.Equal("c", qs.Body.Continuation.Identifier.ToString());

            Assert.NotNull(qs.Body.Continuation.Body);
            Assert.Equal(0, qs.Body.Continuation.Body.Clauses.Count);
            Assert.NotNull(qs.Body.Continuation.Body.SelectOrGroup);

            Assert.Equal(SyntaxKind.SelectClause, qs.Body.Continuation.Body.SelectOrGroup.Kind());
            ss = (SelectClauseSyntax)qs.Body.Continuation.Body.SelectOrGroup;
            Assert.NotEqual(default, ss.SelectKeyword);
            Assert.False(ss.SelectKeyword.IsMissing);
            Assert.Equal("d", ss.Expression.ToString());

            Assert.Null(qs.Body.Continuation.Body.Continuation);
        }

        [Fact]
        public void TestFromWhereSelect()
        {
            var text = "from a in A where b select c";
            var expr = this.ParseExpression(text);

            Assert.NotNull(expr);
            Assert.Equal(SyntaxKind.QueryExpression, expr.Kind());
            Assert.Equal(text, expr.ToString());
            Assert.Equal(0, expr.Errors().Length);

            var qs = (QueryExpressionSyntax)expr;
            Assert.Equal(1, qs.Body.Clauses.Count);

            Assert.Equal(SyntaxKind.FromClause, qs.FromClause.Kind());
            var fs = (FromClauseSyntax)qs.FromClause;
            Assert.NotEqual(default, fs.FromKeyword);
            Assert.False(fs.FromKeyword.IsMissing);
            Assert.Null(fs.Type);
            Assert.Equal("a", fs.Identifier.ToString());
            Assert.NotEqual(default, fs.InKeyword);
            Assert.False(fs.InKeyword.IsMissing);
            Assert.Equal("A", fs.Expression.ToString());

            Assert.Equal(SyntaxKind.WhereClause, qs.Body.Clauses[0].Kind());
            var ws = (WhereClauseSyntax)qs.Body.Clauses[0];
            Assert.NotEqual(default, ws.WhereKeyword);
            Assert.Equal(SyntaxKind.WhereKeyword, ws.WhereKeyword.Kind());
            Assert.False(ws.WhereKeyword.IsMissing);
            Assert.NotNull(ws.Condition);
            Assert.Equal("b", ws.Condition.ToString());

            Assert.Equal(SyntaxKind.SelectClause, qs.Body.SelectOrGroup.Kind());
            var ss = (SelectClauseSyntax)qs.Body.SelectOrGroup;
            Assert.NotEqual(default, ss.SelectKeyword);
            Assert.False(ss.SelectKeyword.IsMissing);
            Assert.Equal("c", ss.Expression.ToString());
            Assert.Null(qs.Body.Continuation);
        }

        [Fact]
        public void TestFromFromSelect()
        {
            var text = "from a in A from b in B select c";
            var expr = this.ParseExpression(text);

            Assert.NotNull(expr);
            Assert.Equal(SyntaxKind.QueryExpression, expr.Kind());
            Assert.Equal(text, expr.ToString());
            Assert.Equal(0, expr.Errors().Length);

            var qs = (QueryExpressionSyntax)expr;
            Assert.Equal(1, qs.Body.Clauses.Count);

            Assert.Equal(SyntaxKind.FromClause, qs.FromClause.Kind());
            var fs = (FromClauseSyntax)qs.FromClause;
            Assert.NotEqual(default, fs.FromKeyword);
            Assert.False(fs.FromKeyword.IsMissing);
            Assert.Null(fs.Type);
            Assert.Equal("a", fs.Identifier.ToString());
            Assert.NotEqual(default, fs.InKeyword);
            Assert.False(fs.InKeyword.IsMissing);
            Assert.Equal("A", fs.Expression.ToString());
            Assert.Equal(SyntaxKind.SelectClause, qs.Body.SelectOrGroup.Kind());

            Assert.Equal(SyntaxKind.FromClause, qs.Body.Clauses[0].Kind());
            fs = (FromClauseSyntax)qs.Body.Clauses[0];
            Assert.NotEqual(default, fs.FromKeyword);
            Assert.False(fs.FromKeyword.IsMissing);
            Assert.Null(fs.Type);
            Assert.Equal("b", fs.Identifier.ToString());
            Assert.NotEqual(default, fs.InKeyword);
            Assert.False(fs.InKeyword.IsMissing);
            Assert.Equal("B", fs.Expression.ToString());

            Assert.Equal(SyntaxKind.SelectClause, qs.Body.SelectOrGroup.Kind());
            var ss = (SelectClauseSyntax)qs.Body.SelectOrGroup;
            Assert.NotEqual(default, ss.SelectKeyword);
            Assert.False(ss.SelectKeyword.IsMissing);
            Assert.Equal("c", ss.Expression.ToString());
            Assert.Null(qs.Body.Continuation);
        }

        [Fact]
        public void TestFromLetSelect()
        {
            var text = "from a in A let b = B select c";
            var expr = this.ParseExpression(text);

            Assert.NotNull(expr);
            Assert.Equal(SyntaxKind.QueryExpression, expr.Kind());
            Assert.Equal(text, expr.ToString());
            Assert.Equal(0, expr.Errors().Length);

            var qs = (QueryExpressionSyntax)expr;
            Assert.Equal(1, qs.Body.Clauses.Count);

            Assert.Equal(SyntaxKind.FromClause, qs.FromClause.Kind());
            var fs = (FromClauseSyntax)qs.FromClause;
            Assert.NotEqual(default, fs.FromKeyword);
            Assert.False(fs.FromKeyword.IsMissing);
            Assert.Null(fs.Type);
            Assert.Equal("a", fs.Identifier.ToString());
            Assert.NotEqual(default, fs.InKeyword);
            Assert.False(fs.InKeyword.IsMissing);
            Assert.Equal("A", fs.Expression.ToString());
            Assert.Equal(SyntaxKind.SelectClause, qs.Body.SelectOrGroup.Kind());

            Assert.Equal(SyntaxKind.LetClause, qs.Body.Clauses[0].Kind());
            var ls = (LetClauseSyntax)qs.Body.Clauses[0];
            Assert.NotEqual(default, ls.LetKeyword);
            Assert.Equal(SyntaxKind.LetKeyword, ls.LetKeyword.Kind());
            Assert.False(ls.LetKeyword.IsMissing);
            Assert.NotEqual(default, ls.Identifier);
            Assert.Equal("b", ls.Identifier.ToString());
            Assert.NotEqual(default, ls.EqualsToken);
            Assert.False(ls.EqualsToken.IsMissing);
            Assert.NotNull(ls.Expression);
            Assert.Equal("B", ls.Expression.ToString());

            Assert.Equal(SyntaxKind.SelectClause, qs.Body.SelectOrGroup.Kind());
            var ss = (SelectClauseSyntax)qs.Body.SelectOrGroup;
            Assert.NotEqual(default, ss.SelectKeyword);
            Assert.False(ss.SelectKeyword.IsMissing);
            Assert.Equal("c", ss.Expression.ToString());
            Assert.Null(qs.Body.Continuation);
        }

        [Fact]
        public void TestFromOrderBySelect()
        {
            var text = "from a in A orderby b select c";
            var expr = this.ParseExpression(text);

            Assert.NotNull(expr);
            Assert.Equal(SyntaxKind.QueryExpression, expr.Kind());
            Assert.Equal(text, expr.ToString());
            Assert.Equal(0, expr.Errors().Length);

            var qs = (QueryExpressionSyntax)expr;
            Assert.Equal(1, qs.Body.Clauses.Count);

            Assert.Equal(SyntaxKind.FromClause, qs.FromClause.Kind());
            var fs = (FromClauseSyntax)qs.FromClause;
            Assert.NotEqual(default, fs.FromKeyword);
            Assert.False(fs.FromKeyword.IsMissing);
            Assert.Null(fs.Type);
            Assert.Equal("a", fs.Identifier.ToString());
            Assert.NotEqual(default, fs.InKeyword);
            Assert.False(fs.InKeyword.IsMissing);
            Assert.Equal("A", fs.Expression.ToString());
            Assert.Equal(SyntaxKind.SelectClause, qs.Body.SelectOrGroup.Kind());

            Assert.Equal(SyntaxKind.OrderByClause, qs.Body.Clauses[0].Kind());
            var obs = (OrderByClauseSyntax)qs.Body.Clauses[0];
            Assert.NotEqual(default, obs.OrderByKeyword);
            Assert.Equal(SyntaxKind.OrderByKeyword, obs.OrderByKeyword.Kind());
            Assert.False(obs.OrderByKeyword.IsMissing);
            Assert.Equal(1, obs.Orderings.Count);

            var os = (OrderingSyntax)obs.Orderings[0];
            Assert.Equal(SyntaxKind.None, os.AscendingOrDescendingKeyword.Kind());
            Assert.NotNull(os.Expression);
            Assert.Equal("b", os.Expression.ToString());

            Assert.Equal(SyntaxKind.SelectClause, qs.Body.SelectOrGroup.Kind());
            var ss = (SelectClauseSyntax)qs.Body.SelectOrGroup;
            Assert.NotEqual(default, ss.SelectKeyword);
            Assert.False(ss.SelectKeyword.IsMissing);
            Assert.Equal("c", ss.Expression.ToString());
            Assert.Null(qs.Body.Continuation);
        }

        [Fact]
        public void TestFromOrderBy2Select()
        {
            var text = "from a in A orderby b, b2 select c";
            var expr = this.ParseExpression(text);

            Assert.NotNull(expr);
            Assert.Equal(SyntaxKind.QueryExpression, expr.Kind());
            Assert.Equal(text, expr.ToString());
            Assert.Equal(0, expr.Errors().Length);

            var qs = (QueryExpressionSyntax)expr;
            Assert.Equal(1, qs.Body.Clauses.Count);

            Assert.Equal(SyntaxKind.FromClause, qs.FromClause.Kind());
            var fs = (FromClauseSyntax)qs.FromClause;
            Assert.NotEqual(default, fs.FromKeyword);
            Assert.False(fs.FromKeyword.IsMissing);
            Assert.Null(fs.Type);
            Assert.Equal("a", fs.Identifier.ToString());
            Assert.NotEqual(default, fs.InKeyword);
            Assert.False(fs.InKeyword.IsMissing);
            Assert.Equal("A", fs.Expression.ToString());
            Assert.Equal(SyntaxKind.SelectClause, qs.Body.SelectOrGroup.Kind());

            Assert.Equal(SyntaxKind.OrderByClause, qs.Body.Clauses[0].Kind());
            var obs = (OrderByClauseSyntax)qs.Body.Clauses[0];
            Assert.NotEqual(default, obs.OrderByKeyword);
            Assert.False(obs.OrderByKeyword.IsMissing);
            Assert.Equal(2, obs.Orderings.Count);

            var os = (OrderingSyntax)obs.Orderings[0];
            Assert.Equal(SyntaxKind.None, os.AscendingOrDescendingKeyword.Kind());
            Assert.NotNull(os.Expression);
            Assert.Equal("b", os.Expression.ToString());

            os = (OrderingSyntax)obs.Orderings[1];
            Assert.Equal(SyntaxKind.None, os.AscendingOrDescendingKeyword.Kind());
            Assert.NotNull(os.Expression);
            Assert.Equal("b2", os.Expression.ToString());

            Assert.Equal(SyntaxKind.SelectClause, qs.Body.SelectOrGroup.Kind());
            var ss = (SelectClauseSyntax)qs.Body.SelectOrGroup;
            Assert.NotEqual(default, ss.SelectKeyword);
            Assert.False(ss.SelectKeyword.IsMissing);
            Assert.Equal("c", ss.Expression.ToString());
            Assert.Null(qs.Body.Continuation);
        }

        [Fact]
        public void TestFromOrderByAscendingSelect()
        {
            var text = "from a in A orderby b ascending select c";
            var expr = this.ParseExpression(text);

            Assert.NotNull(expr);
            Assert.Equal(SyntaxKind.QueryExpression, expr.Kind());
            Assert.Equal(text, expr.ToString());
            Assert.Equal(0, expr.Errors().Length);

            var qs = (QueryExpressionSyntax)expr;
            Assert.Equal(1, qs.Body.Clauses.Count);

            Assert.Equal(SyntaxKind.FromClause, qs.FromClause.Kind());
            var fs = (FromClauseSyntax)qs.FromClause;
            Assert.NotEqual(default, fs.FromKeyword);
            Assert.False(fs.FromKeyword.IsMissing);
            Assert.Null(fs.Type);
            Assert.Equal("a", fs.Identifier.ToString());
            Assert.NotEqual(default, fs.InKeyword);
            Assert.False(fs.InKeyword.IsMissing);
            Assert.Equal("A", fs.Expression.ToString());
            Assert.Equal(SyntaxKind.SelectClause, qs.Body.SelectOrGroup.Kind());

            Assert.Equal(SyntaxKind.OrderByClause, qs.Body.Clauses[0].Kind());
            var obs = (OrderByClauseSyntax)qs.Body.Clauses[0];
            Assert.NotEqual(default, obs.OrderByKeyword);
            Assert.False(obs.OrderByKeyword.IsMissing);
            Assert.Equal(1, obs.Orderings.Count);

            var os = (OrderingSyntax)obs.Orderings[0];
            Assert.NotEqual(default, os.AscendingOrDescendingKeyword);
            Assert.Equal(SyntaxKind.AscendingKeyword, os.AscendingOrDescendingKeyword.Kind());
            Assert.False(os.AscendingOrDescendingKeyword.IsMissing);
            Assert.Equal(SyntaxKind.AscendingKeyword, os.AscendingOrDescendingKeyword.ContextualKind());

            Assert.NotNull(os.Expression);
            Assert.Equal("b", os.Expression.ToString());

            Assert.Equal(SyntaxKind.SelectClause, qs.Body.SelectOrGroup.Kind());
            var ss = (SelectClauseSyntax)qs.Body.SelectOrGroup;
            Assert.NotEqual(default, ss.SelectKeyword);
            Assert.False(ss.SelectKeyword.IsMissing);
            Assert.Equal("c", ss.Expression.ToString());
            Assert.Null(qs.Body.Continuation);
        }

        [Fact]
        public void TestFromOrderByDescendingSelect()
        {
            var text = "from a in A orderby b descending select c";
            var expr = this.ParseExpression(text);

            Assert.NotNull(expr);
            Assert.Equal(SyntaxKind.QueryExpression, expr.Kind());
            Assert.Equal(text, expr.ToString());
            Assert.Equal(0, expr.Errors().Length);

            var qs = (QueryExpressionSyntax)expr;
            Assert.Equal(1, qs.Body.Clauses.Count);

            Assert.Equal(SyntaxKind.FromClause, qs.FromClause.Kind());
            var fs = (FromClauseSyntax)qs.FromClause;
            Assert.NotEqual(default, fs.FromKeyword);
            Assert.False(fs.FromKeyword.IsMissing);
            Assert.Null(fs.Type);
            Assert.Equal("a", fs.Identifier.ToString());
            Assert.NotEqual(default, fs.InKeyword);
            Assert.False(fs.InKeyword.IsMissing);
            Assert.Equal("A", fs.Expression.ToString());
            Assert.Equal(SyntaxKind.SelectClause, qs.Body.SelectOrGroup.Kind());

            Assert.Equal(SyntaxKind.OrderByClause, qs.Body.Clauses[0].Kind());
            var obs = (OrderByClauseSyntax)qs.Body.Clauses[0];
            Assert.NotEqual(default, obs.OrderByKeyword);
            Assert.False(obs.OrderByKeyword.IsMissing);
            Assert.Equal(1, obs.Orderings.Count);

            var os = (OrderingSyntax)obs.Orderings[0];
            Assert.NotEqual(default, os.AscendingOrDescendingKeyword);
            Assert.Equal(SyntaxKind.DescendingKeyword, os.AscendingOrDescendingKeyword.Kind());
            Assert.False(os.AscendingOrDescendingKeyword.IsMissing);
            Assert.Equal(SyntaxKind.DescendingKeyword, os.AscendingOrDescendingKeyword.ContextualKind());

            Assert.NotNull(os.Expression);
            Assert.Equal("b", os.Expression.ToString());

            Assert.Equal(SyntaxKind.SelectClause, qs.Body.SelectOrGroup.Kind());
            var ss = (SelectClauseSyntax)qs.Body.SelectOrGroup;
            Assert.NotEqual(default, ss.SelectKeyword);
            Assert.False(ss.SelectKeyword.IsMissing);
            Assert.Equal("c", ss.Expression.ToString());
            Assert.Null(qs.Body.Continuation);
        }

        [Fact]
        public void TestFromGroupBy()
        {
            var text = "from a in A group b by c";
            var expr = this.ParseExpression(text);

            Assert.NotNull(expr);
            Assert.Equal(SyntaxKind.QueryExpression, expr.Kind());
            Assert.Equal(text, expr.ToString());
            Assert.Equal(0, expr.Errors().Length);

            var qs = (QueryExpressionSyntax)expr;
            Assert.Equal(0, qs.Body.Clauses.Count);

            var fs = qs.FromClause;
            Assert.NotEqual(default, fs.FromKeyword);
            Assert.False(fs.FromKeyword.IsMissing);
            Assert.Null(fs.Type);
            Assert.Equal("a", fs.Identifier.ToString());
            Assert.NotEqual(default, fs.InKeyword);
            Assert.False(fs.InKeyword.IsMissing);
            Assert.Equal("A", fs.Expression.ToString());

            Assert.Equal(SyntaxKind.GroupClause, qs.Body.SelectOrGroup.Kind());
            var gbs = (GroupClauseSyntax)qs.Body.SelectOrGroup;
            Assert.NotEqual(default, gbs.GroupKeyword);
            Assert.Equal(SyntaxKind.GroupKeyword, gbs.GroupKeyword.Kind());
            Assert.False(gbs.GroupKeyword.IsMissing);
            Assert.NotNull(gbs.GroupExpression);
            Assert.Equal("b", gbs.GroupExpression.ToString());
            Assert.NotEqual(default, gbs.ByKeyword);
            Assert.Equal(SyntaxKind.ByKeyword, gbs.ByKeyword.Kind());
            Assert.False(gbs.ByKeyword.IsMissing);
            Assert.NotNull(gbs.ByExpression);
            Assert.Equal("c", gbs.ByExpression.ToString());

            Assert.Null(qs.Body.Continuation);
        }

        [Fact]
        public void TestFromGroupByIntoSelect()
        {
            var text = "from a in A group b by c into d select e";
            var expr = this.ParseExpression(text);

            Assert.NotNull(expr);
            Assert.Equal(SyntaxKind.QueryExpression, expr.Kind());
            Assert.Equal(text, expr.ToString());
            Assert.Equal(0, expr.Errors().Length);

            var qs = (QueryExpressionSyntax)expr;
            Assert.Equal(0, qs.Body.Clauses.Count);

            var fs = qs.FromClause;
            Assert.NotEqual(default, fs.FromKeyword);
            Assert.False(fs.FromKeyword.IsMissing);
            Assert.Null(fs.Type);
            Assert.Equal("a", fs.Identifier.ToString());
            Assert.NotEqual(default, fs.InKeyword);
            Assert.False(fs.InKeyword.IsMissing);
            Assert.Equal("A", fs.Expression.ToString());

            Assert.Equal(SyntaxKind.GroupClause, qs.Body.SelectOrGroup.Kind());
            var gbs = (GroupClauseSyntax)qs.Body.SelectOrGroup;
            Assert.NotEqual(default, gbs.GroupKeyword);
            Assert.False(gbs.GroupKeyword.IsMissing);
            Assert.NotNull(gbs.GroupExpression);
            Assert.Equal("b", gbs.GroupExpression.ToString());
            Assert.NotEqual(default, gbs.ByKeyword);
            Assert.False(gbs.ByKeyword.IsMissing);
            Assert.NotNull(gbs.ByExpression);
            Assert.Equal("c", gbs.ByExpression.ToString());

            Assert.NotNull(qs.Body.Continuation);
            Assert.Equal(SyntaxKind.QueryContinuation, qs.Body.Continuation.Kind());
            Assert.NotEqual(default, qs.Body.Continuation.IntoKeyword);
            Assert.False(qs.Body.Continuation.IntoKeyword.IsMissing);
            Assert.Equal("d", qs.Body.Continuation.Identifier.ToString());

            Assert.NotNull(qs.Body.Continuation);
            Assert.Equal(0, qs.Body.Continuation.Body.Clauses.Count);
            Assert.NotNull(qs.Body.Continuation.Body.SelectOrGroup);

            Assert.Equal(SyntaxKind.SelectClause, qs.Body.Continuation.Body.SelectOrGroup.Kind());
            var ss = (SelectClauseSyntax)qs.Body.Continuation.Body.SelectOrGroup;
            Assert.NotEqual(default, ss.SelectKeyword);
            Assert.False(ss.SelectKeyword.IsMissing);
            Assert.Equal("e", ss.Expression.ToString());

            Assert.Null(qs.Body.Continuation.Body.Continuation);
        }

        [Fact]
        public void TestFromJoinSelect()
        {
            var text = "from a in A join b in B on a equals b select c";
            var expr = this.ParseExpression(text);

            Assert.NotNull(expr);
            Assert.Equal(SyntaxKind.QueryExpression, expr.Kind());
            Assert.Equal(text, expr.ToString());
            Assert.Equal(0, expr.Errors().Length);

            var qs = (QueryExpressionSyntax)expr;
            Assert.Equal(1, qs.Body.Clauses.Count);

            Assert.Equal(SyntaxKind.FromClause, qs.FromClause.Kind());
            var fs = (FromClauseSyntax)qs.FromClause;
            Assert.NotEqual(default, fs.FromKeyword);
            Assert.False(fs.FromKeyword.IsMissing);
            Assert.Null(fs.Type);
            Assert.Equal("a", fs.Identifier.ToString());
            Assert.NotEqual(default, fs.InKeyword);
            Assert.False(fs.InKeyword.IsMissing);
            Assert.Equal("A", fs.Expression.ToString());

            Assert.Equal(SyntaxKind.JoinClause, qs.Body.Clauses[0].Kind());
            var js = (JoinClauseSyntax)qs.Body.Clauses[0];
            Assert.NotEqual(default, js.JoinKeyword);
            Assert.Equal(SyntaxKind.JoinKeyword, js.JoinKeyword.Kind());
            Assert.False(js.JoinKeyword.IsMissing);
            Assert.Null(js.Type);
            Assert.NotEqual(default, js.Identifier);
            Assert.Equal("b", js.Identifier.ToString());
            Assert.NotEqual(default, js.InKeyword);
            Assert.False(js.InKeyword.IsMissing);
            Assert.NotNull(js.InExpression);
            Assert.Equal("B", js.InExpression.ToString());
            Assert.NotEqual(default, js.OnKeyword);
            Assert.Equal(SyntaxKind.OnKeyword, js.OnKeyword.Kind());
            Assert.False(js.OnKeyword.IsMissing);
            Assert.NotNull(js.LeftExpression);
            Assert.Equal("a", js.LeftExpression.ToString());
            Assert.NotEqual(default, js.EqualsKeyword);
            Assert.Equal(SyntaxKind.EqualsKeyword, js.EqualsKeyword.Kind());
            Assert.False(js.EqualsKeyword.IsMissing);
            Assert.NotNull(js.RightExpression);
            Assert.Equal("b", js.RightExpression.ToString());
            Assert.Null(js.Into);

            Assert.Equal(SyntaxKind.SelectClause, qs.Body.SelectOrGroup.Kind());
            var ss = (SelectClauseSyntax)qs.Body.SelectOrGroup;
            Assert.NotEqual(default, ss.SelectKeyword);
            Assert.False(ss.SelectKeyword.IsMissing);
            Assert.Equal("c", ss.Expression.ToString());
            Assert.Null(qs.Body.Continuation);
        }

        [Fact]
        public void TestFromJoinWithTypesSelect()
        {
            var text = "from Ta a in A join Tb b in B on a equals b select c";
            var expr = this.ParseExpression(text);

            Assert.NotNull(expr);
            Assert.Equal(SyntaxKind.QueryExpression, expr.Kind());
            Assert.Equal(text, expr.ToString());
            Assert.Equal(0, expr.Errors().Length);

            var qs = (QueryExpressionSyntax)expr;
            Assert.Equal(1, qs.Body.Clauses.Count);

            Assert.Equal(SyntaxKind.FromClause, qs.FromClause.Kind());
            var fs = (FromClauseSyntax)qs.FromClause;
            Assert.NotEqual(default, fs.FromKeyword);
            Assert.False(fs.FromKeyword.IsMissing);
            Assert.NotNull(fs.Type);
            Assert.Equal("Ta", fs.Type.ToString());
            Assert.Equal("a", fs.Identifier.ToString());
            Assert.NotEqual(default, fs.InKeyword);
            Assert.False(fs.InKeyword.IsMissing);
            Assert.Equal("A", fs.Expression.ToString());

            Assert.Equal(SyntaxKind.JoinClause, qs.Body.Clauses[0].Kind());
            var js = (JoinClauseSyntax)qs.Body.Clauses[0];
            Assert.NotEqual(default, js.JoinKeyword);
            Assert.False(js.JoinKeyword.IsMissing);
            Assert.NotNull(js.Type);
            Assert.Equal("Tb", js.Type.ToString());
            Assert.NotEqual(default, js.Identifier);
            Assert.Equal("b", js.Identifier.ToString());
            Assert.NotEqual(default, js.InKeyword);
            Assert.False(js.InKeyword.IsMissing);
            Assert.NotNull(js.InExpression);
            Assert.Equal("B", js.InExpression.ToString());
            Assert.NotEqual(default, js.OnKeyword);
            Assert.False(js.OnKeyword.IsMissing);
            Assert.NotNull(js.LeftExpression);
            Assert.Equal("a", js.LeftExpression.ToString());
            Assert.NotEqual(default, js.EqualsKeyword);
            Assert.False(js.EqualsKeyword.IsMissing);
            Assert.NotNull(js.RightExpression);
            Assert.Equal("b", js.RightExpression.ToString());
            Assert.Null(js.Into);

            Assert.Equal(SyntaxKind.SelectClause, qs.Body.SelectOrGroup.Kind());
            var ss = (SelectClauseSyntax)qs.Body.SelectOrGroup;
            Assert.NotEqual(default, ss.SelectKeyword);
            Assert.False(ss.SelectKeyword.IsMissing);
            Assert.Equal("c", ss.Expression.ToString());
            Assert.Null(qs.Body.Continuation);
        }

        [Fact]
        public void TestFromJoinIntoSelect()
        {
            var text = "from a in A join b in B on a equals b into c select d";
            var expr = this.ParseExpression(text);

            Assert.NotNull(expr);
            Assert.Equal(SyntaxKind.QueryExpression, expr.Kind());
            Assert.Equal(text, expr.ToString());
            Assert.Equal(0, expr.Errors().Length);

            var qs = (QueryExpressionSyntax)expr;
            Assert.Equal(1, qs.Body.Clauses.Count);

            Assert.Equal(SyntaxKind.FromClause, qs.FromClause.Kind());
            var fs = (FromClauseSyntax)qs.FromClause;
            Assert.NotEqual(default, fs.FromKeyword);
            Assert.False(fs.FromKeyword.IsMissing);
            Assert.Null(fs.Type);
            Assert.Equal("a", fs.Identifier.ToString());
            Assert.NotEqual(default, fs.InKeyword);
            Assert.False(fs.InKeyword.IsMissing);
            Assert.Equal("A", fs.Expression.ToString());

            Assert.Equal(SyntaxKind.JoinClause, qs.Body.Clauses[0].Kind());
            var js = (JoinClauseSyntax)qs.Body.Clauses[0];
            Assert.NotEqual(default, js.JoinKeyword);
            Assert.False(js.JoinKeyword.IsMissing);
            Assert.Null(js.Type);
            Assert.NotEqual(default, js.Identifier);
            Assert.Equal("b", js.Identifier.ToString());
            Assert.NotEqual(default, js.InKeyword);
            Assert.False(js.InKeyword.IsMissing);
            Assert.NotNull(js.InExpression);
            Assert.Equal("B", js.InExpression.ToString());
            Assert.NotEqual(default, js.OnKeyword);
            Assert.False(js.OnKeyword.IsMissing);
            Assert.NotNull(js.LeftExpression);
            Assert.Equal("a", js.LeftExpression.ToString());
            Assert.NotEqual(default, js.EqualsKeyword);
            Assert.False(js.EqualsKeyword.IsMissing);
            Assert.NotNull(js.RightExpression);
            Assert.Equal("b", js.RightExpression.ToString());
            Assert.NotNull(js.Into);
            Assert.NotEqual(default, js.Into.IntoKeyword);
            Assert.False(js.Into.IntoKeyword.IsMissing);
            Assert.NotEqual(default, js.Into.Identifier);
            Assert.Equal("c", js.Into.Identifier.ToString());

            Assert.Equal(SyntaxKind.SelectClause, qs.Body.SelectOrGroup.Kind());
            var ss = (SelectClauseSyntax)qs.Body.SelectOrGroup;
            Assert.NotEqual(default, ss.SelectKeyword);
            Assert.False(ss.SelectKeyword.IsMissing);
            Assert.Equal("d", ss.Expression.ToString());
            Assert.Null(qs.Body.Continuation);
        }

        [Fact]
        public void TestFromGroupBy1()
        {
            var text = "from it in goo group x by y";
            var expr = SyntaxFactory.ParseExpression(text);

            Assert.NotNull(expr);
            Assert.Equal(SyntaxKind.QueryExpression, expr.Kind());
            Assert.Equal(text, expr.ToString());
            Assert.Equal(0, expr.Errors().Length);

            var qs = (QueryExpressionSyntax)expr;
            Assert.NotNull(qs.Body.SelectOrGroup);
            Assert.IsType<GroupClauseSyntax>(qs.Body.SelectOrGroup);

            var gs = (GroupClauseSyntax)qs.Body.SelectOrGroup;
            Assert.NotNull(gs.GroupExpression);
            Assert.Equal("x", gs.GroupExpression.ToString());
            Assert.Equal("y", gs.ByExpression.ToString());
        }

        [WorkItem(543075, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543075")]
        [Fact]
        public void UnterminatedRankSpecifier()
        {
            var text = "new int[";
            var expr = this.ParseExpression(text);

            Assert.NotNull(expr);
            Assert.Equal(SyntaxKind.ArrayCreationExpression, expr.Kind());

            var arrayCreation = (ArrayCreationExpressionSyntax)expr;
            Assert.Equal(1, arrayCreation.Type.RankSpecifiers.Single().Rank);
        }

        [WorkItem(543075, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543075")]
        [Fact]
        public void UnterminatedTypeArgumentList()
        {
            var text = "new C<";
            var expr = this.ParseExpression(text);

            Assert.NotNull(expr);
            Assert.Equal(SyntaxKind.ObjectCreationExpression, expr.Kind());

            var objectCreation = (ObjectCreationExpressionSyntax)expr;
            Assert.Equal(1, ((NameSyntax)objectCreation.Type).Arity);
        }

        [WorkItem(675602, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/675602")]
        [Fact]
        public void QueryKeywordInObjectInitializer()
        {
            //'on' is a keyword here
            var text = "from elem in aRay select new Result { A = on = true }";
            var expr = this.ParseExpression(text);

            Assert.NotNull(expr);
            Assert.Equal(SyntaxKind.QueryExpression, expr.Kind());
            Assert.Equal(text, expr.ToString());
            Assert.NotEqual(0, expr.Errors().Length);
        }

        [Fact]
        public void IndexingExpressionInParens()
        {
            var text = "(aRay[i,j])";
            var expr = this.ParseExpression(text);

            Assert.NotNull(expr);
            Assert.Equal(SyntaxKind.ParenthesizedExpression, expr.Kind());

            var parenExp = (ParenthesizedExpressionSyntax)expr;
            Assert.Equal(SyntaxKind.ElementAccessExpression, parenExp.Expression.Kind());
        }

        [WorkItem(543993, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543993")]
        [Fact]
        public void ShiftOperator()
        {
            UsingTree(@"
class C
{
    int x = 1 << 2 << 3;
}
");
            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.ClassDeclaration);
                {
                    N(SyntaxKind.ClassKeyword);
                    N(SyntaxKind.IdentifierToken);
                    N(SyntaxKind.OpenBraceToken);

                    N(SyntaxKind.FieldDeclaration);
                    {
                        N(SyntaxKind.VariableDeclaration);
                        {
                            N(SyntaxKind.PredefinedType); N(SyntaxKind.IntKeyword);
                            N(SyntaxKind.VariableDeclarator);
                            {
                                N(SyntaxKind.IdentifierToken);
                                N(SyntaxKind.EqualsValueClause);
                                {
                                    N(SyntaxKind.EqualsToken);
                                    // NB: left associative
                                    N(SyntaxKind.LeftShiftExpression);
                                    {
                                        N(SyntaxKind.LeftShiftExpression);
                                        {
                                            N(SyntaxKind.NumericLiteralExpression); N(SyntaxKind.NumericLiteralToken);
                                            N(SyntaxKind.LessThanLessThanToken);
                                            N(SyntaxKind.NumericLiteralExpression); N(SyntaxKind.NumericLiteralToken);
                                        }
                                        N(SyntaxKind.LessThanLessThanToken);
                                        N(SyntaxKind.NumericLiteralExpression); N(SyntaxKind.NumericLiteralToken);
                                    }
                                }
                            }
                            N(SyntaxKind.SemicolonToken);
                        }
                    }

                    N(SyntaxKind.CloseBraceToken);
                }
                N(SyntaxKind.EndOfFileToken);
            }
        }

        [WorkItem(1091974, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1091974")]
        [Fact]
        public void ParseBigExpression()
        {
            var text = @"

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace WB.Core.SharedKernels.DataCollection.Generated
{
   internal partial class QuestionnaireTopLevel
   {   
      private bool IsValid_a()
      {
            return (stackDepth == 100) || ((stackDepth == 200) || ((stackDepth == 300) || ((stackDepth == 400) || ((stackDepth == 501) || ((stackDepth == 502) || ((stackDepth == 600) || ((stackDepth == 701) || ((stackDepth == 702) || ((stackDepth == 801) || ((stackDepth == 802) || ((stackDepth == 901) || ((stackDepth == 902) || ((stackDepth == 903) || ((stackDepth == 1001) || ((stackDepth == 1002) || ((stackDepth == 1101) || ((stackDepth == 1102) || ((stackDepth == 1201) || ((stackDepth == 1202) || ((stackDepth == 1301) || ((stackDepth == 1302) || ((stackDepth == 1401) || ((stackDepth == 1402) || ((stackDepth == 1403) || ((stackDepth == 1404) || ((stackDepth == 1405) || ((stackDepth == 1406) || ((stackDepth == 1407) || ((stackDepth == 1408) || ((stackDepth == 1409) || ((stackDepth == 1410) || ((stackDepth == 1411) || ((stackDepth == 1412) || ((stackDepth == 1413) || ((stackDepth == 1500) || ((stackDepth == 1601) || ((stackDepth == 1602) || ((stackDepth == 1701) || ((stackDepth == 1702) || ((stackDepth == 1703) || ((stackDepth == 1800) || ((stackDepth == 1901) || ((stackDepth == 1902) || ((stackDepth == 1903) || ((stackDepth == 1904) || ((stackDepth == 2000) || ((stackDepth == 2101) || ((stackDepth == 2102) || ((stackDepth == 2103) || ((stackDepth == 2104) || ((stackDepth == 2105) || ((stackDepth == 2106) || ((stackDepth == 2107) || ((stackDepth == 2201) || ((stackDepth == 2202) || ((stackDepth == 2203) || ((stackDepth == 2301) || ((stackDepth == 2302) || ((stackDepth == 2303) || ((stackDepth == 2304) || ((stackDepth == 2305) || ((stackDepth == 2401) || ((stackDepth == 2402) || ((stackDepth == 2403) || ((stackDepth == 2404) || ((stackDepth == 2501) || ((stackDepth == 2502) || ((stackDepth == 2503) || ((stackDepth == 2504) || ((stackDepth == 2505) || ((stackDepth == 2601) || ((stackDepth == 2602) || ((stackDepth == 2603) || ((stackDepth == 2604) || ((stackDepth == 2605) || ((stackDepth == 2606) || ((stackDepth == 2607) || ((stackDepth == 2608) || ((stackDepth == 2701) || ((stackDepth == 2702) || ((stackDepth == 2703) || ((stackDepth == 2704) || ((stackDepth == 2705) || ((stackDepth == 2706) || ((stackDepth == 2801) || ((stackDepth == 2802) || ((stackDepth == 2803) || ((stackDepth == 2804) || ((stackDepth == 2805) || ((stackDepth == 2806) || ((stackDepth == 2807) || ((stackDepth == 2808) || ((stackDepth == 2809) || ((stackDepth == 2810) || ((stackDepth == 2901) || ((stackDepth == 2902) || ((stackDepth == 3001) || ((stackDepth == 3002) || ((stackDepth == 3101) || ((stackDepth == 3102) || ((stackDepth == 3103) || ((stackDepth == 3104) || ((stackDepth == 3105) || ((stackDepth == 3201) || ((stackDepth == 3202) || ((stackDepth == 3203) || ((stackDepth == 3301) || ((stackDepth == 3302) || ((stackDepth == 3401) || ((stackDepth == 3402) || ((stackDepth == 3403) || ((stackDepth == 3404) || ((stackDepth == 3405) || ((stackDepth == 3406) || ((stackDepth == 3407) || ((stackDepth == 3408) || ((stackDepth == 3409) || ((stackDepth == 3410) || ((stackDepth == 3501) || ((stackDepth == 3502) || ((stackDepth == 3503) || ((stackDepth == 3504) || ((stackDepth == 3505) || ((stackDepth == 3506) || ((stackDepth == 3507) || ((stackDepth == 3508) || ((stackDepth == 3509) || ((stackDepth == 3601) || ((stackDepth == 3602) || ((stackDepth == 3701) || ((stackDepth == 3702) || ((stackDepth == 3703) || ((stackDepth == 3704) || ((stackDepth == 3705) || ((stackDepth == 3706) || ((stackDepth == 3801) || ((stackDepth == 3802) || ((stackDepth == 3803) || ((stackDepth == 3804) || ((stackDepth == 3805) || ((stackDepth == 3901) || ((stackDepth == 3902) || ((stackDepth == 3903) || ((stackDepth == 3904) || ((stackDepth == 3905) || ((stackDepth == 4001) || ((stackDepth == 4002) || ((stackDepth == 4003) || ((stackDepth == 4004) || ((stackDepth == 4005) || ((stackDepth == 4006) || ((stackDepth == 4007) || ((stackDepth == 4100) || ((stackDepth == 4201) || ((stackDepth == 4202) || ((stackDepth == 4203) || ((stackDepth == 4204) || ((stackDepth == 4301) || ((stackDepth == 4302) || ((stackDepth == 4304) || ((stackDepth == 4401) || ((stackDepth == 4402) || ((stackDepth == 4403) || ((stackDepth == 4404) || ((stackDepth == 4501) || ((stackDepth == 4502) || ((stackDepth == 4503) || ((stackDepth == 4504) || ((stackDepth == 4600) || ((stackDepth == 4701) || ((stackDepth == 4702) || ((stackDepth == 4801) || ((stackDepth == 4802) || ((stackDepth == 4803) || ((stackDepth == 4804) || ((stackDepth == 4805) || ((stackDepth == 4806) || ((stackDepth == 4807) || ((stackDepth == 4808) || ((stackDepth == 4809) || ((stackDepth == 4811) || ((stackDepth == 4901) || ((stackDepth == 4902) || ((stackDepth == 4903) || ((stackDepth == 4904) || ((stackDepth == 4905) || ((stackDepth == 4906) || ((stackDepth == 4907) || ((stackDepth == 4908) || ((stackDepth == 4909) || ((stackDepth == 4910) || ((stackDepth == 4911) || ((stackDepth == 4912) || ((stackDepth == 4913) || ((stackDepth == 4914) || ((stackDepth == 4915) || ((stackDepth == 4916) || ((stackDepth == 4917) || ((stackDepth == 4918) || ((stackDepth == 4919) || ((stackDepth == 4920) || ((stackDepth == 4921) || ((stackDepth == 4922) || ((stackDepth == 4923) || ((stackDepth == 5001) || ((stackDepth == 5002) || ((stackDepth == 5003) || ((stackDepth == 5004) || ((stackDepth == 5005) || ((stackDepth == 5006) || ((stackDepth == 5100) || ((stackDepth == 5200) || ((stackDepth == 5301) || ((stackDepth == 5302) || ((stackDepth == 5400) || ((stackDepth == 5500) || ((stackDepth == 5600) || ((stackDepth == 5700) || ((stackDepth == 5801) || ((stackDepth == 5802) || ((stackDepth == 5901) || ((stackDepth == 5902) || ((stackDepth == 6001) || ((stackDepth == 6002) || ((stackDepth == 6101) || ((stackDepth == 6102) || ((stackDepth == 6201) || ((stackDepth == 6202) || ((stackDepth == 6203) || ((stackDepth == 6204) || ((stackDepth == 6205) || ((stackDepth == 6301) || ((stackDepth == 6302) || ((stackDepth == 6401) || ((stackDepth == 6402) || ((stackDepth == 6501) || ((stackDepth == 6502) || ((stackDepth == 6503) || ((stackDepth == 6504) || ((stackDepth == 6601) || ((stackDepth == 6602) || ((stackDepth == 6701) || ((stackDepth == 6702) || ((stackDepth == 6703) || ((stackDepth == 6704) || ((stackDepth == 6801) || ((stackDepth == 6802) || ((stackDepth == 6901) || ((stackDepth == 6902) || ((stackDepth == 6903) || ((stackDepth == 6904) || ((stackDepth == 7001) || ((stackDepth == 7002) || ((stackDepth == 7101) || ((stackDepth == 7102) || ((stackDepth == 7103) || ((stackDepth == 7200) || ((stackDepth == 7301) || ((stackDepth == 7302) || ((stackDepth == 7400) || ((stackDepth == 7501) || ((stackDepth == 7502) || ((stackDepth == 7503) || ((stackDepth == 7600) || ((stackDepth == 7700) || ((stackDepth == 7800) || ((stackDepth == 7900) || ((stackDepth == 8001) || ((stackDepth == 8002) || ((stackDepth == 8101) || ((stackDepth == 8102) || ((stackDepth == 8103) || ((stackDepth == 8200) || ((stackDepth == 8300) || ((stackDepth == 8400) || ((stackDepth == 8501) || ((stackDepth == 8502) || ((stackDepth == 8601) || ((stackDepth == 8602) || ((stackDepth == 8700) || ((stackDepth == 8801) || ((stackDepth == 8802) || ((stackDepth == 8901) || ((stackDepth == 8902) || ((stackDepth == 8903) || ((stackDepth == 9001) || ((stackDepth == 9002) || ((stackDepth == 9003) || ((stackDepth == 9004) || ((stackDepth == 9005) || ((stackDepth == 9101) || ((stackDepth == 9102) || ((stackDepth == 9200) || ((stackDepth == 9300) || ((stackDepth == 9401) || ((stackDepth == 9402) || ((stackDepth == 9403) || ((stackDepth == 9500) || ((stackDepth == 9601) || ((stackDepth == 9602) || ((stackDepth == 9701) || ((stackDepth == 9702) || ((stackDepth == 9801) || ((stackDepth == 9802) || ((stackDepth == 9900) || ((stackDepth == 10000) || ((stackDepth == 10100) || ((stackDepth == 10201) || ((stackDepth == 10202) || ((stackDepth == 10301) || ((stackDepth == 10302) || ((stackDepth == 10401) || ((stackDepth == 10402) || ((stackDepth == 10403) || ((stackDepth == 10501) || ((stackDepth == 10502) || ((stackDepth == 10601) || ((stackDepth == 10602) || ((stackDepth == 10701) || ((stackDepth == 10702) || ((stackDepth == 10703) || ((stackDepth == 10704) || ((stackDepth == 10705) || ((stackDepth == 10706) || ((stackDepth == 10801) || ((stackDepth == 10802) || ((stackDepth == 10803) || ((stackDepth == 10804) || ((stackDepth == 10805) || ((stackDepth == 10806) || ((stackDepth == 10807) || ((stackDepth == 10808) || ((stackDepth == 10809) || ((stackDepth == 10900) || ((stackDepth == 11000) || ((stackDepth == 11100) || ((stackDepth == 11201) || ((stackDepth == 11202) || ((stackDepth == 11203) || ((stackDepth == 11204) || ((stackDepth == 11205) || ((stackDepth == 11206) || ((stackDepth == 11207) || ((stackDepth == 11208) || ((stackDepth == 11209) || ((stackDepth == 11210) || ((stackDepth == 11211) || ((stackDepth == 11212) || ((stackDepth == 11213) || ((stackDepth == 11214) || ((stackDepth == 11301) || ((stackDepth == 11302) || ((stackDepth == 11303) || ((stackDepth == 11304) || ((stackDepth == 11305) || ((stackDepth == 11306) || ((stackDepth == 11307) || ((stackDepth == 11308) || ((stackDepth == 11309) || ((stackDepth == 11401) || ((stackDepth == 11402) || ((stackDepth == 11403) || ((stackDepth == 11404) || ((stackDepth == 11501) || ((stackDepth == 11502) || ((stackDepth == 11503) || ((stackDepth == 11504) || ((stackDepth == 11505) || ((stackDepth == 11601) || ((stackDepth == 11602) || ((stackDepth == 11603) || ((stackDepth == 11604) || ((stackDepth == 11605) || ((stackDepth == 11606) || ((stackDepth == 11701) || ((stackDepth == 11702) || ((stackDepth == 11800) || ((stackDepth == 11901) || ((stackDepth == 11902) || ((stackDepth == 11903) || ((stackDepth == 11904) || ((stackDepth == 11905) || ((stackDepth == 12001) || ((stackDepth == 12002) || ((stackDepth == 12003) || ((stackDepth == 12004) || ((stackDepth == 12101) || ((stackDepth == 12102) || ((stackDepth == 12103) || ((stackDepth == 12104) || ((stackDepth == 12105) || ((stackDepth == 12106) || ((stackDepth == 12107) || ((stackDepth == 12108) || ((stackDepth == 12109) || ((stackDepth == 12110) || ((stackDepth == 12111) || ((stackDepth == 12112) || ((stackDepth == 12113) || ((stackDepth == 12114) || ((stackDepth == 12115) || ((stackDepth == 12116) || ((stackDepth == 12201) || ((stackDepth == 12202) || ((stackDepth == 12203) || ((stackDepth == 12204) || ((stackDepth == 12205) || ((stackDepth == 12301) || ((stackDepth == 12302) || ((stackDepth == 12401) || ((stackDepth == 12402) || ((stackDepth == 12403) || ((stackDepth == 12404) || ((stackDepth == 12405) || ((stackDepth == 12406) || ((stackDepth == 12501) || ((stackDepth == 12502) || ((stackDepth == 12601) || ((stackDepth == 12602) || ((stackDepth == 12603) || ((stackDepth == 12700) || ((stackDepth == 12800) || ((stackDepth == 12900) || ((stackDepth == 13001) || ((stackDepth == 13002) || ((stackDepth == 13003) || ((stackDepth == 13004) || ((stackDepth == 13005) || ((stackDepth == 13101) || ((stackDepth == 13102) || ((stackDepth == 13103) || ((stackDepth == 13201) || ((stackDepth == 13202) || ((stackDepth == 13203) || ((stackDepth == 13301) || ((stackDepth == 13302) || ((stackDepth == 13303) || ((stackDepth == 13304) || ((stackDepth == 13401) || ((stackDepth == 13402) || ((stackDepth == 13403) || ((stackDepth == 13404) || ((stackDepth == 13405) || ((stackDepth == 13501) || ((stackDepth == 13502) || ((stackDepth == 13600) || ((stackDepth == 13701) || ((stackDepth == 13702) || ((stackDepth == 13703) || ((stackDepth == 13800) || ((stackDepth == 13901) || ((stackDepth == 13902) || ((stackDepth == 13903) || ((stackDepth == 14001) || ((stackDepth == 14002) || ((stackDepth == 14100) || ((stackDepth == 14200) || ((stackDepth == 14301) || ((stackDepth == 14302) || ((stackDepth == 14400) || ((stackDepth == 14501) || ((stackDepth == 14502) || ((stackDepth == 14601) || ((stackDepth == 14602) || ((stackDepth == 14603) || ((stackDepth == 14604) || ((stackDepth == 14605) || ((stackDepth == 14606) || ((stackDepth == 14607) || ((stackDepth == 14701) || ((stackDepth == 14702) || ((stackDepth == 14703) || ((stackDepth == 14704) || ((stackDepth == 14705) || ((stackDepth == 14706) || ((stackDepth == 14707) || ((stackDepth == 14708) || ((stackDepth == 14709) || ((stackDepth == 14710) || ((stackDepth == 14711) || ((stackDepth == 14712) || ((stackDepth == 14713) || ((stackDepth == 14714) || ((stackDepth == 14715) || ((stackDepth == 14716) || ((stackDepth == 14717) || ((stackDepth == 14718) || ((stackDepth == 14719) || ((stackDepth == 14720 || ((stackDepth == 14717 || ((stackDepth == 14717) || ((stackDepth == 14717) || ((stackDepth == 14717) || ((stackDepth == 14717) || ((stackDepth == 14717) || ((stackDepth == 14717) || ((stackDepth == 14717) || ((stackDepth == 14717) || ((stackDepth == 14717) || ((stackDepth == 14717) || ((stackDepth == 14717) || ((stackDepth == 14717) || ((stackDepth == 14717) || ((stackDepth == 14717) || ((stackDepth == 14717) || ((stackDepth == 14717) || ((stackDepth == 14717) || ((stackDepth == 14717) || ((stackDepth == 14717) || ((stackDepth == 14717) || ((stackDepth == 14717) || ((stackDepth == 14717) || ((stackDepth == 14717) || ((stackDepth == 14717) || ((stackDepth == 14717) || ((stackDepth == 14717) || ((stackDepth == 14717) || ((stackDepth == 14717) || ((stackDepth == 14717) || ((stackDepth == 14717 || ((stackDepth == 14717) || ((stackDepth == 14717) || ((stackDepth == 14717) || ((stackDepth == 14717) || ((stackDepth == 14717) || ((stackDepth == 14717) || ((stackDepth == 14717) || ((stackDepth == 14717) || ((stackDepth == 14717) || ((stackDepth == 14717) || ((stackDepth == 14717) || ((stackDepth == 14717) || ((stackDepth == 14717) || ((stackDepth == 14717) || ((stackDepth == 14717) || ((stackDepth == 14717) || ((stackDepth == 14717) || ((stackDepth == 14717) || ((stackDepth == 14717) || ((stackDepth == 14717) || ((stackDepth == 14717) || ((stackDepth == 14717) || ((stackDepth == 14717) || ((stackDepth == 14717) || ((stackDepth == 14717) || ((stackDepth == 14717) || ((stackDepth == 14717) || ((stackDepth == 14717) || ((stackDepth == 14717) || ((stackDepth == 14717) || ((stackDepth == 14717) || ((stackDepth == 14717) || ((stackDepth == 14717) || ((stackDepth == 14717) || ((stackDepth == 14717) || ((stackDepth == 14717) || ((stackDepth == 14717) || ((stackDepth == 14717) || ((stackDepth == 14717) || ((stackDepth == 14717) || ((stackDepth == 14717) || ((stackDepth == 14717) || ((stackDepth == 14717) || ((stackDepth == 14717) || ((stackDepth == 14717) || ((stackDepth == 14717) || ((stackDepth == 14717) || ((stackDepth == 14717) || ((stackDepth == 14717) || ((stackDepth == 14717 || ((stackDepth == 14717) || ((stackDepth == 14717) || ((stackDepth == 14717) || ((stackDepth == 14717) || ((stackDepth == 14717) || ((stackDepth == 14717) || ((stackDepth == 14717) || ((stackDepth == 14717) || ((stackDepth == 14717) || ((stackDepth == 14717) || ((stackDepth == 14717) || ((stackDepth == 14717) || ((stackDepth == 14717) || ((stackDepth == 14717) || ((stackDepth == 14717) || ((stackDepth == 14717) || ((stackDepth == 14717) || ((stackDepth == 14717) || ((stackDepth == 14717) || ((stackDepth == 14717) || ((stackDepth == 14717) || ((stackDepth == 14717) || ((stackDepth == 14717) || ((stackDepth == 14717) || ((stackDepth == 14717) || ((stackDepth == 14717) || ((stackDepth == 14717) || ((stackDepth == 14717) || ((stackDepth == 14717) || ((stackDepth == 14717) || ((stackDepth == 14717) || ((stackDepth == 14717) || ((stackDepth == 14717) || ((stackDepth == 14717) || ((stackDepth == 14717) || ((stackDepth == 14717) || ((stackDepth == 14717) || ((stackDepth == 14717) || ((stackDepth == 14717) || ((stackDepth == 14717) || ((stackDepth == 14717) || ((stackDepth == 14717) || ((stackDepth == 14717) || ((stackDepth == 14717) || ((stackDepth == 14717) || ((stackDepth == 14717) || ((stackDepth == 14717) || ((stackDepth == 14717) || ((stackDepth == 14717) || ((stackDepth == 14717) || ((stackDepth == 14717) || ((stackDepth == 14717) || ((stackDepth == 14717) || ((stackDepth == 14717) || ((stackDepth == 14717) || ((stackDepth == 14717) || ((stackDepth == 14717) || ((stackDepth == 14717) || ((stackDepth == 14717) || ((stackDepth == 14717) || ((stackDepth == 14717) || ((stackDepth == 14717) || ((stackDepth == 14717) || ((stackDepth == 14717) || ((stackDepth == 14717) || ((stackDepth == 14717) || ((stackDepth == 14717) || ((stackDepth == 14717) || ((stackDepth == 14717) || ((stackDepth == 14717) || ((stackDepth == 14717) || ((stackDepth == 14717) || ((stackDepth == 14717) || ((stackDepth == 14717) || ((stackDepth == 14717) || ((stackDepth == 14717) || ((stackDepth == 14717) || ((stackDepth == 14717) || ((stackDepth == 14717) || ((stackDepth == 14717) || ((stackDepth == 14717) || ((stackDepth == 14717) || ((stackDepth == 14717) || ((stackDepth == 14717) || ((stackDepth == 14717) || ((stackDepth == 14717) || ((stackDepth == 14717) || ((stackDepth == 14717) || ((stackDepth == 14717) || ((stackDepth == 14717) || ((stackDepth == 14717) || ((stackDepth == 14717) || ((stackDepth == 14717) || ((stackDepth == 14717) || ((stackDepth == 14717) || ((stackDepth == 14717) || ((stackDepth == 14717) || ((stackDepth == 14717) || ((stackDepth == 14717) || ((stackDepth == 14717) || ((stackDepth == 14717) || ((stackDepth == 14717) || ((stackDepth == 14717) || ((stackDepth == 14717) || ((stackDepth == 14717) || ((stackDepth == 14717) || ((stackDepth == 14717) || ((stackDepth == 14717) || ((stackDepth == 14717) || ((stackDepth == 14717) || ((stackDepth == 14717) || ((stackDepth == 14717) || ((stackDepth == 14717) || ((stackDepth == 14717) || ((stackDepth == 14717) || ((stackDepth == 14717) || ((stackDepth == 14717) || ((stackDepth == 14717) || ((stackDepth == 14717) || ((stackDepth == 14717) || ((stackDepth == 14717) || ((stackDepth == 14717) || ((stackDepth == 14717) || ((stackDepth == 14717) || ((stackDepth == 14717) || ((stackDepth == 14717) || ((stackDepth == 14717) || ((stackDepth == 14717) || ((stackDepth == 14717) || ((stackDepth == 14717) || ((stackDepth == 14717) || ((stackDepth == 14717) || ((stackDepth == 14717) || ((stackDepth == 14717) || ((stackDepth == 14717) || ((stackDepth == 14717) || ((stackDepth == 14717) || ((stackDepth == 14717) || ((stackDepth == 14717) || ((stackDepth == 14717) || ((stackDepth == 14717) || ((stackDepth == 14717) || ((stackDepth == 14717) || ((stackDepth == 14717) || ((stackDepth == 14717) || ((stackDepth == 14717) || ((stackDepth == 14717) || ((stackDepth == 14717) || ((stackDepth == 14717) || ((stackDepth == 14717) || ((stackDepth == 14717) || ((stackDepth == 14717) || ((stackDepth == 14717) || ((stackDepth == 14717) || ((stackDepth == 14717) || ((stackDepth == 14717) || ((stackDepth == 14717) || ((stackDepth == 14717) || ((stackDepth == 14717) || ((stackDepth == 14717) || ((stackDepth == 14717) || ((stackDepth == 14717) || ((stackDepth == 14717) || ((stackDepth == 14717) || ((stackDepth == 14717))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))))));
      }      
   }
}
";
            var root = SyntaxFactory.ParseSyntaxTree(text).GetRoot();

            Assert.NotNull(root);
            Assert.Equal(SyntaxKind.CompilationUnit, root.Kind());
        }

        [Fact, WorkItem(15885, "https://github.com/dotnet/roslyn/pull/15885")]
        public void InProgressLocalDeclaration1()
        {
            const string text = @"
class C
{
    async void M()
    {
        Task.
        await Task.Delay();
    }
}
";
            ParseAndValidate(text,
                // (6,14): error CS1001: Identifier expected
                //         Task.
                Diagnostic(ErrorCode.ERR_IdentifierExpected, "").WithLocation(6, 14),
                // (6,14): error CS1002: ; expected
                //         Task.
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "").WithLocation(6, 14));

            UsingTree(text);
            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.ClassDeclaration);
                {
                    N(SyntaxKind.ClassKeyword);
                    N(SyntaxKind.IdentifierToken, "C");
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.MethodDeclaration);
                    {
                        N(SyntaxKind.AsyncKeyword);
                        N(SyntaxKind.PredefinedType);
                        {
                            N(SyntaxKind.VoidKeyword);
                        }
                        N(SyntaxKind.IdentifierToken, "M");
                        N(SyntaxKind.ParameterList);
                        {
                            N(SyntaxKind.OpenParenToken);
                            N(SyntaxKind.CloseParenToken);
                        }
                        N(SyntaxKind.Block);
                        {
                            N(SyntaxKind.OpenBraceToken);
                            N(SyntaxKind.ExpressionStatement);
                            {
                                N(SyntaxKind.SimpleMemberAccessExpression);
                                {
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "Task");
                                    }
                                    N(SyntaxKind.DotToken);
                                    M(SyntaxKind.IdentifierName);
                                    {
                                        M(SyntaxKind.IdentifierToken);
                                    }
                                }
                                M(SyntaxKind.SemicolonToken);
                            }
                            N(SyntaxKind.ExpressionStatement);
                            {
                                N(SyntaxKind.AwaitExpression);
                                {
                                    N(SyntaxKind.AwaitKeyword);
                                    N(SyntaxKind.InvocationExpression);
                                    {
                                        N(SyntaxKind.SimpleMemberAccessExpression);
                                        {
                                            N(SyntaxKind.IdentifierName);
                                            {
                                                N(SyntaxKind.IdentifierToken, "Task");
                                            }
                                            N(SyntaxKind.DotToken);
                                            N(SyntaxKind.IdentifierName);
                                            {
                                                N(SyntaxKind.IdentifierToken, "Delay");
                                            }
                                        }
                                        N(SyntaxKind.ArgumentList);
                                        {
                                            N(SyntaxKind.OpenParenToken);
                                            N(SyntaxKind.CloseParenToken);
                                        }
                                    }
                                }
                                N(SyntaxKind.SemicolonToken);
                            }
                            N(SyntaxKind.CloseBraceToken);
                        }
                    }
                    N(SyntaxKind.CloseBraceToken);
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact, WorkItem(15885, "https://github.com/dotnet/roslyn/pull/15885")]
        public void InProgressLocalDeclaration2()
        {
            const string text = @"
class C
{
    async void M()
    {
        Task.await Task.Delay();
    }
}
";
            ParseAndValidate(text,
                // (6,14): error CS4003: 'await' cannot be used as an identifier within an async method or lambda expression
                //         Task.await Task.Delay();
                Diagnostic(ErrorCode.ERR_BadAwaitAsIdentifier, "await").WithLocation(6, 14),
                // (6,24): error CS1003: Syntax error, ',' expected
                //         Task.await Task.Delay();
                Diagnostic(ErrorCode.ERR_SyntaxError, ".").WithArguments(",", ".").WithLocation(6, 24),
                // (6,25): error CS1002: ; expected
                //         Task.await Task.Delay();
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "Delay").WithLocation(6, 25));

            UsingTree(text);
            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.ClassDeclaration);
                {
                    N(SyntaxKind.ClassKeyword);
                    N(SyntaxKind.IdentifierToken, "C");
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.MethodDeclaration);
                    {
                        N(SyntaxKind.AsyncKeyword);
                        N(SyntaxKind.PredefinedType);
                        {
                            N(SyntaxKind.VoidKeyword);
                        }
                        N(SyntaxKind.IdentifierToken, "M");
                        N(SyntaxKind.ParameterList);
                        {
                            N(SyntaxKind.OpenParenToken);
                            N(SyntaxKind.CloseParenToken);
                        }
                        N(SyntaxKind.Block);
                        {
                            N(SyntaxKind.OpenBraceToken);
                            N(SyntaxKind.LocalDeclarationStatement);
                            {
                                N(SyntaxKind.VariableDeclaration);
                                {
                                    N(SyntaxKind.QualifiedName);
                                    {
                                        N(SyntaxKind.IdentifierName);
                                        {
                                            N(SyntaxKind.IdentifierToken, "Task");
                                        }
                                        N(SyntaxKind.DotToken);
                                        N(SyntaxKind.IdentifierName);
                                        {
                                            N(SyntaxKind.IdentifierToken, "await");
                                        }
                                    }
                                    N(SyntaxKind.VariableDeclarator);
                                    {
                                        N(SyntaxKind.IdentifierToken, "Task");
                                    }
                                }
                                M(SyntaxKind.SemicolonToken);
                            }
                            N(SyntaxKind.ExpressionStatement);
                            {
                                N(SyntaxKind.InvocationExpression);
                                {
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "Delay");
                                    }
                                    N(SyntaxKind.ArgumentList);
                                    {
                                        N(SyntaxKind.OpenParenToken);
                                        N(SyntaxKind.CloseParenToken);
                                    }
                                }
                                N(SyntaxKind.SemicolonToken);
                            }
                            N(SyntaxKind.CloseBraceToken);
                        }
                    }
                    N(SyntaxKind.CloseBraceToken);
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact, WorkItem(15885, "https://github.com/dotnet/roslyn/pull/15885")]
        public void InProgressLocalDeclaration3()
        {
            const string text = @"
class C
{
    async void M()
    {
        Task.
        await Task;
    }
}
";
            ParseAndValidate(text,
                // (7,9): error CS4003: 'await' cannot be used as an identifier within an async method or lambda expression
                //         await Task;
                Diagnostic(ErrorCode.ERR_BadAwaitAsIdentifier, "await").WithLocation(7, 9));

            UsingTree(text);
            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.ClassDeclaration);
                {
                    N(SyntaxKind.ClassKeyword);
                    N(SyntaxKind.IdentifierToken, "C");
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.MethodDeclaration);
                    {
                        N(SyntaxKind.AsyncKeyword);
                        N(SyntaxKind.PredefinedType);
                        {
                            N(SyntaxKind.VoidKeyword);
                        }
                        N(SyntaxKind.IdentifierToken, "M");
                        N(SyntaxKind.ParameterList);
                        {
                            N(SyntaxKind.OpenParenToken);
                            N(SyntaxKind.CloseParenToken);
                        }
                        N(SyntaxKind.Block);
                        {
                            N(SyntaxKind.OpenBraceToken);
                            N(SyntaxKind.LocalDeclarationStatement);
                            {
                                N(SyntaxKind.VariableDeclaration);
                                {
                                    N(SyntaxKind.QualifiedName);
                                    {
                                        N(SyntaxKind.IdentifierName);
                                        {
                                            N(SyntaxKind.IdentifierToken, "Task");
                                        }
                                        N(SyntaxKind.DotToken);
                                        N(SyntaxKind.IdentifierName);
                                        {
                                            N(SyntaxKind.IdentifierToken, "await");
                                        }
                                    }
                                    N(SyntaxKind.VariableDeclarator);
                                    {
                                        N(SyntaxKind.IdentifierToken, "Task");
                                    }
                                }
                                N(SyntaxKind.SemicolonToken);
                            }
                            N(SyntaxKind.CloseBraceToken);
                        }
                    }
                    N(SyntaxKind.CloseBraceToken);
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact, WorkItem(15885, "https://github.com/dotnet/roslyn/pull/15885")]
        public void InProgressLocalDeclaration4()
        {
            const string text = @"
class C
{
    async void M()
    {
        Task.
        await Task = 1;
    }
}
";
            ParseAndValidate(text,
                // (7,9): error CS4003: 'await' cannot be used as an identifier within an async method or lambda expression
                //         await Task = 1;
                Diagnostic(ErrorCode.ERR_BadAwaitAsIdentifier, "await").WithLocation(7, 9));

            UsingTree(text);
            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.ClassDeclaration);
                {
                    N(SyntaxKind.ClassKeyword);
                    N(SyntaxKind.IdentifierToken, "C");
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.MethodDeclaration);
                    {
                        N(SyntaxKind.AsyncKeyword);
                        N(SyntaxKind.PredefinedType);
                        {
                            N(SyntaxKind.VoidKeyword);
                        }
                        N(SyntaxKind.IdentifierToken, "M");
                        N(SyntaxKind.ParameterList);
                        {
                            N(SyntaxKind.OpenParenToken);
                            N(SyntaxKind.CloseParenToken);
                        }
                        N(SyntaxKind.Block);
                        {
                            N(SyntaxKind.OpenBraceToken);
                            N(SyntaxKind.LocalDeclarationStatement);
                            {
                                N(SyntaxKind.VariableDeclaration);
                                {
                                    N(SyntaxKind.QualifiedName);
                                    {
                                        N(SyntaxKind.IdentifierName);
                                        {
                                            N(SyntaxKind.IdentifierToken, "Task");
                                        }
                                        N(SyntaxKind.DotToken);
                                        N(SyntaxKind.IdentifierName);
                                        {
                                            N(SyntaxKind.IdentifierToken, "await");
                                        }
                                    }
                                    N(SyntaxKind.VariableDeclarator);
                                    {
                                        N(SyntaxKind.IdentifierToken, "Task");
                                        N(SyntaxKind.EqualsValueClause);
                                        {
                                            N(SyntaxKind.EqualsToken);
                                            N(SyntaxKind.NumericLiteralExpression);
                                            {
                                                N(SyntaxKind.NumericLiteralToken, "1");
                                            }
                                        }
                                    }
                                }
                                N(SyntaxKind.SemicolonToken);
                            }
                            N(SyntaxKind.CloseBraceToken);
                        }
                    }
                    N(SyntaxKind.CloseBraceToken);
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact, WorkItem(15885, "https://github.com/dotnet/roslyn/pull/15885")]
        public void InProgressLocalDeclaration5()
        {
            const string text = @"
class C
{
    async void M()
    {
        Task.
        await Task, Task2;
    }
}
";
            ParseAndValidate(text,
                // (7,9): error CS4003: 'await' cannot be used as an identifier within an async method or lambda expression
                //         await Task, Task2;
                Diagnostic(ErrorCode.ERR_BadAwaitAsIdentifier, "await").WithLocation(7, 9));

            UsingTree(text);
            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.ClassDeclaration);
                {
                    N(SyntaxKind.ClassKeyword);
                    N(SyntaxKind.IdentifierToken, "C");
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.MethodDeclaration);
                    {
                        N(SyntaxKind.AsyncKeyword);
                        N(SyntaxKind.PredefinedType);
                        {
                            N(SyntaxKind.VoidKeyword);
                        }
                        N(SyntaxKind.IdentifierToken, "M");
                        N(SyntaxKind.ParameterList);
                        {
                            N(SyntaxKind.OpenParenToken);
                            N(SyntaxKind.CloseParenToken);
                        }
                        N(SyntaxKind.Block);
                        {
                            N(SyntaxKind.OpenBraceToken);
                            N(SyntaxKind.LocalDeclarationStatement);
                            {
                                N(SyntaxKind.VariableDeclaration);
                                {
                                    N(SyntaxKind.QualifiedName);
                                    {
                                        N(SyntaxKind.IdentifierName);
                                        {
                                            N(SyntaxKind.IdentifierToken, "Task");
                                        }
                                        N(SyntaxKind.DotToken);
                                        N(SyntaxKind.IdentifierName);
                                        {
                                            N(SyntaxKind.IdentifierToken, "await");
                                        }
                                    }
                                    N(SyntaxKind.VariableDeclarator);
                                    {
                                        N(SyntaxKind.IdentifierToken, "Task");
                                    }
                                    N(SyntaxKind.CommaToken);
                                    N(SyntaxKind.VariableDeclarator);
                                    {
                                        N(SyntaxKind.IdentifierToken, "Task2");
                                    }
                                }
                                N(SyntaxKind.SemicolonToken);
                            }
                            N(SyntaxKind.CloseBraceToken);
                        }
                    }
                    N(SyntaxKind.CloseBraceToken);
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact, WorkItem(15885, "https://github.com/dotnet/roslyn/pull/15885")]
        public void InProgressLocalDeclaration6()
        {
            const string text = @"
class C
{
    async void M()
    {
        Task.
        await Task();
    }
}
";
            ParseAndValidate(text,
                // (7,9): error CS4003: 'await' cannot be used as an identifier within an async method or lambda expression
                //         await Task();
                Diagnostic(ErrorCode.ERR_BadAwaitAsIdentifier, "await").WithLocation(7, 9));

            UsingTree(text);
            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.ClassDeclaration);
                {
                    N(SyntaxKind.ClassKeyword);
                    N(SyntaxKind.IdentifierToken, "C");
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.MethodDeclaration);
                    {
                        N(SyntaxKind.AsyncKeyword);
                        N(SyntaxKind.PredefinedType);
                        {
                            N(SyntaxKind.VoidKeyword);
                        }
                        N(SyntaxKind.IdentifierToken, "M");
                        N(SyntaxKind.ParameterList);
                        {
                            N(SyntaxKind.OpenParenToken);
                            N(SyntaxKind.CloseParenToken);
                        }
                        N(SyntaxKind.Block);
                        {
                            N(SyntaxKind.OpenBraceToken);
                            N(SyntaxKind.LocalFunctionStatement);
                            {
                                N(SyntaxKind.QualifiedName);
                                {
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "Task");
                                    }
                                    N(SyntaxKind.DotToken);
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "await");
                                    }
                                }
                                N(SyntaxKind.IdentifierToken, "Task");
                                N(SyntaxKind.ParameterList);
                                {
                                    N(SyntaxKind.OpenParenToken);
                                    N(SyntaxKind.CloseParenToken);
                                }
                                N(SyntaxKind.SemicolonToken);
                            }
                            N(SyntaxKind.CloseBraceToken);
                        }
                    }
                    N(SyntaxKind.CloseBraceToken);
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact, WorkItem(15885, "https://github.com/dotnet/roslyn/pull/15885")]
        public void InProgressLocalDeclaration7()
        {
            const string text = @"
class C
{
    async void M()
    {
        Task.
        await Task<T>();
    }
}
";
            ParseAndValidate(text,
                // (7,9): error CS4003: 'await' cannot be used as an identifier within an async method or lambda expression
                //         await Task<T>();
                Diagnostic(ErrorCode.ERR_BadAwaitAsIdentifier, "await").WithLocation(7, 9));

            UsingTree(text);
            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.ClassDeclaration);
                {
                    N(SyntaxKind.ClassKeyword);
                    N(SyntaxKind.IdentifierToken, "C");
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.MethodDeclaration);
                    {
                        N(SyntaxKind.AsyncKeyword);
                        N(SyntaxKind.PredefinedType);
                        {
                            N(SyntaxKind.VoidKeyword);
                        }
                        N(SyntaxKind.IdentifierToken, "M");
                        N(SyntaxKind.ParameterList);
                        {
                            N(SyntaxKind.OpenParenToken);
                            N(SyntaxKind.CloseParenToken);
                        }
                        N(SyntaxKind.Block);
                        {
                            N(SyntaxKind.OpenBraceToken);
                            N(SyntaxKind.LocalFunctionStatement);
                            {
                                N(SyntaxKind.QualifiedName);
                                {
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "Task");
                                    }
                                    N(SyntaxKind.DotToken);
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "await");
                                    }
                                }
                                N(SyntaxKind.IdentifierToken, "Task");
                                N(SyntaxKind.TypeParameterList);
                                {
                                    N(SyntaxKind.LessThanToken);
                                    N(SyntaxKind.TypeParameter);
                                    {
                                        N(SyntaxKind.IdentifierToken, "T");
                                    }
                                    N(SyntaxKind.GreaterThanToken);
                                }
                                N(SyntaxKind.ParameterList);
                                {
                                    N(SyntaxKind.OpenParenToken);
                                    N(SyntaxKind.CloseParenToken);
                                }
                                N(SyntaxKind.SemicolonToken);
                            }
                            N(SyntaxKind.CloseBraceToken);
                        }
                    }
                    N(SyntaxKind.CloseBraceToken);
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact, WorkItem(15885, "https://github.com/dotnet/roslyn/pull/15885")]
        public void InProgressLocalDeclaration8()
        {
            const string text = @"
class C
{
    async void M()
    {
        Task.
        await Task[1];
    }
}
";
            ParseAndValidate(text,
                // (6,14): error CS1001: Identifier expected
                //         Task.
                Diagnostic(ErrorCode.ERR_IdentifierExpected, "").WithLocation(6, 14),
                // (6,14): error CS1002: ; expected
                //         Task.
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "").WithLocation(6, 14));

            UsingTree(text);
            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.ClassDeclaration);
                {
                    N(SyntaxKind.ClassKeyword);
                    N(SyntaxKind.IdentifierToken, "C");
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.MethodDeclaration);
                    {
                        N(SyntaxKind.AsyncKeyword);
                        N(SyntaxKind.PredefinedType);
                        {
                            N(SyntaxKind.VoidKeyword);
                        }
                        N(SyntaxKind.IdentifierToken, "M");
                        N(SyntaxKind.ParameterList);
                        {
                            N(SyntaxKind.OpenParenToken);
                            N(SyntaxKind.CloseParenToken);
                        }
                        N(SyntaxKind.Block);
                        {
                            N(SyntaxKind.OpenBraceToken);
                            N(SyntaxKind.ExpressionStatement);
                            {
                                N(SyntaxKind.SimpleMemberAccessExpression);
                                {
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "Task");
                                    }
                                    N(SyntaxKind.DotToken);
                                    M(SyntaxKind.IdentifierName);
                                    {
                                        M(SyntaxKind.IdentifierToken);
                                    }
                                }
                                M(SyntaxKind.SemicolonToken);
                            }
                            N(SyntaxKind.ExpressionStatement);
                            {
                                N(SyntaxKind.AwaitExpression);
                                {
                                    N(SyntaxKind.AwaitKeyword);
                                    N(SyntaxKind.ElementAccessExpression);
                                    {
                                        N(SyntaxKind.IdentifierName);
                                        {
                                            N(SyntaxKind.IdentifierToken, "Task");
                                        }
                                        N(SyntaxKind.BracketedArgumentList);
                                        {
                                            N(SyntaxKind.OpenBracketToken);
                                            N(SyntaxKind.Argument);
                                            {
                                                N(SyntaxKind.NumericLiteralExpression);
                                                {
                                                    N(SyntaxKind.NumericLiteralToken, "1");
                                                }
                                            }
                                            N(SyntaxKind.CloseBracketToken);
                                        }
                                    }
                                }
                                N(SyntaxKind.SemicolonToken);
                            }
                            N(SyntaxKind.CloseBraceToken);
                        }
                    }
                    N(SyntaxKind.CloseBraceToken);
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact, WorkItem(377556, "https://devdiv.visualstudio.com/DefaultCollection/DevDiv/_workitems?id=377556")]
        public void TypeArgumentShiftAmbiguity_01()
        {
            const string text = @"
class C
{
    void M()
    {
        //int a = 1;
        //int i = 1;
        var j = a < i >> 2;
    }
}
";
            var tree = UsingTree(text);
            tree.GetDiagnostics().Verify();
            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.ClassDeclaration);
                {
                    N(SyntaxKind.ClassKeyword);
                    N(SyntaxKind.IdentifierToken, "C");
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.MethodDeclaration);
                    {
                        N(SyntaxKind.PredefinedType);
                        {
                            N(SyntaxKind.VoidKeyword);
                        }
                        N(SyntaxKind.IdentifierToken, "M");
                        N(SyntaxKind.ParameterList);
                        {
                            N(SyntaxKind.OpenParenToken);
                            N(SyntaxKind.CloseParenToken);
                        }
                        N(SyntaxKind.Block);
                        {
                            N(SyntaxKind.OpenBraceToken);
                            N(SyntaxKind.LocalDeclarationStatement);
                            {
                                N(SyntaxKind.VariableDeclaration);
                                {
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "var");
                                    }
                                    N(SyntaxKind.VariableDeclarator);
                                    {
                                        N(SyntaxKind.IdentifierToken, "j");
                                        N(SyntaxKind.EqualsValueClause);
                                        {
                                            N(SyntaxKind.EqualsToken);
                                            N(SyntaxKind.LessThanExpression);
                                            {
                                                N(SyntaxKind.IdentifierName);
                                                {
                                                    N(SyntaxKind.IdentifierToken, "a");
                                                }
                                                N(SyntaxKind.LessThanToken);
                                                N(SyntaxKind.RightShiftExpression);
                                                {
                                                    N(SyntaxKind.IdentifierName);
                                                    {
                                                        N(SyntaxKind.IdentifierToken, "i");
                                                    }
                                                    N(SyntaxKind.GreaterThanGreaterThanToken);
                                                    N(SyntaxKind.NumericLiteralExpression);
                                                    {
                                                        N(SyntaxKind.NumericLiteralToken, "2");
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                                N(SyntaxKind.SemicolonToken);
                            }
                            N(SyntaxKind.CloseBraceToken);
                        }
                    }
                    N(SyntaxKind.CloseBraceToken);
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact, WorkItem(377556, "https://devdiv.visualstudio.com/DefaultCollection/DevDiv/_workitems?id=377556")]
        public void TypeArgumentShiftAmbiguity_02()
        {
            const string text = @"
class C
{
    void M()
    {
        //const int a = 1;
        //const int i = 2;
        switch (false)
        {
            case a < i >> 2: break;
        }
    }
}
";
            var tree = UsingTree(text);
            tree.GetDiagnostics().Verify();
            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.ClassDeclaration);
                {
                    N(SyntaxKind.ClassKeyword);
                    N(SyntaxKind.IdentifierToken, "C");
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.MethodDeclaration);
                    {
                        N(SyntaxKind.PredefinedType);
                        {
                            N(SyntaxKind.VoidKeyword);
                        }
                        N(SyntaxKind.IdentifierToken, "M");
                        N(SyntaxKind.ParameterList);
                        {
                            N(SyntaxKind.OpenParenToken);
                            N(SyntaxKind.CloseParenToken);
                        }
                        N(SyntaxKind.Block);
                        {
                            N(SyntaxKind.OpenBraceToken);
                            N(SyntaxKind.SwitchStatement);
                            {
                                N(SyntaxKind.SwitchKeyword);
                                N(SyntaxKind.OpenParenToken);
                                N(SyntaxKind.FalseLiteralExpression);
                                {
                                    N(SyntaxKind.FalseKeyword);
                                }
                                N(SyntaxKind.CloseParenToken);
                                N(SyntaxKind.OpenBraceToken);
                                N(SyntaxKind.SwitchSection);
                                {
                                    N(SyntaxKind.CaseSwitchLabel);
                                    {
                                        N(SyntaxKind.CaseKeyword);
                                        N(SyntaxKind.LessThanExpression);
                                        {
                                            N(SyntaxKind.IdentifierName);
                                            {
                                                N(SyntaxKind.IdentifierToken, "a");
                                            }
                                            N(SyntaxKind.LessThanToken);
                                            N(SyntaxKind.RightShiftExpression);
                                            {
                                                N(SyntaxKind.IdentifierName);
                                                {
                                                    N(SyntaxKind.IdentifierToken, "i");
                                                }
                                                N(SyntaxKind.GreaterThanGreaterThanToken);
                                                N(SyntaxKind.NumericLiteralExpression);
                                                {
                                                    N(SyntaxKind.NumericLiteralToken, "2");
                                                }
                                            }
                                        }
                                        N(SyntaxKind.ColonToken);
                                    }
                                    N(SyntaxKind.BreakStatement);
                                    {
                                        N(SyntaxKind.BreakKeyword);
                                        N(SyntaxKind.SemicolonToken);
                                    }
                                }
                                N(SyntaxKind.CloseBraceToken);
                            }
                            N(SyntaxKind.CloseBraceToken);
                        }
                    }
                    N(SyntaxKind.CloseBraceToken);
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact, WorkItem(377556, "https://devdiv.visualstudio.com/DefaultCollection/DevDiv/_workitems?id=377556")]
        public void TypeArgumentShiftAmbiguity_03()
        {
            const string text = @"
class C
{
    void M()
    {
        M(out a < i >> 2);
    }
}
";
            var tree = UsingTree(text);
            tree.GetDiagnostics().Verify();
            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.ClassDeclaration);
                {
                    N(SyntaxKind.ClassKeyword);
                    N(SyntaxKind.IdentifierToken, "C");
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.MethodDeclaration);
                    {
                        N(SyntaxKind.PredefinedType);
                        {
                            N(SyntaxKind.VoidKeyword);
                        }
                        N(SyntaxKind.IdentifierToken, "M");
                        N(SyntaxKind.ParameterList);
                        {
                            N(SyntaxKind.OpenParenToken);
                            N(SyntaxKind.CloseParenToken);
                        }
                        N(SyntaxKind.Block);
                        {
                            N(SyntaxKind.OpenBraceToken);
                            N(SyntaxKind.ExpressionStatement);
                            {
                                N(SyntaxKind.InvocationExpression);
                                {
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "M");
                                    }
                                    N(SyntaxKind.ArgumentList);
                                    {
                                        N(SyntaxKind.OpenParenToken);
                                        N(SyntaxKind.Argument);
                                        {
                                            N(SyntaxKind.OutKeyword);
                                            N(SyntaxKind.LessThanExpression);
                                            {
                                                N(SyntaxKind.IdentifierName);
                                                {
                                                    N(SyntaxKind.IdentifierToken, "a");
                                                }
                                                N(SyntaxKind.LessThanToken);
                                                N(SyntaxKind.RightShiftExpression);
                                                {
                                                    N(SyntaxKind.IdentifierName);
                                                    {
                                                        N(SyntaxKind.IdentifierToken, "i");
                                                    }
                                                    N(SyntaxKind.GreaterThanGreaterThanToken);
                                                    N(SyntaxKind.NumericLiteralExpression);
                                                    {
                                                        N(SyntaxKind.NumericLiteralToken, "2");
                                                    }
                                                }
                                            }
                                        }
                                        N(SyntaxKind.CloseParenToken);
                                    }
                                }
                                N(SyntaxKind.SemicolonToken);
                            }
                            N(SyntaxKind.CloseBraceToken);
                        }
                    }
                    N(SyntaxKind.CloseBraceToken);
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact, WorkItem(377556, "https://devdiv.visualstudio.com/DefaultCollection/DevDiv/_workitems?id=377556")]
        public void TypeArgumentShiftAmbiguity_04()
        {
            const string text = @"
class C
{
    void M()
    {
        // (e is a<i>) > 2
        var j = e is a < i >> 2;
    }
}
";
            var tree = UsingTree(text);
            tree.GetDiagnostics().Verify();
            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.ClassDeclaration);
                {
                    N(SyntaxKind.ClassKeyword);
                    N(SyntaxKind.IdentifierToken, "C");
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.MethodDeclaration);
                    {
                        N(SyntaxKind.PredefinedType);
                        {
                            N(SyntaxKind.VoidKeyword);
                        }
                        N(SyntaxKind.IdentifierToken, "M");
                        N(SyntaxKind.ParameterList);
                        {
                            N(SyntaxKind.OpenParenToken);
                            N(SyntaxKind.CloseParenToken);
                        }
                        N(SyntaxKind.Block);
                        {
                            N(SyntaxKind.OpenBraceToken);
                            N(SyntaxKind.LocalDeclarationStatement);
                            {
                                N(SyntaxKind.VariableDeclaration);
                                {
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "var");
                                    }
                                    N(SyntaxKind.VariableDeclarator);
                                    {
                                        N(SyntaxKind.IdentifierToken, "j");
                                        N(SyntaxKind.EqualsValueClause);
                                        {
                                            N(SyntaxKind.EqualsToken);
                                            N(SyntaxKind.GreaterThanExpression);
                                            {
                                                N(SyntaxKind.IsExpression);
                                                {
                                                    N(SyntaxKind.IdentifierName);
                                                    {
                                                        N(SyntaxKind.IdentifierToken, "e");
                                                    }
                                                    N(SyntaxKind.IsKeyword);
                                                    N(SyntaxKind.GenericName);
                                                    {
                                                        N(SyntaxKind.IdentifierToken, "a");
                                                        N(SyntaxKind.TypeArgumentList);
                                                        {
                                                            N(SyntaxKind.LessThanToken);
                                                            N(SyntaxKind.IdentifierName);
                                                            {
                                                                N(SyntaxKind.IdentifierToken, "i");
                                                            }
                                                            N(SyntaxKind.GreaterThanToken);
                                                        }
                                                    }
                                                }
                                                N(SyntaxKind.GreaterThanToken);
                                                N(SyntaxKind.NumericLiteralExpression);
                                                {
                                                    N(SyntaxKind.NumericLiteralToken, "2");
                                                }
                                            }
                                        }
                                    }
                                }
                                N(SyntaxKind.SemicolonToken);
                            }
                            N(SyntaxKind.CloseBraceToken);
                        }
                    }
                    N(SyntaxKind.CloseBraceToken);
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact, WorkItem(377556, "https://devdiv.visualstudio.com/DefaultCollection/DevDiv/_workitems?id=377556")]
        public void TypeArgumentShiftAmbiguity_05()
        {
            const string text = @"
class C
{
    void M()
    {
        // syntax error
        var j = e is a < i >>> 2;
    }
}
";
            var tree = UsingTree(text);
            tree.GetDiagnostics().Verify(
                // (7,30): error CS1525: Invalid expression term '>'
                //         var j = e is a < i >>> 2;
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, ">").WithArguments(">").WithLocation(7, 30)
                );
            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.ClassDeclaration);
                {
                    N(SyntaxKind.ClassKeyword);
                    N(SyntaxKind.IdentifierToken, "C");
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.MethodDeclaration);
                    {
                        N(SyntaxKind.PredefinedType);
                        {
                            N(SyntaxKind.VoidKeyword);
                        }
                        N(SyntaxKind.IdentifierToken, "M");
                        N(SyntaxKind.ParameterList);
                        {
                            N(SyntaxKind.OpenParenToken);
                            N(SyntaxKind.CloseParenToken);
                        }
                        N(SyntaxKind.Block);
                        {
                            N(SyntaxKind.OpenBraceToken);
                            N(SyntaxKind.LocalDeclarationStatement);
                            {
                                N(SyntaxKind.VariableDeclaration);
                                {
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "var");
                                    }
                                    N(SyntaxKind.VariableDeclarator);
                                    {
                                        N(SyntaxKind.IdentifierToken, "j");
                                        N(SyntaxKind.EqualsValueClause);
                                        {
                                            N(SyntaxKind.EqualsToken);
                                            N(SyntaxKind.GreaterThanExpression);
                                            {
                                                N(SyntaxKind.LessThanExpression);
                                                {
                                                    N(SyntaxKind.IsExpression);
                                                    {
                                                        N(SyntaxKind.IdentifierName);
                                                        {
                                                            N(SyntaxKind.IdentifierToken, "e");
                                                        }
                                                        N(SyntaxKind.IsKeyword);
                                                        N(SyntaxKind.IdentifierName);
                                                        {
                                                            N(SyntaxKind.IdentifierToken, "a");
                                                        }
                                                    }
                                                    N(SyntaxKind.LessThanToken);
                                                    N(SyntaxKind.RightShiftExpression);
                                                    {
                                                        N(SyntaxKind.IdentifierName);
                                                        {
                                                            N(SyntaxKind.IdentifierToken, "i");
                                                        }
                                                        N(SyntaxKind.GreaterThanGreaterThanToken);
                                                        M(SyntaxKind.IdentifierName);
                                                        {
                                                            M(SyntaxKind.IdentifierToken);
                                                        }
                                                    }
                                                }
                                                N(SyntaxKind.GreaterThanToken);
                                                N(SyntaxKind.NumericLiteralExpression);
                                                {
                                                    N(SyntaxKind.NumericLiteralToken, "2");
                                                }
                                            }
                                        }
                                    }
                                }
                                N(SyntaxKind.SemicolonToken);
                            }
                            N(SyntaxKind.CloseBraceToken);
                        }
                    }
                    N(SyntaxKind.CloseBraceToken);
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact, WorkItem(377556, "https://devdiv.visualstudio.com/DefaultCollection/DevDiv/_workitems?id=377556")]
        public void TypeArgumentShiftAmbiguity_06()
        {
            const string text = @"
class C
{
    void M()
    {
        // syntax error
        var j = e is a < i > << 2;
    }
}
";
            var tree = UsingTree(text);
            tree.GetDiagnostics().Verify(
                // (7,30): error CS1525: Invalid expression term '<<'
                //         var j = e is a < i > << 2;
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, "<<").WithArguments("<<").WithLocation(7, 30)
                );
            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.ClassDeclaration);
                {
                    N(SyntaxKind.ClassKeyword);
                    N(SyntaxKind.IdentifierToken, "C");
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.MethodDeclaration);
                    {
                        N(SyntaxKind.PredefinedType);
                        {
                            N(SyntaxKind.VoidKeyword);
                        }
                        N(SyntaxKind.IdentifierToken, "M");
                        N(SyntaxKind.ParameterList);
                        {
                            N(SyntaxKind.OpenParenToken);
                            N(SyntaxKind.CloseParenToken);
                        }
                        N(SyntaxKind.Block);
                        {
                            N(SyntaxKind.OpenBraceToken);
                            N(SyntaxKind.LocalDeclarationStatement);
                            {
                                N(SyntaxKind.VariableDeclaration);
                                {
                                    N(SyntaxKind.IdentifierName);
                                    {
                                        N(SyntaxKind.IdentifierToken, "var");
                                    }
                                    N(SyntaxKind.VariableDeclarator);
                                    {
                                        N(SyntaxKind.IdentifierToken, "j");
                                        N(SyntaxKind.EqualsValueClause);
                                        {
                                            N(SyntaxKind.EqualsToken);
                                            N(SyntaxKind.GreaterThanExpression);
                                            {
                                                N(SyntaxKind.LessThanExpression);
                                                {
                                                    N(SyntaxKind.IsExpression);
                                                    {
                                                        N(SyntaxKind.IdentifierName);
                                                        {
                                                            N(SyntaxKind.IdentifierToken, "e");
                                                        }
                                                        N(SyntaxKind.IsKeyword);
                                                        N(SyntaxKind.IdentifierName);
                                                        {
                                                            N(SyntaxKind.IdentifierToken, "a");
                                                        }
                                                    }
                                                    N(SyntaxKind.LessThanToken);
                                                    N(SyntaxKind.IdentifierName);
                                                    {
                                                        N(SyntaxKind.IdentifierToken, "i");
                                                    }
                                                }
                                                N(SyntaxKind.GreaterThanToken);
                                                N(SyntaxKind.LeftShiftExpression);
                                                {
                                                    M(SyntaxKind.IdentifierName);
                                                    {
                                                        M(SyntaxKind.IdentifierToken);
                                                    }
                                                    N(SyntaxKind.LessThanLessThanToken);
                                                    N(SyntaxKind.NumericLiteralExpression);
                                                    {
                                                        N(SyntaxKind.NumericLiteralToken, "2");
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                                N(SyntaxKind.SemicolonToken);
                            }
                            N(SyntaxKind.CloseBraceToken);
                        }
                    }
                    N(SyntaxKind.CloseBraceToken);
                }
                N(SyntaxKind.EndOfFileToken);
            }
            EOF();
        }

        [Fact]
        public void TestTargetTypedDefaultWithCSharp7_1()
        {
            var text = "default";
            var expr = this.ParseExpression(text, TestOptions.Regular.WithLanguageVersion(LanguageVersion.CSharp7_1));

            Assert.NotNull(expr);
            Assert.Equal(SyntaxKind.DefaultLiteralExpression, expr.Kind());
            Assert.Equal(text, expr.ToString());
            Assert.Equal(0, expr.Errors().Length);
        }

        [Fact, WorkItem(17683, "https://github.com/dotnet/roslyn/issues/17683")]
        public void Bug17683a()
        {
            var source =
@"from t in e
where
t == Int32.
MinValue
select t";
            UsingExpression(source);
            N(SyntaxKind.QueryExpression);
            {
                N(SyntaxKind.FromClause);
                {
                    N(SyntaxKind.FromKeyword);
                    N(SyntaxKind.IdentifierToken, "t");
                    N(SyntaxKind.InKeyword);
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "e");
                    }
                }
                N(SyntaxKind.QueryBody);
                {
                    N(SyntaxKind.WhereClause);
                    {
                        N(SyntaxKind.WhereKeyword);
                        N(SyntaxKind.EqualsExpression);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "t");
                            }
                            N(SyntaxKind.EqualsEqualsToken);
                            N(SyntaxKind.SimpleMemberAccessExpression);
                            {
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "Int32");
                                }
                                N(SyntaxKind.DotToken);
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "MinValue");
                                }
                            }
                        }
                    }
                    N(SyntaxKind.SelectClause);
                    {
                        N(SyntaxKind.SelectKeyword);
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "t");
                        }
                    }
                }
            }
            EOF();
        }

        [Fact]
        public void Bug17683b()
        {
            var source =
@"switch (e)
{
    case Int32.
               MaxValue when true:
            break;
}";
            UsingStatement(source);
            N(SyntaxKind.SwitchStatement);
            {
                N(SyntaxKind.SwitchKeyword);
                N(SyntaxKind.OpenParenToken);
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "e");
                }
                N(SyntaxKind.CloseParenToken);
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.SwitchSection);
                {
                    N(SyntaxKind.CasePatternSwitchLabel);
                    {
                        N(SyntaxKind.CaseKeyword);
                        N(SyntaxKind.ConstantPattern);
                        {
                            N(SyntaxKind.SimpleMemberAccessExpression);
                            {
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "Int32");
                                }
                                N(SyntaxKind.DotToken);
                                N(SyntaxKind.IdentifierName);
                                {
                                    N(SyntaxKind.IdentifierToken, "MaxValue");
                                }
                            }
                        }
                        N(SyntaxKind.WhenClause);
                        {
                            N(SyntaxKind.WhenKeyword);
                            N(SyntaxKind.TrueLiteralExpression);
                            {
                                N(SyntaxKind.TrueKeyword);
                            }
                        }
                        N(SyntaxKind.ColonToken);
                    }
                    N(SyntaxKind.BreakStatement);
                    {
                        N(SyntaxKind.BreakKeyword);
                        N(SyntaxKind.SemicolonToken);
                    }
                }
                N(SyntaxKind.CloseBraceToken);
            }
            EOF();
        }

        [Fact, WorkItem(22830, "https://github.com/dotnet/roslyn/issues/22830")]
        public void TypeArgumentIndexerInitializer()
        {
            UsingExpression("new C { [0] = op1 < op2, [1] = true }");
            N(SyntaxKind.ObjectCreationExpression);
            {
                N(SyntaxKind.NewKeyword);
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "C");
                }
                N(SyntaxKind.ObjectInitializerExpression);
                {
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.SimpleAssignmentExpression);
                    {
                        N(SyntaxKind.ImplicitElementAccess);
                        {
                            N(SyntaxKind.BracketedArgumentList);
                            {
                                N(SyntaxKind.OpenBracketToken);
                                N(SyntaxKind.Argument);
                                {
                                    N(SyntaxKind.NumericLiteralExpression);
                                    {
                                        N(SyntaxKind.NumericLiteralToken, "0");
                                    }
                                }
                                N(SyntaxKind.CloseBracketToken);
                            }
                        }
                        N(SyntaxKind.EqualsToken);
                        N(SyntaxKind.LessThanExpression);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "op1");
                            }
                            N(SyntaxKind.LessThanToken);
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "op2");
                            }
                        }
                    }
                    N(SyntaxKind.CommaToken);
                    N(SyntaxKind.SimpleAssignmentExpression);
                    {
                        N(SyntaxKind.ImplicitElementAccess);
                        {
                            N(SyntaxKind.BracketedArgumentList);
                            {
                                N(SyntaxKind.OpenBracketToken);
                                N(SyntaxKind.Argument);
                                {
                                    N(SyntaxKind.NumericLiteralExpression);
                                    {
                                        N(SyntaxKind.NumericLiteralToken, "1");
                                    }
                                }
                                N(SyntaxKind.CloseBracketToken);
                            }
                        }
                        N(SyntaxKind.EqualsToken);
                        N(SyntaxKind.TrueLiteralExpression);
                        {
                            N(SyntaxKind.TrueKeyword);
                        }
                    }
                    N(SyntaxKind.CloseBraceToken);
                }
            }
            EOF();
        }

        [Fact, WorkItem(12214, "https://github.com/dotnet/roslyn/issues/12214")]
        public void ConditionalExpressionInInterpolation()
        {
            UsingExpression("$\"{a ? b : d}\"",
                // (1,4): error CS8361: A conditional expression cannot be used directly in a string interpolation because the ':' ends the interpolation. Parenthesize the conditional expression.
                // $"{a ? b : d}"
                Diagnostic(ErrorCode.ERR_ConditionalInInterpolation, "a ? b ").WithLocation(1, 4)
                );
            N(SyntaxKind.InterpolatedStringExpression);
            {
                N(SyntaxKind.InterpolatedStringStartToken);
                N(SyntaxKind.Interpolation);
                {
                    N(SyntaxKind.OpenBraceToken);
                    N(SyntaxKind.ConditionalExpression);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "a");
                        }
                        N(SyntaxKind.QuestionToken);
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "b");
                        }
                        M(SyntaxKind.ColonToken);
                        M(SyntaxKind.IdentifierName);
                        {
                            M(SyntaxKind.IdentifierToken);
                        }
                    }
                    N(SyntaxKind.InterpolationFormatClause);
                    {
                        N(SyntaxKind.ColonToken);
                        N(SyntaxKind.InterpolatedStringTextToken);
                    }
                    N(SyntaxKind.CloseBraceToken);
                }
                N(SyntaxKind.InterpolatedStringEndToken);
            }
            EOF();
        }

        [Fact]
        public void NullCoalescingAssignmentExpression()
        {
            UsingExpression("a ??= b");
            N(SyntaxKind.CoalesceAssignmentExpression);
            {
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "a");
                }
                N(SyntaxKind.QuestionQuestionEqualsToken);
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "b");
                }
            }
            EOF();
        }

        [Fact]
        public void NullCoalescingAssignmentExpressionParenthesized()
        {
            UsingExpression("(a) ??= b");
            N(SyntaxKind.CoalesceAssignmentExpression);
            {
                N(SyntaxKind.ParenthesizedExpression);
                {
                    N(SyntaxKind.OpenParenToken);
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "a");
                    }
                    N(SyntaxKind.CloseParenToken);
                }
                N(SyntaxKind.QuestionQuestionEqualsToken);
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "b");
                }
            }
            EOF();
        }

        [Fact]
        public void NullCoalescingAssignmentExpressionInvocation()
        {
            UsingExpression("M(a) ??= b");
            N(SyntaxKind.CoalesceAssignmentExpression);
            {
                N(SyntaxKind.InvocationExpression);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "M");
                    }
                    N(SyntaxKind.ArgumentList);
                    {
                        N(SyntaxKind.OpenParenToken);
                        N(SyntaxKind.Argument);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "a");
                            }
                        }
                        N(SyntaxKind.CloseParenToken);
                    }
                }
                N(SyntaxKind.QuestionQuestionEqualsToken);
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "b");
                }
            }
            EOF();
        }

        [Fact]
        public void NullCoalescingAssignmentExpressionAndCoalescingOperator()
        {
            UsingExpression("a ?? b ??= c");
            N(SyntaxKind.CoalesceAssignmentExpression);
            {
                N(SyntaxKind.CoalesceExpression);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "a");
                    }
                    N(SyntaxKind.QuestionQuestionToken);
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "b");
                    }
                }
                N(SyntaxKind.QuestionQuestionEqualsToken);
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "c");
                }
            }
            EOF();
        }

        [Fact]
        public void NullCoalescingAssignmentExpressionNested()
        {
            UsingExpression("a ??= b ??= c");
            N(SyntaxKind.CoalesceAssignmentExpression);
            {
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "a");
                }
                N(SyntaxKind.QuestionQuestionEqualsToken);
                N(SyntaxKind.CoalesceAssignmentExpression);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "b");
                    }
                    N(SyntaxKind.QuestionQuestionEqualsToken);
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "c");
                    }
                }
            }
            EOF();
        }

        [Fact]
        public void NullCoalescingAssignmentParenthesizedNested()
        {
            UsingExpression("(a ??= b) ??= c");
            N(SyntaxKind.CoalesceAssignmentExpression);
            {
                N(SyntaxKind.ParenthesizedExpression);
                {
                    N(SyntaxKind.OpenParenToken);
                    N(SyntaxKind.CoalesceAssignmentExpression);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "a");
                        }
                        N(SyntaxKind.QuestionQuestionEqualsToken);
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "b");
                        }
                    }
                    N(SyntaxKind.CloseParenToken);
                }
                N(SyntaxKind.QuestionQuestionEqualsToken);
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "c");
                }
            }
            EOF();
        }

        [Fact]
        public void NullCoalescingAssignmentCSharp7_3()
        {
            UsingExpression("a ??= b", TestOptions.Regular7_3,
                // (1,3): error CS8652: The feature 'coalescing assignment' is not available in C# 7.3. Please use language version 8.0 or greater.
                // a ??= b
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7_3, "??=").WithArguments("coalescing assignment", "8.0").WithLocation(1, 3));

            N(SyntaxKind.CoalesceAssignmentExpression);
            {
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "a");
                }
                N(SyntaxKind.QuestionQuestionEqualsToken);
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "b");
                }
            }
            EOF();
        }

        [Fact]
        public void IndexExpression()
        {
            UsingExpression("^1");
            N(SyntaxKind.IndexExpression);
            {
                N(SyntaxKind.CaretToken);
                N(SyntaxKind.NumericLiteralExpression);
                {
                    N(SyntaxKind.NumericLiteralToken, "1");
                }
            }
            EOF();
        }

        [Fact]
        public void RangeExpression_ThreeDots()
        {
            UsingExpression("1...2",
                // (1,2): error CS8401: Unexpected character sequence '...'
                // 1...2
                Diagnostic(ErrorCode.ERR_TripleDotNotAllowed, "").WithLocation(1, 2));
        }

        [Fact]
        public void RangeExpression_Binary()
        {
            UsingExpression("1..1");
            N(SyntaxKind.RangeExpression);
            {
                N(SyntaxKind.NumericLiteralExpression);
                {
                    N(SyntaxKind.NumericLiteralToken, "1");
                }
                N(SyntaxKind.DotDotToken);
                N(SyntaxKind.NumericLiteralExpression);
                {
                    N(SyntaxKind.NumericLiteralToken, "1");
                }
            }
            EOF();
        }

        [Fact]
        public void RangeExpression_Binary_WithIndexes()
        {
            UsingExpression("^5..^3");
            N(SyntaxKind.RangeExpression);
            {
                N(SyntaxKind.IndexExpression);
                {
                    N(SyntaxKind.CaretToken);
                    N(SyntaxKind.NumericLiteralExpression);
                    {
                        N(SyntaxKind.NumericLiteralToken, "5");
                    }
                }
                N(SyntaxKind.DotDotToken);
                N(SyntaxKind.IndexExpression);
                {
                    N(SyntaxKind.CaretToken);
                    N(SyntaxKind.NumericLiteralExpression);
                    {
                        N(SyntaxKind.NumericLiteralToken, "3");
                    }
                }
            }
            EOF();
        }

        [Fact]
        public void RangeExpression_Binary_WithALowerPrecedenceOperator()
        {
            UsingExpression("1<<2..3>>4");
            N(SyntaxKind.RightShiftExpression);
            {
                N(SyntaxKind.LeftShiftExpression);
                {
                    N(SyntaxKind.NumericLiteralExpression);
                    {
                        N(SyntaxKind.NumericLiteralToken, "1");
                    }
                    N(SyntaxKind.LessThanLessThanToken);
                    N(SyntaxKind.RangeExpression);
                    {
                        N(SyntaxKind.NumericLiteralExpression);
                        {
                            N(SyntaxKind.NumericLiteralToken, "2");
                        }
                        N(SyntaxKind.DotDotToken);
                        N(SyntaxKind.NumericLiteralExpression);
                        {
                            N(SyntaxKind.NumericLiteralToken, "3");
                        }
                    }
                }
                N(SyntaxKind.GreaterThanGreaterThanToken);
                N(SyntaxKind.NumericLiteralExpression);
                {
                    N(SyntaxKind.NumericLiteralToken, "4");
                }
            }
            EOF();
        }

        [Fact]
        public void RangeExpression_Binary_WithAHigherPrecedenceOperator()
        {
            UsingExpression("1+2..3-4");
            N(SyntaxKind.SubtractExpression);
            {
                N(SyntaxKind.AddExpression);
                {
                    N(SyntaxKind.NumericLiteralExpression);
                    {
                        N(SyntaxKind.NumericLiteralToken, "1");
                    }
                    N(SyntaxKind.PlusToken);
                    N(SyntaxKind.RangeExpression);
                    {
                        N(SyntaxKind.NumericLiteralExpression);
                        {
                            N(SyntaxKind.NumericLiteralToken, "2");
                        }
                        N(SyntaxKind.DotDotToken);
                        N(SyntaxKind.NumericLiteralExpression);
                        {
                            N(SyntaxKind.NumericLiteralToken, "3");
                        }
                    }
                }
                N(SyntaxKind.MinusToken);
                N(SyntaxKind.NumericLiteralExpression);
                {
                    N(SyntaxKind.NumericLiteralToken, "4");
                }
            }
            EOF();
        }

        [Fact]
        public void RangeExpression_UnaryBadLeft()
        {
            UsingExpression("a*..b");
            N(SyntaxKind.MultiplyExpression);
            {
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "a");
                }
                N(SyntaxKind.AsteriskToken);
                N(SyntaxKind.RangeExpression);
                {
                    N(SyntaxKind.DotDotToken);
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "b");
                    }
                }
            }
            EOF();
        }

        [Fact]
        public void RangeExpression_BinaryLeftPlus()
        {
            UsingExpression("a + b..c");
            N(SyntaxKind.AddExpression);
            {
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "a");
                }
                N(SyntaxKind.PlusToken);
                N(SyntaxKind.RangeExpression);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "b");
                    }
                    N(SyntaxKind.DotDotToken);
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "c");
                    }
                }
            }
            EOF();
        }

        [Fact]
        public void RangeExpression_UnaryLeftPlus()
        {
            UsingExpression("a + b..");
            N(SyntaxKind.AddExpression);
            {
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "a");
                }
                N(SyntaxKind.PlusToken);
                N(SyntaxKind.RangeExpression);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "b");
                    }
                    N(SyntaxKind.DotDotToken);
                }
            }
            EOF();
        }

        [Fact]
        public void RangeExpression_UnaryRightMult()
        {
            UsingExpression("a.. && b");
            N(SyntaxKind.LogicalAndExpression);
            {
                N(SyntaxKind.RangeExpression);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "a");
                    }
                    N(SyntaxKind.DotDotToken);
                }
                N(SyntaxKind.AmpersandAmpersandToken);
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "b");
                }
            }
            EOF();
        }

        [Fact]
        public void RangeExpression_UnaryRightMult2()
        {
            UsingExpression("..a && b");
            N(SyntaxKind.LogicalAndExpression);
            {
                N(SyntaxKind.RangeExpression);
                {
                    N(SyntaxKind.DotDotToken);
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "a");
                    }
                }
                N(SyntaxKind.AmpersandAmpersandToken);
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "b");
                }
            }
            EOF();
        }

        [Fact]
        public void RangeExpression_Ambiguity1()
        {
            UsingExpression(".. ..");
            N(SyntaxKind.RangeExpression);
            {
                N(SyntaxKind.DotDotToken);
                N(SyntaxKind.RangeExpression);
                {
                    N(SyntaxKind.DotDotToken);
                }
            }
            EOF();
        }

        [Fact]
        public void RangeExpression_Ambiguity2()
        {
            UsingExpression(".. .. e");
            N(SyntaxKind.RangeExpression);
            {
                N(SyntaxKind.DotDotToken);
                N(SyntaxKind.RangeExpression);
                {
                    N(SyntaxKind.DotDotToken);
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "e");
                    }
                }
            }
            EOF();
        }

        [Fact(Skip = "https://github.com/dotnet/roslyn/issues/36514")]
        public void RangeExpression_Ambiguity3()
        {
            UsingExpression(".. e ..");
            N(SyntaxKind.RangeExpression);
            {
                N(SyntaxKind.DotDotToken);
                N(SyntaxKind.RangeExpression);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "e");
                    }
                    N(SyntaxKind.DotDotToken);
                }
            }
            EOF();
        }

        [Fact]
        public void RangeExpression_Ambiguity4()
        {
            UsingExpression("a .. .. b");
            N(SyntaxKind.RangeExpression);
            {
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "a");
                }
                N(SyntaxKind.DotDotToken);
                N(SyntaxKind.RangeExpression);
                {
                    N(SyntaxKind.DotDotToken);
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "b");
                    }
                }
            }
            EOF();
        }

        [Fact(Skip = "https://github.com/dotnet/roslyn/issues/36514")]
        public void RangeExpression_Ambiguity5()
        {
            UsingExpression("a .. b ..");
            N(SyntaxKind.RangeExpression);
            {
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "a");
                }
                N(SyntaxKind.DotDotToken);
                N(SyntaxKind.RangeExpression);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "b");
                    }
                    N(SyntaxKind.DotDotToken);
                }
            }
            EOF();
        }

        [Fact(Skip = "https://github.com/dotnet/roslyn/issues/36514")]
        public void RangeExpression_Ambiguity6()
        {
            UsingExpression("a .. b .. c");
            N(SyntaxKind.RangeExpression);
            {
                N(SyntaxKind.IdentifierName);
                {
                    N(SyntaxKind.IdentifierToken, "a");
                }
                N(SyntaxKind.DotDotToken);
                N(SyntaxKind.RangeExpression);
                {
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "b");
                    }
                    N(SyntaxKind.DotDotToken);
                }
            }
            EOF();
        }

        [Fact, WorkItem(36122, "https://github.com/dotnet/roslyn/issues/36122")]
        public void RangeExpression_NotCast()
        {
            UsingExpression("(Offset)..(Offset + Count)");
            N(SyntaxKind.RangeExpression);
            {
                N(SyntaxKind.ParenthesizedExpression);
                {
                    N(SyntaxKind.OpenParenToken);
                    N(SyntaxKind.IdentifierName);
                    {
                        N(SyntaxKind.IdentifierToken, "Offset");
                    }
                    N(SyntaxKind.CloseParenToken);
                }
                N(SyntaxKind.DotDotToken);
                N(SyntaxKind.ParenthesizedExpression);
                {
                    N(SyntaxKind.OpenParenToken);
                    N(SyntaxKind.AddExpression);
                    {
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "Offset");
                        }
                        N(SyntaxKind.PlusToken);
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "Count");
                        }
                    }
                    N(SyntaxKind.CloseParenToken);
                }
            }
            EOF();
        }

        [Fact]
        public void RangeExpression_Right()
        {
            UsingExpression("..1");
            N(SyntaxKind.RangeExpression);
            {
                N(SyntaxKind.DotDotToken);
                N(SyntaxKind.NumericLiteralExpression);
                {
                    N(SyntaxKind.NumericLiteralToken, "1");
                }
            }
            EOF();
        }

        [Fact]
        public void RangeExpression_Right_WithIndexes()
        {
            UsingExpression("..^3");
            N(SyntaxKind.RangeExpression);
            {
                N(SyntaxKind.DotDotToken);
                N(SyntaxKind.IndexExpression);
                {
                    N(SyntaxKind.CaretToken);
                    N(SyntaxKind.NumericLiteralExpression);
                    {
                        N(SyntaxKind.NumericLiteralToken, "3");
                    }
                }
            }
            EOF();
        }

        [Fact]
        public void RangeExpression_Left()
        {
            UsingExpression("1..");
            N(SyntaxKind.RangeExpression);
            {
                N(SyntaxKind.NumericLiteralExpression);
                {
                    N(SyntaxKind.NumericLiteralToken, "1");
                }
                N(SyntaxKind.DotDotToken);
            }
            EOF();
        }

        [Fact]
        public void RangeExpression_Left_WithIndexes()
        {
            UsingExpression("^5..");
            N(SyntaxKind.RangeExpression);
            {
                N(SyntaxKind.IndexExpression);
                {
                    N(SyntaxKind.CaretToken);
                    N(SyntaxKind.NumericLiteralExpression);
                    {
                        N(SyntaxKind.NumericLiteralToken, "5");
                    }
                }
                N(SyntaxKind.DotDotToken);
            }
            EOF();
        }

        [Fact]
        public void RangeExpression_NoOperands()
        {
            UsingExpression("..");
            N(SyntaxKind.RangeExpression);
            {
                N(SyntaxKind.DotDotToken);
            }
            EOF();
        }

        [Fact]
        public void RangeExpression_NoOperands_WithOtherOperators()
        {
            UsingExpression("1+..<<2");
            N(SyntaxKind.LeftShiftExpression);
            {
                N(SyntaxKind.AddExpression);
                {
                    N(SyntaxKind.NumericLiteralExpression);
                    {
                        N(SyntaxKind.NumericLiteralToken, "1");
                    }
                    N(SyntaxKind.PlusToken);
                    N(SyntaxKind.RangeExpression);
                    {
                        N(SyntaxKind.DotDotToken);
                    }
                }
                N(SyntaxKind.LessThanLessThanToken);
                N(SyntaxKind.NumericLiteralExpression);
                {
                    N(SyntaxKind.NumericLiteralToken, "2");
                }
            }
            EOF();
        }

        [Fact]
        public void RangeExpression_DotSpaceDot()
        {
            UsingExpression("1. .2",
                // (1,1): error CS1073: Unexpected token '.2'
                // 1. .2
                Diagnostic(ErrorCode.ERR_UnexpectedToken, "1. ").WithArguments(".2").WithLocation(1, 1),
                // (1,4): error CS1001: Identifier expected
                // 1. .2
                Diagnostic(ErrorCode.ERR_IdentifierExpected, ".2").WithLocation(1, 4));
        }

        [Fact]
        public void RangeExpression_MethodInvocation_NoOperands()
        {
            UsingExpression(".. .ToString()",
                // (1,1): error CS1073: Unexpected token '.'
                // .. .ToString()
                Diagnostic(ErrorCode.ERR_UnexpectedToken, "..").WithArguments(".").WithLocation(1, 1));
        }

        [Fact]
        public void RangeExpression_MethodInvocation_LeftOperand()
        {
            UsingExpression("1.. .ToString()",
                // (1,1): error CS1073: Unexpected token '.'
                // 1.. .ToString()
                Diagnostic(ErrorCode.ERR_UnexpectedToken, "1..").WithArguments(".").WithLocation(1, 1));
        }

        [Fact]
        public void RangeExpression_MethodInvocation_RightOperand()
        {
            UsingExpression("..2 .ToString()");
            N(SyntaxKind.RangeExpression);
            {
                N(SyntaxKind.DotDotToken);
                N(SyntaxKind.InvocationExpression);
                {
                    N(SyntaxKind.SimpleMemberAccessExpression);
                    {
                        N(SyntaxKind.NumericLiteralExpression);
                        {
                            N(SyntaxKind.NumericLiteralToken, "2");
                        }
                        N(SyntaxKind.DotToken);
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "ToString");
                        }
                        N(SyntaxKind.ArgumentList);
                        {
                            N(SyntaxKind.OpenParenToken);
                            N(SyntaxKind.CloseParenToken);
                        }
                    }
                }
            }
            EOF();
        }

        [Fact]
        public void RangeExpression_MethodInvocation_TwoOperands()
        {
            UsingExpression("1..2 .ToString()");
            N(SyntaxKind.RangeExpression);
            {
                N(SyntaxKind.NumericLiteralExpression);
                {
                    N(SyntaxKind.NumericLiteralToken, "1");
                }
                N(SyntaxKind.DotDotToken);
                N(SyntaxKind.InvocationExpression);
                {
                    N(SyntaxKind.SimpleMemberAccessExpression);
                    {
                        N(SyntaxKind.NumericLiteralExpression);
                        {
                            N(SyntaxKind.NumericLiteralToken, "2");
                        }
                        N(SyntaxKind.DotToken);
                        N(SyntaxKind.IdentifierName);
                        {
                            N(SyntaxKind.IdentifierToken, "ToString");
                        }
                        N(SyntaxKind.ArgumentList);
                        {
                            N(SyntaxKind.OpenParenToken);
                            N(SyntaxKind.CloseParenToken);
                        }
                    }
                }
            }
            EOF();
        }

        [Fact]
        public void RangeExpression_ConditionalAccessExpression()
        {
            UsingExpression("c?..b",
                // (1,6): error CS1003: Syntax error, ':' expected
                // c?..b
                Diagnostic(ErrorCode.ERR_SyntaxError, "").WithArguments(":", "").WithLocation(1, 6),
                // (1,6): error CS1733: Expected expression
                // c?..b
                Diagnostic(ErrorCode.ERR_ExpressionExpected, "").WithLocation(1, 6));
        }

        [Fact]
        public void BaseExpression_01()
        {
            UsingExpression("base");
            N(SyntaxKind.BaseExpression);
            {
                N(SyntaxKind.BaseKeyword);
            }
            EOF();
        }
    }
}
