// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;

namespace Microsoft.CodeAnalysis.Completion
{
    /// <summary>
    /// The action that triggered completion to start.
    /// </summary>
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
            new CompletionTrigger(CompletionTriggerKind.Other);

        /// <summary>
        /// The default <see cref="CompletionTrigger"/> when none is specified.
        /// </summary>
        public static readonly CompletionTrigger Invoke =
            new CompletionTrigger(CompletionTriggerKind.Invoke);

        /// <summary>
        /// Creates a new instance of a <see cref="CompletionTrigger"/> association with the insertion of a typed character into the document.
        /// </summary>
        public static CompletionTrigger CreateInsertionTrigger(char insertedCharacter)
            => new CompletionTrigger(CompletionTriggerKind.Insertion, insertedCharacter);

        /// <summary>
        /// Creates a new instance of a <see cref="CompletionTrigger"/> association with the deletion of a character from the document.
        /// </summary>
        public static CompletionTrigger CreateDeletionTrigger(char deletedCharacter)
            => new CompletionTrigger(CompletionTriggerKind.Deletion, deletedCharacter);
    }
}
