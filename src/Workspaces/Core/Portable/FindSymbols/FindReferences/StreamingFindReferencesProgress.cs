// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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

        public Task ReportProgressAsync(int current, int maximum) => SpecializedTasks.EmptyTask;

        public Task OnCompletedAsync() => SpecializedTasks.EmptyTask;
        public Task OnStartedAsync() => SpecializedTasks.EmptyTask;
        public Task OnDefinitionFoundAsync(SymbolAndProjectId symbol) => SpecializedTasks.EmptyTask;
        public Task OnReferenceFoundAsync(SymbolAndProjectId symbol, ReferenceLocation location) => SpecializedTasks.EmptyTask;
        public Task OnFindInDocumentStartedAsync(Document document) => SpecializedTasks.EmptyTask;
        public Task OnFindInDocumentCompletedAsync(Document document) => SpecializedTasks.EmptyTask;
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
            return SpecializedTasks.EmptyTask;
        }

        public Task OnDefinitionFoundAsync(SymbolAndProjectId symbolAndProjectId)
        {
            _progress.OnDefinitionFound(symbolAndProjectId.Symbol);
            return SpecializedTasks.EmptyTask;
        }

        public Task OnFindInDocumentCompletedAsync(Document document)
        {
            _progress.OnFindInDocumentCompleted(document);
            return SpecializedTasks.EmptyTask;
        }

        public Task OnFindInDocumentStartedAsync(Document document)
        {
            _progress.OnFindInDocumentStarted(document);
            return SpecializedTasks.EmptyTask;
        }

        public Task OnReferenceFoundAsync(SymbolAndProjectId symbolAndProjectId, ReferenceLocation location)
        {
            _progress.OnReferenceFound(symbolAndProjectId.Symbol, location);
            return SpecializedTasks.EmptyTask;
        }

        public Task OnStartedAsync()
        {
            _progress.OnStarted();
            return SpecializedTasks.EmptyTask;
        }

        public Task ReportProgressAsync(int current, int maximum)
        {
            _progress.ReportProgress(current, maximum);
            return SpecializedTasks.EmptyTask;
        }
    }
}