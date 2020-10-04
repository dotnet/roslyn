// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
using Microsoft.CodeAnalysis.LanguageServer.Handler.SemanticTokens;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using LSP = Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler
{
    [Shared]
    [ExportLspMethod(LSP.Methods.InitializeName, mutatesSolutionState: false)]
    internal class InitializeHandler : IRequestHandler<LSP.InitializeParams, LSP.InitializeResult>
    {
        private readonly ImmutableArray<Lazy<CompletionProvider, Completion.Providers.CompletionProviderMetadata>> _completionProviders;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public InitializeHandler(
            [ImportMany] IEnumerable<Lazy<CompletionProvider, Completion.Providers.CompletionProviderMetadata>> completionProviders)
        {
            _completionProviders = completionProviders
                .Where(lz => lz.Metadata.Language == LanguageNames.CSharp || lz.Metadata.Language == LanguageNames.VisualBasic)
                .ToImmutableArray();
        }

        public TextDocumentIdentifier? GetTextDocumentIdentifier(InitializeParams request) => null;

        public Task<LSP.InitializeResult> HandleRequestAsync(LSP.InitializeParams request, RequestContext context, CancellationToken cancellationToken)
        {
            var commitCharacters = CompletionRules.Default.DefaultCommitCharacters.Select(c => c.ToString()).ToArray();
            var triggerCharacters = _completionProviders.SelectMany(
                lz => CompletionHandler.GetTriggerCharacters(lz.Value)).Distinct().Select(c => c.ToString()).ToArray();

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
                        TriggerCharacters = triggerCharacters
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
                    SemanticTokensOptions = new LSP.SemanticTokensOptions
                    {
                        DocumentProvider = new LSP.SemanticTokensDocumentProviderOptions { Edits = true },
                        RangeProvider = true,
                        Legend = new LSP.SemanticTokensLegend
                        {
                            TokenTypes = LSP.SemanticTokenTypes.AllTypes.Concat(SemanticTokensHelpers.RoslynCustomTokenTypes).ToArray(),
                            TokenModifiers = new string[] { LSP.SemanticTokenModifiers.Static }
                        }
                    },
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
