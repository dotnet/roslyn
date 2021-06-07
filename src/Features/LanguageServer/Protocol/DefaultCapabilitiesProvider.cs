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

namespace Microsoft.CodeAnalysis.LanguageServer
{
    [Export(typeof(DefaultCapabilitiesProvider)), Shared]
    internal class DefaultCapabilitiesProvider : ICapabilitiesProvider
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

        public ServerCapabilities GetCapabilities(ClientCapabilities clientCapabilities)
        {
            var capabilities = new ServerCapabilities();
            if (clientCapabilities is VSClientCapabilities vsClientCapabilities && vsClientCapabilities.SupportsVisualStudioExtensions)
            {
                capabilities = GetVSServerCapabilities();
            }

            var commitCharacters = CompletionRules.Default.DefaultCommitCharacters.Select(c => c.ToString()).ToArray();
            var triggerCharacters = _completionProviders.SelectMany(
                lz => CompletionHandler.GetTriggerCharacters(lz.Value)).Distinct().Select(c => c.ToString()).ToArray();

            capabilities.DefinitionProvider = true;
            capabilities.RenameProvider = true;
            capabilities.ImplementationProvider = true;
            capabilities.CodeActionProvider = new CodeActionOptions { CodeActionKinds = new[] { CodeActionKind.QuickFix, CodeActionKind.Refactor } };
            capabilities.CompletionProvider = new VisualStudio.LanguageServer.Protocol.CompletionOptions
            {
                ResolveProvider = true,
                AllCommitCharacters = commitCharacters,
                TriggerCharacters = triggerCharacters,
            };

            capabilities.SignatureHelpProvider = new SignatureHelpOptions { TriggerCharacters = new[] { "(", "," } };
            capabilities.DocumentSymbolProvider = true;
            capabilities.WorkspaceSymbolProvider = true;
            capabilities.DocumentFormattingProvider = true;
            capabilities.DocumentRangeFormattingProvider = true;
            capabilities.DocumentOnTypeFormattingProvider = new DocumentOnTypeFormattingOptions { FirstTriggerCharacter = "}", MoreTriggerCharacter = new[] { ";", "\n" } };
            capabilities.ReferencesProvider = true;
            capabilities.FoldingRangeProvider = true;
            capabilities.ExecuteCommandProvider = new ExecuteCommandOptions();
            capabilities.TextDocumentSync = new TextDocumentSyncOptions
            {
                Change = TextDocumentSyncKind.Incremental,
                OpenClose = true
            };

            capabilities.HoverProvider = true;

            return capabilities;
        }

        private static VSServerCapabilities GetVSServerCapabilities()
        {
            var vsServerCapabilities = new VSServerCapabilities();
            vsServerCapabilities.CodeActionsResolveProvider = true;
            vsServerCapabilities.OnAutoInsertProvider = new DocumentOnAutoInsertOptions { TriggerCharacters = new[] { "'", "/", "\n" } };
            vsServerCapabilities.DocumentHighlightProvider = true;
            vsServerCapabilities.ProjectContextProvider = true;
            vsServerCapabilities.SemanticTokensOptions = new SemanticTokensOptions
            {
                DocumentProvider = new SemanticTokensDocumentProviderOptions { Edits = true },
                RangeProvider = true,
                Legend = new SemanticTokensLegend
                {
                    TokenTypes = SemanticTokenTypes.AllTypes.Concat(SemanticTokensHelpers.RoslynCustomTokenTypes).ToArray(),
                    TokenModifiers = new string[] { SemanticTokenModifiers.Static }
                }
            };

            // Diagnostic requests are only supported from PullDiagnosticsInProcLanguageClient.
            vsServerCapabilities.SupportsDiagnosticRequests = false;
            return vsServerCapabilities;
        }
    }
}
