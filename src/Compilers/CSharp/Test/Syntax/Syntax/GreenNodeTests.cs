// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using Roslyn.Test.Utilities;
using Xunit;
using InternalSyntax = Microsoft.CodeAnalysis.CSharp.Syntax.InternalSyntax;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public partial class GreenNodeTests
    {
        private static void AttachAndCheckDiagnostics(InternalSyntax.CSharpSyntaxNode node)
        {
            var nodeWithDiags = node.SetDiagnostics(new DiagnosticInfo[] { new CSDiagnosticInfo(ErrorCode.ERR_NoBaseClass) });
            var diags = nodeWithDiags.GetDiagnostics();

            Assert.NotEqual(node, nodeWithDiags);
            Assert.Equal(1, diags.Length);
            Assert.Equal(ErrorCode.ERR_NoBaseClass, (ErrorCode)diags[0].Code);
        }

        private class TokenDeleteRewriter : InternalSyntax.CSharpSyntaxRewriter
        {
            public override InternalSyntax.CSharpSyntaxNode VisitToken(InternalSyntax.SyntaxToken token)
            {
                return InternalSyntax.SyntaxFactory.MissingToken(token.Kind);
            }
        }

        private class IdentityRewriter : InternalSyntax.CSharpSyntaxRewriter
        {
            protected override InternalSyntax.CSharpSyntaxNode DefaultVisit(InternalSyntax.CSharpSyntaxNode node)
            {
                return node;
            }
        }

        [Fact, WorkItem(33685, "https://github.com/dotnet/roslyn/issues/33685")]
        public void ConvenienceSwitchStatementFactoriesAddParensWhenNeeded_01()
        {
            var expression = SyntaxFactory.ParseExpression("x");
            var sw1 = SyntaxFactory.SwitchStatement(expression);
            Assert.Equal(SyntaxKind.OpenParenToken, sw1.OpenParenToken.Kind());
            Assert.Equal(SyntaxKind.CloseParenToken, sw1.CloseParenToken.Kind());
            var sw2 = SyntaxFactory.SwitchStatement(expression, default);
            Assert.Equal(SyntaxKind.OpenParenToken, sw2.OpenParenToken.Kind());
            Assert.Equal(SyntaxKind.CloseParenToken, sw2.CloseParenToken.Kind());
        }

        [Fact, WorkItem(33685, "https://github.com/dotnet/roslyn/issues/33685")]
        public void ConvenienceSwitchStatementFactoriesAddParensWhenNeeded_02()
        {
            var expression = SyntaxFactory.ParseExpression("(x)");
            var sw1 = SyntaxFactory.SwitchStatement(expression);
            Assert.Equal(SyntaxKind.OpenParenToken, sw1.OpenParenToken.Kind());
            Assert.Equal(SyntaxKind.CloseParenToken, sw1.CloseParenToken.Kind());
            var sw2 = SyntaxFactory.SwitchStatement(expression, default);
            Assert.Equal(SyntaxKind.OpenParenToken, sw2.OpenParenToken.Kind());
            Assert.Equal(SyntaxKind.CloseParenToken, sw2.CloseParenToken.Kind());
        }

        [Fact, WorkItem(33685, "https://github.com/dotnet/roslyn/issues/33685")]
        public void ConvenienceSwitchStatementFactoriesOmitParensWhenPossible()
        {
            var expression = SyntaxFactory.ParseExpression("(1, 2)");
            var sw1 = SyntaxFactory.SwitchStatement(expression);
            Assert.True(sw1.OpenParenToken == default);
            Assert.True(sw1.CloseParenToken == default);
            var sw2 = SyntaxFactory.SwitchStatement(expression, default);
            Assert.True(sw2.OpenParenToken == default);
            Assert.True(sw2.CloseParenToken == default);
        }
    }
}
