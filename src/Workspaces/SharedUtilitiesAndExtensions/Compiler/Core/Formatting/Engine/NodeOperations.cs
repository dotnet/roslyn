// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Microsoft.CodeAnalysis.Formatting.Rules;

namespace Microsoft.CodeAnalysis.Formatting
{
    /// <summary>
    /// this collector gathers formatting operations that are based on a node
    /// </summary>
    internal class NodeOperations
    {
        public static NodeOperations Empty = new();

        public List<IndentBlockOperation> IndentBlockOperation { get; } = new();
        public List<SuppressOperation> SuppressOperation { get; } = new();
        public List<AlignTokensOperation> AlignmentOperation { get; } = new();
        public List<AnchorIndentationOperation> AnchorIndentationOperations { get; } = new();
    }
}
