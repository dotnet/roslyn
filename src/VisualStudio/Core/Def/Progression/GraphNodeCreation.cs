// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.GraphModel;

#pragma warning disable IDE0060 // Remove unused parameter

namespace Microsoft.VisualStudio.LanguageServices.Progression;

/// <summary>
/// A helper class that implements the creation of <see cref="GraphNode"/>s.
/// </summary>
public static class GraphNodeCreation
{
    public static Task<GraphNodeId> CreateNodeIdAsync(ISymbol symbol, Solution solution, CancellationToken cancellationToken)
        => Task.FromResult(GraphNodeId.Empty);

    public static Task<GraphNode> CreateNodeAsync(this Graph graph, ISymbol symbol, Solution solution, CancellationToken cancellationToken)
        => Task.FromResult(graph.Nodes.GetOrCreate(GraphNodeId.Empty));
}
