// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Remote;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.FindSymbols
{
    public static partial class SymbolFinder
    {
        internal sealed class FindLiteralsServerCallback : IRemoteSymbolFinderService.ICallback
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

            public ValueTask AddItemsAsync(int count)
                => _progress.ProgressTracker.AddItemsAsync(count);

            public ValueTask ItemCompletedAsync()
                => _progress.ProgressTracker.ItemCompletedAsync();

            public async ValueTask OnLiteralReferenceFoundAsync(DocumentId documentId, TextSpan span)
            {
                var document = _solution.GetDocument(documentId);
                await _progress.OnReferenceFoundAsync(document, span).ConfigureAwait(false);
            }

            public ValueTask OnCompletedAsync()
                => throw ExceptionUtilities.Unreachable;

            public ValueTask OnDefinitionFoundAsync(SerializableSymbolAndProjectId definition)
                => throw ExceptionUtilities.Unreachable;

            public ValueTask OnFindInDocumentCompletedAsync(DocumentId documentId)
                => throw ExceptionUtilities.Unreachable;

            public ValueTask OnFindInDocumentStartedAsync(DocumentId documentId)
                => throw ExceptionUtilities.Unreachable;

            public ValueTask OnReferenceFoundAsync(SerializableSymbolAndProjectId definition, SerializableReferenceLocation reference)
                => throw ExceptionUtilities.Unreachable;

            public ValueTask OnStartedAsync()
                => throw ExceptionUtilities.Unreachable;
        }
    }
}
