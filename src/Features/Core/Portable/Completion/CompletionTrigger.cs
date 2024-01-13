// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace Microsoft.CodeAnalysis.Completion
{
    /// <summary>
    /// The action that triggered completion to start.
    /// </summary>
    /// <remarks>
    /// NOTE: Roslyn's LSP completion implementation uses this struct. If a new property is added, either:
    ///     1: The property's type must be serializable
    ///     OR
    ///     2. LSP will need to be updated to not use CompletionTrigger - see
    ///        Features\LanguageServer\Protocol\Handler\Completion\CompletionResolveData.cs
    /// </remarks>
    public readonly struct CompletionTrigger
    {
        /// <summary>
        /// The reason that completion was started.
        /// </summary>
        public CompletionTriggerKind Kind { get; }

        /// <summary>
        /// The character associated with the triggering action.
        /// </summary>
        public char Character { get; }

        internal CompletionTrigger(CompletionTriggerKind kind, char character = (char)0)
            : this()
        {
            Kind = kind;
            Character = character;
        }

        /// <summary>
        /// Do not use.  Use <see cref="Invoke"/> instead.
        /// </summary>
        [Obsolete("Use 'Invoke' instead.")]
        public static readonly CompletionTrigger Default =
            new(CompletionTriggerKind.Other);

        /// <summary>
        /// The default <see cref="CompletionTrigger"/> when none is specified.
        /// </summary>
        public static readonly CompletionTrigger Invoke =
            new(CompletionTriggerKind.Invoke);

        /// <summary>
        /// Creates a new instance of a <see cref="CompletionTrigger"/> association with the insertion of a typed character into the document.
        /// </summary>
        public static CompletionTrigger CreateInsertionTrigger(char insertedCharacter)
            => new(CompletionTriggerKind.Insertion, insertedCharacter);

        /// <summary>
        /// Creates a new instance of a <see cref="CompletionTrigger"/> association with the deletion of a character from the document.
        /// </summary>
        public static CompletionTrigger CreateDeletionTrigger(char deletedCharacter)
            => new(CompletionTriggerKind.Deletion, deletedCharacter);
    }
}
