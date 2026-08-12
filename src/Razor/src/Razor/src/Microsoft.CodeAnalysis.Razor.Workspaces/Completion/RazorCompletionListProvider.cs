// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using Microsoft.AspNetCore.Razor;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Language.Syntax;
using Microsoft.AspNetCore.Razor.PooledObjects;
using Microsoft.CodeAnalysis.Razor.Logging;
using Microsoft.CodeAnalysis.Text;
using RoslynPatternMatch = Microsoft.CodeAnalysis.PatternMatching.PatternMatch;

namespace Microsoft.CodeAnalysis.Razor.Completion;

internal class RazorCompletionListProvider(
    IRazorCompletionFactsService completionFactsService,
    CompletionListCache completionListCache,
    ILoggerFactory loggerFactory)
{
    private readonly IRazorCompletionFactsService _completionFactsService = completionFactsService;
    private readonly CompletionListCache _completionListCache = completionListCache;
    private readonly ILogger _logger = loggerFactory.GetOrCreateLogger<RazorCompletionListProvider>();
    private static readonly Command s_retriggerCompletionCommand = new()
    {
        CommandIdentifier = "editor.action.triggerSuggest",
        Title = SR.ReTrigger_Completions_Title,
    };

    // virtual for tests
    public virtual RazorVSInternalCompletionList? GetCompletionList(
        RazorCompletionContext razorCompletionContext,
        VSInternalClientCapabilities clientCapabilities)
    {
        var result = _completionFactsService.GetCompletionItems(razorCompletionContext);

        _logger.LogTrace($"Resolved {result.Length} completion items.");

        if (result.Length == 0)
        {
            return null;
        }

        return CreateAndCacheCompletionList(razorCompletionContext.CodeDocument, result, clientCapabilities, razorCompletionContext.AbsoluteIndex);
    }

    internal static RazorCompletionContext CreateCompletionContext(
        RazorCodeDocument codeDocument,
        int absoluteIndex,
        VSInternalCompletionContext completionContext,
        RazorCompletionOptions completionOptions)
    {
        var reason = completionContext.TriggerKind switch
        {
            CompletionTriggerKind.TriggerForIncompleteCompletions => CompletionReason.Invoked,
            CompletionTriggerKind.Invoked => CompletionReason.Invoked,
            CompletionTriggerKind.TriggerCharacter => CompletionReason.Typing,
            _ => CompletionReason.Typing,
        };

        var syntaxTree = codeDocument.GetRequiredTagHelperRewrittenSyntaxTree();
        var tagHelperContext = codeDocument.GetRequiredTagHelperContext();

        var owner = syntaxTree.Root.FindInnermostNode(absoluteIndex, includeWhitespace: true, walkMarkersBack: true);
        owner = AbstractRazorCompletionFactsService.AdjustSyntaxNodeForWordBoundary(owner, absoluteIndex);

        return new RazorCompletionContext(
            codeDocument,
            absoluteIndex,
            owner,
            syntaxTree,
            tagHelperContext,
            reason,
            completionOptions);
    }

    private RazorVSInternalCompletionList CreateAndCacheCompletionList(
        RazorCodeDocument codeDocument,
        ImmutableArray<RazorCompletionItem> razorCompletionItems,
        VSInternalClientCapabilities clientCapabilities,
        int absoluteIndex)
    {
        var completionList = CreateLSPCompletionList(razorCompletionItems, clientCapabilities);

        FilterCompletionItems(completionList, codeDocument.Source.Text, absoluteIndex);

        // The completion list is cached and can be retrieved via this result id to enable the resolve completion functionality.
        var filePath = codeDocument.Source.FilePath.AssumeNotNull();
        var razorResolveContext = new RazorCompletionResolveContext(filePath, razorCompletionItems);
        var resultId = _completionListCache.Add(completionList, razorResolveContext);
        completionList.SetResultId(resultId, clientCapabilities);

        return completionList;
    }

    // Internal for benchmarking and testing
    internal static RazorVSInternalCompletionList CreateLSPCompletionList(
        ImmutableArray<RazorCompletionItem> razorCompletionItems,
        VSInternalClientCapabilities clientCapabilities)
    {
        using var items = new PooledArrayBuilder<VSInternalCompletionItem>();

        foreach (var razorCompletionItem in razorCompletionItems)
        {
            if (TryConvert(razorCompletionItem, clientCapabilities, out var completionItem))
            {
                items.Add(completionItem);
            }
        }

        var completionList = new RazorVSInternalCompletionList()
        {
            Items = items.ToArray(),
            IsIncomplete = false,
        };

        var completionCapability = clientCapabilities.TextDocument?.Completion;

        return CompletionListOptimizer.Optimize(completionList, completionCapability);
    }

    // Internal for testing
    internal static bool TryConvert(
        RazorCompletionItem razorCompletionItem,
        VSInternalClientCapabilities clientCapabilities,
        [NotNullWhen(true)] out VSInternalCompletionItem? completionItem)
    {
        ArgHelper.ThrowIfNull(razorCompletionItem);

        var tagHelperCompletionItemKind = CompletionItemKind.TypeParameter;
        var supportedItemKinds = clientCapabilities.TextDocument?.Completion?.CompletionItemKind?.ValueSet ?? [];
        if (supportedItemKinds?.Contains(CompletionItemKind.TagHelper) == true)
        {
            tagHelperCompletionItemKind = CompletionItemKind.TagHelper;
        }

        var insertTextFormat = razorCompletionItem.IsSnippet ? InsertTextFormat.Snippet : InsertTextFormat.Plaintext;

        switch (razorCompletionItem.Kind)
        {
            case RazorCompletionItemKind.Directive:
                {
                    var directiveCompletionItem = new VSInternalCompletionItem()
                    {
                        Label = razorCompletionItem.DisplayText,
                        InsertText = razorCompletionItem.InsertText,
                        FilterText = razorCompletionItem.DisplayText,
                        SortText = razorCompletionItem.SortText,
                        InsertTextFormat = insertTextFormat,
                        Kind = razorCompletionItem.IsSnippet ? CompletionItemKind.Snippet : CompletionItemKind.Keyword,
                        AdditionalTextEdits = razorCompletionItem.AdditionalTextEdits,
                    };

                    directiveCompletionItem.UseCommitCharactersFrom(razorCompletionItem, clientCapabilities);

                    if (DirectiveAttributeTransitionCompletionItemProvider.IsTransitionCompletionItem(razorCompletionItem))
                    {
                        directiveCompletionItem.Command = s_retriggerCompletionCommand;
                        directiveCompletionItem.Kind = tagHelperCompletionItemKind;
                    }

                    completionItem = directiveCompletionItem;
                    return true;
                }
            case RazorCompletionItemKind.DirectiveAttribute:
            case RazorCompletionItemKind.DirectiveAttributeParameter:
                {
                    completionItem = CreateDirectiveAttributeCompletionItem(
                        razorCompletionItem, clientCapabilities, insertTextFormat, tagHelperCompletionItemKind);
                    return true;
                }
            case RazorCompletionItemKind.DirectiveAttributeParameterEventValue:
                {
                    var eventValueCompletionItem = new VSInternalCompletionItem()
                    {
                        Label = razorCompletionItem.DisplayText,
                        InsertText = razorCompletionItem.InsertText,
                        FilterText = razorCompletionItem.InsertText,
                        SortText = razorCompletionItem.SortText,
                        InsertTextFormat = insertTextFormat,
                        Kind = CompletionItemKind.Event,
                        AdditionalTextEdits = razorCompletionItem.AdditionalTextEdits,
                    };

                    eventValueCompletionItem.UseCommitCharactersFrom(razorCompletionItem, clientCapabilities);

                    completionItem = eventValueCompletionItem;
                    return true;
                }
            case RazorCompletionItemKind.MarkupTransition:
                {
                    var markupTransitionCompletionItem = new VSInternalCompletionItem()
                    {
                        Label = razorCompletionItem.DisplayText,
                        InsertText = razorCompletionItem.InsertText,
                        FilterText = razorCompletionItem.DisplayText,
                        SortText = razorCompletionItem.SortText,
                        InsertTextFormat = insertTextFormat,
                        Kind = tagHelperCompletionItemKind,
                        AdditionalTextEdits = razorCompletionItem.AdditionalTextEdits,
                    };

                    markupTransitionCompletionItem.UseCommitCharactersFrom(razorCompletionItem, clientCapabilities);

                    completionItem = markupTransitionCompletionItem;
                    return true;
                }
            case RazorCompletionItemKind.TagHelperElement:
                {
                    var tagHelperElementCompletionItem = new VSInternalCompletionItem()
                    {
                        Label = razorCompletionItem.DisplayText,
                        InsertText = razorCompletionItem.InsertText,
                        FilterText = razorCompletionItem.DisplayText,
                        SortText = razorCompletionItem.SortText,
                        InsertTextFormat = insertTextFormat,
                        Kind = tagHelperCompletionItemKind,
                        AdditionalTextEdits = razorCompletionItem.AdditionalTextEdits,
                    };

                    tagHelperElementCompletionItem.UseCommitCharactersFrom(razorCompletionItem, clientCapabilities);

                    completionItem = tagHelperElementCompletionItem;
                    return true;
                }
            case RazorCompletionItemKind.TagHelperAttribute:
                {
                    var tagHelperAttributeCompletionItem = new VSInternalCompletionItem()
                    {
                        Label = razorCompletionItem.DisplayText,
                        InsertText = razorCompletionItem.InsertText,
                        FilterText = razorCompletionItem.DisplayText,
                        SortText = razorCompletionItem.SortText,
                        InsertTextFormat = insertTextFormat,
                        Kind = tagHelperCompletionItemKind,
                        AdditionalTextEdits = razorCompletionItem.AdditionalTextEdits,
                    };

                    tagHelperAttributeCompletionItem.UseCommitCharactersFrom(razorCompletionItem, clientCapabilities);

                    completionItem = tagHelperAttributeCompletionItem;
                    return true;
                }
            case RazorCompletionItemKind.Attribute:
                {
                    var attributeCompletionItem = new VSInternalCompletionItem()
                    {
                        Label = razorCompletionItem.DisplayText,
                        InsertText = razorCompletionItem.InsertText,
                        FilterText = razorCompletionItem.DisplayText,
                        SortText = razorCompletionItem.SortText,
                        InsertTextFormat = insertTextFormat,
                        Kind = CompletionItemKind.Property,
                    };

                    attributeCompletionItem.UseCommitCharactersFrom(razorCompletionItem, clientCapabilities);

                    completionItem = attributeCompletionItem;
                    return true;
                }
            case RazorCompletionItemKind.CSharpRazorKeyword:
                {
                    var csharpRazorKeywordCompletionItem = new VSInternalCompletionItem()
                    {
                        Label = razorCompletionItem.DisplayText,
                        InsertText = razorCompletionItem.InsertText,
                        FilterText = razorCompletionItem.DisplayText,
                        SortText = razorCompletionItem.SortText,
                        InsertTextFormat = insertTextFormat,
                        Kind = CompletionItemKind.Keyword,
                    };

                    csharpRazorKeywordCompletionItem.UseCommitCharactersFrom(razorCompletionItem, clientCapabilities);

                    completionItem = csharpRazorKeywordCompletionItem;
                    return true;
                }
        }

        completionItem = null;
        return false;
    }

    /// <summary>
    /// Creates a completion item for directive attribute and directive attribute parameter kinds.
    /// When a <see cref="RazorCompletionItem.ReplacementRange"/> is set, uses an explicit TextEdit
    /// to define the replaced range. This prevents duplication when the InsertText contains a ':'
    /// (e.g., "bind-Value:after") because the editor's word-boundary heuristic would stop at ':'
    /// and only replace part of the existing text.
    /// </summary>
    private static VSInternalCompletionItem CreateDirectiveAttributeCompletionItem(
        RazorCompletionItem razorCompletionItem,
        VSInternalClientCapabilities clientCapabilities,
        InsertTextFormat insertTextFormat,
        CompletionItemKind kind)
    {
        var completionItem = new VSInternalCompletionItem()
        {
            Label = razorCompletionItem.DisplayText,
            InsertText = razorCompletionItem.InsertText,
            FilterText = razorCompletionItem.DisplayText,
            SortText = razorCompletionItem.SortText,
            InsertTextFormat = insertTextFormat,
            Kind = kind,
            AdditionalTextEdits = razorCompletionItem.AdditionalTextEdits,
        };

        if (razorCompletionItem.ReplacementRange is { } replacementRange)
        {
            // The replacement range includes '@', and InsertText also includes '@'.
            // For simple items (non-snippet, non-indexer), InsertText == DisplayText == Label,
            // so we use it directly — zero allocations, and the optimizer can omit TextEditText
            // when NewText equals Label.
            completionItem.TextEdit = new TextEdit()
            {
                Range = replacementRange.ToRange(),
                NewText = razorCompletionItem.InsertText,
            };
            completionItem.InsertText = null;
        }

        completionItem.UseCommitCharactersFrom(razorCompletionItem, clientCapabilities);

        return completionItem;
    }

    private static void FilterCompletionItems(
        RazorVSInternalCompletionList completionList,
        SourceText sourceText,
        int absoluteIndex,
        int maxCompletionListSize = 1000)
    {
        // If our completion list hasn't hit the max size, we don't need to filter
        if (completionList.Items.Length <= maxCompletionListSize)
        {
            return;
        }

        var filterText = GetFilterText(sourceText, absoluteIndex);

        // Pattern match all items against filter text (like C# does in CompletionHandler.FilterCompletionList)
        using var matchResults = new PooledArrayBuilder<(VSInternalCompletionItem Item, int Index, RoslynPatternMatch? Match)>(completionList.Items.Length);
        AddMatchResults(completionList, filterText, ref matchResults.AsRef());

        // Take top maxCompletionListSize items (like C# does)
        var takeCount = Math.Min(maxCompletionListSize, matchResults.Count);
        var preselectCount = 0;

        // Count preselected items beyond takeCount (like C# does)
        for (var i = takeCount; i < matchResults.Count; i++)
        {
            if (matchResults[i].Item.Preselect)
            {
                preselectCount++;
            }
        }

        var result = new VSInternalCompletionItem[takeCount + preselectCount];

        // Add the first takeCount items
        for (var i = 0; i < takeCount; i++)
        {
            result[i] = matchResults[i].Item;
        }

        // Add preselected items beyond takeCount (like C# does)
        if (preselectCount > 0)
        {
            var resultIndex = takeCount;
            for (var i = takeCount; i < matchResults.Count; i++)
            {
                var matchResult = matchResults[i];
                if (matchResult.Item.Preselect)
                {
                    result[resultIndex++] = matchResult.Item;
                }
            }
        }

        completionList.Items = result;
        completionList.IsIncomplete = true;

        return;

        static string GetFilterText(SourceText sourceText, int absoluteIndex)
        {
            // Clamp to valid range to avoid IndexOutOfRangeException
            var end = Math.Min(absoluteIndex, sourceText.Length);
            var start = end;
            while (start > 0 && IsWordCharacter(sourceText[start - 1]))
            {
                start--;
            }

            return (start < end)
                ? sourceText.ToString(new TextSpan(start, end - start))
                : string.Empty;

            static bool IsWordCharacter(char ch)
            {
                return char.IsLetterOrDigit(ch) || ch == '_' || ch == '-' || ch == ':' || ch == '@';
            }
        }

        static void AddMatchResults(RazorVSInternalCompletionList completionList, string filterText, ref PooledArrayBuilder<(VSInternalCompletionItem Item, int Index, RoslynPatternMatch? Match)> matchResults)
        {
            using var helper = string.IsNullOrEmpty(filterText) ? null : new Microsoft.CodeAnalysis.Completion.PatternMatchHelper(filterText);

            for (var i = 0; i < completionList.Items.Length; i++)
            {
                var item = completionList.Items[i];

                RoslynPatternMatch? bestMatch = null;
                if (helper != null)
                {
                    var itemText = item.FilterText ?? item.Label;
                    bestMatch = helper.GetMatch(itemText, includeMatchSpans: false, CultureInfo.CurrentCulture);
                }

                matchResults.Add((item, i, bestMatch));
            }

            // Sort by match quality, then alphabetically, then original order
            matchResults.Sort(static (a, b) =>
            {
                if (a.Match.HasValue != b.Match.HasValue)
                {
                    // Items with matches come before items without matches
                    return a.Match.HasValue ? -1 : 1;
                }

                if (a.Match.HasValue && b.Match.HasValue)
                {
                    // Both have matches - compare match quality
                    var matchComparison = a.Match.Value.CompareTo(b.Match.Value);
                    if (matchComparison != 0)
                    {
                        return matchComparison;
                    }
                }

                // Equal match quality - sort by SortText, or Label if there is no SortText
                var aSortText = a.Item.SortText ?? a.Item.Label;
                var bSortText = b.Item.SortText ?? b.Item.Label;
                var sortComparison = string.Compare(aSortText, bSortText, StringComparison.OrdinalIgnoreCase);
                if (sortComparison != 0)
                {
                    return sortComparison;
                }

                // Preserve original order for equal items
                return a.Index.CompareTo(b.Index);
            });
        }
    }

    internal readonly struct TestAccessor
    {
        public static void FilterCompletionItems(
            RazorVSInternalCompletionList completionList,
            SourceText sourceText,
            int absoluteIndex,
            int maxCompletionListSize = 1000)
            => RazorCompletionListProvider.FilterCompletionItems(completionList, sourceText, absoluteIndex, maxCompletionListSize);
    }
}
