// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CodeRefactorings.ConvertConversionOperators;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Microsoft.CodeAnalysis.CSharp.CodeRefactorings.ConvertConversionOperators
{
    /// <summary>
    /// Refactor:
    ///     var o = 1 as object;
    ///
    /// Into:
    ///     var o = (object)1;
    /// </summary>
    [ExportCodeRefactoringProvider(LanguageNames.CSharp, Name = PredefinedCodeRefactoringProviderNames.ConvertConversionOperators), Shared]
    internal partial class CSharpConvertConversionOperatorsFromAsRefactoringProvider
        : AbstractConvertConversionOperatorsRefactoringProvider<BinaryExpressionSyntax>
    {
        [ImportingConstructor]
        [SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification = "Used in test code: https://github.com/dotnet/roslyn/issues/42814")]
        public CSharpConvertConversionOperatorsFromAsRefactoringProvider()
        {
        }

        protected override Task<ImmutableArray<BinaryExpressionSyntax>> FilterFromExpressionCandidatesAsync(
            ImmutableArray<BinaryExpressionSyntax> asExpressions,
            Document document,
            CancellationToken cancellationToken)
        {
            asExpressions = asExpressions.WhereAsArray(
                binaryExpression => binaryExpression is
                {
                    RawKind: (int)SyntaxKind.AsExpression,
                    Right: TypeSyntax { IsMissing: false },
                });

            return Task.FromResult(asExpressions);
        }

        protected override SyntaxNode ConvertExpression(BinaryExpressionSyntax asExpression)
        {
            var expression = asExpression.Left;
            var typeNode = (TypeSyntax)asExpression.Right;

            // Trivia handling:
            // #0 exp #1 as #2 Type #3
            // #0 #2 (Type)exp #1 #3
            // Some trivia in the middle (#1 and #2) is moved to the front or behind  the expression
            // #1 and #2 change their position in the expression (#2 goes in front to stay near the type and #1 to the end to stay near the expression)
            // Some whitespace around the as operator is removed to follow the formatting rules of (Type)expr 
            var openParen = Token(SyntaxTriviaList.Empty, SyntaxKind.OpenParenToken, SyntaxTriviaList.Empty);
            var closeParen = Token(SyntaxTriviaList.Empty, SyntaxKind.CloseParenToken, SyntaxTriviaList.Empty);
            var newTrailingTrivia = asExpression.Left.GetTrailingTrivia().SkipInitialWhitespace().ToSyntaxTriviaList().AddRange(asExpression.GetTrailingTrivia());
            var newLeadingTrivia = asExpression.GetLeadingTrivia().AddRange(asExpression.OperatorToken.TrailingTrivia.SkipInitialWhitespace());
            typeNode = typeNode.WithoutTrailingTrivia();

            var castExpression = CastExpression(openParen, typeNode, closeParen, expression.WithoutTrailingTrivia())
                .WithLeadingTrivia(newLeadingTrivia)
                .WithTrailingTrivia(newTrailingTrivia);

            return castExpression;
        }

        protected override string GetTitle()
            => "TODO";
    }
}
