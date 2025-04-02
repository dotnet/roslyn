// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.CodeAnalysis.Syntax;

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

        public ShebangDirectiveTriviaSyntax Update(SyntaxToken hashToken, SyntaxToken exclamationToken, SyntaxToken content, SyntaxToken endOfDirectiveToken, bool isActive)
        {
            if (hashToken != this.HashToken || exclamationToken != this.ExclamationToken || content != this.Content || endOfDirectiveToken != this.EndOfDirectiveToken)
            {
                var newNode = SyntaxFactory.ShebangDirectiveTrivia(hashToken, exclamationToken, content, endOfDirectiveToken, isActive);
                var annotations = GetAnnotations();
                return annotations?.Length > 0 ? newNode.WithAnnotations(annotations) : newNode;
            }

            return this;
        }

        public ShebangDirectiveTriviaSyntax WithContent(SyntaxToken content)
        {
            return Update(
                this.HashToken,
                this.ExclamationToken,
                content,
                this.EndOfDirectiveToken.HasLeadingTrivia ? this.EndOfDirectiveToken.WithLeadingTrivia(default(SyntaxTriviaList)) : this.EndOfDirectiveToken,
                this.IsActive);
        }
    }
}

namespace Microsoft.CodeAnalysis.CSharp.Syntax.InternalSyntax
{
    partial class SyntaxFactory
    {
        public static ShebangDirectiveTriviaSyntax ShebangDirectiveTrivia(SyntaxToken hashToken, SyntaxToken exclamationToken, SyntaxToken? content, SyntaxToken endOfDirectiveToken, bool isActive)
        {
#if DEBUG
            if (hashToken == null) throw new ArgumentNullException(nameof(hashToken));
            if (hashToken.Kind != SyntaxKind.HashToken) throw new ArgumentException(nameof(hashToken));
            if (exclamationToken == null) throw new ArgumentNullException(nameof(exclamationToken));
            if (exclamationToken.Kind != SyntaxKind.ExclamationToken) throw new ArgumentException(nameof(exclamationToken));
            if (content != null)
            {
                switch (content.Kind)
                {
                    case SyntaxKind.StringLiteralToken:
                    case SyntaxKind.None: break;
                    default: throw new ArgumentException(nameof(content));
                }
            }
            if (endOfDirectiveToken == null) throw new ArgumentNullException(nameof(endOfDirectiveToken));
            if (endOfDirectiveToken.Kind != SyntaxKind.EndOfDirectiveToken) throw new ArgumentException(nameof(endOfDirectiveToken));
#endif

            if (content is { Kind: SyntaxKind.StringLiteralToken })
            {
                var triviaBuilder = SyntaxTriviaListBuilder.Create();
                triviaBuilder.Add(new SyntaxTriviaList(default, content.LeadingTrivia.Node));
                triviaBuilder.Add(SyntaxFactory.PreprocessingMessage(content.ToString()));
                triviaBuilder.Add(new SyntaxTriviaList(default, content.TrailingTrivia.Node));
                triviaBuilder.Add(new SyntaxTriviaList(default, endOfDirectiveToken.LeadingTrivia.Node));
                endOfDirectiveToken = endOfDirectiveToken.TokenWithLeadingTrivia(triviaBuilder.ToList().Node);
            }

            return new ShebangDirectiveTriviaSyntax(SyntaxKind.ShebangDirectiveTrivia, hashToken, exclamationToken, endOfDirectiveToken, isActive);
        }
    }
}

namespace Microsoft.CodeAnalysis.CSharp
{
    using Syntax;

    partial class SyntaxFactory
    {
        public static ShebangDirectiveTriviaSyntax ShebangDirectiveTrivia(SyntaxToken hashToken, SyntaxToken exclamationToken, SyntaxToken content, SyntaxToken endOfDirectiveToken, bool isActive)
        {
            if (hashToken.Kind() != SyntaxKind.HashToken) throw new ArgumentException(nameof(hashToken));
            if (exclamationToken.Kind() != SyntaxKind.ExclamationToken) throw new ArgumentException(nameof(exclamationToken));
            switch (content.Kind())
            {
                case SyntaxKind.StringLiteralToken:
                case SyntaxKind.None: break;
                default: throw new ArgumentException(nameof(content));
            }
            if (endOfDirectiveToken.Kind() != SyntaxKind.EndOfDirectiveToken) throw new ArgumentException(nameof(endOfDirectiveToken));
            return (ShebangDirectiveTriviaSyntax)Syntax.InternalSyntax.SyntaxFactory.ShebangDirectiveTrivia((Syntax.InternalSyntax.SyntaxToken)hashToken.Node!, (Syntax.InternalSyntax.SyntaxToken)exclamationToken.Node!, (Syntax.InternalSyntax.SyntaxToken)content.Node!, (Syntax.InternalSyntax.SyntaxToken)endOfDirectiveToken.Node!, isActive).CreateRed();
        }
    }
}
