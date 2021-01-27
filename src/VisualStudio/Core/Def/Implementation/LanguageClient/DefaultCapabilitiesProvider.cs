// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Completion.Providers;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServer.Handler;
using Microsoft.CodeAnalysis.LanguageServer.Handler.SemanticTokens;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.LanguageClient
{
    [Export, Shared]
    internal class DefaultCapabilitiesProvider
    {
        private readonly ImmutableArray<Lazy<CompletionProvider, CompletionProviderMetadata>> _completionProviders;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public DefaultCapabilitiesProvider(
            [ImportMany] IEnumerable<Lazy<CompletionProvider, CompletionProviderMetadata>> completionProviders)
        {
            _completionProviders = completionProviders
                .Where(lz => lz.Metadata.Language == LanguageNames.CSharp || lz.Metadata.Language == LanguageNames.VisualBasic)
                .ToImmutableArray();
        }

        public VSServerCapabilities GetCapabilities()
        {
            var commitCharacters = CompletionRules.Default.DefaultCommitCharacters.Select(c => c.ToString()).ToArray();
            var triggerCharacters = _completionProviders.SelectMany(
                lz => CompletionHandler.GetTriggerCharacters(lz.Value)).Distinct().Select(c => c.ToString()).ToArray();

            return new VSServerCapabilities
            {
                DefinitionProvider = true,
                RenameProvider = true,
                ImplementationProvider = true,
                CodeActionProvider = new CodeActionOptions { CodeActionKinds = new[] { CodeActionKind.QuickFix, CodeActionKind.Refactor } },
                CodeActionsResolveProvider = true,
                CompletionProvider = new LanguageServer.Protocol.CompletionOptions
                {
                    ResolveProvider = true,
                    AllCommitCharacters = commitCharacters,
                    TriggerCharacters = triggerCharacters
                },
                SignatureHelpProvider = new SignatureHelpOptions { TriggerCharacters = new[] { "(", "," } },
                DocumentSymbolProvider = true,
                WorkspaceSymbolProvider = true,
                DocumentFormattingProvider = true,
                DocumentRangeFormattingProvider = true,
                DocumentOnTypeFormattingProvider = new DocumentOnTypeFormattingOptions { FirstTriggerCharacter = "}", MoreTriggerCharacter = new[] { ";", "\n" } },
                OnAutoInsertProvider = new DocumentOnAutoInsertOptions { TriggerCharacters = new[] { "'", "/", "\n" } },
                DocumentHighlightProvider = true,
                ReferencesProvider = true,
                ProjectContextProvider = true,
                FoldingRangeProvider = true,
                SemanticTokensOptions = new SemanticTokensOptions
                {
                    DocumentProvider = new SemanticTokensDocumentProviderOptions { Edits = true },
                    RangeProvider = true,
                    Legend = new SemanticTokensLegend
                    {
                        TokenTypes = SemanticTokenTypes.AllTypes.Concat(SemanticTokensHelpers.RoslynCustomTokenTypes).ToArray(),
                        TokenModifiers = new string[] { SemanticTokenModifiers.Static }
                    }
                },
                ExecuteCommandProvider = new ExecuteCommandOptions(),
                TextDocumentSync = new TextDocumentSyncOptions
                {
                    Change = TextDocumentSyncKind.Incremental,
                    OpenClose = true
                },

                // Always support hover - if any LSP client for a content type advertises support,
                // then the liveshare provider is disabled.  So we must provide for both C# and razor
                // until https://devdiv.visualstudio.com/DevDiv/_workitems/edit/1106064/ is fixed
                // or we have different content types.
                HoverProvider = true,

                // Diagnostic requests are only supported from PullDiagnosticsInProcLanguageClient.
                SupportsDiagnosticRequests = false,
            };
        }
    }
}
