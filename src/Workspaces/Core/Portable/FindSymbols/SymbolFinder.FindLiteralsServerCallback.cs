// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.FindSymbols
{
    public static partial class SymbolFinder
    {
        internal sealed class FindLiteralsServerCallback
        {
            private readonly Solution _solution;
            private readonly IStreamingFindLiteralReferencesProgress _progress;

            public FindLiteralsServerCallback(
                Solution solution,
                IStreamingFindLiteralReferencesProgress progress)
            {
                _solution = solution;
                _progress = progress;
            }

            public ValueTask AddItemsAsync(int count, CancellationToken cancellationToken)
                => _progress.ProgressTracker.AddItemsAsync(count, cancellationToken);

            public ValueTask ItemCompletedAsync(CancellationToken cancellationToken)
                => _progress.ProgressTracker.ItemCompletedAsync(cancellationToken);

            public async ValueTask OnLiteralReferenceFoundAsync(DocumentId documentId, TextSpan span, CancellationToken cancellationToken)
            {
                var document = _solution.GetRequiredDocument(documentId);
                await _progress.OnReferenceFoundAsync(document, span, cancellationToken).ConfigureAwait(false);
            }
        }
    }
}
