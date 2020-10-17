// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CodeRefactorings.ConvertConversionOperators;
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
    ///
    /// Or:
    ///     visa versa
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
        protected override Task<ImmutableArray<BinaryExpressionSyntax>> FilterFromExpressionCandidatesAsync(ImmutableArray<BinaryExpressionSyntax> asExpressions, Document document, CancellationToken cancellationToken)
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
            if (asExpression.Right is not TypeSyntax typeNode)
            {
                throw new InvalidOperationException("asExpression.Right must be a TypeSyntax. This check is done before the CodeAction registration.");
            }
            var openParen = Token(SyntaxKind.OpenParenToken).WithoutTrivia();
            var closeParen = Token(SyntaxKind.CloseParenToken).WithoutTrivia();
            var castExpression = CastExpression(openParen, typeNode, closeParen, expression.WithoutTrailingTrivia()).WithTriviaFrom(asExpression);

            return castExpression;
        }

        protected override string GetTitle()
            => "TODO";
    }
}
