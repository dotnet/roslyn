// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Shared.Utilities;

namespace Microsoft.CodeAnalysis.FindSymbols
{
    /// <summary>
    /// Wraps an <see cref="IFindReferencesProgress"/> into an <see cref="IStreamingFindReferencesProgress"/>
    /// so it can be used from the new streaming find references APIs.
    /// </summary>
    internal class StreamingFindReferencesProgressAdapter : IStreamingFindReferencesProgress
    {
        private readonly IFindReferencesProgress _progress;

        public IStreamingProgressTracker ProgressTracker { get; }

        public StreamingFindReferencesProgressAdapter(IFindReferencesProgress progress)
        {
            _progress = progress;
            ProgressTracker = new StreamingProgressTracker((current, max) =>
            {
                _progress.ReportProgress(current, max);
                return default;
            });
        }

        public ValueTask OnCompletedAsync()
        {
            _progress.OnCompleted();
            return default;
        }

        public ValueTask OnDefinitionFoundAsync(ISymbol symbol)
        {
            _progress.OnDefinitionFound(symbol);
            return default;
        }

        public ValueTask OnFindInDocumentCompletedAsync(Document document)
        {
            _progress.OnFindInDocumentCompleted(document);
            return default;
        }

        public ValueTask OnFindInDocumentStartedAsync(Document document)
        {
            _progress.OnFindInDocumentStarted(document);
            return default;
        }

        public ValueTask OnReferenceFoundAsync(ISymbol symbol, ReferenceLocation location)
        {
            _progress.OnReferenceFound(symbol, location);
            return default;
        }

        public ValueTask OnStartedAsync()
        {
            _progress.OnStarted();
            return default;
        }
    }
}
