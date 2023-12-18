// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.Text;
using LSP = Roslyn.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler.Completion
{
    internal interface ILspCompletionResultCreationService : IWorkspaceService
    {
        Task<LSP.CompletionList> ConvertToLspCompletionListAsync(
            Document document,
            int position,
            CompletionCapabilityHelper capabilityHelper,
            CompletionList list, bool isIncomplete, long resultId,
            CancellationToken cancellationToken);

        Task<LSP.CompletionItem> ResolveAsync(
            LSP.CompletionItem lspItem,
            CompletionItem roslynItem,
            LSP.TextDocumentIdentifier textDocumentIdentifier,
            Document document,
            CompletionCapabilityHelper capabilityHelper,
            CompletionService completionService,
            CompletionOptions completionOptions,
            SymbolDescriptionOptions symbolDescriptionOptions,
            CancellationToken cancellationToken);
    }
}
