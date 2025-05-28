// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.GraphModel;

namespace Microsoft.VisualStudio.LanguageServices.Progression;

/// <summary>
/// A helper class that implements the creation of <see cref="GraphNode"/>s.
/// </summary>
public static class GraphNodeCreation
{
    public static Task<GraphNodeId> CreateNodeIdAsync(ISymbol symbol, Solution solution, CancellationToken cancellationToken)
    {
        throw new NotSupportedException();
    }

    public static Task<GraphNode> CreateNodeAsync(this Graph graph, ISymbol symbol, Solution solution, CancellationToken cancellationToken)
    {
        throw new NotSupportedException();
    }
}
