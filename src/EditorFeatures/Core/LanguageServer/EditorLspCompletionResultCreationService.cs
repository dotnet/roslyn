// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServer.Handler.Completion;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Text.Adornments;
using LSP = Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer
{
    [ExportWorkspaceService(typeof(ILspCompletionResultCreationService), ServiceLayer.Editor), Shared]
    internal sealed class EditorLspCompletionResultCreationService : ILspCompletionResultCreationService
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public EditorLspCompletionResultCreationService()
        {
        }

        public async Task<LSP.CompletionItem> CreateAsync(
            Document document,
            SourceText documentText,
            bool snippetsSupported,
            bool itemDefaultsSupported,
            TextSpan defaultSpan,
            CompletionItem item,
            CancellationToken cancellationToken)
        {
            var lspItem = new LSP.VSInternalCompletionItem
            {
                Icon = new ImageElement(item.Tags.GetFirstGlyph().GetImageId())
            };

            // Complex text edits (e.g. override and partial method completions) are always populated in the
            // resolve handler, so we leave both TextEdit and InsertText unpopulated in these cases.
            if (item.IsComplexTextEdit)
            {
                lspItem.VsResolveTextEditOnCommit = true;

                // Razor C# is currently the only language client that supports LSP.InsertTextFormat.Snippet.
                // We can enable it for regular C# once LSP is used for local completion.
                if (snippetsSupported)
                    lspItem.InsertTextFormat = LSP.InsertTextFormat.Snippet;
            }
            else
            {
                await DefaultLspCompletionResultCreationService.PopulateTextEditAsync(
                    document, documentText, itemDefaultsSupported, defaultSpan, item, lspItem, cancellationToken).ConfigureAwait(false);
            }

            return lspItem;
        }
    }
}
