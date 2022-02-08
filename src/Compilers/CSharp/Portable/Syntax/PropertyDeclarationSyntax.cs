// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.ComponentModel;

namespace Microsoft.CodeAnalysis.CSharp.Syntax
{
    public partial class PropertyDeclarationSyntax
    {
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("This member is obsolete.", true)]
        public SyntaxToken Semicolon
        {
            get
            {
                return this.SemicolonToken;
            }
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("This member is obsolete.", true)]
        public PropertyDeclarationSyntax WithSemicolon(SyntaxToken semicolon)
        {
            return this.WithSemicolonToken(semicolon);
        }
    }

    // backwards compatibility for API extension
    public sealed partial class AccessorDeclarationSyntax : CSharpSyntaxNode
    {
        public AccessorDeclarationSyntax Update(SyntaxList<AttributeListSyntax> attributeLists, SyntaxTokenList modifiers, SyntaxToken keyword, BlockSyntax? body, SyntaxToken semicolonToken)
            => Update(attributeLists, modifiers, keyword, body, expressionBody: null, semicolonToken);
    }
}

namespace Microsoft.CodeAnalysis.CSharp
{
    public partial class SyntaxFactory
    {
        /// <summary>Creates a new AccessorDeclarationSyntax instance.</summary>
        public static AccessorDeclarationSyntax AccessorDeclaration(SyntaxKind kind, BlockSyntax? body)
        {
            return SyntaxFactory.AccessorDeclaration(kind, default(SyntaxList<AttributeListSyntax>), default(SyntaxTokenList), SyntaxFactory.Token(GetAccessorDeclarationKeywordKind(kind)), body, expressionBody: null, default(SyntaxToken));
        }
    }
}

