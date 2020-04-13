// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Composition;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.UseIsNullCheck;

namespace Microsoft.CodeAnalysis.CSharp.UseIsNullCheck
{
    using static SyntaxFactory;

    [ExportCodeFixProvider(LanguageNames.CSharp), Shared]
    internal class CSharpUseIsNullCheckForReferenceEqualsCodeFixProvider : AbstractUseIsNullCheckForReferenceEqualsCodeFixProvider
    {
        [ImportingConstructor]
        [SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification = "Used in test code: https://github.com/dotnet/roslyn/issues/42814")]
        public CSharpUseIsNullCheckForReferenceEqualsCodeFixProvider()
        {
        }

        protected override string GetIsNullTitle()
            => CSharpAnalyzersResources.Use_is_null_check;

        protected override string GetIsNotNullTitle()
            => GetIsNullTitle();

        private static SyntaxNode CreateEqualsNullCheck(SyntaxNode argument)
            => BinaryExpression(
                SyntaxKind.EqualsExpression,
                (ExpressionSyntax)argument,
                LiteralExpression(SyntaxKind.NullLiteralExpression)).Parenthesize();

        private static SyntaxNode CreateIsNullCheck(SyntaxNode argument)
            => IsPatternExpression(
                (ExpressionSyntax)argument,
                ConstantPattern(LiteralExpression(SyntaxKind.NullLiteralExpression))).Parenthesize();

        private static SyntaxNode CreateIsNotNullCheck(SyntaxNode argument)
        {
            return
                BinaryExpression(
                    SyntaxKind.IsExpression,
                    (ExpressionSyntax)argument,
                    PredefinedType(Token(SyntaxKind.ObjectKeyword)))
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
