// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis;
using System.Collections.Immutable;
using System.Threading;
using Microsoft.VisualStudio.LanguageServices.Remote;
using Microsoft.VisualStudio.LanguageServices.Implementation.Extensions;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.FindSymbols
{
    [ExportWorkspaceService(typeof(ISymbolFinderEngineService), ServiceLayer.Host), Shared]
    internal class VisualStudioSymbolFinderEngineService : ISymbolFinderEngineService
    {
        public Task<IEnumerable<ReferencedSymbol>> FindReferencesAsync(
            SymbolAndProjectId symbolAndProjectId, Solution solution, 
            IStreamingFindReferencesProgress progress,
            IImmutableSet<Document> documents, CancellationToken cancellationToken)
        {
            return DefaultSymbolFinderEngineService.FindReferencesInCurrentProcessAsync(
                symbolAndProjectId, solution, progress, documents, cancellationToken);
#if false
            var symbol = symbolAndProjectId.Symbol;
            var symbolKeyData = symbol.GetSymbolKey().ToString();
            var progressCallback = new ProgressCallback(progress);

            var clientService = solution.Workspace.Services.GetService<IRemoteHostClientService>();
            var client = await clientService.GetRemoteHostClientAsync(cancellationToken).ConfigureAwait(false);

            using (var session = await client.CreateCodeAnalysisServiceSessionAsync(solution, progressCallback, cancellationToken).ConfigureAwait(false))
            {
                // await session.InvokeAsync()
            }

            return null;
#endif
        }

#if false
        private class ProgressCallback
        {
            private readonly Solution _solution;
            private readonly IStreamingFindReferencesProgress _progress;

            public ProgressCallback(
                Solution solution, 
                IStreamingFindReferencesProgress progress)
            {
                _solution = solution;
                _progress = progress;
            }

            public Task OnStarted() => _progress.OnStartedAsync();
            public Task OnCompletedAsync() => _progress.OnCompletedAsync();
            public Task ReportProgressAsync(int current, int maximum) => _progress.ReportProgressAsync(current, maximum);

            public void OnFindInDocumentStarted(
                Guid projectGuid, string projectDebugName,
                Guid documentGuid, string documentDebugName)
            {
                var document = GetDocument(projectGuid, projectDebugName, documentGuid, documentDebugName);
                _progress.OnFindInDocumentStarted(document);
            }

            public void OnFindInDocumentCompleted(
                Guid projectGuid, string projectDebugName,
                Guid documentGuid, string documentDebugName)
            {
                var document = GetDocument(projectGuid, projectDebugName, documentGuid, documentDebugName);
                _progress.OnFindInDocumentCompleted(document);
            }

            private Document GetDocument(Guid projectGuid, string projectDebugName, Guid documentGuid, string documentDebugName)
            {
                var projectId = ProjectId.CreateFromSerialized(projectGuid, projectDebugName);
                var documentId = DocumentId.CreateFromSerialized(projectId, documentGuid, documentDebugName);

                var document = _solution.GetDocument(documentId);
                return document;
            }

            public void OnDefinitionFound(string symbolKeyData)
            {
                var symbolKey = SymbolKey.Resolve(symbolKey, )
            }
            public void OnReferenceFound(ISymbol symbol, ReferenceLocation location);
        }
#endif
    }
}