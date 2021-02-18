// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.Formatting.Rules;

namespace Microsoft.CodeAnalysis.Formatting
{
    /// <summary>
    /// this collector gathers formatting operations that are based on a node
    /// </summary>
    internal class NodeOperations
    {
        public static NodeOperations Empty = new();

        public SegmentedList<IndentBlockOperation> IndentBlockOperation { get; }
        public SegmentedList<SuppressOperation> SuppressOperation { get; }
        public SegmentedList<AlignTokensOperation> AlignmentOperation { get; }
        public SegmentedList<AnchorIndentationOperation> AnchorIndentationOperations { get; }

        public NodeOperations(
            SegmentedList<IndentBlockOperation> indentBlockOperation,
            SegmentedList<SuppressOperation> suppressOperation,
            SegmentedList<AnchorIndentationOperation> anchorIndentationOperations,
            SegmentedList<AlignTokensOperation> alignmentOperation)
        {
            this.IndentBlockOperation = indentBlockOperation;
            this.SuppressOperation = suppressOperation;
            this.AlignmentOperation = alignmentOperation;
            this.AnchorIndentationOperations = anchorIndentationOperations;
        }

        private NodeOperations()
        {
            this.IndentBlockOperation = new SegmentedList<IndentBlockOperation>();
            this.SuppressOperation = new SegmentedList<SuppressOperation>();
            this.AlignmentOperation = new SegmentedList<AlignTokensOperation>();
            this.AnchorIndentationOperations = new SegmentedList<AnchorIndentationOperation>();
        }
    }
}
