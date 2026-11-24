// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.CodeAnalysis.CSharp.Syntax
{
    partial class ShebangDirectiveTriviaSyntax
    {
        public ShebangDirectiveTriviaSyntax Update(
            SyntaxToken hashToken,
            SyntaxToken exclamationToken,
            SyntaxToken endOfDirectiveToken,
            bool isActive)
        {
            if (endOfDirectiveToken.HasLeadingTrivia)
            {
                var newNode = SyntaxFactory.ShebangDirectiveTrivia(hashToken, exclamationToken, endOfDirectiveToken, isActive);
                var annotations = GetAnnotations();
                return annotations?.Length > 0 ? newNode.WithAnnotations(annotations) : newNode;
            }

            return Update(hashToken, exclamationToken, this.Content, endOfDirectiveToken, isActive);
        }
    }
}

namespace Microsoft.CodeAnalysis.CSharp
{
    public static partial class SyntaxFactory
    {
        public static ShebangDirectiveTriviaSyntax ShebangDirectiveTrivia(
            SyntaxToken hashToken,
            SyntaxToken exclamationToken,
            SyntaxToken endOfDirectiveToken,
            bool isActive)
        {
            var content = endOfDirectiveToken.LeadingTrivia.ToString();
            return ShebangDirectiveTrivia(
                hashToken,
                exclamationToken,
                Token(default, SyntaxKind.StringLiteralToken, content, content, default),
                endOfDirectiveToken.WithLeadingTrivia(),
                isActive);
        }

        public static ShebangDirectiveTriviaSyntax ShebangDirectiveTrivia(bool isActive)
            => ShebangDirectiveTrivia(
                Token(SyntaxKind.HashToken),
                Token(SyntaxKind.ExclamationToken),
                Token(SyntaxKind.EndOfDirectiveToken),
                isActive);
    }
}
