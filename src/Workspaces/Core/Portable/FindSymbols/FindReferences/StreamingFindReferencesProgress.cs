// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
            this.ProgressTracker = new StreamingProgressTracker((current, max) =>
            {
                _progress.ReportProgress(current, max);
                return Task.CompletedTask;
            });
        }

        public Task OnCompletedAsync()
        {
            _progress.OnCompleted();
            return Task.CompletedTask;
        }

        public Task OnDefinitionFoundAsync(ISymbol symbol)
        {
            _progress.OnDefinitionFound(symbol);
            return Task.CompletedTask;
        }

        public Task OnFindInDocumentCompletedAsync(Document document)
        {
            _progress.OnFindInDocumentCompleted(document);
            return Task.CompletedTask;
        }

        public Task OnFindInDocumentStartedAsync(Document document)
        {
            _progress.OnFindInDocumentStarted(document);
            return Task.CompletedTask;
        }

        public Task OnReferenceFoundAsync(ISymbol symbol, ReferenceLocation location)
        {
            _progress.OnReferenceFound(symbol, location);
            return Task.CompletedTask;
        }

        public Task OnStartedAsync()
        {
            _progress.OnStarted();
            return Task.CompletedTask;
        }
    }
}
