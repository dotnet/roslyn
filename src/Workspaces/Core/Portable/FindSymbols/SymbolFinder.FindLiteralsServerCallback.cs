// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.FindSymbols;

public static partial class SymbolFinder
{
    internal sealed class FindLiteralsServerCallback(
        Solution solution,
        IStreamingFindLiteralReferencesProgress progress)
    {
        public ValueTask AddItemsAsync(int count, CancellationToken cancellationToken)
            => progress.ProgressTracker.AddItemsAsync(count, cancellationToken);

        public ValueTask ItemsCompletedAsync(int count, CancellationToken cancellationToken)
            => progress.ProgressTracker.ItemsCompletedAsync(count, cancellationToken);

        public async ValueTask OnLiteralReferenceFoundAsync(DocumentId documentId, TextSpan span, CancellationToken cancellationToken)
        {
            var document = solution.GetRequiredDocument(documentId);
            await progress.OnReferenceFoundAsync(document, span, cancellationToken).ConfigureAwait(false);
        }
    }
}
