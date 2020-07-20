// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
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

            public Task AddItemsAsync(int count)
                => _progress.ProgressTracker.AddItemsAsync(count);

            public Task ItemCompletedAsync()
                => _progress.ProgressTracker.ItemCompletedAsync();

            public async Task OnReferenceFoundAsync(
                DocumentId documentId, TextSpan span)
            {
                var document = _solution.GetDocument(documentId);
                await _progress.OnReferenceFoundAsync(document, span).ConfigureAwait(false);
            }
        }
    }
}
