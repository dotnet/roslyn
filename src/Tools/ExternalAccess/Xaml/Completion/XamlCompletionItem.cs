// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Text.Adornments;

namespace Microsoft.CodeAnalysis.ExternalAccess.Xaml.Completion;

/// <summary>
/// Represents a single completion item.
/// </summary>
internal class XamlCompletionItem(string displayText)
{
    /// <summary>
    /// Gets or sets commit characters associated with the completion item.
    /// </summary>
    public string[]? CommitCharacters { get; set; }

    /// <summary>
    /// Gets or sets XAML specialized commit characters associated with the completion item.
    /// </summary>
    public XamlCommitCharacters? XamlCommitCharacters { get; set; }

    /// <summary>
    /// Gets the display text of the completion item.
    /// </summary>
    public string DisplayText { get; } = displayText;

    /// <summary>
    /// Gets or sets the text to be inserted when the item is committed. If none is provided the display <see cref="DisplayText"/> will be used.
    /// </summary>
    public string? InsertText { get; set; }

    /// <summary>
    /// Gets or sets a short description of the completion item.
    /// </summary>
    public string? Detail { get; set; }

    /// <summary>
    /// Gets or sets the text used to filter the completion item.
    /// </summary>
    public string? FilterText { get; set; }

    /// <summary>
    /// Gets or sets the text used to sort the item.
    /// </summary>
    public string? SortText { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the item should be preselected in the list of completions.
    /// </summary>
    public bool? Preselect { get; set; }

    /// <summary>
    /// Gets or sets the text span within the document where the completion item should be inserted.
    /// </summary>
    public TextSpan? Span { get; set; }

    /// <summary>
    /// Gets or sets the kind of XAML completion item.
    /// </summary>
    public XamlCompletionKind Kind { get; set; }

    /// <summary>
    /// Gets or sets the description of the completion item.
    /// </summary>
    public ClassifiedTextElement? Description { get; set; }

    /// <summary>
    /// Gets or sets the icon of the completion item.
    /// </summary>
    public ImageElement? Icon { get; set; }

    /// <summary>
    /// Gets or sets the <see cref="ISymbol"/> that is associated with the completion item.
    /// </summary>
    public ISymbol? Symbol { get; set; }

    /// <summary>
    /// Gets or sets a XAML event description for event completions.
    /// </summary>
    public XamlEventDescription? EventDescription { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the completing the item should retrigger completion.
    /// </summary>
    public bool? RetriggerCompletion { get; set; }

    /// <summary>
    /// Gets or sets whether the item represents a snippet.
    /// </summary>
    public bool? IsSnippet { get; set; }
}
