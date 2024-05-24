// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Linq;
using System.Threading;

namespace Microsoft.CodeAnalysis.Completion;

/// <summary>
/// The description of a <see cref="CompletionItem"/>.
/// </summary>
public sealed class CompletionDescription
{
    private string? _text;

    /// <summary>
    /// The <see cref="CompletionDescription"/> used when there is no description.
    /// </summary>
    public static readonly CompletionDescription Empty = new([]);

    /// <summary>
    /// The individual tagged parts of the description.
    /// </summary>
    public ImmutableArray<TaggedText> TaggedParts { get; }

    private CompletionDescription(ImmutableArray<TaggedText> taggedParts)
        => TaggedParts = taggedParts.NullToEmpty();

    /// <summary>
    /// Creates a new instance of <see cref="CompletionDescription"/> with the specified <see cref="TaggedText"/> parts.
    /// </summary>
    /// <param name="taggedParts">The individual tagged parts of the description.</param>
    public static CompletionDescription Create(ImmutableArray<TaggedText> taggedParts)
        => new(taggedParts);

    /// <summary>
    /// Creates a new instance of <see cref="CompletionDescription"/> from untagged text.
    /// </summary>
    public static CompletionDescription FromText(string text)
        => new([new TaggedText(TextTags.Text, text)]);

    /// <summary>
    /// Creates a copy of this <see cref="CompletionDescription"/> with the <see cref="TaggedParts"/> property changed.
    /// </summary>
    public CompletionDescription WithTaggedParts(ImmutableArray<TaggedText> taggedParts)
    {
        if (taggedParts != TaggedParts)
        {
            return new CompletionDescription(taggedParts);
        }
        else
        {
            return this;
        }
    }

    /// <summary>
    /// The text of the description without tags.
    /// </summary>
    public string Text
    {
        get
        {
            if (_text == null)
            {
                Interlocked.CompareExchange(ref _text, string.Concat(TaggedParts.Select(p => p.Text)), null);
            }

            return _text;
        }
    }
}
