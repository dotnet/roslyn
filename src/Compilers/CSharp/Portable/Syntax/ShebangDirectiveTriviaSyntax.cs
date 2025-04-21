// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace Microsoft.CodeAnalysis.CSharp.Syntax
{
    partial class ShebangDirectiveTriviaSyntax
    {
        public SyntaxToken Content
        {
            get
            {
                var token = InternalSyntax.SyntaxToken.StringLiteral(this.EndOfDirectiveToken.LeadingTrivia.ToString());
                return token != null ? new SyntaxToken(this, token, GetChildPosition(2), GetChildIndex(2)) : default;
            }
        }

        public ShebangDirectiveTriviaSyntax WithContent(SyntaxToken content)
        {
            if (content != this.Content)
            {
                return (ShebangDirectiveTriviaSyntax)((InternalSyntax.ShebangDirectiveTriviaSyntax)this.Green)
                    .WithContent((InternalSyntax.SyntaxToken)content.Node!).CreateRed();
            }

            return this;
        }
    }
}

namespace Microsoft.CodeAnalysis.CSharp.Syntax.InternalSyntax
{
    partial class ShebangDirectiveTriviaSyntax
    {
        public ShebangDirectiveTriviaSyntax WithContent(SyntaxToken content)
        {
            SyntaxToken endOfDirectiveToken = this.EndOfDirectiveToken;

            if (content.Kind is SyntaxKind.StringLiteralToken)
            {
                endOfDirectiveToken = endOfDirectiveToken.TokenWithLeadingTrivia(SyntaxFactory.PreprocessingMessage(content.ToString()));
            }
            else if (content.Kind is not SyntaxKind.None)
            {
                throw new ArgumentException(nameof(content));
            }

            return Update(
                this.HashToken,
                this.ExclamationToken,
                endOfDirectiveToken,
                this.IsActive);
        }
    }
}
