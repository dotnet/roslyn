// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Editor.Implementation.CommentSelection
{
    internal readonly struct CommentSelectionResult
    {
        /// <summary>
        /// Text changes to make for this operation.
        /// </summary>
        public ImmutableArray<TextChange> TextChanges { get; }
        /// <summary>
        /// Tracking spans used to format and set the output selection after edits.
        /// </summary>
        public ImmutableArray<CommentTrackingSpan> TrackingSpans { get; }
        /// <summary>
        /// The type of text changes being made.
        /// This is known beforehand in some cases (comment selection)
        /// and determined after as a result in others (toggle comment).
        /// </summary>
        public Operation ResultOperation { get; }

        public CommentSelectionResult(IEnumerable<TextChange> textChanges, IEnumerable<CommentTrackingSpan> trackingSpans, Operation resultOperation)
        {
            TextChanges = textChanges.ToImmutableArray();
            TrackingSpans = trackingSpans.ToImmutableArray();
            ResultOperation = resultOperation;
        }
    }
}
