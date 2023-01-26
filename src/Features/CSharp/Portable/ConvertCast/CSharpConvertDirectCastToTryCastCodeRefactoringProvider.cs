// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Composition;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.ConvertCast;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Microsoft.CodeAnalysis.CSharp.ConvertCast
{
    /// <summary>
    /// Refactor:
    ///     var o = (object)1;
    ///
    /// Into:
    ///     var o = 1 as object;
    /// </summary>
    [ExportCodeRefactoringProvider(LanguageNames.CSharp, Name = PredefinedCodeRefactoringProviderNames.ConvertDirectCastToTryCast), Shared]
    internal partial class CSharpConvertDirectCastToTryCastCodeRefactoringProvider
        : AbstractConvertCastCodeRefactoringProvider<TypeSyntax, CastExpressionSyntax, BinaryExpressionSyntax>
    {
        [ImportingConstructor]
        [SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification = "Used in test code: https://github.com/dotnet/roslyn/issues/42814")]
        public CSharpConvertDirectCastToTryCastCodeRefactoringProvider()
        {
        }

        protected override string GetTitle()
            => CSharpFeaturesResources.Change_to_as_expression;

        protected override int FromKind => (int)SyntaxKind.CastExpression;

        protected override TypeSyntax GetTypeNode(CastExpressionSyntax from)
            => from.Type;

        protected override BinaryExpressionSyntax ConvertExpression(CastExpressionSyntax castExpression, NullableContext nullableContext, bool isReferenceType)
        {
            var typeNode = castExpression.Type;
            var expression = castExpression.Expression;

            // Cannot use nullable reference types in `as` expression
            // This check ensures we unwrap any nullables, e.g.
            // `(string?)null` -> `null as string`
            if (typeNode is NullableTypeSyntax nullableType && isReferenceType)
                typeNode = nullableType.ElementType;

            // Trivia handling
            // #0 ( #1 Type #2 ) #3 expr #4
            // #0 #3 expr as #1 Type #2 #4
            // If #1 is present a new line is added after "as" because of elastic trivia on "as"
            // #3 is kept with the expression and moves
            typeNode = typeNode.WithLeadingTrivia(castExpression.OpenParenToken.TrailingTrivia);
            var middleTrivia = castExpression.CloseParenToken.TrailingTrivia.SkipInitialWhitespace();
            var newLeadingTrivia = castExpression.GetLeadingTrivia().AddRange(middleTrivia);
            var newTrailingTrivia = typeNode.GetTrailingTrivia().WithoutLeadingBlankLines().AddRange(expression.GetTrailingTrivia().WithoutLeadingBlankLines());
            expression = expression.WithoutTrailingTrivia();
            typeNode = typeNode.WithoutTrailingTrivia();

            var asExpression = BinaryExpression(SyntaxKind.AsExpression, expression, typeNode)
                .WithLeadingTrivia(newLeadingTrivia)
                .WithTrailingTrivia(newTrailingTrivia);

            return asExpression;
        }
    }
}
