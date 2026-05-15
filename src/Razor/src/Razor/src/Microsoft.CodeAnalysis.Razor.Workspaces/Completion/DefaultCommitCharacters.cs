// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.Razor.Completion;

/// <summary>
/// Single source of truth for completion commit character arrays. All completion providers
/// (HTML, directive attribute, TagHelper) use these shared arrays to ensure reference-equal
/// instances, enabling <see cref="CompletionListOptimizer"/> to promote them to list-level
/// defaults via ReferenceEquals grouping. Using the same static <see cref="ImmutableArray{T}"/>
/// fields also ensures cache hits in <see cref="RazorCommitCharacter.ToVsCommitCharacters"/>,
/// producing identical output references across providers.
/// </summary>
internal static class DefaultCommitCharacters
{
    // Element commit characters: space and/or '>'
    private static readonly ImmutableArray<RazorCommitCharacter> s_elementCommitCharacters = RazorCommitCharacter.CreateArray([" ", ">"]);
    private static readonly ImmutableArray<RazorCommitCharacter> s_elementCommitCharactersWithoutSpace = RazorCommitCharacter.CreateArray([">"]);

    // Attribute commit characters: '=' and/or space, both with Insert=false
    private static readonly ImmutableArray<RazorCommitCharacter> s_attributeEqualsCommitCharacters = [new("=", Insert: false)];
    private static readonly ImmutableArray<RazorCommitCharacter> s_attributeEqualsSpaceCommitCharacters = [new("=", Insert: false), new(" ", Insert: false)];
    private static readonly ImmutableArray<RazorCommitCharacter> s_attributeSpaceCommitCharacters = [new(" ", Insert: false)];

    // Prefix group commit characters: '-' commits without inserting since the InsertText already ends with it.
    private static readonly ImmutableArray<RazorCommitCharacter> s_prefixGroupCommitCharacters = [new("-", Insert: false)];

    // Directive commit characters
    private static readonly ImmutableArray<RazorCommitCharacter> s_singleLineDirectiveCommitCharacters = RazorCommitCharacter.CreateArray([" "]);
    private static readonly ImmutableArray<RazorCommitCharacter> s_blockDirectiveCommitCharacters = RazorCommitCharacter.CreateArray([" ", "{"]);

    /// <summary>
    /// Gets commit characters for element name completions.
    /// </summary>
    /// <param name="useSpace">Whether space commits (e.g., to start typing attributes).</param>
    public static ImmutableArray<RazorCommitCharacter> GetElementCommitCharacters(bool useSpace)
        => useSpace ? s_elementCommitCharacters : s_elementCommitCharactersWithoutSpace;

    /// <summary>
    /// Gets commit characters for attribute name completions. The '=' character always uses
    /// <c>Insert=false</c> because in all contexts where '=' is a commit character, '=' is
    /// already present — either in the snippet insert text (e.g., <c>class="$0"</c>) or in the
    /// existing document text preserved by the edit range (e.g., replacing <c>data-X="value"</c>).
    /// </summary>
    /// <param name="useEquals">Whether '=' commits the completion.</param>
    /// <param name="useSpace">Whether space commits the completion.</param>
    public static ImmutableArray<RazorCommitCharacter> GetAttributeCommitCharacters(bool useEquals, bool useSpace)
        => (useEquals, useSpace) switch
        {
            (true, true) => s_attributeEqualsSpaceCommitCharacters,
            (true, false) => s_attributeEqualsCommitCharacters,
            (false, true) => s_attributeSpaceCommitCharacters,
            (false, false) => [],
        };

    /// <summary>
    /// Gets commit characters for prefix group items (e.g., <c>aria-</c>, <c>data-</c>).
    /// The '-' commits without inserting since the InsertText already ends with it.
    /// </summary>
    public static ImmutableArray<RazorCommitCharacter> GetPrefixGroupCommitCharacters()
        => s_prefixGroupCommitCharacters;

    /// <summary>
    /// Gets commit characters for directive completions (e.g., <c>@page</c>, <c>@using</c>).
    /// Space commits since directives are followed by their argument. Block directives also
    /// commit with '{'.
    /// </summary>
    public static ImmutableArray<RazorCommitCharacter> GetDirectiveCommitCharacters(bool isBlock)
        => isBlock ? s_blockDirectiveCommitCharacters : s_singleLineDirectiveCommitCharacters;

    /// <summary>
    /// Gets commit characters for C# keyword completions in Razor (e.g., <c>if</c>, <c>for</c>).
    /// Space commits since keywords are followed by their expression/statement.
    /// </summary>
    public static ImmutableArray<RazorCommitCharacter> GetKeywordCommitCharacters()
        => s_singleLineDirectiveCommitCharacters; // Same as single-line directives: just space
}
