// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Composition;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.UseIsNullCheck;

namespace Microsoft.CodeAnalysis.CSharp.UseIsNullCheck
{
    [ExportCodeFixProvider(LanguageNames.CSharp), Shared]
    internal class CSharpUseIsNullCheckForReferenceEqualsCodeFixProvider : AbstractUseIsNullCheckForReferenceEqualsCodeFixProvider
    {
        [ImportingConstructor]
        public CSharpUseIsNullCheckForReferenceEqualsCodeFixProvider()
        {
        }

        protected override string GetIsNullTitle()
            => CSharpFeaturesResources.Use_is_null_check;

        protected override string GetIsNotNullTitle()
            => GetIsNullTitle();

        private static SyntaxNode CreateEqualsNullCheck(SyntaxNode argument)
            => SyntaxFactory.BinaryExpression(
                SyntaxKind.EqualsExpression,
                (ExpressionSyntax)argument,
                SyntaxFactory.LiteralExpression(SyntaxKind.NullLiteralExpression)).Parenthesize();

        private static SyntaxNode CreateIsNullCheck(SyntaxNode argument)
            => SyntaxFactory.IsPatternExpression(
                (ExpressionSyntax)argument,
                SyntaxFactory.ConstantPattern(SyntaxFactory.LiteralExpression(SyntaxKind.NullLiteralExpression))).Parenthesize();

        private static SyntaxNode CreateIsNotNullCheck(SyntaxNode argument)
        {
            return SyntaxFactory
                .BinaryExpression(
                    SyntaxKind.IsExpression,
                    (ExpressionSyntax)argument,
                    SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.ObjectKeyword)))
                .Parenthesize();
        }

        protected override SyntaxNode CreateNullCheck(SyntaxNode argument, bool isUnconstrainedGeneric)
            => isUnconstrainedGeneric
                ? CreateEqualsNullCheck(argument)
                : CreateIsNullCheck(argument);

        protected override SyntaxNode CreateNotNullCheck(SyntaxNode argument)
            => CreateIsNotNullCheck(argument);
    }
}
