// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public class ExpressionParsingTexts : ParsingTests
    {
        protected override SyntaxTree ParseTree(string text, CSharpParseOptions options)
        {
            return SyntaxFactory.ParseSyntaxTree(text, options: options);
        }

        private ExpressionSyntax ParseExpression(string text, ParseOptions options = null)
        {
            return SyntaxFactory.ParseExpression(text, options: options);
        }

        private ExpressionSyntax ParseExpressionExperimental(string text)
        {
            return SyntaxFactory.ParseExpression(text, options: CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.Experimental));
        }

        [Fact]
        public void TestEmptyString()
        {
            var text = string.Empty;
            var expr = this.ParseExpression(text);

            Assert.NotNull(expr);
            Assert.Equal(SyntaxKind.IdentifierName, expr.Kind);
            Assert.True(((IdentifierNameSyntax)expr).Identifier.IsMissing);
            Assert.Equal(text, expr.ToString());
            Assert.Equal(1, expr.Errors().Length);
            Assert.Equal((int)ErrorCode.ERR_ExpressionExpected, expr.Errors()[0].Code);
        }

        [Fact]
        public void TestName()
        {
            var text = "foo";
            var expr = this.ParseExpression(text);

            Assert.NotNull(expr);
            Assert.Equal(SyntaxKind.IdentifierName, expr.Kind);
            Assert.False(((IdentifierNameSyntax)expr).Identifier.IsMissing);
            Assert.Equal(text, expr.ToString());
            Assert.Equal(0, expr.Errors().Length);
        }

        [Fact]
        public void TestParenthesizedExpression()
        {
            var text = "(foo)";
            var expr = this.ParseExpression(text);

            Assert.NotNull(expr);
            Assert.Equal(SyntaxKind.ParenthesizedExpression, expr.Kind);
            Assert.Equal(text, expr.ToString());
            Assert.Equal(0, expr.Errors().Length);
        }

        private void TestLiteralExpression(SyntaxKind kind)
        {
            var text = SyntaxFacts.GetText(kind);
            var expr = this.ParseExpression(text);

            Assert.NotNull(expr);
            var opKind = SyntaxFacts.GetLiteralExpression(kind);
            Assert.Equal(opKind, expr.Kind);
            Assert.Equal(0, expr.Errors().Length);
            var us = (LiteralExpressionSyntax)expr;
            Assert.NotNull(us.Token);
            Assert.Equal(kind, us.Token.CSharpKind());
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
            Assert.Equal(opKind, expr.Kind);
            Assert.Equal(0, expr.Errors().Length);
            SyntaxToken token;
            switch (expr.Kind)
            {
                case SyntaxKind.ThisExpression:
                    token = ((ThisExpressionSyntax)expr).Token;
                    Assert.NotNull(token);
                    Assert.Equal(kind, token.CSharpKind());
                    break;
                case SyntaxKind.BaseExpression:
                    token = ((BaseExpressionSyntax)expr).Token;
                    Assert.NotNull(token);
                    Assert.Equal(kind, token.CSharpKind());
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
            Assert.Equal(SyntaxKind.StringLiteralExpression, expr.Kind);
            Assert.Equal(0, expr.Errors().Length);
            var us = (LiteralExpressionSyntax)expr;
            Assert.NotNull(us.Token);
            Assert.Equal(SyntaxKind.StringLiteralToken, us.Token.CSharpKind());
        }

        [WorkItem(540379, "DevDiv")]
        [Fact]
        public void TestVerbatimLiteralExpression()
        {
            var text = "@\"\"\"stuff\"\"\"";
            var expr = this.ParseExpression(text);

            Assert.NotNull(expr);
            Assert.Equal(SyntaxKind.StringLiteralExpression, expr.Kind);
            Assert.Equal(0, expr.Errors().Length);
            var us = (LiteralExpressionSyntax)expr;
            Assert.NotNull(us.Token);
            Assert.Equal(SyntaxKind.StringLiteralToken, us.Token.CSharpKind());
            Assert.Equal("\"stuff\"", us.Token.ValueText);
        }

        [Fact]
        public void TestCharacterLiteralExpression()
        {
            var text = "'c'";
            var expr = this.ParseExpression(text);

            Assert.NotNull(expr);
            Assert.Equal(SyntaxKind.CharacterLiteralExpression, expr.Kind);
            Assert.Equal(0, expr.Errors().Length);
            var us = (LiteralExpressionSyntax)expr;
            Assert.NotNull(us.Token);
            Assert.Equal(SyntaxKind.CharacterLiteralToken, us.Token.CSharpKind());
        }

        [Fact]
        public void TestNumericLiteralExpression()
        {
            var text = "0";
            var expr = this.ParseExpression(text);

            Assert.NotNull(expr);
            Assert.Equal(SyntaxKind.NumericLiteralExpression, expr.Kind);
            Assert.Equal(0, expr.Errors().Length);
            var us = (LiteralExpressionSyntax)expr;
            Assert.NotNull(us.Token);
            Assert.Equal(SyntaxKind.NumericLiteralToken, us.Token.CSharpKind());
        }

        private void TestPrefixUnary(SyntaxKind kind)
        {
            var text = SyntaxFacts.GetText(kind) + "a";
            var expr = this.ParseExpression(text);

            Assert.NotNull(expr);
            var opKind = SyntaxFacts.GetPrefixUnaryExpression(kind);
            Assert.Equal(opKind, expr.Kind);
            Assert.Equal(text, expr.ToString());
            Assert.Equal(0, expr.Errors().Length);
            var us = (PrefixUnaryExpressionSyntax)expr;
            Assert.NotNull(us.OperatorToken);
            Assert.Equal(kind, us.OperatorToken.CSharpKind());
            Assert.NotNull(us.Operand);
            Assert.Equal(SyntaxKind.IdentifierName, us.Operand.Kind);
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

        private void TestPostfixUnary(SyntaxKind kind)
        {
            var text = "a" + SyntaxFacts.GetText(kind);
            var expr = this.ParseExpression(text);

            Assert.NotNull(expr);
            var opKind = SyntaxFacts.GetPostfixUnaryExpression(kind);
            Assert.Equal(opKind, expr.Kind);
            Assert.Equal(text, expr.ToString());
            Assert.Equal(0, expr.Errors().Length);
            var us = (PostfixUnaryExpressionSyntax)expr;
            Assert.NotNull(us.OperatorToken);
            Assert.Equal(kind, us.OperatorToken.CSharpKind());
            Assert.NotNull(us.Operand);
            Assert.Equal(SyntaxKind.IdentifierName, us.Operand.Kind);
            Assert.Equal("a", us.Operand.ToString());
        }

        [Fact]
        public void TestPostfixUnaryOperators()
        {
            TestPostfixUnary(SyntaxKind.PlusPlusToken);
            TestPostfixUnary(SyntaxKind.MinusMinusToken);
        }

        private void TestBinary(SyntaxKind kind)
        {
            var text = "(a) " + SyntaxFacts.GetText(kind) + " b";
            var expr = this.ParseExpression(text);

            Assert.NotNull(expr);
            var opKind = SyntaxFacts.GetBinaryExpression(kind);
            Assert.Equal(opKind, expr.Kind);
            Assert.Equal(text, expr.ToString());
            Assert.Equal(0, expr.Errors().Length);
            var b = (BinaryExpressionSyntax)expr;
            Assert.NotNull(b.OperatorToken);
            Assert.Equal(kind, b.OperatorToken.CSharpKind());
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

        private void TestAssignment(SyntaxKind kind)
        {
            var text = "(a) " + SyntaxFacts.GetText(kind) + " b";
            var expr = this.ParseExpression(text);

            Assert.NotNull(expr);
            var opKind = SyntaxFacts.GetAssignmentExpression(kind);
            Assert.Equal(opKind, expr.Kind);
            Assert.Equal(text, expr.ToString());
            Assert.Equal(0, expr.Errors().Length);
            var a = (AssignmentExpressionSyntax)expr;
            Assert.NotNull(a.OperatorToken);
            Assert.Equal(kind, a.OperatorToken.CSharpKind());
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
        }

        private void TestMemberAccess(SyntaxKind kind)
        {
            var text = "(a)" + SyntaxFacts.GetText(kind) + " b";
            var expr = this.ParseExpression(text);

            Assert.NotNull(expr);
            Assert.Equal(text, expr.ToString());
            Assert.Equal(0, expr.Errors().Length);
            var e = (MemberAccessExpressionSyntax)expr;
            Assert.NotNull(e.OperatorToken);
            Assert.Equal(kind, e.OperatorToken.CSharpKind());
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
        private void TestConditionalAccessNotVersion5()
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
        private void TestConditionalAccess()
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
            Assert.Equal(cons.Kind, SyntaxKind.ConditionalAccessExpression);

            e = e.WhenNotNull as ConditionalAccessExpressionSyntax;
            Assert.Equal(".c.d", e.Expression.ToString());
            cons = e.WhenNotNull;
            Assert.Equal("[1]?.e()?.f", cons.ToString());
            Assert.Equal(cons.Kind, SyntaxKind.ConditionalAccessExpression);

            e = e.WhenNotNull as ConditionalAccessExpressionSyntax;
            Assert.Equal("[1]", e.Expression.ToString());
            cons = e.WhenNotNull;
            Assert.Equal(".e()?.f", cons.ToString());
            Assert.Equal(cons.Kind, SyntaxKind.ConditionalAccessExpression);

            e = e.WhenNotNull as ConditionalAccessExpressionSyntax;
            Assert.Equal(".e()", e.Expression.ToString());
            cons = e.WhenNotNull;
            Assert.Equal(".f", cons.ToString());
            Assert.Equal(cons.Kind, SyntaxKind.MemberBindingExpression);
        }

        private void TestFunctionKeyword(SyntaxKind kind, SyntaxToken keyword)
        {
            Assert.NotNull(keyword);
            Assert.Equal(kind, keyword.CSharpKind());
        }

        private void TestParenthesizedArgument(SyntaxToken openParen, CSharpSyntaxNode arg, SyntaxToken closeParen)
        {
            Assert.NotNull(openParen);
            Assert.False(openParen.IsMissing);
            Assert.NotNull(closeParen);
            Assert.False(closeParen.IsMissing);
            Assert.Equal("a", arg.ToString());
        }

        private void TestSingleParamFunctionalOperator(SyntaxKind kind)
        {
            var text = SyntaxFacts.GetText(kind) + "(a)";
            var expr = this.ParseExpression(text);

            Assert.NotNull(expr);
            var opKind = SyntaxFacts.GetPrimaryFunction(kind);
            Assert.Equal(opKind, expr.Kind);
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
            Assert.Equal(SyntaxKind.RefValueExpression, expr.Kind);
            Assert.Equal(text, expr.ToString());
            Assert.Equal(0, expr.Errors().Length);
            var fs = (RefValueExpressionSyntax)expr;
            Assert.NotNull(fs.Keyword);
            Assert.Equal(SyntaxKind.RefValueKeyword, fs.Keyword.CSharpKind());
            Assert.NotNull(fs.OpenParenToken);
            Assert.False(fs.OpenParenToken.IsMissing);
            Assert.NotNull(fs.CloseParenToken);
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
            Assert.Equal(SyntaxKind.ConditionalExpression, expr.Kind);
            Assert.Equal(text, expr.ToString());
            Assert.Equal(0, expr.Errors().Length);
            var ts = (ConditionalExpressionSyntax)expr;
            Assert.NotNull(ts.QuestionToken);
            Assert.NotNull(ts.ColonToken);
            Assert.Equal(SyntaxKind.QuestionToken, ts.QuestionToken.CSharpKind());
            Assert.Equal(SyntaxKind.ColonToken, ts.ColonToken.CSharpKind());
            Assert.Equal("a", ts.Condition.ToString());
            Assert.Equal("b", ts.WhenTrue.ToString());
            Assert.Equal("c", ts.WhenFalse.ToString());
        }

        [Fact]
        public void TestCast()
        {
            var text = "(a) b";
            var expr = this.ParseExpression(text);

            Assert.NotNull(expr);
            Assert.Equal(SyntaxKind.CastExpression, expr.Kind);
            Assert.Equal(text, expr.ToString());
            Assert.Equal(0, expr.Errors().Length);
            var cs = (CastExpressionSyntax)expr;
            Assert.NotNull(cs.OpenParenToken);
            Assert.NotNull(cs.CloseParenToken);
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
            Assert.Equal(SyntaxKind.InvocationExpression, expr.Kind);
            Assert.Equal(text, expr.ToString());
            Assert.Equal(0, expr.Errors().Length);
            var cs = (InvocationExpressionSyntax)expr;
            Assert.NotNull(cs.ArgumentList.OpenParenToken);
            Assert.NotNull(cs.ArgumentList.CloseParenToken);
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
            Assert.Equal(SyntaxKind.InvocationExpression, expr.Kind);
            Assert.Equal(text, expr.ToString());
            Assert.Equal(0, expr.Errors().Length);
            var cs = (InvocationExpressionSyntax)expr;
            Assert.NotNull(cs.ArgumentList.OpenParenToken);
            Assert.NotNull(cs.ArgumentList.CloseParenToken);
            Assert.False(cs.ArgumentList.OpenParenToken.IsMissing);
            Assert.False(cs.ArgumentList.CloseParenToken.IsMissing);
            Assert.NotNull(cs.Expression);
            Assert.Equal(1, cs.ArgumentList.Arguments.Count);
            Assert.Equal("a", cs.Expression.ToString());
            Assert.Equal("ref b", cs.ArgumentList.Arguments[0].ToString());
            Assert.NotNull(cs.ArgumentList.Arguments[0].RefOrOutKeyword);
            Assert.Equal(SyntaxKind.RefKeyword, cs.ArgumentList.Arguments[0].RefOrOutKeyword.CSharpKind());
            Assert.NotNull(cs.ArgumentList.Arguments[0].Expression);
            Assert.Equal("b", cs.ArgumentList.Arguments[0].Expression.ToString());
        }

        [Fact]
        public void TestCallWithOut()
        {
            var text = "a(out b)";
            var expr = this.ParseExpression(text);

            Assert.NotNull(expr);
            Assert.Equal(SyntaxKind.InvocationExpression, expr.Kind);
            Assert.Equal(text, expr.ToString());
            Assert.Equal(0, expr.Errors().Length);
            var cs = (InvocationExpressionSyntax)expr;
            Assert.NotNull(cs.ArgumentList.OpenParenToken);
            Assert.NotNull(cs.ArgumentList.CloseParenToken);
            Assert.False(cs.ArgumentList.OpenParenToken.IsMissing);
            Assert.False(cs.ArgumentList.CloseParenToken.IsMissing);
            Assert.NotNull(cs.Expression);
            Assert.Equal(1, cs.ArgumentList.Arguments.Count);
            Assert.Equal("a", cs.Expression.ToString());
            Assert.Equal("out b", cs.ArgumentList.Arguments[0].ToString());
            Assert.NotNull(cs.ArgumentList.Arguments[0].RefOrOutKeyword);
            Assert.Equal(SyntaxKind.OutKeyword, cs.ArgumentList.Arguments[0].RefOrOutKeyword.CSharpKind());
            Assert.NotNull(cs.ArgumentList.Arguments[0].Expression);
            Assert.Equal("b", cs.ArgumentList.Arguments[0].Expression.ToString());
        }

        [Fact]
        public void TestCallWithNamedArgument()
        {
            var text = "a(B: b)";
            var expr = this.ParseExpression(text);

            Assert.NotNull(expr);
            Assert.Equal(SyntaxKind.InvocationExpression, expr.Kind);
            Assert.Equal(text, expr.ToString());
            Assert.Equal(0, expr.Errors().Length);
            var cs = (InvocationExpressionSyntax)expr;
            Assert.NotNull(cs.ArgumentList.OpenParenToken);
            Assert.NotNull(cs.ArgumentList.CloseParenToken);
            Assert.False(cs.ArgumentList.OpenParenToken.IsMissing);
            Assert.False(cs.ArgumentList.CloseParenToken.IsMissing);
            Assert.NotNull(cs.Expression);
            Assert.Equal(1, cs.ArgumentList.Arguments.Count);
            Assert.Equal("a", cs.Expression.ToString());
            Assert.Equal("B: b", cs.ArgumentList.Arguments[0].ToString());
            Assert.NotNull(cs.ArgumentList.Arguments[0].NameColon);
            Assert.Equal("B", cs.ArgumentList.Arguments[0].NameColon.Name.ToString());
            Assert.NotNull(cs.ArgumentList.Arguments[0].NameColon.ColonToken);
            Assert.Equal("b", cs.ArgumentList.Arguments[0].Expression.ToString());
        }

        [Fact]
        public void TestIndex()
        {
            var text = "a[b]";
            var expr = this.ParseExpression(text);

            Assert.NotNull(expr);
            Assert.Equal(SyntaxKind.ElementAccessExpression, expr.Kind);
            Assert.Equal(text, expr.ToString());
            Assert.Equal(0, expr.Errors().Length);
            var ea = (ElementAccessExpressionSyntax)expr;
            Assert.NotNull(ea.ArgumentList.OpenBracketToken);
            Assert.NotNull(ea.ArgumentList.CloseBracketToken);
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
            Assert.Equal(SyntaxKind.ElementAccessExpression, expr.Kind);
            Assert.Equal(text, expr.ToString());
            Assert.Equal(0, expr.Errors().Length);
            var ea = (ElementAccessExpressionSyntax)expr;
            Assert.NotNull(ea.ArgumentList.OpenBracketToken);
            Assert.NotNull(ea.ArgumentList.CloseBracketToken);
            Assert.False(ea.ArgumentList.OpenBracketToken.IsMissing);
            Assert.False(ea.ArgumentList.CloseBracketToken.IsMissing);
            Assert.NotNull(ea.Expression);
            Assert.Equal(1, ea.ArgumentList.Arguments.Count);
            Assert.Equal("a", ea.Expression.ToString());
            Assert.Equal("ref b", ea.ArgumentList.Arguments[0].ToString());
            Assert.NotNull(ea.ArgumentList.Arguments[0].RefOrOutKeyword);
            Assert.Equal(SyntaxKind.RefKeyword, ea.ArgumentList.Arguments[0].RefOrOutKeyword.CSharpKind());
            Assert.NotNull(ea.ArgumentList.Arguments[0].Expression);
            Assert.Equal("b", ea.ArgumentList.Arguments[0].Expression.ToString());
        }

        [Fact]
        public void TestIndexWithOut()
        {
            var text = "a[out b]";
            var expr = this.ParseExpression(text);

            Assert.NotNull(expr);
            Assert.Equal(SyntaxKind.ElementAccessExpression, expr.Kind);
            Assert.Equal(text, expr.ToString());
            Assert.Equal(0, expr.Errors().Length);
            var ea = (ElementAccessExpressionSyntax)expr;
            Assert.NotNull(ea.ArgumentList.OpenBracketToken);
            Assert.NotNull(ea.ArgumentList.CloseBracketToken);
            Assert.False(ea.ArgumentList.OpenBracketToken.IsMissing);
            Assert.False(ea.ArgumentList.CloseBracketToken.IsMissing);
            Assert.NotNull(ea.Expression);
            Assert.Equal(1, ea.ArgumentList.Arguments.Count);
            Assert.Equal("a", ea.Expression.ToString());
            Assert.Equal("out b", ea.ArgumentList.Arguments[0].ToString());
            Assert.NotNull(ea.ArgumentList.Arguments[0].RefOrOutKeyword);
            Assert.Equal(SyntaxKind.OutKeyword, ea.ArgumentList.Arguments[0].RefOrOutKeyword.CSharpKind());
            Assert.NotNull(ea.ArgumentList.Arguments[0].Expression);
            Assert.Equal("b", ea.ArgumentList.Arguments[0].Expression.ToString());
        }

        [Fact]
        public void TestIndexWithNamedArgument()
        {
            var text = "a[B: b]";
            var expr = this.ParseExpression(text);

            Assert.NotNull(expr);
            Assert.Equal(SyntaxKind.ElementAccessExpression, expr.Kind);
            Assert.Equal(text, expr.ToString());
            Assert.Equal(0, expr.Errors().Length);
            var ea = (ElementAccessExpressionSyntax)expr;
            Assert.NotNull(ea.ArgumentList.OpenBracketToken);
            Assert.NotNull(ea.ArgumentList.CloseBracketToken);
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
            Assert.Equal(SyntaxKind.ObjectCreationExpression, expr.Kind);
            Assert.Equal(text, expr.ToString());
            Assert.Equal(0, expr.Errors().Length);
            var oc = (ObjectCreationExpressionSyntax)expr;
            Assert.NotNull(oc.ArgumentList);
            Assert.NotNull(oc.ArgumentList.OpenParenToken);
            Assert.NotNull(oc.ArgumentList.CloseParenToken);
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
            Assert.Equal(SyntaxKind.ObjectCreationExpression, expr.Kind);
            Assert.Equal(text, expr.ToString());
            Assert.Equal(0, expr.Errors().Length);
            var oc = (ObjectCreationExpressionSyntax)expr;
            Assert.NotNull(oc.ArgumentList);
            Assert.NotNull(oc.ArgumentList.OpenParenToken);
            Assert.NotNull(oc.ArgumentList.CloseParenToken);
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
            Assert.Equal(SyntaxKind.ObjectCreationExpression, expr.Kind);
            Assert.Equal(text, expr.ToString());
            Assert.Equal(0, expr.Errors().Length);
            var oc = (ObjectCreationExpressionSyntax)expr;
            Assert.NotNull(oc.ArgumentList);
            Assert.NotNull(oc.ArgumentList.OpenParenToken);
            Assert.NotNull(oc.ArgumentList.CloseParenToken);
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
            Assert.Equal(SyntaxKind.ObjectCreationExpression, expr.Kind);
            Assert.Equal(text, expr.ToString());
            Assert.Equal(0, expr.Errors().Length);
            var oc = (ObjectCreationExpressionSyntax)expr;
            Assert.NotNull(oc.ArgumentList);
            Assert.NotNull(oc.ArgumentList.OpenParenToken);
            Assert.NotNull(oc.ArgumentList.CloseParenToken);
            Assert.False(oc.ArgumentList.OpenParenToken.IsMissing);
            Assert.False(oc.ArgumentList.CloseParenToken.IsMissing);
            Assert.Equal(0, oc.ArgumentList.Arguments.Count);
            Assert.NotNull(oc.Type);
            Assert.Equal("a", oc.Type.ToString());

            Assert.NotNull(oc.Initializer);
            Assert.NotNull(oc.Initializer.OpenBraceToken);
            Assert.NotNull(oc.Initializer.CloseBraceToken);
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
            Assert.Equal(SyntaxKind.ObjectCreationExpression, expr.Kind);
            Assert.Equal(text, expr.ToString());
            Assert.Equal(0, expr.Errors().Length);
            var oc = (ObjectCreationExpressionSyntax)expr;
            Assert.Null(oc.ArgumentList);
            Assert.NotNull(oc.Type);
            Assert.Equal("a", oc.Type.ToString());

            Assert.NotNull(oc.Initializer);
            Assert.NotNull(oc.Initializer.OpenBraceToken);
            Assert.NotNull(oc.Initializer.CloseBraceToken);
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
            Assert.Equal(SyntaxKind.ObjectCreationExpression, expr.Kind);
            Assert.Equal(text, expr.ToString());
            Assert.Equal(0, expr.Errors().Length);
            var oc = (ObjectCreationExpressionSyntax)expr;
            Assert.Null(oc.ArgumentList);
            Assert.NotNull(oc.Type);
            Assert.Equal("a", oc.Type.ToString());

            Assert.NotNull(oc.Initializer);
            Assert.NotNull(oc.Initializer.OpenBraceToken);
            Assert.NotNull(oc.Initializer.CloseBraceToken);
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
            Assert.Equal(SyntaxKind.ObjectCreationExpression, expr.Kind);
            Assert.Equal(text, expr.ToString());
            Assert.Equal(0, expr.Errors().Length);
            var oc = (ObjectCreationExpressionSyntax)expr;
            Assert.Null(oc.ArgumentList);
            Assert.NotNull(oc.Type);
            Assert.Equal("a", oc.Type.ToString());

            Assert.NotNull(oc.Initializer);
            Assert.NotNull(oc.Initializer.OpenBraceToken);
            Assert.NotNull(oc.Initializer.CloseBraceToken);
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
            Assert.Equal(SyntaxKind.ObjectCreationExpression, expr.Kind);
            Assert.Equal(text, expr.ToString());
            Assert.Equal(0, expr.Errors().Length);
            var oc = (ObjectCreationExpressionSyntax)expr;
            Assert.Null(oc.ArgumentList);
            Assert.NotNull(oc.Type);
            Assert.Equal("a", oc.Type.ToString());

            Assert.NotNull(oc.Initializer);
            Assert.NotNull(oc.Initializer.OpenBraceToken);
            Assert.NotNull(oc.Initializer.CloseBraceToken);
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
            Assert.Equal(SyntaxKind.ObjectCreationExpression, expr.Kind);
            Assert.Equal(text, expr.ToString());
            Assert.Equal(0, expr.Errors().Length);
            var oc = (ObjectCreationExpressionSyntax)expr;
            Assert.Null(oc.ArgumentList);
            Assert.NotNull(oc.Type);
            Assert.Equal("a", oc.Type.ToString());

            Assert.NotNull(oc.Initializer);
            Assert.NotNull(oc.Initializer.OpenBraceToken);
            Assert.NotNull(oc.Initializer.CloseBraceToken);
            Assert.False(oc.Initializer.OpenBraceToken.IsMissing);
            Assert.False(oc.Initializer.CloseBraceToken.IsMissing);
            Assert.Equal(1, oc.Initializer.Expressions.Count);
            Assert.Equal("B = { X = x }", oc.Initializer.Expressions[0].ToString());
            Assert.Equal(SyntaxKind.SimpleAssignmentExpression, oc.Initializer.Expressions[0].Kind);
            var b = (AssignmentExpressionSyntax)oc.Initializer.Expressions[0];
            Assert.Equal("B", b.Left.ToString());
            Assert.Equal(SyntaxKind.ObjectInitializerExpression, b.Right.Kind);
        }

        [Fact]
        public void TestNewArray()
        {
            var text = "new a[1]";
            var expr = this.ParseExpression(text);

            Assert.NotNull(expr);
            Assert.Equal(SyntaxKind.ArrayCreationExpression, expr.Kind);
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
            Assert.Equal(SyntaxKind.ArrayCreationExpression, expr.Kind);
            Assert.Equal(text, expr.ToString());
            Assert.Equal(0, expr.Errors().Length);
            var ac = (ArrayCreationExpressionSyntax)expr;
            Assert.NotNull(ac.Type);
            Assert.Equal("a[]", ac.Type.ToString());
            Assert.NotNull(ac.Initializer);
            Assert.NotNull(ac.Initializer.OpenBraceToken);
            Assert.NotNull(ac.Initializer.CloseBraceToken);
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
            Assert.Equal(SyntaxKind.ArrayCreationExpression, expr.Kind);
            Assert.Equal(text, expr.ToString());
            Assert.Equal(0, expr.Errors().Length);
            var ac = (ArrayCreationExpressionSyntax)expr;
            Assert.NotNull(ac.Type);
            Assert.Equal("a[]", ac.Type.ToString());
            Assert.NotNull(ac.Initializer);
            Assert.NotNull(ac.Initializer.OpenBraceToken);
            Assert.NotNull(ac.Initializer.CloseBraceToken);
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
            Assert.Equal(SyntaxKind.ArrayCreationExpression, expr.Kind);
            Assert.Equal(text, expr.ToString());
            Assert.Equal(0, expr.Errors().Length);
            var ac = (ArrayCreationExpressionSyntax)expr;
            Assert.NotNull(ac.Type);
            Assert.Equal("a[][,][,,]", ac.Type.ToString());
            Assert.NotNull(ac.Initializer);
            Assert.NotNull(ac.Initializer.OpenBraceToken);
            Assert.NotNull(ac.Initializer.CloseBraceToken);
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
            Assert.Equal(SyntaxKind.ImplicitArrayCreationExpression, expr.Kind);
            Assert.Equal(text, expr.ToString());
            Assert.Equal(0, expr.Errors().Length);
            var ac = (ImplicitArrayCreationExpressionSyntax)expr;
            Assert.NotNull(ac.Initializer);
            Assert.NotNull(ac.Initializer.OpenBraceToken);
            Assert.NotNull(ac.Initializer.CloseBraceToken);
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
            Assert.Equal(SyntaxKind.AnonymousObjectCreationExpression, expr.Kind);
            Assert.Equal(text, expr.ToString());
            Assert.Equal(0, expr.Errors().Length);
            var ac = (AnonymousObjectCreationExpressionSyntax)expr;
            Assert.NotNull(ac.NewKeyword);
            Assert.NotNull(ac.OpenBraceToken);
            Assert.NotNull(ac.CloseBraceToken);
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
            Assert.Equal(SyntaxKind.AnonymousMethodExpression, expr.Kind);
            Assert.Equal(text, expr.ToString());
            Assert.Equal(0, expr.Errors().Length);
            var am = (AnonymousMethodExpressionSyntax)expr;

            Assert.NotNull(am.DelegateKeyword);
            Assert.False(am.DelegateKeyword.IsMissing);

            Assert.NotNull(am.ParameterList);
            Assert.NotNull(am.ParameterList.OpenParenToken);
            Assert.NotNull(am.ParameterList.CloseParenToken);
            Assert.False(am.ParameterList.OpenParenToken.IsMissing);
            Assert.False(am.ParameterList.CloseParenToken.IsMissing);
            Assert.Equal(1, am.ParameterList.Parameters.Count);
            Assert.Equal("int a", am.ParameterList.Parameters[0].ToString());

            Assert.NotNull(am.Block);
            Assert.NotNull(am.Block.OpenBraceToken);
            Assert.NotNull(am.Block.CloseBraceToken);
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
            Assert.Equal(SyntaxKind.AnonymousMethodExpression, expr.Kind);
            Assert.Equal(text, expr.ToString());
            Assert.Equal(0, expr.Errors().Length);
            var am = (AnonymousMethodExpressionSyntax)expr;

            Assert.NotNull(am.DelegateKeyword);
            Assert.False(am.DelegateKeyword.IsMissing);

            Assert.NotNull(am.ParameterList);
            Assert.NotNull(am.ParameterList.OpenParenToken);
            Assert.NotNull(am.ParameterList.CloseParenToken);
            Assert.False(am.ParameterList.OpenParenToken.IsMissing);
            Assert.False(am.ParameterList.CloseParenToken.IsMissing);
            Assert.Equal(0, am.ParameterList.Parameters.Count);

            Assert.NotNull(am.Block);
            Assert.NotNull(am.Block.OpenBraceToken);
            Assert.NotNull(am.Block.CloseBraceToken);
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
            Assert.Equal(SyntaxKind.AnonymousMethodExpression, expr.Kind);
            Assert.Equal(text, expr.ToString());
            Assert.Equal(0, expr.Errors().Length);
            var am = (AnonymousMethodExpressionSyntax)expr;

            Assert.NotNull(am.DelegateKeyword);
            Assert.False(am.DelegateKeyword.IsMissing);

            Assert.Null(am.ParameterList);

            Assert.NotNull(am.Block);
            Assert.NotNull(am.Block.OpenBraceToken);
            Assert.NotNull(am.Block.CloseBraceToken);
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
            Assert.Equal(SyntaxKind.SimpleLambdaExpression, expr.Kind);
            Assert.Equal(text, expr.ToString());
            Assert.Equal(0, expr.Errors().Length);
            var lambda = (SimpleLambdaExpressionSyntax)expr;
            Assert.NotNull(lambda.Parameter.Identifier);
            Assert.False(lambda.Parameter.Identifier.IsMissing);
            Assert.Equal("a", lambda.Parameter.Identifier.ToString());
            Assert.NotNull(lambda.Body);
            Assert.Equal("b", lambda.Body.ToString());
        }

        [Fact]
        public void TestSimpleLambdaWithBlock()
        {
            var text = "a => { }";
            var expr = this.ParseExpression(text);

            Assert.NotNull(expr);
            Assert.Equal(SyntaxKind.SimpleLambdaExpression, expr.Kind);
            Assert.Equal(text, expr.ToString());
            Assert.Equal(0, expr.Errors().Length);
            var lambda = (SimpleLambdaExpressionSyntax)expr;
            Assert.NotNull(lambda.Parameter.Identifier);
            Assert.False(lambda.Parameter.Identifier.IsMissing);
            Assert.Equal("a", lambda.Parameter.Identifier.ToString());
            Assert.NotNull(lambda.Body);
            Assert.Equal(SyntaxKind.Block, lambda.Body.Kind);
            var b = (BlockSyntax)lambda.Body;
            Assert.Equal("{ }", lambda.Body.ToString());
        }

        [Fact]
        public void TestLambdaWithNoParameters()
        {
            var text = "() => b";
            var expr = this.ParseExpression(text);

            Assert.NotNull(expr);
            Assert.Equal(SyntaxKind.ParenthesizedLambdaExpression, expr.Kind);
            Assert.Equal(text, expr.ToString());
            Assert.Equal(0, expr.Errors().Length);
            var lambda = (ParenthesizedLambdaExpressionSyntax)expr;
            Assert.NotNull(lambda.ParameterList.OpenParenToken);
            Assert.NotNull(lambda.ParameterList.CloseParenToken);
            Assert.False(lambda.ParameterList.OpenParenToken.IsMissing);
            Assert.False(lambda.ParameterList.CloseParenToken.IsMissing);
            Assert.Equal(0, lambda.ParameterList.Parameters.Count);
            Assert.NotNull(lambda.Body);
            Assert.Equal("b", lambda.Body.ToString());
        }

        [Fact]
        public void TestLambdaWithNoParametersAndBlock()
        {
            var text = "() => { }";
            var expr = this.ParseExpression(text);

            Assert.NotNull(expr);
            Assert.Equal(SyntaxKind.ParenthesizedLambdaExpression, expr.Kind);
            Assert.Equal(text, expr.ToString());
            Assert.Equal(0, expr.Errors().Length);
            var lambda = (ParenthesizedLambdaExpressionSyntax)expr;
            Assert.NotNull(lambda.ParameterList.OpenParenToken);
            Assert.NotNull(lambda.ParameterList.CloseParenToken);
            Assert.False(lambda.ParameterList.OpenParenToken.IsMissing);
            Assert.False(lambda.ParameterList.CloseParenToken.IsMissing);
            Assert.Equal(0, lambda.ParameterList.Parameters.Count);
            Assert.NotNull(lambda.Body);
            Assert.Equal(SyntaxKind.Block, lambda.Body.Kind);
            var b = (BlockSyntax)lambda.Body;
            Assert.Equal("{ }", lambda.Body.ToString());
        }

        [Fact]
        public void TestLambdaWithOneParameter()
        {
            var text = "(a) => b";
            var expr = this.ParseExpression(text);

            Assert.NotNull(expr);
            Assert.Equal(SyntaxKind.ParenthesizedLambdaExpression, expr.Kind);
            Assert.Equal(text, expr.ToString());
            Assert.Equal(0, expr.Errors().Length);
            var lambda = (ParenthesizedLambdaExpressionSyntax)expr;
            Assert.NotNull(lambda.ParameterList.OpenParenToken);
            Assert.NotNull(lambda.ParameterList.CloseParenToken);
            Assert.False(lambda.ParameterList.OpenParenToken.IsMissing);
            Assert.False(lambda.ParameterList.CloseParenToken.IsMissing);
            Assert.Equal(1, lambda.ParameterList.Parameters.Count);
            Assert.Equal(SyntaxKind.Parameter, lambda.ParameterList.Parameters[0].Kind);
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
            Assert.Equal(SyntaxKind.ParenthesizedLambdaExpression, expr.Kind);
            Assert.Equal(text, expr.ToString());
            Assert.Equal(0, expr.Errors().Length);
            var lambda = (ParenthesizedLambdaExpressionSyntax)expr;
            Assert.NotNull(lambda.ParameterList.OpenParenToken);
            Assert.NotNull(lambda.ParameterList.CloseParenToken);
            Assert.False(lambda.ParameterList.OpenParenToken.IsMissing);
            Assert.False(lambda.ParameterList.CloseParenToken.IsMissing);
            Assert.Equal(2, lambda.ParameterList.Parameters.Count);
            Assert.Equal(SyntaxKind.Parameter, lambda.ParameterList.Parameters[0].Kind);
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
            Assert.Equal(SyntaxKind.ParenthesizedLambdaExpression, expr.Kind);
            Assert.Equal(text, expr.ToString());
            Assert.Equal(0, expr.Errors().Length);
            var lambda = (ParenthesizedLambdaExpressionSyntax)expr;
            Assert.NotNull(lambda.ParameterList.OpenParenToken);
            Assert.NotNull(lambda.ParameterList.CloseParenToken);
            Assert.False(lambda.ParameterList.OpenParenToken.IsMissing);
            Assert.False(lambda.ParameterList.CloseParenToken.IsMissing);
            Assert.Equal(1, lambda.ParameterList.Parameters.Count);
            Assert.Equal(SyntaxKind.Parameter, lambda.ParameterList.Parameters[0].Kind);
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
            Assert.Equal(SyntaxKind.ParenthesizedLambdaExpression, expr.Kind);
            Assert.Equal(text, expr.ToString());
            Assert.Equal(0, expr.Errors().Length);
            var lambda = (ParenthesizedLambdaExpressionSyntax)expr;
            Assert.NotNull(lambda.ParameterList.OpenParenToken);
            Assert.NotNull(lambda.ParameterList.CloseParenToken);
            Assert.False(lambda.ParameterList.OpenParenToken.IsMissing);
            Assert.False(lambda.ParameterList.CloseParenToken.IsMissing);
            Assert.Equal(1, lambda.ParameterList.Parameters.Count);
            Assert.Equal(SyntaxKind.Parameter, lambda.ParameterList.Parameters[0].Kind);
            var ps = (ParameterSyntax)lambda.ParameterList.Parameters[0];
            Assert.NotNull(ps.Type);
            Assert.Equal("T", ps.Type.ToString());
            Assert.Equal("a", ps.Identifier.ToString());
            Assert.Equal(1, ps.Modifiers.Count);
            Assert.Equal(SyntaxKind.RefKeyword, ps.Modifiers[0].CSharpKind());
            Assert.NotNull(lambda.Body);
            Assert.Equal("b", lambda.Body.ToString());
        }

        [Fact]
        public void TestFromSelect()
        {
            var text = "from a in A select b";
            var expr = this.ParseExpression(text);

            Assert.NotNull(expr);
            Assert.Equal(SyntaxKind.QueryExpression, expr.Kind);
            Assert.Equal(text, expr.ToString());
            Assert.Equal(0, expr.Errors().Length);

            var qs = (QueryExpressionSyntax)expr;
            Assert.Equal(0, qs.Body.Clauses.Count);

            var fs = (FromClauseSyntax)qs.FromClause;
            Assert.NotNull(fs.FromKeyword);
            Assert.Equal(SyntaxKind.FromKeyword, fs.FromKeyword.CSharpKind());
            Assert.False(fs.FromKeyword.IsMissing);
            Assert.Null(fs.Type);
            Assert.Equal("a", fs.Identifier.ToString());
            Assert.NotNull(fs.InKeyword);
            Assert.False(fs.InKeyword.IsMissing);
            Assert.Equal("A", fs.Expression.ToString());

            Assert.Equal(SyntaxKind.SelectClause, qs.Body.SelectOrGroup.Kind);
            var ss = (SelectClauseSyntax)qs.Body.SelectOrGroup;
            Assert.NotNull(ss.SelectKeyword);
            Assert.Equal(SyntaxKind.SelectKeyword, ss.SelectKeyword.CSharpKind());
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
            Assert.Equal(SyntaxKind.QueryExpression, expr.Kind);
            Assert.Equal(text, expr.ToString());
            Assert.Equal(0, expr.Errors().Length);

            var qs = (QueryExpressionSyntax)expr;
            Assert.Equal(0, qs.Body.Clauses.Count);

            Assert.Equal(SyntaxKind.FromClause, qs.FromClause.Kind);
            var fs = (FromClauseSyntax)qs.FromClause;
            Assert.NotNull(fs.FromKeyword);
            Assert.False(fs.FromKeyword.IsMissing);
            Assert.NotNull(fs.Type);
            Assert.Equal("T", fs.Type.ToString());
            Assert.Equal("a", fs.Identifier.ToString());
            Assert.NotNull(fs.InKeyword);
            Assert.False(fs.InKeyword.IsMissing);
            Assert.Equal("A", fs.Expression.ToString());

            Assert.Equal(SyntaxKind.SelectClause, qs.Body.SelectOrGroup.Kind);
            var ss = (SelectClauseSyntax)qs.Body.SelectOrGroup;
            Assert.NotNull(ss.SelectKeyword);
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
            Assert.Equal(SyntaxKind.QueryExpression, expr.Kind);
            Assert.Equal(text, expr.ToString());
            Assert.Equal(0, expr.Errors().Length);

            var qs = (QueryExpressionSyntax)expr;
            Assert.Equal(0, qs.Body.Clauses.Count);
            Assert.Equal(SyntaxKind.FromClause, qs.FromClause.Kind);
            Assert.Equal(SyntaxKind.SelectClause, qs.Body.SelectOrGroup.Kind);

            var fs = (FromClauseSyntax)qs.FromClause;
            Assert.NotNull(fs.FromKeyword);
            Assert.False(fs.FromKeyword.IsMissing);
            Assert.Null(fs.Type);
            Assert.Equal("a", fs.Identifier.ToString());
            Assert.NotNull(fs.InKeyword);
            Assert.False(fs.InKeyword.IsMissing);
            Assert.Equal("A", fs.Expression.ToString());

            Assert.Equal(SyntaxKind.SelectClause, qs.Body.SelectOrGroup.Kind);
            var ss = (SelectClauseSyntax)qs.Body.SelectOrGroup;
            Assert.NotNull(ss.SelectKeyword);
            Assert.False(ss.SelectKeyword.IsMissing);
            Assert.Equal("b", ss.Expression.ToString());

            Assert.NotNull(qs.Body.Continuation);
            Assert.Equal(SyntaxKind.QueryContinuation, qs.Body.Continuation.Kind);
            Assert.NotNull(qs.Body.Continuation.IntoKeyword);
            Assert.Equal(SyntaxKind.IntoKeyword, qs.Body.Continuation.IntoKeyword.CSharpKind());
            Assert.False(qs.Body.Continuation.IntoKeyword.IsMissing);
            Assert.Equal("c", qs.Body.Continuation.Identifier.ToString());

            Assert.NotNull(qs.Body.Continuation.Body);
            Assert.Equal(0, qs.Body.Continuation.Body.Clauses.Count);
            Assert.NotNull(qs.Body.Continuation.Body.SelectOrGroup);

            Assert.Equal(SyntaxKind.SelectClause, qs.Body.Continuation.Body.SelectOrGroup.Kind);
            ss = (SelectClauseSyntax)qs.Body.Continuation.Body.SelectOrGroup;
            Assert.NotNull(ss.SelectKeyword);
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
            Assert.Equal(SyntaxKind.QueryExpression, expr.Kind);
            Assert.Equal(text, expr.ToString());
            Assert.Equal(0, expr.Errors().Length);

            var qs = (QueryExpressionSyntax)expr;
            Assert.Equal(1, qs.Body.Clauses.Count);

            Assert.Equal(SyntaxKind.FromClause, qs.FromClause.Kind);
            var fs = (FromClauseSyntax)qs.FromClause;
            Assert.NotNull(fs.FromKeyword);
            Assert.False(fs.FromKeyword.IsMissing);
            Assert.Null(fs.Type);
            Assert.Equal("a", fs.Identifier.ToString());
            Assert.NotNull(fs.InKeyword);
            Assert.False(fs.InKeyword.IsMissing);
            Assert.Equal("A", fs.Expression.ToString());

            Assert.Equal(SyntaxKind.WhereClause, qs.Body.Clauses[0].Kind);
            var ws = (WhereClauseSyntax)qs.Body.Clauses[0];
            Assert.NotNull(ws.WhereKeyword);
            Assert.Equal(SyntaxKind.WhereKeyword, ws.WhereKeyword.CSharpKind());
            Assert.False(ws.WhereKeyword.IsMissing);
            Assert.NotNull(ws.Condition);
            Assert.Equal("b", ws.Condition.ToString());

            Assert.Equal(SyntaxKind.SelectClause, qs.Body.SelectOrGroup.Kind);
            var ss = (SelectClauseSyntax)qs.Body.SelectOrGroup;
            Assert.NotNull(ss.SelectKeyword);
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
            Assert.Equal(SyntaxKind.QueryExpression, expr.Kind);
            Assert.Equal(text, expr.ToString());
            Assert.Equal(0, expr.Errors().Length);

            var qs = (QueryExpressionSyntax)expr;
            Assert.Equal(1, qs.Body.Clauses.Count);

            Assert.Equal(SyntaxKind.FromClause, qs.FromClause.Kind);
            var fs = (FromClauseSyntax)qs.FromClause;
            Assert.NotNull(fs.FromKeyword);
            Assert.False(fs.FromKeyword.IsMissing);
            Assert.Null(fs.Type);
            Assert.Equal("a", fs.Identifier.ToString());
            Assert.NotNull(fs.InKeyword);
            Assert.False(fs.InKeyword.IsMissing);
            Assert.Equal("A", fs.Expression.ToString());
            Assert.Equal(SyntaxKind.SelectClause, qs.Body.SelectOrGroup.Kind);

            Assert.Equal(SyntaxKind.FromClause, qs.Body.Clauses[0].Kind);
            fs = (FromClauseSyntax)qs.Body.Clauses[0];
            Assert.NotNull(fs.FromKeyword);
            Assert.False(fs.FromKeyword.IsMissing);
            Assert.Null(fs.Type);
            Assert.Equal("b", fs.Identifier.ToString());
            Assert.NotNull(fs.InKeyword);
            Assert.False(fs.InKeyword.IsMissing);
            Assert.Equal("B", fs.Expression.ToString());

            Assert.Equal(SyntaxKind.SelectClause, qs.Body.SelectOrGroup.Kind);
            var ss = (SelectClauseSyntax)qs.Body.SelectOrGroup;
            Assert.NotNull(ss.SelectKeyword);
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
            Assert.Equal(SyntaxKind.QueryExpression, expr.Kind);
            Assert.Equal(text, expr.ToString());
            Assert.Equal(0, expr.Errors().Length);

            var qs = (QueryExpressionSyntax)expr;
            Assert.Equal(1, qs.Body.Clauses.Count);

            Assert.Equal(SyntaxKind.FromClause, qs.FromClause.Kind);
            var fs = (FromClauseSyntax)qs.FromClause;
            Assert.NotNull(fs.FromKeyword);
            Assert.False(fs.FromKeyword.IsMissing);
            Assert.Null(fs.Type);
            Assert.Equal("a", fs.Identifier.ToString());
            Assert.NotNull(fs.InKeyword);
            Assert.False(fs.InKeyword.IsMissing);
            Assert.Equal("A", fs.Expression.ToString());
            Assert.Equal(SyntaxKind.SelectClause, qs.Body.SelectOrGroup.Kind);

            Assert.Equal(SyntaxKind.LetClause, qs.Body.Clauses[0].Kind);
            var ls = (LetClauseSyntax)qs.Body.Clauses[0];
            Assert.NotNull(ls.LetKeyword);
            Assert.Equal(SyntaxKind.LetKeyword, ls.LetKeyword.CSharpKind());
            Assert.False(ls.LetKeyword.IsMissing);
            Assert.NotNull(ls.Identifier);
            Assert.Equal("b", ls.Identifier.ToString());
            Assert.NotNull(ls.EqualsToken);
            Assert.False(ls.EqualsToken.IsMissing);
            Assert.NotNull(ls.Expression);
            Assert.Equal("B", ls.Expression.ToString());

            Assert.Equal(SyntaxKind.SelectClause, qs.Body.SelectOrGroup.Kind);
            var ss = (SelectClauseSyntax)qs.Body.SelectOrGroup;
            Assert.NotNull(ss.SelectKeyword);
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
            Assert.Equal(SyntaxKind.QueryExpression, expr.Kind);
            Assert.Equal(text, expr.ToString());
            Assert.Equal(0, expr.Errors().Length);

            var qs = (QueryExpressionSyntax)expr;
            Assert.Equal(1, qs.Body.Clauses.Count);

            Assert.Equal(SyntaxKind.FromClause, qs.FromClause.Kind);
            var fs = (FromClauseSyntax)qs.FromClause;
            Assert.NotNull(fs.FromKeyword);
            Assert.False(fs.FromKeyword.IsMissing);
            Assert.Null(fs.Type);
            Assert.Equal("a", fs.Identifier.ToString());
            Assert.NotNull(fs.InKeyword);
            Assert.False(fs.InKeyword.IsMissing);
            Assert.Equal("A", fs.Expression.ToString());
            Assert.Equal(SyntaxKind.SelectClause, qs.Body.SelectOrGroup.Kind);

            Assert.Equal(SyntaxKind.OrderByClause, qs.Body.Clauses[0].Kind);
            var obs = (OrderByClauseSyntax)qs.Body.Clauses[0];
            Assert.NotNull(obs.OrderByKeyword);
            Assert.Equal(SyntaxKind.OrderByKeyword, obs.OrderByKeyword.CSharpKind());
            Assert.False(obs.OrderByKeyword.IsMissing);
            Assert.Equal(1, obs.Orderings.Count);

            var os = (OrderingSyntax)obs.Orderings[0];
            Assert.Equal(SyntaxKind.None, os.AscendingOrDescendingKeyword.CSharpKind());
            Assert.NotNull(os.Expression);
            Assert.Equal("b", os.Expression.ToString());

            Assert.Equal(SyntaxKind.SelectClause, qs.Body.SelectOrGroup.Kind);
            var ss = (SelectClauseSyntax)qs.Body.SelectOrGroup;
            Assert.NotNull(ss.SelectKeyword);
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
            Assert.Equal(SyntaxKind.QueryExpression, expr.Kind);
            Assert.Equal(text, expr.ToString());
            Assert.Equal(0, expr.Errors().Length);

            var qs = (QueryExpressionSyntax)expr;
            Assert.Equal(1, qs.Body.Clauses.Count);

            Assert.Equal(SyntaxKind.FromClause, qs.FromClause.Kind);
            var fs = (FromClauseSyntax)qs.FromClause;
            Assert.NotNull(fs.FromKeyword);
            Assert.False(fs.FromKeyword.IsMissing);
            Assert.Null(fs.Type);
            Assert.Equal("a", fs.Identifier.ToString());
            Assert.NotNull(fs.InKeyword);
            Assert.False(fs.InKeyword.IsMissing);
            Assert.Equal("A", fs.Expression.ToString());
            Assert.Equal(SyntaxKind.SelectClause, qs.Body.SelectOrGroup.Kind);

            Assert.Equal(SyntaxKind.OrderByClause, qs.Body.Clauses[0].Kind);
            var obs = (OrderByClauseSyntax)qs.Body.Clauses[0];
            Assert.NotNull(obs.OrderByKeyword);
            Assert.False(obs.OrderByKeyword.IsMissing);
            Assert.Equal(2, obs.Orderings.Count);

            var os = (OrderingSyntax)obs.Orderings[0];
            Assert.Equal(SyntaxKind.None, os.AscendingOrDescendingKeyword.CSharpKind());
            Assert.NotNull(os.Expression);
            Assert.Equal("b", os.Expression.ToString());

            os = (OrderingSyntax)obs.Orderings[1];
            Assert.Equal(SyntaxKind.None, os.AscendingOrDescendingKeyword.CSharpKind());
            Assert.NotNull(os.Expression);
            Assert.Equal("b2", os.Expression.ToString());

            Assert.Equal(SyntaxKind.SelectClause, qs.Body.SelectOrGroup.Kind);
            var ss = (SelectClauseSyntax)qs.Body.SelectOrGroup;
            Assert.NotNull(ss.SelectKeyword);
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
            Assert.Equal(SyntaxKind.QueryExpression, expr.Kind);
            Assert.Equal(text, expr.ToString());
            Assert.Equal(0, expr.Errors().Length);

            var qs = (QueryExpressionSyntax)expr;
            Assert.Equal(1, qs.Body.Clauses.Count);

            Assert.Equal(SyntaxKind.FromClause, qs.FromClause.Kind);
            var fs = (FromClauseSyntax)qs.FromClause;
            Assert.NotNull(fs.FromKeyword);
            Assert.False(fs.FromKeyword.IsMissing);
            Assert.Null(fs.Type);
            Assert.Equal("a", fs.Identifier.ToString());
            Assert.NotNull(fs.InKeyword);
            Assert.False(fs.InKeyword.IsMissing);
            Assert.Equal("A", fs.Expression.ToString());
            Assert.Equal(SyntaxKind.SelectClause, qs.Body.SelectOrGroup.Kind);

            Assert.Equal(SyntaxKind.OrderByClause, qs.Body.Clauses[0].Kind);
            var obs = (OrderByClauseSyntax)qs.Body.Clauses[0];
            Assert.NotNull(obs.OrderByKeyword);
            Assert.False(obs.OrderByKeyword.IsMissing);
            Assert.Equal(1, obs.Orderings.Count);

            var os = (OrderingSyntax)obs.Orderings[0];
            Assert.NotNull(os.AscendingOrDescendingKeyword);
            Assert.Equal(SyntaxKind.AscendingKeyword, os.AscendingOrDescendingKeyword.CSharpKind());
            Assert.False(os.AscendingOrDescendingKeyword.IsMissing);
            Assert.Equal(SyntaxKind.AscendingKeyword, os.AscendingOrDescendingKeyword.CSharpContextualKind());

            Assert.NotNull(os.Expression);
            Assert.Equal("b", os.Expression.ToString());

            Assert.Equal(SyntaxKind.SelectClause, qs.Body.SelectOrGroup.Kind);
            var ss = (SelectClauseSyntax)qs.Body.SelectOrGroup;
            Assert.NotNull(ss.SelectKeyword);
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
            Assert.Equal(SyntaxKind.QueryExpression, expr.Kind);
            Assert.Equal(text, expr.ToString());
            Assert.Equal(0, expr.Errors().Length);

            var qs = (QueryExpressionSyntax)expr;
            Assert.Equal(1, qs.Body.Clauses.Count);

            Assert.Equal(SyntaxKind.FromClause, qs.FromClause.Kind);
            var fs = (FromClauseSyntax)qs.FromClause;
            Assert.NotNull(fs.FromKeyword);
            Assert.False(fs.FromKeyword.IsMissing);
            Assert.Null(fs.Type);
            Assert.Equal("a", fs.Identifier.ToString());
            Assert.NotNull(fs.InKeyword);
            Assert.False(fs.InKeyword.IsMissing);
            Assert.Equal("A", fs.Expression.ToString());
            Assert.Equal(SyntaxKind.SelectClause, qs.Body.SelectOrGroup.Kind);

            Assert.Equal(SyntaxKind.OrderByClause, qs.Body.Clauses[0].Kind);
            var obs = (OrderByClauseSyntax)qs.Body.Clauses[0];
            Assert.NotNull(obs.OrderByKeyword);
            Assert.False(obs.OrderByKeyword.IsMissing);
            Assert.Equal(1, obs.Orderings.Count);

            var os = (OrderingSyntax)obs.Orderings[0];
            Assert.NotNull(os.AscendingOrDescendingKeyword);
            Assert.Equal(SyntaxKind.DescendingKeyword, os.AscendingOrDescendingKeyword.CSharpKind());
            Assert.False(os.AscendingOrDescendingKeyword.IsMissing);
            Assert.Equal(SyntaxKind.DescendingKeyword, os.AscendingOrDescendingKeyword.CSharpContextualKind());

            Assert.NotNull(os.Expression);
            Assert.Equal("b", os.Expression.ToString());

            Assert.Equal(SyntaxKind.SelectClause, qs.Body.SelectOrGroup.Kind);
            var ss = (SelectClauseSyntax)qs.Body.SelectOrGroup;
            Assert.NotNull(ss.SelectKeyword);
            Assert.False(ss.SelectKeyword.IsMissing);
            Assert.Equal("c", ss.Expression.ToString());
            Assert.Null(qs.Body.Continuation);
        }

        public void TestFromGroupBy()
        {
            var text = "from a in A group b by c";
            var expr = this.ParseExpression(text);

            Assert.NotNull(expr);
            Assert.Equal(SyntaxKind.QueryExpression, expr.Kind);
            Assert.Equal(text, expr.ToString());
            Assert.Equal(0, expr.Errors().Length);

            var qs = (QueryExpressionSyntax)expr;
            Assert.Equal(1, qs.Body.Clauses.Count);
            Assert.Equal(SyntaxKind.FromClause, qs.Body.Clauses[0].Kind);

            var fs = (FromClauseSyntax)qs.Body.Clauses[0];
            Assert.NotNull(fs.FromKeyword);
            Assert.False(fs.FromKeyword.IsMissing);
            Assert.Null(fs.Type);
            Assert.Equal("a", fs.Identifier.ToString());
            Assert.NotNull(fs.InKeyword);
            Assert.False(fs.InKeyword.IsMissing);
            Assert.Equal("A", fs.Expression.ToString());

            Assert.Equal(SyntaxKind.GroupClause, qs.Body.SelectOrGroup.Kind);
            var gbs = (GroupClauseSyntax)qs.Body.SelectOrGroup;
            Assert.NotNull(gbs.GroupKeyword);
            Assert.Equal(SyntaxKind.GroupKeyword, gbs.GroupKeyword.CSharpKind());
            Assert.False(gbs.GroupKeyword.IsMissing);
            Assert.NotNull(gbs.GroupExpression);
            Assert.Equal("b", gbs.GroupExpression.ToString());
            Assert.NotNull(gbs.ByKeyword);
            Assert.Equal(SyntaxKind.ByKeyword, gbs.ByKeyword.CSharpKind());
            Assert.False(gbs.ByKeyword.IsMissing);
            Assert.NotNull(gbs.ByExpression);
            Assert.Equal("c", gbs.ByExpression.ToString());

            Assert.Null(qs.Body.Continuation);
        }

        public void TestFromGroupByIntoSelect()
        {
            var text = "from a in A group b by c into d select e";
            var expr = this.ParseExpression(text);

            Assert.NotNull(expr);
            Assert.Equal(SyntaxKind.QueryExpression, expr.Kind);
            Assert.Equal(text, expr.ToString());
            Assert.Equal(0, expr.Errors().Length);

            var qs = (QueryExpressionSyntax)expr;
            Assert.Equal(1, qs.Body.Clauses.Count);
            Assert.Equal(SyntaxKind.FromClause, qs.Body.Clauses[0].Kind);

            var fs = (FromClauseSyntax)qs.Body.Clauses[0];
            Assert.NotNull(fs.FromKeyword);
            Assert.False(fs.FromKeyword.IsMissing);
            Assert.Null(fs.Type);
            Assert.Equal("a", fs.Identifier.ToString());
            Assert.NotNull(fs.InKeyword);
            Assert.False(fs.InKeyword.IsMissing);
            Assert.Equal("A", fs.Expression.ToString());

            Assert.Equal(SyntaxKind.GroupClause, qs.Body.SelectOrGroup.Kind);
            var gbs = (GroupClauseSyntax)qs.Body.SelectOrGroup;
            Assert.NotNull(gbs.GroupKeyword);
            Assert.False(gbs.GroupKeyword.IsMissing);
            Assert.NotNull(gbs.GroupExpression);
            Assert.Equal("b", gbs.GroupExpression.ToString());
            Assert.NotNull(gbs.ByKeyword);
            Assert.False(gbs.ByKeyword.IsMissing);
            Assert.NotNull(gbs.ByExpression);
            Assert.Equal("c", gbs.ByExpression.ToString());

            Assert.NotNull(qs.Body.Continuation);
            Assert.Equal(SyntaxKind.QueryContinuation, qs.Body.Continuation.Kind);
            Assert.NotNull(qs.Body.Continuation.IntoKeyword);
            Assert.False(qs.Body.Continuation.IntoKeyword.IsMissing);
            Assert.Equal("d", qs.Body.Continuation.Identifier.ToString());

            Assert.NotNull(qs.Body.Continuation);
            Assert.Equal(0, qs.Body.Continuation.Body.Clauses.Count);
            Assert.NotNull(qs.Body.Continuation.Body.SelectOrGroup);

            Assert.Equal(SyntaxKind.SelectClause, qs.Body.Continuation.Body.SelectOrGroup.Kind);
            var ss = (SelectClauseSyntax)qs.Body.Continuation.Body.SelectOrGroup;
            Assert.NotNull(ss.SelectKeyword);
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
            Assert.Equal(SyntaxKind.QueryExpression, expr.Kind);
            Assert.Equal(text, expr.ToString());
            Assert.Equal(0, expr.Errors().Length);

            var qs = (QueryExpressionSyntax)expr;
            Assert.Equal(1, qs.Body.Clauses.Count);

            Assert.Equal(SyntaxKind.FromClause, qs.FromClause.Kind);
            var fs = (FromClauseSyntax)qs.FromClause;
            Assert.NotNull(fs.FromKeyword);
            Assert.False(fs.FromKeyword.IsMissing);
            Assert.Null(fs.Type);
            Assert.Equal("a", fs.Identifier.ToString());
            Assert.NotNull(fs.InKeyword);
            Assert.False(fs.InKeyword.IsMissing);
            Assert.Equal("A", fs.Expression.ToString());

            Assert.Equal(SyntaxKind.JoinClause, qs.Body.Clauses[0].Kind);
            var js = (JoinClauseSyntax)qs.Body.Clauses[0];
            Assert.NotNull(js.JoinKeyword);
            Assert.Equal(SyntaxKind.JoinKeyword, js.JoinKeyword.CSharpKind());
            Assert.False(js.JoinKeyword.IsMissing);
            Assert.Null(js.Type);
            Assert.NotNull(js.Identifier);
            Assert.Equal("b", js.Identifier.ToString());
            Assert.NotNull(js.InKeyword);
            Assert.False(js.InKeyword.IsMissing);
            Assert.NotNull(js.InExpression);
            Assert.Equal("B", js.InExpression.ToString());
            Assert.NotNull(js.OnKeyword);
            Assert.Equal(SyntaxKind.OnKeyword, js.OnKeyword.CSharpKind());
            Assert.False(js.OnKeyword.IsMissing);
            Assert.NotNull(js.LeftExpression);
            Assert.Equal("a", js.LeftExpression.ToString());
            Assert.NotNull(js.EqualsKeyword);
            Assert.Equal(SyntaxKind.EqualsKeyword, js.EqualsKeyword.CSharpKind());
            Assert.False(js.EqualsKeyword.IsMissing);
            Assert.NotNull(js.RightExpression);
            Assert.Equal("b", js.RightExpression.ToString());
            Assert.Null(js.Into);

            Assert.Equal(SyntaxKind.SelectClause, qs.Body.SelectOrGroup.Kind);
            var ss = (SelectClauseSyntax)qs.Body.SelectOrGroup;
            Assert.NotNull(ss.SelectKeyword);
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
            Assert.Equal(SyntaxKind.QueryExpression, expr.Kind);
            Assert.Equal(text, expr.ToString());
            Assert.Equal(0, expr.Errors().Length);

            var qs = (QueryExpressionSyntax)expr;
            Assert.Equal(1, qs.Body.Clauses.Count);

            Assert.Equal(SyntaxKind.FromClause, qs.FromClause.Kind);
            var fs = (FromClauseSyntax)qs.FromClause;
            Assert.NotNull(fs.FromKeyword);
            Assert.False(fs.FromKeyword.IsMissing);
            Assert.NotNull(fs.Type);
            Assert.Equal("Ta", fs.Type.ToString());
            Assert.Equal("a", fs.Identifier.ToString());
            Assert.NotNull(fs.InKeyword);
            Assert.False(fs.InKeyword.IsMissing);
            Assert.Equal("A", fs.Expression.ToString());

            Assert.Equal(SyntaxKind.JoinClause, qs.Body.Clauses[0].Kind);
            var js = (JoinClauseSyntax)qs.Body.Clauses[0];
            Assert.NotNull(js.JoinKeyword);
            Assert.False(js.JoinKeyword.IsMissing);
            Assert.NotNull(js.Type);
            Assert.Equal("Tb", js.Type.ToString());
            Assert.NotNull(js.Identifier);
            Assert.Equal("b", js.Identifier.ToString());
            Assert.NotNull(js.InKeyword);
            Assert.False(js.InKeyword.IsMissing);
            Assert.NotNull(js.InExpression);
            Assert.Equal("B", js.InExpression.ToString());
            Assert.NotNull(js.OnKeyword);
            Assert.False(js.OnKeyword.IsMissing);
            Assert.NotNull(js.LeftExpression);
            Assert.Equal("a", js.LeftExpression.ToString());
            Assert.NotNull(js.EqualsKeyword);
            Assert.False(js.EqualsKeyword.IsMissing);
            Assert.NotNull(js.RightExpression);
            Assert.Equal("b", js.RightExpression.ToString());
            Assert.Null(js.Into);

            Assert.Equal(SyntaxKind.SelectClause, qs.Body.SelectOrGroup.Kind);
            var ss = (SelectClauseSyntax)qs.Body.SelectOrGroup;
            Assert.NotNull(ss.SelectKeyword);
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
            Assert.Equal(SyntaxKind.QueryExpression, expr.Kind);
            Assert.Equal(text, expr.ToString());
            Assert.Equal(0, expr.Errors().Length);

            var qs = (QueryExpressionSyntax)expr;
            Assert.Equal(1, qs.Body.Clauses.Count);

            Assert.Equal(SyntaxKind.FromClause, qs.FromClause.Kind);
            var fs = (FromClauseSyntax)qs.FromClause;
            Assert.NotNull(fs.FromKeyword);
            Assert.False(fs.FromKeyword.IsMissing);
            Assert.Null(fs.Type);
            Assert.Equal("a", fs.Identifier.ToString());
            Assert.NotNull(fs.InKeyword);
            Assert.False(fs.InKeyword.IsMissing);
            Assert.Equal("A", fs.Expression.ToString());

            Assert.Equal(SyntaxKind.JoinClause, qs.Body.Clauses[0].Kind);
            var js = (JoinClauseSyntax)qs.Body.Clauses[0];
            Assert.NotNull(js.JoinKeyword);
            Assert.False(js.JoinKeyword.IsMissing);
            Assert.Null(js.Type);
            Assert.NotNull(js.Identifier);
            Assert.Equal("b", js.Identifier.ToString());
            Assert.NotNull(js.InKeyword);
            Assert.False(js.InKeyword.IsMissing);
            Assert.NotNull(js.InExpression);
            Assert.Equal("B", js.InExpression.ToString());
            Assert.NotNull(js.OnKeyword);
            Assert.False(js.OnKeyword.IsMissing);
            Assert.NotNull(js.LeftExpression);
            Assert.Equal("a", js.LeftExpression.ToString());
            Assert.NotNull(js.EqualsKeyword);
            Assert.False(js.EqualsKeyword.IsMissing);
            Assert.NotNull(js.RightExpression);
            Assert.Equal("b", js.RightExpression.ToString());
            Assert.NotNull(js.Into);
            Assert.NotNull(js.Into.IntoKeyword);
            Assert.False(js.Into.IntoKeyword.IsMissing);
            Assert.NotNull(js.Into.Identifier);
            Assert.Equal("c", js.Into.Identifier.ToString());

            Assert.Equal(SyntaxKind.SelectClause, qs.Body.SelectOrGroup.Kind);
            var ss = (SelectClauseSyntax)qs.Body.SelectOrGroup;
            Assert.NotNull(ss.SelectKeyword);
            Assert.False(ss.SelectKeyword.IsMissing);
            Assert.Equal("d", ss.Expression.ToString());
            Assert.Null(qs.Body.Continuation);
        }

        [Fact]
        public void TestFromGroupBy1()
        {
            var text = "from it in foo group x by y";
            var expr = SyntaxFactory.ParseExpression(text);

            Assert.NotNull(expr);
            Assert.Equal(SyntaxKind.QueryExpression, expr.Kind);
            Assert.Equal(text, expr.ToString());
            Assert.Equal(0, expr.Errors().Length);

            var qs = (QueryExpressionSyntax)expr;
            Assert.NotNull(qs.Body.SelectOrGroup);
            Assert.IsType(typeof(GroupClauseSyntax), qs.Body.SelectOrGroup);

            var gs = (GroupClauseSyntax)qs.Body.SelectOrGroup;
            Assert.NotNull(gs.GroupExpression);
            Assert.Equal("x", gs.GroupExpression.ToString());
            Assert.Equal("y", gs.ByExpression.ToString());
        }

        [WorkItem(543075, "DevDiv")]
        [Fact]
        public void UnterminatedRankSpecifier()
        {
            var text = "new int[";
            var expr = this.ParseExpression(text);

            Assert.NotNull(expr);
            Assert.Equal(SyntaxKind.ArrayCreationExpression, expr.Kind);

            var arrayCreation = (ArrayCreationExpressionSyntax)expr;
            Assert.Equal(1, arrayCreation.Type.RankSpecifiers.Single().Rank);
        }

        [WorkItem(543075, "DevDiv")]
        [Fact]
        public void UnterminatedTypeArgumentList()
        {
            var text = "new C<";
            var expr = this.ParseExpression(text);

            Assert.NotNull(expr);
            Assert.Equal(SyntaxKind.ObjectCreationExpression, expr.Kind);

            var objectCreation = (ObjectCreationExpressionSyntax)expr;
            Assert.Equal(1, ((NameSyntax)objectCreation.Type).Arity);
        }

        [WorkItem(675602, "DevDiv")]
        [Fact]
        public void QueryKeywordInObjectInitializer()
        {
            //'on' is a keyword here
            var text = "from elem in aRay select new Result { A = on = true }";
            var expr = this.ParseExpression(text);

            Assert.NotNull(expr);
            Assert.Equal(SyntaxKind.QueryExpression, expr.Kind);
            Assert.Equal(text, expr.ToString());
            Assert.NotEqual(0, expr.Errors().Length);
        }

        [Fact]
        public void IndexingExpressionInParens()
        {
            var text = "(aRay[i,j])";
            var expr = this.ParseExpression(text);

            Assert.NotNull(expr);
            Assert.Equal(SyntaxKind.ParenthesizedExpression, expr.Kind);

            var parenExp = (ParenthesizedExpressionSyntax)expr;
            Assert.Equal(SyntaxKind.ElementAccessExpression, parenExp.Expression.Kind);
        }

        [WorkItem(543993, "DevDiv")]
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
    }
}
