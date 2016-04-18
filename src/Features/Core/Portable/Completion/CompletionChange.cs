// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Completion
{
    /// <summary>
    /// The change to be applied to the document when a <see cref="CompletionItem"/> is committed.
    /// </summary>
    public sealed class CompletionChange
    {
        /// <summary>
        /// The text changes to be applied to the document.
        /// </summary>
        public ImmutableArray<TextChange> TextChanges { get; }

        /// <summary>
        /// The new caret position after the change has been applied.
        /// If null then the new caret position will be determined by the completion host.
        /// </summary>
        public int? NewPosition { get; }

        /// <summary>
        /// True if the changes include the typed character that caused the <see cref="CompletionItem"/> to be committed.
        /// If false the completion host will determine if and where the commit character is inserted into the document.
        /// </summary>
        public bool IncludesCommitCharacter { get; }

        private CompletionChange(ImmutableArray<TextChange> textChanges, int? newPosition, bool includesCommit)
        {
            this.TextChanges = textChanges.IsDefault ? ImmutableArray<TextChange>.Empty : textChanges;
            this.NewPosition = newPosition;
            this.IncludesCommitCharacter = includesCommit;
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
        public static CompletionChange Create(ImmutableArray<TextChange> textChanges, int? newPosition = null, bool includesCommitCharacter = false)
        {
            return new CompletionChange(textChanges, newPosition, includesCommitCharacter);
        }

        /// <summary>
        /// Creates a copy of this <see cref="CompletionChange"/> with the <see cref="TextChange"/> property changed.
        /// </summary>
        public CompletionChange WithTextChanges(ImmutableArray<TextChange> textChanges)
        {
            return new CompletionChange(textChanges, this.NewPosition, this.IncludesCommitCharacter);
        }

        /// <summary>
        /// Creates a copy of this <see cref="CompletionChange"/> with the <see cref="NewPosition"/> property changed.
        /// </summary>
        public CompletionChange WithNewPosition(int? newPostion)
        {
            return new CompletionChange(this.TextChanges, newPostion, this.IncludesCommitCharacter);
        }

        /// <summary>
        /// Creates a copy of this <see cref="CompletionChange"/> with the <see cref="IncludesCommitCharacter"/> property changed.
        /// </summary>
        public CompletionChange WithIncludesCommitCharacter(bool includesCommitCharacter)
        {
            return new CompletionChange(this.TextChanges, this.NewPosition, includesCommitCharacter);
        }
    }
}