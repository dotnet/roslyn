// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.CodeAnalysis.Formatting.Rules;

namespace Microsoft.CodeAnalysis.Formatting
{
    /// <summary>
    /// this collector gathers formatting operations that are based on a node
    /// </summary>
    internal class NodeOperations
    {
        public static NodeOperations Empty = new NodeOperations();

        public List<IndentBlockOperation> IndentBlockOperation { get; }
        public List<SuppressOperation> SuppressOperation { get; }
        public List<AlignTokensOperation> AlignmentOperation { get; }
        public List<AnchorIndentationOperation> AnchorIndentationOperations { get; }

        public NodeOperations(List<IndentBlockOperation> indentBlockOperation, List<SuppressOperation> suppressOperation, List<AnchorIndentationOperation> anchorIndentationOperations, List<AlignTokensOperation> alignmentOperation)
        {
            this.IndentBlockOperation = indentBlockOperation;
            this.SuppressOperation = suppressOperation;
            this.AlignmentOperation = alignmentOperation;
            this.AnchorIndentationOperations = anchorIndentationOperations;
        }

        private NodeOperations()
        {
            this.IndentBlockOperation = new List<IndentBlockOperation>();
            this.SuppressOperation = new List<SuppressOperation>();
            this.AlignmentOperation = new List<AlignTokensOperation>();
            this.AnchorIndentationOperations = new List<AnchorIndentationOperation>();
        }
    }
}
