// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Composition;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServer.Handler.Completion;
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

        public Task<LSP.CompletionItem> CreateAsync(CancellationToken cancellationToken)
        {
            var completionItem = new LSP.VSInternalCompletionItem();

            completionItem.Label = completeDisplayText;
            completionItem.SortText = item.SortText;
            completionItem.FilterText = item.FilterText;
            completionItem.Kind = GetCompletionKind(item.Tags);
            completionItem.Data = completionResolveData;
            completionItem.Preselect = ShouldItemBePreselected(item);

            var commitCharacters = GetCommitCharacters(item, commitCharacterRulesCache, supportsVSExtensions);

            // Complex text edits (e.g. override and partial method completions) are always populated in the
            // resolve handler, so we leave both TextEdit and InsertText unpopulated in these cases.
            if (item.IsComplexTextEdit && completionItem is LSP.VSInternalCompletionItem vsItem)
            {
                vsItem.VsResolveTextEditOnCommit = true;
                // Razor C# is currently the only language client that supports LSP.InsertTextFormat.Snippet.
                // We can enable it for regular C# once LSP is used for local completion.
                if (snippetsSupported)
                {
                    completionItem.InsertTextFormat = LSP.InsertTextFormat.Snippet;
                }
            }
            else
            {
                await AddTextEdit(
                    document, item, completionItem, completionService, documentText, defaultSpan, itemDefaultsSupported, cancellationToken).ConfigureAwait(false);
            }

            if (commitCharacters != null)
            {
                completionItem.CommitCharacters = commitCharacters;
            }

            if (completionItem is LSP.VSInternalCompletionItem vsCompletionItem)
            {
                vsCompletionItem.Icon = new ImageElement(item.Tags.GetFirstGlyph().GetImageId());
            }

            return completionItem;

            static async Task AddTextEdit(
                Document document,
                CompletionItem item,
                LSP.CompletionItem lspItem,
                CompletionService completionService,
                SourceText documentText,
                TextSpan defaultSpan,
                bool itemDefaultsSupported,
                CancellationToken cancellationToken)
            {
                var completionChange = await completionService.GetChangeAsync(
                    document, item, cancellationToken: cancellationToken).ConfigureAwait(false);
                var completionChangeSpan = completionChange.TextChange.Span;
                var newText = completionChange.TextChange.NewText ?? "";

                if (itemDefaultsSupported && completionChangeSpan == defaultSpan)
                {
                    // The span is the same as the default, we just need to store the new text as
                    // the insert text so the client can create the text edit from it and the default range.
                    lspItem.InsertText = newText;
                }
                else
                {
                    var textEdit = new LSP.TextEdit()
                    {
                        NewText = newText,
                        Range = ProtocolConversions.TextSpanToRange(completionChangeSpan, documentText),
                    };
                    lspItem.TextEdit = textEdit;
                }
            }
        }
    }
}
