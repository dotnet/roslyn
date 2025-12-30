// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.GraphModel;

namespace Microsoft.VisualStudio.LanguageServices.Progression;

[Obsolete("This class is not implemented and should not be used.", error: true)]
public static class GraphNodeCreation
{
    [Obsolete("This method is not implemented and always returns an empty GraphNodeId.", error: true)]
    public static async Task<GraphNodeId> CreateNodeIdAsync(ISymbol symbol, Solution solution, CancellationToken cancellationToken)
        => GraphNodeId.Empty;

    [Obsolete("This method is not implemented and always returns an empty GraphNode.", error: true)]
    public static async Task<GraphNode> CreateNodeAsync(this Graph graph, ISymbol symbol, Solution solution, CancellationToken cancellationToken)
       => graph.Nodes.GetOrCreate(GraphNodeId.Empty);
}
