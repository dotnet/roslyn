﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Editor.Xaml;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Microsoft.VisualStudio.LanguageServices.Xaml.LanguageServer.Handler;

namespace Microsoft.VisualStudio.LanguageServices.Xaml
{
    internal static class XamlCapabilities
    {
        /// <summary>
        /// The currently supported set of XAML LSP Server capabilities
        /// </summary>
        public static VSServerCapabilities Current => new()
        {
            CompletionProvider = new CompletionOptions { ResolveProvider = true, TriggerCharacters = new string[] { "<", " ", ":", ".", "=", "\"", "'", "{", ",", "(" } },
            HoverProvider = true,
            FoldingRangeProvider = new FoldingRangeOptions { },
            DocumentFormattingProvider = true,
            DocumentRangeFormattingProvider = true,
            DocumentOnTypeFormattingProvider = new DocumentOnTypeFormattingOptions { FirstTriggerCharacter = ">", MoreTriggerCharacter = new string[] { "\n" } },
            OnAutoInsertProvider = new DocumentOnAutoInsertOptions { TriggerCharacters = new[] { "=", "/", ">" } },
            TextDocumentSync = new TextDocumentSyncOptions
            {
                Change = TextDocumentSyncKind.None,
                OpenClose = false
            },
            SupportsDiagnosticRequests = true,
            OnTypeRenameProvider = new DocumentOnTypeRenameOptions { WordPattern = OnTypeRenameHandler.NamePattern },
            ExecuteCommandProvider = new ExecuteCommandOptions { Commands = new[] { StringConstants.CreateEventHandlerCommand } },
        };

        /// <summary>
        /// An empty set of capabilities used to disable the XAML LSP Server
        /// </summary>
        public static VSServerCapabilities None => new()
        {
            TextDocumentSync = new TextDocumentSyncOptions
            {
                Change = TextDocumentSyncKind.None,
                OpenClose = false
            },
        };
    }
}
