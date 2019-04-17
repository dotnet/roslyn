// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.LiveShare.LanguageServices;
using Microsoft.VisualStudio.LiveShare.LanguageServices.Protocol;
using LSP = Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.VisualStudio.LanguageServices.LiveShare
{
    /// <summary>
    /// Handle the initialize request and report the capabilities of the server.
    /// Uses liveshare custom server capabilities.
    /// </summary>
    [ExportLspRequestHandler(LiveShareConstants.RoslynContractName, LSP.Methods.InitializeName)]
    internal class InitializeHandler : ILspRequestHandler<LSP.InitializeParams, LSP.InitializeResult, Solution>
    {
        public Task<LSP.InitializeResult> HandleAsync(LSP.InitializeParams request, RequestContext<Solution> requestContext, CancellationToken cancellationToken)
        {
            var result = new LSP.InitializeResult
            {
                Capabilities = new ServerCapabilities_v40
                {
                    DefinitionProvider = true,
                    ReferencesProvider = true,
                    ImplementationProvider = true,
                    CompletionProvider = new LSP.CompletionOptions { ResolveProvider = true, TriggerCharacters = new[] { "." } },
                    HoverProvider = false,
                    SignatureHelpProvider = new LSP.SignatureHelpOptions { TriggerCharacters = new[] { "(", "," } },
                    CodeActionProvider = true,
                    DocumentSymbolProvider = true,
                    WorkspaceSymbolProvider = true,
                    DocumentFormattingProvider = true,
                    DocumentRangeFormattingProvider = true,
                    DocumentOnTypeFormattingProvider = new LSP.DocumentOnTypeFormattingOptions { FirstTriggerCharacter = "}", MoreTriggerCharacter = new[] { ";", "\n" } },
                    DocumentHighlightProvider = true,
                    RenameProvider = true,
                    ExecuteCommandProvider = new LSP.ExecuteCommandOptions()
                }
            };

            return Task.FromResult(result);
        }
    }
}
