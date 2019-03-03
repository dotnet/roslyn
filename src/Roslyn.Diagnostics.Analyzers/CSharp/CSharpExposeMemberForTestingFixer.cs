// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Roslyn.Diagnostics.Analyzers;

namespace Roslyn.Diagnostics.CSharp.Analyzers
{
    [ExportCodeFixProvider(LanguageNames.CSharp), Shared]
    public sealed class CSharpExposeMemberForTestingFixer : ExposeMemberForTestingFixer
    {
        protected override bool HasRefReturns => true;

        protected override SyntaxNode GetTypeDeclarationForNode(SyntaxNode reportedNode)
        {
            return reportedNode.FirstAncestorOrSelf<TypeDeclarationSyntax>();
        }

        protected override SyntaxNode GetByRefType(SyntaxNode type, RefKind refKind)
        {
            var refKeyword = SyntaxFactory.Token(SyntaxKind.RefKeyword);
            var readOnlyKeyword = refKind switch
            {
                RefKind.Ref => default,
                RefKind.RefReadOnly => SyntaxFactory.Token(SyntaxKind.ReadOnlyKeyword),
                _ => throw new ArgumentOutOfRangeException(nameof(refKind)),
            };

            return SyntaxFactory.RefType(refKeyword, readOnlyKeyword, (TypeSyntax)type);
        }

        protected override SyntaxNode GetByRefExpression(SyntaxNode expression)
        {
            return SyntaxFactory.RefExpression((ExpressionSyntax)expression);
        }
    }
}
