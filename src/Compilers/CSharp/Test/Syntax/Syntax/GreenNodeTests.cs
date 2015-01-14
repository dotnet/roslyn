// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Xunit;
using InternalSyntax = Microsoft.CodeAnalysis.CSharp.Syntax.InternalSyntax;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public partial class GreenNodeTests
    {
        private static void AttachAndCheckDiagnostics(InternalSyntax.CSharpSyntaxNode node)
        {
            var nodeWithDiags = node.SetDiagnostics(new DiagnosticInfo[] { new CSDiagnosticInfo(ErrorCode.ERR_AbstractAndExtern) });
            var diags = nodeWithDiags.GetDiagnostics();

            Assert.NotEqual(node, nodeWithDiags);
            Assert.Equal(1, diags.Length);
            Assert.Equal(ErrorCode.ERR_AbstractAndExtern, (ErrorCode)diags[0].Code);
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
    }
}
