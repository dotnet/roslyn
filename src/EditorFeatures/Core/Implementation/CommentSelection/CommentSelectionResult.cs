// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Editor.Implementation.CommentSelection
{
    internal struct CommentSelectionResult
    {
        /// <summary>
        /// Text changes to make for this operation.
        /// </summary>
        public ImmutableList<TextChange> TextChanges { get; }
        /// <summary>
        /// Tracking spans used to format and set the output selection after edits.
        /// </summary>
        public ImmutableList<CommentTrackingSpan> TrackingSpans { get; }
        /// <summary>
        /// The type of text changes being made.
        /// </summary>
        public Operation ResultOperation { get; }

        public CommentSelectionResult(List<TextChange> textChanges, List<CommentTrackingSpan> trackingSpans, Operation resultOperation)
        {
            TextChanges = textChanges.ToImmutableList();
            TrackingSpans = trackingSpans.ToImmutableList();
            ResultOperation = resultOperation;
        }
    }
}
