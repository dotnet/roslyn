// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading;
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

        public Task ReportProgressAsync(int current, int maximum, CancellationToken cancellationToken) => SpecializedTasks.EmptyTask;

        public Task OnCompletedAsync(CancellationToken cancellationToken) => SpecializedTasks.EmptyTask;
        public Task OnStartedAsync(CancellationToken cancellationToken) => SpecializedTasks.EmptyTask;
        public Task OnDefinitionFoundAsync(SymbolAndProjectId symbol, CancellationToken cancellationToken) => SpecializedTasks.EmptyTask;
        public Task OnReferenceFoundAsync(SymbolAndProjectId symbol, ReferenceLocation location, CancellationToken cancellationToken) => SpecializedTasks.EmptyTask;
        public Task OnFindInDocumentStartedAsync(Document document, CancellationToken cancellationToken) => SpecializedTasks.EmptyTask;
        public Task OnFindInDocumentCompletedAsync(Document document, CancellationToken cancellationToken) => SpecializedTasks.EmptyTask;
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

        public Task OnCompletedAsync(CancellationToken cancellationToken)
        {
            _progress.OnCompleted();
            return SpecializedTasks.EmptyTask;
        }

        public Task OnDefinitionFoundAsync(SymbolAndProjectId symbolAndProjectId, CancellationToken cancellationToken)
        {
            _progress.OnDefinitionFound(symbolAndProjectId.Symbol);
            return SpecializedTasks.EmptyTask;
        }

        public Task OnFindInDocumentCompletedAsync(Document document, CancellationToken cancellationToken)
        {
            _progress.OnFindInDocumentCompleted(document);
            return SpecializedTasks.EmptyTask;
        }

        public Task OnFindInDocumentStartedAsync(Document document, CancellationToken cancellationToken)
        {
            _progress.OnFindInDocumentStarted(document);
            return SpecializedTasks.EmptyTask;
        }

        public Task OnReferenceFoundAsync(SymbolAndProjectId symbolAndProjectId, ReferenceLocation location, CancellationToken cancellationToken)
        {
            _progress.OnReferenceFound(symbolAndProjectId.Symbol, location);
            return SpecializedTasks.EmptyTask;
        }

        public Task OnStartedAsync(CancellationToken cancellationToken)
        {
            _progress.OnStarted();
            return SpecializedTasks.EmptyTask;
        }

        public Task ReportProgressAsync(int current, int maximum, CancellationToken cancellationToken)
        {
            _progress.ReportProgress(current, maximum);
            return SpecializedTasks.EmptyTask;
        }
    }
}