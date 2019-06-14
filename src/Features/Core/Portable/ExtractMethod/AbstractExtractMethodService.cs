// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.ExtractMethod
{
    internal abstract class AbstractExtractMethodService<TValidator, TExtractor, TResult> : IExtractMethodService
        where TValidator : SelectionValidator
        where TExtractor : MethodExtractor
        where TResult : SelectionResult
    {
        protected abstract TValidator CreateSelectionValidator(SemanticDocument document, TextSpan textSpan, OptionSet options);
        protected abstract TExtractor CreateMethodExtractor(TResult selectionResult);

        public async Task<ExtractMethodResult> ExtractMethodAsync(
            Document document,
            TextSpan textSpan,
            OptionSet options,
            CancellationToken cancellationToken)
        {
            options ??= await document.GetOptionsAsync(cancellationToken).ConfigureAwait(false);

            var semanticDocument = await SemanticDocument.CreateAsync(document, cancellationToken).ConfigureAwait(false);

            var validator = CreateSelectionValidator(semanticDocument, textSpan, options);

            var selectionResult = await validator.GetValidSelectionAsync(cancellationToken).ConfigureAwait(false);
            if (!selectionResult.ContainsValidContext)
            {
                return new FailedExtractMethodResult(selectionResult.Status);
            }

            cancellationToken.ThrowIfCancellationRequested();

            // extract method
            var extractor = CreateMethodExtractor((TResult)selectionResult);

            return await extractor.ExtractMethodAsync(cancellationToken).ConfigureAwait(false);
        }
    }
}
