// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.Completion
{
    /// <summary>
    /// A rule that modifies a set of characters.
    /// </summary>
    public readonly struct CharacterSetModificationRule
    {
        /// <summary>
        /// The kind of modification.
        /// </summary>
        public CharacterSetModificationKind Kind { get; }

        /// <summary>
        /// One or more characters.
        /// </summary>
        public ImmutableArray<char> Characters { get; }

        private CharacterSetModificationRule(CharacterSetModificationKind kind, ImmutableArray<char> characters)
        {
            Kind = kind;
            Characters = characters;
        }

        /// <summary>
        /// Creates a new <see cref="CharacterSetModificationRule"/> instance.
        /// </summary>
        /// <param name="kind">The kind of rule.</param>
        /// <param name="characters">One or more characters. These are typically punctuation characters.</param>
        /// <returns></returns>
        public static CharacterSetModificationRule Create(CharacterSetModificationKind kind, ImmutableArray<char> characters)
        {
            return new CharacterSetModificationRule(kind, characters);
        }

        /// <summary>
        /// Creates a new <see cref="CharacterSetModificationRule"/> instance.
        /// </summary>
        /// <param name="kind">The kind of rule.</param>
        /// <param name="characters">One or more characters. These are typically punctuation characters.</param>
        /// <returns></returns>
        public static CharacterSetModificationRule Create(CharacterSetModificationKind kind, params char[] characters)
        {
            return new CharacterSetModificationRule(kind, characters.ToImmutableArray());
        }
    }
}
