// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.Razor.Completion;

internal partial class DirectiveAttributeCompletionItemProvider
{
    private static class DefaultCommitCharacters
    {
        private static readonly ImmutableArray<RazorCommitCharacter> s_equalsCommitCharacters = [EqualsCommit(false)];
        private static readonly ImmutableArray<RazorCommitCharacter> s_equalsSpaceCommitCharacters = [EqualsCommit(false), SpaceCommit];
        private static readonly ImmutableArray<RazorCommitCharacter> s_snippetEqualsCommitCharacters = [EqualsCommit(true)];
        private static readonly ImmutableArray<RazorCommitCharacter> s_snippetEqualsSpaceCommitCharacters = [EqualsCommit(true), SpaceCommit];
        private static readonly ImmutableArray<RazorCommitCharacter> s_spaceCommitCharacters = [SpaceCommit];

        private static RazorCommitCharacter EqualsCommit(bool snippet) => new("=", Insert: !snippet);
        private static RazorCommitCharacter SpaceCommit => new(" ");

        public static ImmutableArray<RazorCommitCharacter> Get(bool useEquals, bool useSpace, bool useSnippets)
            => (useEquals, useSpace, useSnippets) switch
            {
                // Use equals with or without space (no snippets)
                (true, false, false) => s_equalsCommitCharacters,
                (true, true, false) => s_equalsSpaceCommitCharacters,

                // Use equals with or without space (using snippets)
                (true, false, true) => s_snippetEqualsCommitCharacters,
                (true, true, true) => s_snippetEqualsSpaceCommitCharacters,

                // No equals and with or without space (snippets not relevant)
                (false, true, _) => s_spaceCommitCharacters,
                (false, false, _) => []
            };
    }
}
