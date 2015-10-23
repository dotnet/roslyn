// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.


namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public partial class RedNodeTests
    {
        private class TokenDeleteRewriter : CSharpSyntaxRewriter
        {
            public override SyntaxToken VisitToken(SyntaxToken token)
            {
                return SyntaxFactory.MissingToken(token.Kind());
            }
        }

        private class IdentityRewriter : CSharpSyntaxRewriter
        {
            public override SyntaxNode DefaultVisit(SyntaxNode node)
            {
                return node;
            }
        }
    }
}
