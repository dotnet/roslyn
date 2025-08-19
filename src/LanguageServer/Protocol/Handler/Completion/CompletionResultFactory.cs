// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Completion.Providers.Snippets;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Collections;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Text.Adornments;
using Roslyn.Utilities;
using LSP = Roslyn.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler.Completion;

internal static class CompletionResultFactory
{
    /// <summary>
    /// Command name implemented by the client and invoked when an item with complex edit is committed.
    /// </summary>
    public const string CompleteComplexEditCommand = "roslyn.client.completionComplexEdit";

    public static string[] DefaultCommitCharactersArray { get; } = CreateCommitCharacterArrayFromRules(CompletionItemRules.Default);

    public static async Task<LSP.VSInternalCompletionList> ConvertToLspCompletionListAsync(
        Document document,
        CompletionCapabilityHelper capabilityHelper,
        CompletionList list,
        bool isIncomplete,
        bool isHardSelection,
        long resultId,
        CancellationToken cancellationToken)
    {
        var isSuggestionMode = !isHardSelection;
        if (list.ItemsList.Count == 0)
        {
            return new LSP.VSInternalCompletionList
            {
                Items = [],
                // If we have a suggestion mode item, we just need to keep the list in suggestion mode.
                // We don't need to return the fake suggestion mode item.
                SuggestionMode = isSuggestionMode,
                IsIncomplete = isIncomplete,
            };
        }

        var lspVSClientCapability = capabilityHelper.SupportVSInternalClientCapabilities;
        var defaultEditRangeSupported = capabilityHelper.SupportDefaultEditRange;

        var documentText = await document.GetValueTextAsync(cancellationToken).ConfigureAwait(false);

        // Set resolve data on list if the client supports it, otherwise set it on each item.
        var resolveData = new CompletionResolveData(resultId, ProtocolConversions.DocumentToTextDocumentIdentifier(document));
        var completionItemResolveData = capabilityHelper.SupportCompletionListData || capabilityHelper.SupportVSInternalCompletionListData
            ? null : resolveData;

        using var _ = ArrayBuilder<LSP.CompletionItem>.GetInstance(out var lspCompletionItems);
        var commitCharactersRuleCache = new Dictionary<ImmutableArray<CharacterSetModificationRule>, string[]>(CommitCharacterArrayComparer.Instance);

        var completionService = document.GetRequiredLanguageService<CompletionService>();

        var defaultSpan = list.Span;
        var typedText = documentText.GetSubText(defaultSpan).ToString();
        for (var i = 0; i < list.ItemsList.Count; i++)
        {
            var item = list.ItemsList[i];
            item.Span = defaultSpan; // item.Span will be used to generate change, adjust it if needed
            lspCompletionItems.Add(await CreateLSPCompletionItemAsync(item, typedText, i).ConfigureAwait(false));
        }

        var completionList = new LSP.VSInternalCompletionList
        {
            // public LSP
            Items = lspCompletionItems.ToArray(),
            IsIncomplete = isIncomplete,
            ItemDefaults = new LSP.CompletionListItemDefaults
            {
                EditRange = capabilityHelper.SupportDefaultEditRange ? ProtocolConversions.TextSpanToRange(defaultSpan, documentText) : null,
                Data = capabilityHelper.SupportCompletionListData ? resolveData : null,
            },

            // VS internal
            //
            // If we have a suggestion mode item, we just need to keep the list in suggestion mode.
            // We don't need to return the fake suggestion mode item.
            SuggestionMode = isSuggestionMode,
            Data = capabilityHelper.SupportVSInternalCompletionListData ? resolveData : null,
        };

        if (capabilityHelper.SupportedInsertTextModes.Contains(LSP.InsertTextMode.AsIs))
        {
            // By default, all text edits we create include the appropriate whitespace, so tell the client to leave it as-is (if it supports the option).
            completionList.ItemDefaults.InsertTextMode = LSP.InsertTextMode.AsIs;
        }

        PromoteCommonCommitCharactersOntoList();

        if (completionList.ItemDefaults is { EditRange: null, CommitCharacters: null, Data: null })
            completionList.ItemDefaults = null;

        return capabilityHelper.SupportVSInternalClientCapabilities
            ? new LSP.OptimizedVSCompletionList(completionList)
            : completionList;

        async Task<LSP.CompletionItem> CreateLSPCompletionItemAsync(CompletionItem item, string typedText, int index)
        {
            // Defer to host to create the actual completion item (including potential subclasses),
            // and add any custom information.
            var lspItem = await CreateItemAndPopulateTextEditAsync(
                document,
                documentText,
                lspVSClientCapability,
                capabilityHelper.SupportSnippets,
                defaultEditRangeSupported,
                defaultSpan,
                typedText,
                item,
                completionService,
                cancellationToken).ConfigureAwait(false);

            if (!item.InlineDescription.IsEmpty())
                lspItem.LabelDetails = new() { Description = item.InlineDescription };

            // Now add data common to all hosts.
            lspItem.Data = completionItemResolveData;

            if (!lspItem.Label.Equals(item.SortText, StringComparison.Ordinal))
            {
                lspItem.SortText = item.SortText;
            }

            if (!capabilityHelper.SupportVSInternalClientCapabilities)
            {
                // VSCode doesn't handle casing very well, but we do, and we already sorted the list
                // Add sort text to ensure that items are sorted by their position in the list.
                // The max length of the list is 1000 items, so we only need 4 characters to represent the index.
                lspItem.SortText = $"{index:D4}{lspItem.SortText}";
            }

            if (!lspItem.Label.Equals(item.FilterText, StringComparison.Ordinal))
                lspItem.FilterText = item.FilterText;

            lspItem.Kind = GetCompletionKind(item.Tags, capabilityHelper.SupportedItemKinds);
            lspItem.Tags = GetCompletionTags(item.Tags, capabilityHelper.SupportedItemTags);
            lspItem.Preselect = item.Rules.MatchPriority == MatchPriority.Preselect;

            if (lspVSClientCapability)
            {
                lspItem.CommitCharacters = GetCommitCharacters(item, commitCharactersRuleCache);
                return lspItem;
            }

            // VSCode does not have the concept of soft selection, the list is always hard selected.
            // In order to emulate soft selection behavior for things like suggestion mode, argument completion, regex completion,
            // datetime completion, etc. we create a completion item without any specific commit characters.
            // This means only tab / enter will commit. VS supports soft selection, so we only do this for non-VS clients.
            if (isSuggestionMode)
            {
                lspItem.CommitCharacters = [];
            }
            else if (typedText.Length == 0 && item.Rules.SelectionBehavior != CompletionItemSelectionBehavior.HardSelection)
            {
                // Note this also applies when user hasn't actually typed anything and completion provider does not request the item
                // to be hard-selected. Otherwise, we set its commit characters as normal. This means we'd need to set IsIncomplete to true
                // to make sure the client will ask us again when user starts typing so we can provide items with proper commit characters.
                lspItem.CommitCharacters = [];
                isIncomplete = true;
            }
            else
            {
                lspItem.CommitCharacters = GetCommitCharacters(item, commitCharactersRuleCache);
            }

            return lspItem;
        }

        static LSP.CompletionItemKind GetCompletionKind(
            ImmutableArray<string> tags,
            ISet<LSP.CompletionItemKind> supportedClientKinds)
        {
            foreach (var tag in tags)
            {
                if (ProtocolConversions.RoslynTagToCompletionItemKinds.TryGetValue(tag, out var completionItemKinds))
                {
                    // Always at least pick the core kind provided.
                    var kind = completionItemKinds[0];

                    // If better kinds are preferred, return them if the client supports them.
                    for (var i = 1; i < completionItemKinds.Length; i++)
                    {
                        var preferredKind = completionItemKinds[i];
                        if (supportedClientKinds.Contains(preferredKind))
                            kind = preferredKind;
                    }

                    return kind;
                }
            }

            return LSP.CompletionItemKind.Text;
        }

        static LSP.CompletionItemTag[]? GetCompletionTags(
            ImmutableArray<string> tags,
            ISet<LSP.CompletionItemTag> supportedClientTags)
        {
            using var result = TemporaryArray<LSP.CompletionItemTag>.Empty;

            foreach (var tag in tags)
            {
                if (ProtocolConversions.RoslynTagToCompletionItemTags.TryGetValue(tag, out var completionItemTags))
                {
                    // Always at least pick the core tag provided.
                    var lspTag = completionItemTags[0];

                    // If better kinds are preferred, return them if the client supports them.
                    for (var i = 1; i < completionItemTags.Length; i++)
                    {
                        var preferredTag = completionItemTags[i];
                        if (supportedClientTags.Contains(preferredTag))
                            lspTag = preferredTag;
                    }

                    result.Add(lspTag);
                }
            }

            if (result.Count == 0)
                return null;

            return [.. result.ToImmutableAndClear()];
        }

        static string[] GetCommitCharacters(
            CompletionItem item,
            Dictionary<ImmutableArray<CharacterSetModificationRule>, string[]> currentRuleCache)
        {
            if (item.Rules.CommitCharacterRules.IsEmpty)
                return DefaultCommitCharactersArray;

            if (!currentRuleCache.TryGetValue(item.Rules.CommitCharacterRules, out var cachedCommitCharacters))
            {
                cachedCommitCharacters = CreateCommitCharacterArrayFromRules(item.Rules);
                currentRuleCache.Add(item.Rules.CommitCharacterRules, cachedCommitCharacters);
            }

            return cachedCommitCharacters;
        }

        void PromoteCommonCommitCharactersOntoList()
        {
            // If client doesn't support default commit characters on list, we want to set commit characters for each item with default to null.
            // This way client will default to the commit chars server provided in ServerCapabilities.CompletionProvider.AllCommitCharacters.
            if (!(capabilityHelper.SupportDefaultCommitCharacters || capabilityHelper.SupportVSInternalDefaultCommitCharacters))
            {
                foreach (var completionItem in completionList.Items)
                {
                    if (completionItem.CommitCharacters == DefaultCommitCharactersArray)
                        completionItem.CommitCharacters = null;
                }

                return;
            }

            if (completionList.Items.IsEmpty())
                return;

            var commitCharacterReferences = new Dictionary<object, int>();
            var mostUsedCount = 0;
            string[]? mostUsedCommitCharacters = null;

            for (var i = 0; i < completionList.Items.Length; i++)
            {
                var completionItem = completionList.Items[i];
                var commitCharacters = completionItem.CommitCharacters;

                Contract.ThrowIfNull(commitCharacters);

                commitCharacterReferences.TryGetValue(commitCharacters, out var existingCount);
                existingCount++;

                if (existingCount > mostUsedCount)
                {
                    // Capture the most used commit character counts so we don't need to re-iterate the array later
                    mostUsedCommitCharacters = commitCharacters;
                    mostUsedCount = existingCount;
                }

                commitCharacterReferences[commitCharacters] = existingCount;
            }

            // Promoted the most used commit characters onto the list and then remove these from child items.
            // public LSP
            if (capabilityHelper.SupportDefaultCommitCharacters)
            {
                completionList.ItemDefaults.CommitCharacters = mostUsedCommitCharacters;
            }

            // VS internal
            if (capabilityHelper.SupportVSInternalDefaultCommitCharacters)
            {
                completionList.CommitCharacters = mostUsedCommitCharacters;
            }

            foreach (var completionItem in completionList.Items)
            {
                if (completionItem.CommitCharacters == mostUsedCommitCharacters)
                {
                    completionItem.CommitCharacters = null;
                }
            }
        }
    }

    private static async Task<LSP.CompletionItem> CreateItemAndPopulateTextEditAsync(
        Document document,
        SourceText documentText,
        bool supportsVSExtensions,
        bool snippetsSupported,
        bool itemDefaultsSupported,
        TextSpan defaultSpan,
        string typedText,
        CompletionItem item,
        CompletionService completionService,
        CancellationToken cancellationToken)
    {
        if (supportsVSExtensions)
        {
            return await CreateVsItemAndPopulateTextEditAsync(
                document,
                documentText,
                snippetsSupported,
                itemDefaultsSupported,
                defaultSpan,
                item,
                completionService,
                cancellationToken).ConfigureAwait(false);
        }

        var lspItem = new LSP.CompletionItem() { Label = item.GetEntireDisplayText() };

        if (item.IsComplexTextEdit)
        {
            // For unimported item, we use display text (type or method name) as the text edit text, and rely on resolve handler to add missing import as additional edit.
            // For other complex edit item, we return a no-op edit and rely on resolve handler to compute the actual change and provide the command to apply it.
            var completionChangeNewText = item.Flags.IsExpanded() ? item.DisplayText : typedText;
            PopulateTextEdit(lspItem, completionChangeSpan: defaultSpan, completionChangeNewText, documentText, itemDefaultsSupported, defaultSpan: defaultSpan);
        }
        else
        {
            await GetChangeAndPopulateSimpleTextEditAsync(
                document,
                documentText,
                itemDefaultsSupported,
                defaultSpan,
                item,
                lspItem,
                completionService,
                cancellationToken).ConfigureAwait(false);
        }

        return lspItem;
    }

    private static async Task<LSP.CompletionItem> CreateVsItemAndPopulateTextEditAsync(
        Document document,
        SourceText documentText,
        bool snippetsSupported,
        bool itemDefaultsSupported,
        TextSpan defaultSpan,
        CompletionItem item,
        CompletionService completionService,
        CancellationToken cancellationToken)
    {
        var lspItem = new LSP.VSInternalCompletionItem
        {
            Label = item.GetEntireDisplayText(),
            Icon = new ImageElement(item.Tags.GetFirstGlyph().ToLSPImageId()),
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
            await GetChangeAndPopulateSimpleTextEditAsync(
                document,
                documentText,
                itemDefaultsSupported,
                defaultSpan,
                item,
                lspItem,
                completionService,
                cancellationToken).ConfigureAwait(false);
        }

        return lspItem;
    }

    public static string[] CreateCommitCharacterArrayFromRules(CompletionItemRules rules)
    {
        using var _ = PooledHashSet<char>.GetInstance(out var commitCharacters);
        commitCharacters.AddAll(CompletionRules.Default.DefaultCommitCharacters);
        foreach (var rule in rules.CommitCharacterRules)
        {
            switch (rule.Kind)
            {
                case CharacterSetModificationKind.Add:
                    commitCharacters.UnionWith(rule.Characters);
                    continue;
                case CharacterSetModificationKind.Remove:
                    commitCharacters.ExceptWith(rule.Characters);
                    continue;
                case CharacterSetModificationKind.Replace:
                    commitCharacters.Clear();
                    commitCharacters.AddRange(rule.Characters);
                    break;
            }
        }

        return [.. commitCharacters.Select(c => c.ToString())];
    }

    private sealed class CommitCharacterArrayComparer : IEqualityComparer<ImmutableArray<CharacterSetModificationRule>>
    {
        public static readonly CommitCharacterArrayComparer Instance = new();

        private CommitCharacterArrayComparer()
        {
        }

        public bool Equals([AllowNull] ImmutableArray<CharacterSetModificationRule> x, [AllowNull] ImmutableArray<CharacterSetModificationRule> y)
        {
            if (x == y)
                return true;

            for (var i = 0; i < x.Length; i++)
            {
                var xKind = x[i].Kind;
                var yKind = y[i].Kind;
                if (xKind != yKind)
                {
                    return false;
                }

                var xCharacters = x[i].Characters;
                var yCharacters = y[i].Characters;
                if (xCharacters.Length != yCharacters.Length)
                {
                    return false;
                }

                for (var j = 0; j < xCharacters.Length; j++)
                {
                    if (xCharacters[j] != yCharacters[j])
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        public int GetHashCode([DisallowNull] ImmutableArray<CharacterSetModificationRule> obj)
        {
            var combinedHash = Hash.CombineValues(obj);
            return combinedHash;
        }
    }

    private static void PopulateTextEdit(
        LSP.CompletionItem lspItem,
        TextSpan completionChangeSpan,
        string completionChangeNewText,
        SourceText documentText,
        bool itemDefaultsSupported,
        TextSpan defaultSpan)
    {
        if (itemDefaultsSupported && completionChangeSpan == defaultSpan)
        {
            // We only need to store the new text as the text edit text when it differs from Label.
            if (!lspItem.Label.Equals(completionChangeNewText, StringComparison.Ordinal))
                lspItem.TextEditText = completionChangeNewText;
        }
        else
        {
            lspItem.TextEdit = new LSP.TextEdit()
            {
                NewText = completionChangeNewText,
                Range = ProtocolConversions.TextSpanToRange(completionChangeSpan, documentText),
            };
        }
    }

    private static async Task GetChangeAndPopulateSimpleTextEditAsync(
        Document document,
        SourceText documentText,
        bool itemDefaultsSupported,
        TextSpan defaultSpan,
        CompletionItem item,
        LSP.CompletionItem lspItem,
        CompletionService completionService,
        CancellationToken cancellationToken)
    {
        Contract.ThrowIfTrue(item.IsComplexTextEdit);
        Contract.ThrowIfNull(lspItem.Label);

        var completionChange = await GetCompletionChangeOrDisplayNameInCaseOfExceptionAsync(completionService, document, item, cancellationToken).ConfigureAwait(false);
        var change = completionChange.TextChange;

        // If the change's span is different from default, then the item should be mark as IsComplexTextEdit.
        // But since we don't have a way to enforce this, we'll just check for it here.
        Debug.Assert(change.Span == defaultSpan);
        PopulateTextEdit(lspItem, change.Span, change.NewText ?? string.Empty, documentText, itemDefaultsSupported, defaultSpan);
    }

    public static async Task<LSP.TextEdit[]?> GenerateAdditionalTextEditForImportCompletionAsync(
        CompletionItem selectedItem,
        Document document,
        CompletionService completionService,
        CancellationToken cancellationToken)
    {
        Debug.Assert(selectedItem.Flags.IsExpanded());
        var completionChange = await GetCompletionChangeOrDisplayNameInCaseOfExceptionAsync(completionService, document, selectedItem, cancellationToken).ConfigureAwait(false);

        var sourceText = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);
        using var _ = ArrayBuilder<LSP.TextEdit>.GetInstance(out var builder);
        foreach (var change in completionChange.TextChanges)
        {
            if (change.NewText == selectedItem.DisplayText)
                continue;

            builder.Add(new LSP.TextEdit()
            {
                NewText = change.NewText!,
                Range = ProtocolConversions.TextSpanToRange(change.Span, sourceText),
            });
        }

        return builder.ToArray();
    }

    private static async Task<CompletionChange> GetCompletionChangeOrDisplayNameInCaseOfExceptionAsync(CompletionService completionService, Document document, CompletionItem completionItem, CancellationToken cancellationToken)
    {
        try
        {
            return await completionService.GetChangeAsync(document, completionItem, cancellationToken: cancellationToken).ConfigureAwait(false);
        }
        catch (Exception e) when (FatalError.ReportAndCatchUnlessCanceled(e))
        {
            // In case of exception, we simply return DisplayText with default span as the change.
            return CompletionChange.Create(new TextChange(completionItem.Span, completionItem.DisplayText));
        }
    }

    public static Task<LSP.CompletionItem> ResolveAsync(
        LSP.CompletionItem lspItem,
        CompletionItem roslynItem,
        LSP.TextDocumentIdentifier textDocumentIdentifier,
        Document document,
        CompletionCapabilityHelper capabilityHelper,
        CompletionService completionService,
        CompletionOptions completionOptions,
        SymbolDescriptionOptions symbolDescriptionOptions,
        CancellationToken cancellationToken)
    {
        return capabilityHelper.SupportVSInternalClientCapabilities
            ? VsResolveAsync(lspItem, roslynItem, document, capabilityHelper, completionService, completionOptions, symbolDescriptionOptions, cancellationToken)
            : DefaultResolveAsync(lspItem, roslynItem, textDocumentIdentifier, document, capabilityHelper, completionService, completionOptions, symbolDescriptionOptions, cancellationToken);
    }

    private static async Task<LSP.CompletionItem> DefaultResolveAsync(
        LSP.CompletionItem lspItem,
        CompletionItem roslynItem,
        LSP.TextDocumentIdentifier textDocumentIdentifier,
        Document document,
        CompletionCapabilityHelper capabilityHelper,
        CompletionService completionService,
        CompletionOptions completionOptions,
        SymbolDescriptionOptions symbolDescriptionOptions,
        CancellationToken cancellationToken)
    {
        var description = await completionService.GetDescriptionAsync(
            document, roslynItem, completionOptions, symbolDescriptionOptions, cancellationToken).ConfigureAwait(false);

        if (description != null)
        {
            lspItem.Documentation = ProtocolConversions.GetDocumentationMarkupContent(
                description.TaggedParts, document, capabilityHelper.SupportsMarkdownDocumentation);
        }

        if (roslynItem.IsComplexTextEdit)
        {
            if (roslynItem.Flags.IsExpanded())
            {
                var additionalEdits = await GenerateAdditionalTextEditForImportCompletionAsync(
                    roslynItem, document, completionService, cancellationToken).ConfigureAwait(false);
                lspItem.AdditionalTextEdits = additionalEdits;
            }
            else
            {
                var (textEdit, isSnippetString, newPosition) = await GenerateComplexTextEditAsync(
                    document, completionService, roslynItem, capabilityHelper.SupportSnippets, insertNewPositionPlaceholder: false, cancellationToken).ConfigureAwait(false);

                var lspOffset = newPosition is null ? -1 : newPosition.Value;

                lspItem.Command = lspItem.Command = new LSP.Command()
                {
                    CommandIdentifier = CompleteComplexEditCommand,
                    Title = nameof(CompleteComplexEditCommand),
                    Arguments = [textDocumentIdentifier, textEdit, isSnippetString, lspOffset]
                };
            }
        }

        if (!roslynItem.InlineDescription.IsEmpty())
            lspItem.LabelDetails = new() { Description = roslynItem.InlineDescription };

        return lspItem;
    }

    private static async Task<LSP.CompletionItem> VsResolveAsync(
        LSP.CompletionItem lspItem,
        CompletionItem roslynItem,
        Document document,
        CompletionCapabilityHelper capabilityHelper,
        CompletionService completionService,
        CompletionOptions completionOptions,
        SymbolDescriptionOptions symbolDescriptionOptions,
        CancellationToken cancellationToken)
    {
        var description = await completionService.GetDescriptionAsync(
            document, roslynItem, completionOptions, symbolDescriptionOptions, cancellationToken).ConfigureAwait(false);

        if (description != null)
        {
            var vsCompletionItem = (LSP.VSInternalCompletionItem)lspItem;
            vsCompletionItem.Description = new ClassifiedTextElement(description.TaggedParts
                .Select(tp => new ClassifiedTextRun(tp.Tag.ToClassificationTypeName(), tp.Text)));
        }

        // We compute the TextEdit resolves for complex text edits (e.g. override and partial
        // method completions) here. Lazily resolving TextEdits is technically a violation of
        // the LSP spec, but is currently supported by the VS client anyway. Once the VS client
        // adheres to the spec, this logic will need to change and VS will need to provide
        // official support for TextEdit resolution in some form.
        if (roslynItem.IsComplexTextEdit)
        {
            Contract.ThrowIfTrue(lspItem.InsertText != null);
            Contract.ThrowIfTrue(lspItem.TextEdit != null);

            var sourceText = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);
            var (edit, _, _) = await GenerateComplexTextEditAsync(
                document, completionService, roslynItem, capabilityHelper.SupportSnippets, insertNewPositionPlaceholder: true, cancellationToken).ConfigureAwait(false);

            lspItem.TextEdit = edit;
        }

        return lspItem;
    }

    public static async Task<(LSP.TextEdit edit, bool isSnippetString, int? newPosition)> GenerateComplexTextEditAsync(
        Document document,
        CompletionService completionService,
        CompletionItem selectedItem,
        bool snippetsSupported,
        bool insertNewPositionPlaceholder,
        CancellationToken cancellationToken)
    {
        Debug.Assert(selectedItem.IsComplexTextEdit);

        var completionChange = await GetCompletionChangeOrDisplayNameInCaseOfExceptionAsync(completionService, document, selectedItem, cancellationToken).ConfigureAwait(false);
        var completionChangeSpan = completionChange.TextChange.Span;
        var newText = completionChange.TextChange.NewText;
        Contract.ThrowIfNull(newText);

        var documentText = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);
        var textEdit = new LSP.TextEdit()
        {
            NewText = newText,
            Range = ProtocolConversions.TextSpanToRange(completionChangeSpan, documentText),
        };

        var isSnippetString = false;
        var newPosition = completionChange.NewPosition;

        if (snippetsSupported)
        {
            if (SnippetCompletionItem.IsSnippet(selectedItem)
                && completionChange.Properties.TryGetValue(SnippetCompletionItem.LSPSnippetKey, out var lspSnippetChangeText))
            {
                textEdit.NewText = lspSnippetChangeText;
                isSnippetString = true;
                newPosition = null;
            }
            else if (insertNewPositionPlaceholder)
            {
                var caretPosition = completionChange.NewPosition;
                if (caretPosition.HasValue)
                {
                    // caretPosition is the absolute position of the caret in the document.
                    // We want the position relative to the start of the snippet.
                    var relativeCaretPosition = caretPosition.Value - completionChangeSpan.Start;

                    // The caret could technically be placed outside the bounds of the text
                    // being inserted. This situation is currently unsupported in LSP, so in
                    // these cases we won't move the caret.
                    if (relativeCaretPosition >= 0 && relativeCaretPosition <= newText.Length)
                    {
                        textEdit.NewText = textEdit.NewText.Insert(relativeCaretPosition, "$0");
                    }
                }
            }
        }

        return (textEdit, isSnippetString, newPosition);
    }
}
