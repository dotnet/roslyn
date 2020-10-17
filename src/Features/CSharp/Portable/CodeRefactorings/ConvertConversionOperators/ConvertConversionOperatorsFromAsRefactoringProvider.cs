// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Composition;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Shared.Extensions;
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
        : CodeRefactoringProvider
    {
        [ImportingConstructor]
        [SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification = "Used in test code: https://github.com/dotnet/roslyn/issues/42814")]
        public CSharpConvertConversionOperatorsFromAsRefactoringProvider()
        {
        }

        public sealed override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
        {
            var asExpressions = await context.GetRelevantNodesAsync<BinaryExpressionSyntax>().ConfigureAwait(false);
            if (asExpressions.IsEmpty)
            {
                return;
            }

            asExpressions = asExpressions.WhereAsArray(
                binaryExpression => binaryExpression is
                {
                    RawKind: (int)SyntaxKind.AsExpression,
                    Right: TypeSyntax { IsMissing: false },
                });

            foreach (var node in asExpressions.Distinct())
            {
                context.RegisterRefactoring(
                    new MyCodeAction(
                        GetTitle(),
                        c => ConvertAsync(context.Document, node, c)
                    ), node.Span);
            }

            var (document, cancellationToken) = (context.Document, context.CancellationToken);
        }

        protected static async Task<Document> ConvertAsync(Document document, BinaryExpressionSyntax asExpression, CancellationToken cancellationToken)
        {
            var expression = asExpression.Left;
            if (asExpression.Right is not TypeSyntax typeNode)
            {
                throw new InvalidOperationException("asExpression.Right must be a TypeSyntax. This check is done before the CodeAction registration.");
            }
            var openParen = Token(SyntaxKind.OpenParenToken).WithoutTrivia();
            var closeParen = Token(SyntaxKind.CloseParenToken).WithoutTrivia();
            var castExpression = CastExpression(openParen, typeNode, closeParen, expression.WithoutTrailingTrivia()).WithTriviaFrom(asExpression);

            return await document.ReplaceNodeAsync<SyntaxNode>(asExpression, castExpression, cancellationToken).ConfigureAwait(false);
        }

        private static string GetTitle()
            => "TODO";

        private class MyCodeAction : CodeAction.DocumentChangeAction
        {
            public MyCodeAction(string title, Func<CancellationToken, Task<Document>> createChangedDocument)
                : base(title, createChangedDocument)
            {
            }
        }
    }
}
