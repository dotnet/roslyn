// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.AspNetCore.Razor;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Language.Syntax;
using Microsoft.AspNetCore.Razor.PooledObjects;
using Microsoft.CodeAnalysis.Razor.Logging;

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
    public virtual (RazorVSInternalCompletionList? CompletionList, bool NeedsHtmlDependentPhase) GetCompletionList(
        RazorCodeDocument codeDocument,
        int absoluteIndex,
        VSInternalCompletionContext completionContext,
        VSInternalClientCapabilities clientCapabilities,
        RazorCompletionOptions completionOptions)
    {
        var razorCompletionContext = CreateCompletionContext(codeDocument, absoluteIndex, completionContext, completionOptions);

        var result = _completionFactsService.GetCompletionItems(razorCompletionContext);

        _logger.LogTrace($"Resolved {result.Items.Length} completion items.");

        if (result.Items.Length == 0)
        {
            return (null, result.NeedsHtmlDependentCompletionItems);
        }

        var completionList = CreateAndCacheCompletionList(codeDocument, result.Items, clientCapabilities);
        return (completionList, result.NeedsHtmlDependentCompletionItems);
    }

    // virtual for tests
    public virtual RazorVSInternalCompletionList? GetHtmlDependentCompletionList(
        RazorCodeDocument codeDocument,
        int absoluteIndex,
        VSInternalCompletionContext completionContext,
        VSInternalClientCapabilities clientCapabilities,
        RazorCompletionOptions completionOptions,
        HashSet<string> htmlLabels)
    {
        var baseContext = CreateCompletionContext(codeDocument, absoluteIndex, completionContext, completionOptions);
        var razorCompletionContext = new RazorHtmlDependentCompletionContext(baseContext, htmlLabels);

        var razorCompletionItems = _completionFactsService.GetHtmlDependentCompletionItems(razorCompletionContext);

        _logger.LogTrace($"Resolved {razorCompletionItems.Length} HTML-dependent completion items.");

        if (razorCompletionItems.Length == 0)
        {
            return null;
        }

        return CreateAndCacheCompletionList(codeDocument, razorCompletionItems, clientCapabilities);
    }

    private static RazorCompletionContext CreateCompletionContext(
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
        VSInternalClientCapabilities clientCapabilities)
    {
        var completionList = CreateLSPCompletionList(razorCompletionItems, clientCapabilities);

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

        var completionCapability = clientCapabilities.TextDocument?.Completion as VSInternalCompletionSetting;

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
                {
                    var directiveAttributeCompletionItem = new VSInternalCompletionItem()
                    {
                        Label = razorCompletionItem.DisplayText,
                        InsertText = razorCompletionItem.InsertText,
                        FilterText = razorCompletionItem.InsertText,
                        SortText = razorCompletionItem.SortText,
                        InsertTextFormat = insertTextFormat,
                        Kind = tagHelperCompletionItemKind,
                        AdditionalTextEdits = razorCompletionItem.AdditionalTextEdits,
                    };

                    directiveAttributeCompletionItem.UseCommitCharactersFrom(razorCompletionItem, clientCapabilities);

                    completionItem = directiveAttributeCompletionItem;
                    return true;
                }
            case RazorCompletionItemKind.DirectiveAttributeParameter:
                {
                    var parameterCompletionItem = new VSInternalCompletionItem()
                    {
                        Label = razorCompletionItem.DisplayText,
                        InsertText = razorCompletionItem.InsertText,
                        FilterText = razorCompletionItem.InsertText,
                        SortText = razorCompletionItem.SortText,
                        InsertTextFormat = insertTextFormat,
                        Kind = tagHelperCompletionItemKind,
                        AdditionalTextEdits = razorCompletionItem.AdditionalTextEdits,
                    };

                    parameterCompletionItem.UseCommitCharactersFrom(razorCompletionItem, clientCapabilities);

                    completionItem = parameterCompletionItem;
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
}
