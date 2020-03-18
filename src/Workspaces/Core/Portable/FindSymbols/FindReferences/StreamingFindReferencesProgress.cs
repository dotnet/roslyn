// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.FindSymbols
{
    /// <summary>
    /// A class that reports the current progress made when finding references to symbols.  
    /// </summary>
    internal class StreamingFindReferencesProgress : IStreamingFindReferencesProgress
    {
        public static readonly IStreamingFindReferencesProgress Instance =
            new StreamingFindReferencesProgress();

        private StreamingFindReferencesProgress()
        {
        }

        public Task ReportProgressAsync(int current, int maximum) => Task.CompletedTask;

        public Task OnCompletedAsync() => Task.CompletedTask;
        public Task OnStartedAsync() => Task.CompletedTask;
        public Task OnDefinitionFoundAsync(SymbolAndProjectId symbol) => Task.CompletedTask;
        public Task OnReferenceFoundAsync(SymbolAndProjectId symbol, ReferenceLocation location) => Task.CompletedTask;
        public Task OnFindInDocumentStartedAsync(Document document) => Task.CompletedTask;
        public Task OnFindInDocumentCompletedAsync(Document document) => Task.CompletedTask;
    }

    /// <summary>
    /// Wraps an <see cref="IFindReferencesProgress"/> into an <see cref="IStreamingFindReferencesProgress"/>
    /// so it can be used from the new streaming find references APIs.
    /// </summary>
    internal class StreamingFindReferencesProgressAdapter : IStreamingFindReferencesProgress
    {
        private readonly IFindReferencesProgress _progress;

        public StreamingFindReferencesProgressAdapter(IFindReferencesProgress progress)
        {
            _progress = progress;
        }

        public Task OnCompletedAsync()
        {
            _progress.OnCompleted();
            return Task.CompletedTask;
        }

        public Task OnDefinitionFoundAsync(SymbolAndProjectId symbolAndProjectId)
        {
            _progress.OnDefinitionFound(symbolAndProjectId.Symbol);
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

        public Task OnReferenceFoundAsync(SymbolAndProjectId symbolAndProjectId, ReferenceLocation location)
        {
            _progress.OnReferenceFound(symbolAndProjectId.Symbol, location);
            return Task.CompletedTask;
        }

        public Task OnStartedAsync()
        {
            _progress.OnStarted();
            return Task.CompletedTask;
        }

        public Task ReportProgressAsync(int current, int maximum)
        {
            _progress.ReportProgress(current, maximum);
            return Task.CompletedTask;
        }
    }
}
