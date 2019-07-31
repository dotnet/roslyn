// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Completion
{
    /// <summary>
    /// The change to be applied to the document when a <see cref="CompletionItem"/> is committed.
    /// </summary>
    public sealed class CompletionChange
    {
        /// <summary>
        /// The text change to be applied to the document.
        /// </summary>
        public TextChange TextChange { get; }

        [Obsolete("Use TextChange instead", error: true)]
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

        private CompletionChange(ImmutableArray<TextChange> textChanges, int? newPosition, bool includesCommitCharacter)
            : this(textChanges.Single(), newPosition, includesCommitCharacter)
        {
        }

        private CompletionChange(TextChange textChange, int? newPosition, bool includesCommitCharacter)
        {
            TextChange = textChange;
            NewPosition = newPosition;
            IncludesCommitCharacter = includesCommitCharacter;
        }

        /// <summary>
        /// Creates a new <see cref="CompletionChange"/> instance.
        /// </summary>
        /// <param name="textChanges">The text changes to be applied to the document.</param>
        /// <param name="newPosition">The new caret position after the change has been applied. 
        /// If null then the caret position is not specified and will be determined by the completion host.</param>
        /// <param name="includesCommitCharacter">True if the changes include the typed character that caused the <see cref="CompletionItem"/> to be committed.
        /// If false, the completion host will determine if and where the commit character is inserted into the document.</param>
        /// <returns></returns>
        [Obsolete("Use Create overload that only takes a single TextChange", error: true)]
        public static CompletionChange Create(
            ImmutableArray<TextChange> textChanges,
            int? newPosition = null,
            bool includesCommitCharacter = false)
        {
            return new CompletionChange(textChanges, newPosition, includesCommitCharacter);
        }

#pragma warning disable RS0027 // Public API with optional parameter(s) should have the most parameters amongst its public overloads.
#pragma warning disable RS0026 // Do not add multiple public overloads with optional parameters
        public static CompletionChange Create(
#pragma warning restore RS0026 // Do not add multiple public overloads with optional parameters
#pragma warning restore RS0027 // Public API with optional parameter(s) should have the most parameters amongst its public overloads.
            TextChange textChange,
            int? newPosition = null,
            bool includesCommitCharacter = false)
        {
            return new CompletionChange(textChange, newPosition, includesCommitCharacter);
        }

        /// <summary>
        /// Creates a copy of this <see cref="CompletionChange"/> with the <see cref="TextChange"/> property changed.
        /// </summary>
        [Obsolete("Use WithTextChange instead", error: true)]
        public CompletionChange WithTextChanges(ImmutableArray<TextChange> textChanges)
        {
            return new CompletionChange(textChanges, NewPosition, IncludesCommitCharacter);
        }

        public CompletionChange WithTextChange(TextChange textChange)
        {
            return new CompletionChange(textChange, NewPosition, IncludesCommitCharacter);
        }

        /// <summary>
        /// Creates a copy of this <see cref="CompletionChange"/> with the <see cref="NewPosition"/> property changed.
        /// </summary>
        public CompletionChange WithNewPosition(int? newPostion)
        {
            return new CompletionChange(TextChange, newPostion, IncludesCommitCharacter);
        }

        /// <summary>
        /// Creates a copy of this <see cref="CompletionChange"/> with the <see cref="IncludesCommitCharacter"/> property changed.
        /// </summary>
        public CompletionChange WithIncludesCommitCharacter(bool includesCommitCharacter)
        {
            return new CompletionChange(TextChange, NewPosition, includesCommitCharacter);
        }
    }
}
