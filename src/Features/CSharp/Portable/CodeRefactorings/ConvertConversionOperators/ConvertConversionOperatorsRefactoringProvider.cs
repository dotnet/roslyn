// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CodeRefactorings.ConvertConversionOperators;
using Microsoft.CodeAnalysis.CSharp.CodeGeneration;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Shared.Extensions;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Microsoft.CodeAnalysis.CSharp.CodeRefactorings.ConvertConversionOperators
{
    /// <inheritdoc/>
    [ExportCodeRefactoringProvider(LanguageNames.CSharp, Name = PredefinedCodeRefactoringProviderNames.ConvertConversionOperators), Shared]
    internal partial class CSharpConvertConversionOperatorsRefactoringProvider : AbstractConvertConversionOperatorsRefactoringProvider<CastExpressionSyntax, BinaryExpressionSyntax>
    {
        [ImportingConstructor]
        [SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification = "Used in test code: https://github.com/dotnet/roslyn/issues/42814")]
        public CSharpConvertConversionOperatorsRefactoringProvider()
        {
        }

        protected override ImmutableArray<BinaryExpressionSyntax> FilterAsExpressionCandidates(ImmutableArray<BinaryExpressionSyntax> asExpression)
        {
            return asExpression.WhereAsArray(binaryExpression => binaryExpression.IsKind(SyntaxKind.AsExpression));
        }

        protected override async Task<Document> ConvertFromAsToCastAsync(Document document, BinaryExpressionSyntax asExpression, CancellationToken cancellationToken)
        {
            var generator = new CSharpSyntaxGenerator();
            var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var expression = asExpression.Left;
            if (asExpression.Right is not TypeSyntax typeNode)
            {
                return document;
            }

            var castExpression = CastExpression(typeNode, expression.WithoutTrailingTrivia());
            var newRoot = generator.ReplaceNode(root, asExpression, castExpression);
            return document.WithSyntaxRoot(newRoot);
        }

        protected override async Task<Document> ConvertFromCastToAsAsync(Document document, CastExpressionSyntax castExpression, CancellationToken cancellationToken)
        {
            var generator = new CSharpSyntaxGenerator();
            var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var typeNode = castExpression.Type;
            var expression = castExpression.Expression;

            var asExpression = BinaryExpression(SyntaxKind.AsExpression, expression, typeNode);
            var newRoot = generator.ReplaceNode(root, castExpression, asExpression);
            return document.WithSyntaxRoot(newRoot);
        }

        protected override string GetTitle()
            => "TODO";
    }
}
