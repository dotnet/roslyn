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
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CodeRefactorings.ConvertConversionOperators;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Microsoft.CodeAnalysis.CSharp.CodeRefactorings.ConvertConversionOperators
{
    /// <inheritdoc/>
    [ExportCodeRefactoringProvider(LanguageNames.CSharp, Name = PredefinedCodeRefactoringProviderNames.ConvertConversionOperators), Shared]
    internal partial class CSharpConvertConversionOperatorsRefactoringProvider
        : AbstractConvertConversionOperatorsRefactoringProvider<CastExpressionSyntax, BinaryExpressionSyntax>
    {
        [ImportingConstructor]
        [SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification = "Used in test code: https://github.com/dotnet/roslyn/issues/42814")]
        public CSharpConvertConversionOperatorsRefactoringProvider()
        {
        }

        protected override Task<ImmutableArray<BinaryExpressionSyntax>> FilterAsExpressionCandidatesAsync(ImmutableArray<BinaryExpressionSyntax> asExpression, AsyncLazy<SemanticModel> semanticModelFactory, CancellationToken cancellationToken)
        {
            var result = asExpression.WhereAsArray(
                binaryExpression => binaryExpression is
                {
                    RawKind: (int)SyntaxKind.AsExpression,
                    Right: TypeSyntax { IsMissing: false },
                });

            return Task.FromResult(result);
        }

        protected override async Task<ImmutableArray<CastExpressionSyntax>> FilterCastExpressionCandidatesAsync(ImmutableArray<CastExpressionSyntax> castExpressions, AsyncLazy<SemanticModel> semanticModelFactory, CancellationToken cancellationToken)
        {
            if (castExpressions.IsEmpty)
            {
                return castExpressions;
            }

            var semanticModel = await semanticModelFactory.GetValueAsync(cancellationToken).ConfigureAwait(false);
            var candidates = from node in castExpressions
                             let type = semanticModel.GetTypeInfo(node, cancellationToken).Type
                             where type != null && !type.IsValueType
                             select node;

            return candidates.ToImmutableArray();
        }

        protected override async Task<Document> ConvertFromAsToCastAsync(Document document, BinaryExpressionSyntax asExpression, CancellationToken cancellationToken)
        {
            var expression = asExpression.Left;
            if (asExpression.Right is not TypeSyntax typeNode)
            {
                throw new InvalidOperationException("asExpression.Right must be a TypeSyntax. This check is done before the CodeAction registration.");
            }

            var castExpression = CastExpression(typeNode, expression.WithoutTrailingTrivia());

            return await document.ReplaceNodeAsync<SyntaxNode>(asExpression, castExpression, cancellationToken).ConfigureAwait(false);
        }

        protected override async Task<Document> ConvertFromCastToAsAsync(Document document, CastExpressionSyntax castExpression, CancellationToken cancellationToken)
        {
            var typeNode = castExpression.Type;
            var expression = castExpression.Expression;

            var asExpression = BinaryExpression(SyntaxKind.AsExpression, expression, typeNode);

            return await document.ReplaceNodeAsync<SyntaxNode>(castExpression, asExpression, cancellationToken).ConfigureAwait(false);
        }

        protected override string GetTitle()
            => "TODO";
    }
}
