// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Completion;

/// <summary>
/// The change to be applied to the document when a <see cref="CompletionItem"/> is committed.
/// </summary>
public sealed class CompletionChange
{
    /// <summary>
    /// The text change to be applied to the document.  This must always be supplied and is useful for hosts that
    /// can apply a large text change efficiently while only making minimal edits to a file.
    /// </summary>
    public TextChange TextChange { get; }

    /// <summary>
    /// Individual smaller text changes that are more fine grained than the total <see cref="TextChange"/> value.
    /// This can be useful for host that do not support diffing changes to find minimal edits.  Even if this is 
    /// provided, <see cref="TextChange"/> must still be provided as well.
    /// </summary>
    public ImmutableArray<TextChange> TextChanges { get; }

    /// <summary>
    /// The new caret position after the change has been applied.
    /// If null then the new caret position will be determined by the completion host.
    /// </summary>
    public int? NewPosition { get; }

    /// <summary>
    /// True if the changes include the typed character that caused the <see cref="CompletionItem"/>
    /// to be committed.  If false the completion host will determine if and where the commit 
    /// character is inserted into the document.
    /// </summary>
    public bool IncludesCommitCharacter { get; }

    internal ImmutableDictionary<string, string> Properties { get; }

    private CompletionChange(
        TextChange textChange, ImmutableArray<TextChange> textChanges, int? newPosition, bool includesCommitCharacter)
        : this(textChange, textChanges, newPosition, includesCommitCharacter, ImmutableDictionary<string, string>.Empty)
    {
    }

    private CompletionChange(
        TextChange textChange, ImmutableArray<TextChange> textChanges, int? newPosition, bool includesCommitCharacter, ImmutableDictionary<string, string> properties)
    {
        TextChange = textChange;
        NewPosition = newPosition;
        IncludesCommitCharacter = includesCommitCharacter;
        TextChanges = textChanges.NullToEmpty();
        if (TextChanges.IsEmpty)
            TextChanges = [textChange];
        Properties = properties;
    }

    /// <summary>
    /// Creates a new <see cref="CompletionChange"/> instance.
    /// </summary>
    /// <param name="textChanges">The text changes to be applied to the document.</param>
    /// <param name="newPosition">The new caret position after the change has been applied. If null then the caret
    /// position is not specified and will be determined by the completion host.</param>
    /// <param name="includesCommitCharacter">True if the changes include the typed character that caused the <see
    /// cref="CompletionItem"/> to be committed. If false, the completion host will determine if and where the
    /// commit character is inserted into the document.</param>
    /// <remarks>
    /// This factory method is only valid when <paramref name="textChanges"/> has a single entry in it.  If there
    /// are multiple entries, <see cref="Create(TextChange, ImmutableArray{TextChange}, int?, bool)"/> must be called instead,
    /// with both the individual text changes, and an aggregated text change for hosts that only support that.
    /// </remarks>
    [Obsolete("Use Create overload that takes a single TextChange and multiple TextChanges instead", error: true)]
    public static CompletionChange Create(
        ImmutableArray<TextChange> textChanges,
        int? newPosition = null,
        bool includesCommitCharacter = false)
    {
        return new CompletionChange(textChanges.Single(), textChanges, newPosition, includesCommitCharacter);
    }

#pragma warning disable RS0027 // Public API with optional parameter(s) should have the most parameters amongst its public overloads
    public static CompletionChange Create(
#pragma warning restore RS0027 // Public API with optional parameter(s) should have the most parameters amongst its public overloads
        TextChange textChange,
        int? newPosition = null,
        bool includesCommitCharacter = false)
    {
        return new CompletionChange(textChange, textChanges: default, newPosition, includesCommitCharacter);
    }

#pragma warning disable RS0027 // Public API with optional parameter(s) should have the most parameters amongst its public overloads.
#pragma warning disable RS0026 // Do not add multiple public overloads with optional parameters
    public static CompletionChange Create(
#pragma warning restore RS0026 // Do not add multiple public overloads with optional parameters
#pragma warning restore RS0027 // Public API with optional parameter(s) should have the most parameters amongst its public overloads.
        TextChange textChange,
        ImmutableArray<TextChange> textChanges,
        int? newPosition = null,
        bool includesCommitCharacter = false)
    {
        return new CompletionChange(textChange, textChanges, newPosition, includesCommitCharacter);
    }

    internal static CompletionChange Create(
        TextChange textChange,
        ImmutableArray<TextChange> textChanges,
        ImmutableDictionary<string, string> properties,
        int? newPosition,
        bool includesCommitCharacter)
    {
        return new CompletionChange(textChange, textChanges, newPosition, includesCommitCharacter, properties);
    }

    /// <summary>
    /// Creates a copy of this <see cref="CompletionChange"/> with the <see cref="TextChange"/> property changed.
    /// </summary>
    public CompletionChange WithTextChange(TextChange textChange)
        => new(textChange, TextChanges, NewPosition, IncludesCommitCharacter);

    /// <summary>
    /// Creates a copy of this <see cref="CompletionChange"/> with the <see cref="TextChanges"/> property changed.
    /// </summary>
    public CompletionChange WithTextChanges(ImmutableArray<TextChange> textChanges)
        => new(TextChange, textChanges, NewPosition, IncludesCommitCharacter);

    /// <summary>
    /// Creates a copy of this <see cref="CompletionChange"/> with the <see cref="NewPosition"/> property changed.
    /// </summary>
    public CompletionChange WithNewPosition(int? newPostion)
        => new(TextChange, TextChanges, newPostion, IncludesCommitCharacter);

    /// <summary>
    /// Creates a copy of this <see cref="CompletionChange"/> with the <see cref="IncludesCommitCharacter"/> property changed.
    /// </summary>
    public CompletionChange WithIncludesCommitCharacter(bool includesCommitCharacter)
        => new(TextChange, TextChanges, NewPosition, includesCommitCharacter);
}
