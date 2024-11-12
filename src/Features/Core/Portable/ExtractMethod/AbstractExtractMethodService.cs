// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.ExtractMethod;

internal abstract class AbstractExtractMethodService<
    TValidator,
    TExtractor,
    TSelectionResult,
    TStatementSyntax,
    TExpressionSyntax> : IExtractMethodService
    where TValidator : SelectionValidator<TSelectionResult, TStatementSyntax>
    where TExtractor : MethodExtractor<TSelectionResult, TStatementSyntax, TExpressionSyntax>
    where TSelectionResult : SelectionResult<TStatementSyntax>
    where TStatementSyntax : SyntaxNode
    where TExpressionSyntax : SyntaxNode
{
    protected abstract TValidator CreateSelectionValidator(SemanticDocument document, TextSpan textSpan, bool localFunction);
    protected abstract TExtractor CreateMethodExtractor(TSelectionResult selectionResult, ExtractMethodGenerationOptions options, bool localFunction);

    public async Task<ExtractMethodResult> ExtractMethodAsync(
        Document document,
        TextSpan textSpan,
        bool localFunction,
        ExtractMethodGenerationOptions options,
        CancellationToken cancellationToken)
    {
        var semanticDocument = await SemanticDocument.CreateAsync(document, cancellationToken).ConfigureAwait(false);

        var validator = CreateSelectionValidator(semanticDocument, textSpan, localFunction);

        var (selectionResult, status) = await validator.GetValidSelectionAsync(cancellationToken).ConfigureAwait(false);
        if (selectionResult is null)
            return ExtractMethodResult.Fail(status);

        cancellationToken.ThrowIfCancellationRequested();

        // extract method
        var extractor = CreateMethodExtractor(selectionResult, options, localFunction);
        return extractor.ExtractMethod(status, cancellationToken);
    }
}
