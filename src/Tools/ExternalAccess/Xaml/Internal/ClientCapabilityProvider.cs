// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Linq;
using Roslyn.LanguageServer.Protocol;
using LSP = Roslyn.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.ExternalAccess.Xaml;

internal class ClientCapabilityProvider : IClientCapabilityProvider
{
    private readonly LSP.ClientCapabilities _clientCapabilities;

    public ClientCapabilityProvider(LSP.ClientCapabilities clientCapabilities)
    {
        _clientCapabilities = clientCapabilities;
    }

    public bool SupportsMarkdownDocumentation
        => _clientCapabilities.TextDocument?.Completion?.CompletionItem?.DocumentationFormat?.Contains(MarkupKind.Markdown) == true;

    public bool SupportsCompletionListData
        => _clientCapabilities.TextDocument?.Completion?.CompletionListSetting?.ItemDefaults?.Contains("data") == true;

    public bool IsDynamicRegistrationSupported(string methodName)
    {
        switch (methodName)
        {
            case LSP.Methods.TextDocumentDidOpenName:
                return _clientCapabilities?.TextDocument?.Synchronization?.DynamicRegistration == true;
            case LSP.Methods.TextDocumentDidChangeName:
                return _clientCapabilities?.TextDocument?.Synchronization?.DynamicRegistration == true;
            case LSP.Methods.TextDocumentDidCloseName:
                return _clientCapabilities?.TextDocument?.Synchronization?.DynamicRegistration == true;
            case LSP.Methods.TextDocumentCompletionName:
                return _clientCapabilities?.TextDocument?.Completion?.DynamicRegistration == true;
            case LSP.Methods.TextDocumentHoverName:
                return _clientCapabilities?.TextDocument?.Hover?.DynamicRegistration == true;
            case LSP.Methods.TextDocumentFoldingRangeName:
                return _clientCapabilities?.TextDocument?.FoldingRange?.DynamicRegistration == true;
            case LSP.Methods.TextDocumentFormattingName:
                return _clientCapabilities?.TextDocument?.Formatting?.DynamicRegistration == true;
            case LSP.Methods.TextDocumentRangeFormattingName:
                return _clientCapabilities?.TextDocument?.RangeFormatting?.DynamicRegistration == true;
            case LSP.Methods.TextDocumentOnTypeFormattingName:
                return _clientCapabilities?.TextDocument?.OnTypeFormatting?.DynamicRegistration == true;
            case LSP.Methods.TextDocumentDefinitionName:
                return _clientCapabilities?.TextDocument?.Definition?.DynamicRegistration == true;
            case LSP.Methods.TextDocumentDiagnosticName:
                return _clientCapabilities?.TextDocument?.Diagnostic?.DynamicRegistration == true;
            case LSP.Methods.TextDocumentCodeActionName:
                return _clientCapabilities?.TextDocument?.CodeAction?.DynamicRegistration == true;
            case LSP.Methods.WorkspaceExecuteCommandName:
                return _clientCapabilities?.Workspace?.ExecuteCommand?.DynamicRegistration == true;
            case LSP.Methods.TextDocumentDocumentSymbolName:
                return _clientCapabilities?.TextDocument?.DocumentSymbol?.DynamicRegistration == true;
            case LSP.Methods.TextDocumentDocumentHighlightName:
                return _clientCapabilities?.TextDocument?.DocumentHighlight?.DynamicRegistration == true;
            case LSP.Methods.TextDocumentDocumentLinkName:
                return _clientCapabilities?.TextDocument?.DocumentLink?.DynamicRegistration == true;
            case LSP.Methods.TextDocumentRenameName:
                return _clientCapabilities?.TextDocument?.Rename?.DynamicRegistration == true;
            case LSP.Methods.TextDocumentSemanticTokensFullName:
                return _clientCapabilities?.TextDocument?.SemanticTokens?.DynamicRegistration == true;
            case LSP.Methods.TextDocumentSignatureHelpName:
                return _clientCapabilities?.TextDocument?.SignatureHelp?.DynamicRegistration == true;
            case LSP.Methods.WorkspaceSymbolName:
                return _clientCapabilities?.Workspace?.Symbol?.DynamicRegistration == true;
            case LSP.Methods.WorkspaceDidChangeConfigurationName:
                return _clientCapabilities?.Workspace?.DidChangeConfiguration?.DynamicRegistration == true;
            case LSP.Methods.WorkspaceDidChangeWatchedFilesName:
                return _clientCapabilities?.Workspace?.DidChangeWatchedFiles?.DynamicRegistration == true;
            case LSP.VSInternalMethods.OnAutoInsertName:
                if (_clientCapabilities.TextDocument is VSInternalTextDocumentClientCapabilities internalTextDocumentClientCapabilities)
                {
                    return internalTextDocumentClientCapabilities.OnAutoInsert?.DynamicRegistration == true;
                }
                return false;
            default:
                throw new InvalidOperationException($"Unsupported dynamic registration method: {methodName}");
        }
    }
}
