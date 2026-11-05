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
using Microsoft.CodeAnalysis.LanguageServer.Handler.Completion;
using Microsoft.CodeAnalysis.LanguageServer.Handler.SemanticTokens;
using Microsoft.CodeAnalysis.LanguageServer.Handler.TextDocumentContent;
using Microsoft.CodeAnalysis.SignatureHelp;
using Microsoft.CommonLanguageServerProtocol.Framework;
using Roslyn.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer;

/// <summary>
/// Implementation of <see cref="ICapabilitiesProvider"/> that provides all the capabilities that Roslyn supports via LSP.
/// </summary>
[Export(typeof(DefaultCapabilitiesProvider)), Shared]
[ExportCSharpVisualBasicStatelessLspService(typeof(ICapabilitiesProvider), WellKnownLspServerKinds.Any)]
internal sealed class DefaultCapabilitiesProvider : ICapabilitiesProvider
{
    private readonly ImmutableArray<Lazy<CompletionProvider, CompletionProviderMetadata>> _completionProviders;
    private readonly ImmutableArray<Lazy<ISignatureHelpProvider, OrderableLanguageMetadata>> _signatureHelpProviders;
    private readonly IEnumerable<Lazy<ILspWillRenameListener, ILspWillRenameListenerMetadata>> _renameListeners;

    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public DefaultCapabilitiesProvider(
        [ImportMany] IEnumerable<Lazy<CompletionProvider, CompletionProviderMetadata>> completionProviders,
        [ImportMany] IEnumerable<Lazy<ISignatureHelpProvider, OrderableLanguageMetadata>> signatureHelpProviders,
        [ImportMany] IEnumerable<Lazy<ILspWillRenameListener, ILspWillRenameListenerMetadata>> renameListeners)
    {
        _completionProviders = [.. completionProviders.Where(lz => lz.Metadata.Language is LanguageNames.CSharp or LanguageNames.VisualBasic)];
        _signatureHelpProviders = [.. signatureHelpProviders.Where(lz => lz.Metadata.Language is LanguageNames.CSharp or LanguageNames.VisualBasic)];
        _renameListeners = renameListeners;
    }

    public ServerCapabilities GetCapabilities(ClientCapabilities clientCapabilities, ILspServices lspServices)
    {
        var supportsVsExtensions = clientCapabilities.HasVisualStudioLspCapability();
        var capabilities = supportsVsExtensions ? GetVSServerCapabilities() : new ServerCapabilities();

        var commitCharacters = CompletionResultFactory.DefaultCommitCharactersArray;
        var triggerCharacters = _completionProviders.SelectMany(
            lz => CommonCompletionUtilities.GetTriggerCharacters(lz.Value)).Distinct().Select(c => c.ToString()).ToArray();

        capabilities.DefinitionProvider = true;
        capabilities.TypeDefinitionProvider = true;
        capabilities.DocumentHighlightProvider = true;
        capabilities.RenameProvider = new RenameRegistrationOptions
        {
            PrepareProvider = true,
        };
        capabilities.ImplementationProvider = true;
        capabilities.CodeActionProvider = new CodeActionRegistrationOptions { CodeActionKinds = [CodeActionKind.QuickFix, CodeActionKind.Refactor], ResolveProvider = true };
        capabilities.CompletionProvider = new Roslyn.LanguageServer.Protocol.CompletionRegistrationOptions
        {
            ResolveProvider = true,
            AllCommitCharacters = commitCharacters,
            TriggerCharacters = triggerCharacters,
        };

        var signatureHelpTriggerCharacters = _signatureHelpProviders.SelectMany(
            lz => lz.Value.TriggerCharacters).Distinct().Select(c => c.ToString()).ToArray();
        var signatureHelpRetriggerCharacters = _signatureHelpProviders.SelectMany(
            lz => lz.Value.RetriggerCharacters).Distinct().Select(c => c.ToString()).ToArray();

        capabilities.SignatureHelpProvider = new SignatureHelpRegistrationOptions { TriggerCharacters = signatureHelpTriggerCharacters, RetriggerCharacters = signatureHelpRetriggerCharacters };
        capabilities.DocumentSymbolProvider = true;
        capabilities.WorkspaceSymbolProvider = true;
        capabilities.DocumentFormattingProvider = true;
        capabilities.DocumentRangeFormattingProvider = true;
        capabilities.DocumentOnTypeFormattingProvider = new DocumentOnTypeFormattingRegistrationOptions { FirstTriggerCharacter = "}", MoreTriggerCharacter = [";", "\n"] };
        capabilities.ReferencesProvider = new ReferenceRegistrationOptions
        {
            WorkDoneProgress = true,
        };

        capabilities.FoldingRangeProvider = true;
        capabilities.SelectionRangeProvider = true;
        capabilities.CallHierarchyProvider = true;
        capabilities.TypeHierarchyProvider = true;
        capabilities.ExecuteCommandProvider = new ExecuteCommandRegistrationOptions() { Commands = [] };
        capabilities.TextDocumentSync = new TextDocumentSyncOptions
        {
            Change = TextDocumentSyncKind.Incremental,
            OpenClose = true,
            Save = new TextDocumentSaveRegistrationOptions
            {
                IncludeText = false,
            }
        };

        capabilities.HoverProvider = true;

        // Using only range handling has shown to be more performant than using a combination of full/edits/range
        // handling, especially for larger files. With range handling, we only need to compute tokens for whatever
        // is in view, while with full/edits handling we need to compute tokens for the entire file and then
        // potentially run a diff between the old and new tokens. Therefore, we only enable full handling if
        // the client does not support ranges.
        var rangeCapabilities = clientCapabilities.TextDocument?.SemanticTokens?.Requests?.Range;
        var supportsSemanticTokensRange = rangeCapabilities?.Value is not (false or null);
        capabilities.SemanticTokensProvider = new SemanticTokensRegistrationOptions
        {
            Full = !supportsSemanticTokensRange,
            Range = true,
            Legend = new SemanticTokensLegend
            {
                TokenTypes = [.. SemanticTokensSchema.GetSchema(clientCapabilities.HasVisualStudioLspCapability()).AllTokenTypes],
                TokenModifiers = SemanticTokensSchema.TokenModifiers
            }
        };

        capabilities.CodeLensProvider = new CodeLensRegistrationOptions
        {
            ResolveProvider = true,
            // TODO - Code lens should support streaming
            // See https://devdiv.visualstudio.com/DevDiv/_workitems/edit/1730465
            WorkDoneProgress = false,
        };

        capabilities.InlayHintProvider = new InlayHintRegistrationOptions
        {
            ResolveProvider = true,
            WorkDoneProgress = false,
        };

        // Using VS server capabilities because we have our own custom client.
        capabilities.OnAutoInsertProvider = new VSInternalDocumentOnAutoInsertRegistrationOptions { TriggerCharacters = ["'", "/", "\n", "\""] };

        var diagnosticDynamicRegistationCapabilities = clientCapabilities.TextDocument?.Diagnostic?.DynamicRegistration;
        if (diagnosticDynamicRegistationCapabilities is false)
        {
            capabilities.DiagnosticProvider = new DiagnosticRegistrationOptions()
            {
                InterFileDependencies = true
            };
        }

        capabilities.Workspace = new WorkspaceOptions
        {
            FileOperations = GetWorkspaceFileOperationsCapabilities(),
            TextDocumentContent = GetTextDocumentContentCapabilities(),
        };

        return capabilities;

        FileOperationOptions? GetWorkspaceFileOperationsCapabilities()
        {
            if (clientCapabilities.Workspace?.FileOperations is null)
                return null;

            // Register for file rename notifications based on the registered rename listeners.
            using var _ = PooledObjects.ArrayBuilder<FileOperationFilter>.GetInstance(out var filters);
            foreach (var listener in _renameListeners)
            {
                filters.Add(new FileOperationFilter
                {
                    Pattern = new FileOperationPattern { Glob = listener.Metadata.Glob }
                });
            }

            if (filters.Count == 0)
                return null;

            return new FileOperationOptions()
            {
                WillRename = new FileOperationRegistrationOptions()
                {
                    Filters = filters.ToArray()
                }
            };
        }

        TextDocumentContentRegistrationOptions? GetTextDocumentContentCapabilities()
        {
            if (clientCapabilities.Workspace?.TextDocumentContent is null)
                return null;

            // Client supports textDocumentContent - register the schemes from all providers so the client
            // can use workspace/textDocumentContent to retrieve virtual document contents.
            var schemes = lspServices.GetRequiredServices<ITextDocumentContentProvider>().Select(p => p.Scheme).ToArray();
            if (schemes.Length == 0)
                return null;

            return new TextDocumentContentRegistrationOptions
            {
                Schemes = schemes
            };
        }
    }

    private static ServerCapabilities GetVSServerCapabilities()
        => new()
        {
            ProjectContextProvider = true,
            BreakableRangeProvider = true,
        };
}
