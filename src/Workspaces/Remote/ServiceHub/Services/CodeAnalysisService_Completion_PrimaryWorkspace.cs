// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Completion;

namespace Microsoft.CodeAnalysis.Remote
{
    internal partial class CodeAnalysisService
    {
        public Task<CompletionChange> CompletionGetChangeAsync(DocumentId documentId, CompletionItem item, DocumentId triggerDocumentId, char commitCharacter, CancellationToken cancellationToken)
        {
            return RunServiceAsync(async token =>
            {
                using (UserOperationBooster.Boost())
                {
                    // entry point for diagnostic service
                    var solution = await GetSolutionAsync(token).ConfigureAwait(false);
                    var document = solution.GetDocument(documentId);
                    item.Document = solution.GetDocument(triggerDocumentId);

                    var service = CompletionService.GetService(document);
                    return await service.GetChangeAsync(document, item, (commitCharacter == default) ? (char?)null : commitCharacter, token).ConfigureAwait(false);
                }
            }, cancellationToken);
        }

        public Task<CompletionDescription> CompletionGetDescriptionAsync(DocumentId documentId, CompletionItem item, DocumentId triggerDocumentId, CancellationToken cancellationToken)
        {
            return RunServiceAsync(async token =>
            {
                using (UserOperationBooster.Boost())
                {
                    // entry point for diagnostic service
                    var solution = await GetSolutionAsync(token).ConfigureAwait(false);
                    var document = solution.GetDocument(documentId);
                    item.Document = solution.GetDocument(triggerDocumentId);

                    var service = CompletionService.GetService(document);
                    return await service.GetDescriptionAsync(document, item, token).ConfigureAwait(false);
                }
            }, cancellationToken);
        }

        public Task<CompletionListResult> CompletionGetCompletionsAsync(DocumentId documentId, int caretPosition, CompletionTrigger trigger, ISet<string> roles, CancellationToken cancellationToken)
        {
            return RunServiceAsync(async token =>
            {
                using (UserOperationBooster.Boost())
                {
                    // entry point for diagnostic service
                    var solution = await GetSolutionAsync(token).ConfigureAwait(false);
                    var document = solution.GetDocument(documentId);

                    var service = CompletionService.GetService(document);
                    var result = await service.GetCompletionsAsync(document, caretPosition, trigger, roles?.ToImmutableHashSet(), solution.Options).ConfigureAwait(false);
                    if (result == null)
                    {
                        return new CompletionListResult
                        {
                            TriggerDocumentId = ImmutableArray<DocumentId>.Empty,
                            SuggestionModeItemTriggerDocumentId = null,
                            CompletionList = result
                        };
                    }

                    return new CompletionListResult
                    {
                        TriggerDocumentId = result.Items.SelectAsArray(i => i.Document?.Id),
                        SuggestionModeItemTriggerDocumentId = result.SuggestionModeItem?.Document?.Id,
                        CompletionList = result
                    };
                }
            }, cancellationToken);
        }
    }
}
