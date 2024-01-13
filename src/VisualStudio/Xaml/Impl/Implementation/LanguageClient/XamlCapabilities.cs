// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using Microsoft.CodeAnalysis.Editor.Xaml;
using Roslyn.LanguageServer.Protocol;
using Microsoft.VisualStudio.LanguageServices.Xaml.LanguageServer.Handler;
using RoslynCompletion = Microsoft.CodeAnalysis.Completion;

namespace Microsoft.VisualStudio.LanguageServices.Xaml
{
    internal static class XamlCapabilities
    {
        /// <summary>
        /// The currently supported set of XAML LSP Server capabilities
        /// </summary>
        public static VSInternalServerCapabilities Current => new()
        {
            CompletionProvider = new CompletionOptions
            {
                ResolveProvider = true,
                TriggerCharacters = ["<", " ", ":", ".", "=", "\"", "'", "{", ",", "("],
                AllCommitCharacters = RoslynCompletion.CompletionRules.Default.DefaultCommitCharacters.Select(c => c.ToString()).ToArray()
            },
            HoverProvider = true,
            FoldingRangeProvider = new FoldingRangeOptions { },
            DocumentFormattingProvider = true,
            DocumentRangeFormattingProvider = true,
            DocumentOnTypeFormattingProvider = new DocumentOnTypeFormattingOptions { FirstTriggerCharacter = ">" },
            OnAutoInsertProvider = new VSInternalDocumentOnAutoInsertOptions { TriggerCharacters = ["=", "/"] },
            TextDocumentSync = new TextDocumentSyncOptions
            {
                Change = TextDocumentSyncKind.None,
                OpenClose = false
            },
            SupportsDiagnosticRequests = true,
            LinkedEditingRangeProvider = new LinkedEditingRangeOptions { },
            ExecuteCommandProvider = new ExecuteCommandOptions { Commands = [StringConstants.CreateEventHandlerCommand] },
            DefinitionProvider = true,
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
