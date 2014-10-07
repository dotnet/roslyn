// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServices;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    [ExportLanguageService(typeof(ISyntaxVersionLanguageService), LanguageNames.CSharp)]
    internal partial class CSharpSyntaxVersionService : ISyntaxVersionLanguageService
    {
        public int ComputePublicHash(SyntaxNode root, CancellationToken cancellationToken)
        {
            var computer = SharedPools.Default<PublicHashComputer>().Allocate();
            try
            {
                return computer.ComputeHash(root, cancellationToken);
            }
            finally
            {
                SharedPools.Default<PublicHashComputer>().Free(computer);
            }
        }

        private class PublicHashComputer : CSharpSyntaxWalker
        {
            private int hash;
            private CancellationToken cancellationToken;

            public PublicHashComputer()
                : base(SyntaxWalkerDepth.Token)
            {
            }

            public int ComputeHash(SyntaxNode root, CancellationToken cancellationToken)
            {
                this.hash = 0;
                this.cancellationToken = cancellationToken;
                this.Visit(root);
                return this.hash;
            }

            public override void VisitBlock(BlockSyntax node)
            {
                // interior contents of blocks are not considered (nor are the braces)
                return;
            }

            public override void Visit(SyntaxNode node)
            {
                cancellationToken.ThrowIfCancellationRequested();

                // non-const field initializers are not considered
                if (node.Parent != null
                    && node.Parent.CSharpKind() == SyntaxKind.EqualsValueClause
                    && node.Parent.Parent.CSharpKind() == SyntaxKind.VariableDeclarator
                    && node.Parent.Parent.Parent.CSharpKind() == SyntaxKind.VariableDeclaration
                    && node.Parent.Parent.Parent.Parent.CSharpKind() == SyntaxKind.FieldDeclaration)
                {
                    var fd = (FieldDeclarationSyntax)node.Parent.Parent.Parent.Parent;
                    if (!fd.Modifiers.Any(SyntaxKind.ConstKeyword))
                    {
                        return;
                    }
                }

                base.Visit(node);
            }

            public override void VisitToken(SyntaxToken token)
            {
                // trivia is not considered, only the raw form of the token

                this.hash = Hash.Combine((int)token.CSharpKind(), this.hash);

                switch (token.CSharpKind())
                {
                    case SyntaxKind.IdentifierToken:
                        this.hash = Hash.Combine(token.ValueText.GetHashCode(), this.hash);
                        break;

                    case SyntaxKind.NumericLiteralToken:
                    case SyntaxKind.CharacterLiteralToken:
                    case SyntaxKind.StringLiteralToken:
                        this.hash = Hash.Combine(token.ToString().GetHashCode(), this.hash);
                        break;
                }
            }
        }
    }
}