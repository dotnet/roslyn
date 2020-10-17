// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
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
    ///     var o = (object)1;
    ///
    /// Into:
    ///     var o = 1 as object;
    ///
    /// Or:
    ///     visa versa
    /// </summary>
    [ExportCodeRefactoringProvider(LanguageNames.CSharp, Name = PredefinedCodeRefactoringProviderNames.ConvertConversionOperators), Shared]
    internal partial class CSharpConvertConversionOperatorsFromCastRefactoringProvider
        : CodeRefactoringProvider
    {
        [ImportingConstructor]
        [SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification = "Used in test code: https://github.com/dotnet/roslyn/issues/42814")]
        public CSharpConvertConversionOperatorsFromCastRefactoringProvider()
        {
        }

        public sealed override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
        {
            var castExpressions = await context.GetRelevantNodesAsync<CastExpressionSyntax>().ConfigureAwait(false);

            if (castExpressions.IsEmpty)
            {
                return;
            }

            var (document, cancellationToken) = (context.Document, context.CancellationToken);
            var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);

            castExpressions = (from node in castExpressions
                               let type = semanticModel.GetTypeInfo(node, cancellationToken).Type
                               where type != null && !type.IsValueType
                               select node)
                               .Distinct()
                               .ToImmutableArray();

            if (castExpressions.IsEmpty)
            {
                return;
            }

            foreach (var node in castExpressions)
            {
                context.RegisterRefactoring(
                    new MyCodeAction(
                        GetTitle(),
                        c => ConvertAsync(document, node, c)
                    ), node.Span);
            }
        }

        private static async Task<Document> ConvertAsync(Document document, CastExpressionSyntax castExpression, CancellationToken cancellationToken)
        {
            var typeNode = castExpression.Type;
            var expression = castExpression.Expression;

            var asExpression = BinaryExpression(SyntaxKind.AsExpression, expression, typeNode);

            return await document.ReplaceNodeAsync<SyntaxNode>(castExpression, asExpression, cancellationToken).ConfigureAwait(false);
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
