// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.Xaml;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServer.Handler;
using Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.VisualStudio.LanguageServices.Xaml.LanguageServer.Handler
{
    [Shared]
    [ExportLspMethod(Methods.InitializeName, StringConstants.XamlLanguageName)]
    internal class InitializeHandler : IRequestHandler<InitializeParams, InitializeResult>
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public InitializeHandler()
        {
        }

        public Task<InitializeResult> HandleRequestAsync(InitializeParams request, ClientCapabilities clientCapabilities, string? clientName, CancellationToken cancellationToken)
        {

            return Task.FromResult(new InitializeResult
            {
                Capabilities = new ServerCapabilities
                {
                    CompletionProvider = new CompletionOptions { ResolveProvider = true, TriggerCharacters = new string[] { "<", " ", ":", ".", "=", "\"", "'", "{", "," } },
                    HoverProvider = true,
                    //FoldingRangeProvider = new FoldingRangeProviderOptions(),
                    //DocumentHighlightProvider = true,
                    //DocumentFormattingProvider = true,
                    //DocumentRangeFormattingProvider = true,
                    //DocumentOnTypeFormattingProvider = new LSP.DocumentOnTypeFormattingOptions { FirstTriggerCharacter = "}", MoreTriggerCharacter = new[] { ";", "\n" } },
                    //DefinitionProvider = true,
                    //ImplementationProvider = true,
                    //ReferencesProvider = true,
                    //ProjectContextProvider = true,
                    //RenameProvider = true,
                    //DocumentSymbolProvider = true,
                    //WorkspaceSymbolProvider = true,
                    //SignatureHelpProvider = new LSP.SignatureHelpOptions { TriggerCharacters = new[] { "{", "," } },
                    TextDocumentSync = new TextDocumentSyncOptions
                    {
                        Change = TextDocumentSyncKind.None
                    }
                }
            });
        }
    }
}
