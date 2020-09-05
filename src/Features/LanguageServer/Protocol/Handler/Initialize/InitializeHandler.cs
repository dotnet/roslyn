// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using LSP = Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler
{
    [Shared]
    [ExportLspMethod(LSP.Methods.InitializeName)]
    internal class InitializeHandler : IRequestHandler<LSP.InitializeParams, LSP.InitializeResult>
    {
        private readonly ImmutableArray<Lazy<CompletionProvider, Completion.Providers.CompletionProviderMetadata>> _completionProviders;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public InitializeHandler([ImportMany] IEnumerable<Lazy<CompletionProvider, Completion.Providers.CompletionProviderMetadata>> completionProviders)
        {
            _completionProviders = completionProviders.ToImmutableArray();
        }

        public Task<LSP.InitializeResult> HandleRequestAsync(LSP.InitializeParams request, RequestContext context, CancellationToken cancellationToken)
        {
            var commitCharacters = CompletionRules.Default.DefaultCommitCharacters.Select(c => c.ToString()).ToArray();
            var triggerCharacters = CompletionHandler.GetCSharpTriggerCharacters(_completionProviders).Union(CompletionHandler.GetVisualBasicTriggerCharacters(_completionProviders));

            return Task.FromResult(new LSP.InitializeResult
            {
                Capabilities = new LSP.VSServerCapabilities
                {
                    DefinitionProvider = true,
                    RenameProvider = true,
                    ImplementationProvider = true,
                    CodeActionProvider = new LSP.CodeActionOptions { CodeActionKinds = new[] { CodeActionKind.QuickFix, CodeActionKind.Refactor } },
                    CodeActionsResolveProvider = true,
                    CompletionProvider = new LSP.CompletionOptions
                    {
                        ResolveProvider = true,
                        AllCommitCharacters = commitCharacters,
                        TriggerCharacters = triggerCharacters.ToArray()
                    },
                    SignatureHelpProvider = new LSP.SignatureHelpOptions { TriggerCharacters = new[] { "(", "," } },
                    DocumentSymbolProvider = true,
                    WorkspaceSymbolProvider = true,
                    DocumentFormattingProvider = true,
                    DocumentRangeFormattingProvider = true,
                    DocumentOnTypeFormattingProvider = new LSP.DocumentOnTypeFormattingOptions { FirstTriggerCharacter = "}", MoreTriggerCharacter = new[] { ";", "\n" } },
                    OnAutoInsertProvider = new LSP.DocumentOnAutoInsertOptions { TriggerCharacters = new[] { "'", "/", "\n" } },
                    DocumentHighlightProvider = true,
                    ReferencesProvider = true,
                    ProjectContextProvider = true,
                    ExecuteCommandProvider = new LSP.ExecuteCommandOptions(),
                    TextDocumentSync = new LSP.TextDocumentSyncOptions
                    {
                        Change = LSP.TextDocumentSyncKind.None
                    }
                }
            });
        }
    }
}
