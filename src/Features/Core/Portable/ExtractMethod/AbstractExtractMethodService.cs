// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
        protected abstract TExtractor CreateMethodExtractor(TResult selectionResult, bool localFunction);

        public async Task<ExtractMethodResult> ExtractMethodAsync(
            Document document,
            TextSpan textSpan,
            bool localFunction,
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
            var extractor = CreateMethodExtractor((TResult)selectionResult, localFunction);

            return await extractor.ExtractMethodAsync(cancellationToken).ConfigureAwait(false);
        }
    }
}
