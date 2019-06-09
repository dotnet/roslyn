// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.FindSymbols
{
    public static partial class SymbolFinder
    {
        private class FindLiteralsServerCallback
        {
            private readonly Solution _solution;
            private readonly IStreamingFindLiteralReferencesProgress _progress;
            private readonly CancellationToken _cancellationToken;

            public FindLiteralsServerCallback(
                Solution solution,
                IStreamingFindLiteralReferencesProgress progress,
                CancellationToken cancellationToken)
            {
                _solution = solution;
                _progress = progress;
                _cancellationToken = cancellationToken;
            }

            public Task ReportProgressAsync(int current, int maximum)
                => _progress.ReportProgressAsync(current, maximum);

            public async Task OnReferenceFoundAsync(
                DocumentId documentId, TextSpan span)
            {
                var document = _solution.GetDocument(documentId);
                await _progress.OnReferenceFoundAsync(document, span).ConfigureAwait(false);
            }
        }
    }
}
