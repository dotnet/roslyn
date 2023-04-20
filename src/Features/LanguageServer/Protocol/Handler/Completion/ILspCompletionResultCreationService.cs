﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.Text;
using LSP = Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler.Completion
{
    internal interface ILspCompletionResultCreationService : IWorkspaceService
    {
        Task<LSP.CompletionItem> CreateAsync(
            Document document,
            SourceText documentText,
            bool snippetsSupported,
            bool itemDefaultsSupported,
            TextSpan defaultSpan,
            CompletionItem item,
            CompletionService completionService,
            CancellationToken cancellationToken);

        Task<LSP.CompletionItem> ResolveAsync(
            LSP.CompletionItem lspItem,
            CompletionItem roslynItem,
            LSP.TextDocumentIdentifier textDocumentIdentifier,
            Document document,
            LSP.ClientCapabilities clientCapabilities,
            CompletionService completionService,
            CompletionOptions completionOptions,
            SymbolDescriptionOptions symbolDescriptionOptions,
            CancellationToken cancellationToken);
    }
}
