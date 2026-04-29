// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.AspNetCore.Razor;
using Microsoft.CodeAnalysis.Razor.Tooltip;

namespace Microsoft.CodeAnalysis.Razor.Completion;

[DebuggerDisplay($"{{{nameof(GetDebuggerDisplay)}(),nq}}")]
internal sealed class RazorCompletionItem
{
    public RazorCompletionItemKind Kind { get; }
    public string DisplayText { get; }
    public string InsertText { get; }

    /// <summary>
    /// A string that is used to alphabetically sort the completion item.
    /// </summary>
    public string SortText { get; }

    public object DescriptionInfo { get; }
    public ImmutableArray<RazorCommitCharacter> CommitCharacters { get; }
    public bool IsSnippet { get; }
    public TextEdit[]? AdditionalTextEdits { get; }

    private string GetDebuggerDisplay()
        => $"{Kind}: {DisplayText}";

    /// <summary>
    /// Creates a new Razor completion item
    /// </summary>
    /// <param name="kind">The type of completion item this is. Used for icons and resolving extra information like tooltip text.</param>
    /// <param name="displayText">The text to display in the completion list.</param>
    /// <param name="insertText">Content to insert when completion item is committed.</param>
    /// <param name="sortText">A string that is used to alphabetically sort the completion item. If omitted defaults to <paramref name="displayText"/>.</param>
    /// <param name="descriptionInfo">An object that provides description information for this completion item.</param>
    /// <param name="commitCharacters">Characters that can be used to commit the completion item.</param>
    /// <param name="isSnippet">Indicates whether the completion item's <see cref="InsertText"/> is an LSP snippet or not.</param>
    /// <param name="additionalTextEdits">Additional text edits to apply when the completion is committed.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="displayText"/> or <paramref name="insertText"/> are <see langword="null"/>.</exception>
    private RazorCompletionItem(
        RazorCompletionItemKind kind,
        string displayText,
        string insertText,
        string? sortText,
        object descriptionInfo,
        ImmutableArray<RazorCommitCharacter> commitCharacters,
        bool isSnippet,
        TextEdit[]? additionalTextEdits = null)
    {
        ArgHelper.ThrowIfNull(displayText);
        ArgHelper.ThrowIfNull(insertText);

        Kind = kind;
        DisplayText = displayText;
        InsertText = insertText;
        SortText = sortText ?? displayText;
        DescriptionInfo = descriptionInfo;
        CommitCharacters = commitCharacters.NullToEmpty();
        IsSnippet = isSnippet;
        AdditionalTextEdits = additionalTextEdits;
    }

    public static RazorCompletionItem CreateDirective(
        string displayText, string insertText, string? sortText,
        DirectiveCompletionDescription descriptionInfo,
        ImmutableArray<RazorCommitCharacter> commitCharacters, bool isSnippet)
        => new(RazorCompletionItemKind.Directive, displayText, insertText, sortText, descriptionInfo, commitCharacters, isSnippet);

    public static RazorCompletionItem CreateDirectiveAttribute(
        string displayText, string insertText,
        AggregateBoundAttributeDescription descriptionInfo,
        ImmutableArray<RazorCommitCharacter> commitCharacters,
        bool isSnippet)
        => new(RazorCompletionItemKind.DirectiveAttribute, displayText, insertText, sortText: null, descriptionInfo, commitCharacters, isSnippet);

    public static RazorCompletionItem CreateDirectiveAttributeParameter(
        string displayText, string insertText,
        AggregateBoundAttributeDescription descriptionInfo,
        ImmutableArray<RazorCommitCharacter> commitCharacters,
        bool isSnippet)
        => new(RazorCompletionItemKind.DirectiveAttributeParameter, displayText, insertText, sortText: null, descriptionInfo, commitCharacters, isSnippet);

    public static RazorCompletionItem CreateMarkupTransition(
        string displayText, string insertText,
        MarkupTransitionCompletionDescription descriptionInfo,
        ImmutableArray<RazorCommitCharacter> commitCharacters)
        => new(RazorCompletionItemKind.MarkupTransition, displayText, insertText, sortText: null, descriptionInfo, commitCharacters, isSnippet: false);

    public static RazorCompletionItem CreateTagHelperElement(
        string displayText, string insertText,
        AggregateBoundElementDescription descriptionInfo,
        ImmutableArray<RazorCommitCharacter> commitCharacters,
        bool isSnippet = false,
        TextEdit[]? additionalTextEdits = null)
        => new(RazorCompletionItemKind.TagHelperElement, displayText, insertText, sortText: null, descriptionInfo, commitCharacters, isSnippet, additionalTextEdits);

    public static RazorCompletionItem CreateTagHelperAttribute(
        string displayText, string insertText, string? sortText,
        AggregateBoundAttributeDescription descriptionInfo,
        ImmutableArray<RazorCommitCharacter> commitCharacters, bool isSnippet)
        => new(RazorCompletionItemKind.TagHelperAttribute, displayText, insertText, sortText, descriptionInfo, commitCharacters, isSnippet);

    public static RazorCompletionItem CreateDirectiveAttributeEventParameterHtmlEventValue(
        string displayText, string insertText,
        ImmutableArray<RazorCommitCharacter> commitCharacters)
        => new(RazorCompletionItemKind.DirectiveAttributeParameterEventValue, displayText, insertText, sortText: null, descriptionInfo: AggregateBoundAttributeDescription.Empty, commitCharacters, isSnippet: false);

    public static RazorCompletionItem CreateAttribute(
        string displayText, string insertText,
        AttributeDescriptionInfo descriptionInfo,
        ImmutableArray<RazorCommitCharacter> commitCharacters, bool isSnippet)
        => new(RazorCompletionItemKind.Attribute, displayText, insertText, sortText: null, descriptionInfo, commitCharacters, isSnippet);

    public static RazorCompletionItem CreateKeyword(
        string displayText, string insertText,
        ImmutableArray<RazorCommitCharacter> commitCharacters)
        => new(RazorCompletionItemKind.CSharpRazorKeyword, displayText, insertText, sortText: null, new CSharpRazorKeywordCompletionDescription(displayText), commitCharacters, isSnippet: false);
}
