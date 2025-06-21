// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable warnings

using System;
using System.Composition;
using Analyzer.Utilities;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Roslyn.Diagnostics.Analyzers;

namespace Roslyn.Diagnostics.CSharp.Analyzers
{
    [ExportCodeRefactoringProvider(LanguageNames.CSharp, Name = nameof(CSharpExposeMemberForTesting))]
    [Shared]
    public sealed class CSharpExposeMemberForTesting : AbstractExposeMemberForTesting<TypeDeclarationSyntax>
    {
        [ImportingConstructor]
        [Obsolete("This exported object must be obtained through the MEF export provider.", error: true)]
        public CSharpExposeMemberForTesting()
        {
        }

        private protected override IRefactoringHelpers RefactoringHelpers => CSharpRefactoringHelpers.Instance;

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
