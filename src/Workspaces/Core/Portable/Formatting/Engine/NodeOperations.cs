// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Formatting.Rules;

namespace Microsoft.CodeAnalysis.Formatting
{
    /// <summary>
    /// this collector gathers formatting operations that are based on a node
    /// </summary>
    internal class NodeOperations
    {
        public static NodeOperations Empty = new NodeOperations();

        public Task<List<IndentBlockOperation>> IndentBlockOperationTask { get; }
        public Task<List<SuppressOperation>> SuppressOperationTask { get; }
        public Task<List<AlignTokensOperation>> AlignmentOperationTask { get; }
        public Task<List<AnchorIndentationOperation>> AnchorIndentationOperationsTask { get; }

        public NodeOperations(Task<List<IndentBlockOperation>> indentBlockOperationTask, Task<List<SuppressOperation>> suppressOperationTask, Task<List<AnchorIndentationOperation>> anchorIndentationOperationsTask, Task<List<AlignTokensOperation>> alignmentOperationTask)
        {
            this.IndentBlockOperationTask = indentBlockOperationTask;
            this.SuppressOperationTask = suppressOperationTask;
            this.AlignmentOperationTask = alignmentOperationTask;
            this.AnchorIndentationOperationsTask = anchorIndentationOperationsTask;
        }

        private NodeOperations()
        {
            this.IndentBlockOperationTask = Task.FromResult(new List<IndentBlockOperation>());
            this.SuppressOperationTask = Task.FromResult(new List<SuppressOperation>());
            this.AlignmentOperationTask = Task.FromResult(new List<AlignTokensOperation>());
            this.AnchorIndentationOperationsTask = Task.FromResult(new List<AnchorIndentationOperation>());
        }
    }
}
