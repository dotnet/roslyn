// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;

namespace Microsoft.VisualStudio.LanguageServices.Xaml.Features.Completion
{
    public readonly struct XamlCommitCharacters
    {
        /// <summary>
        /// Commit characters.
        /// </summary>
        public ImmutableArray<char> Characters { get; }

        /// <summary>
        /// Commit characters that will not be inserted when commit
        /// </summary>
        public ImmutableArray<char> NonInsertCharacters { get; }

        private XamlCommitCharacters(ImmutableArray<char> characters, ImmutableArray<char> nonInsertCharacters)
        {
            Characters = characters;
            NonInsertCharacters = nonInsertCharacters;
        }

        public static XamlCommitCharacters Create(ImmutableArray<char> characters, ImmutableArray<char> nonInsertCharacters)
            => new(characters, nonInsertCharacters);

        public static XamlCommitCharacters Create(ImmutableArray<char> characters, params char[] nonInsertCharacters)
            => new(characters, nonInsertCharacters?.ToImmutableArray() ?? ImmutableArray<char>.Empty);

        public static XamlCommitCharacters Create(char[] characters, params char[] nonInsertCharacters)
            => Create(characters.ToImmutableArray(), nonInsertCharacters);
    }
}
