// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.Shared.Utilities;

internal static partial class ExtensionOrderer
{
    private sealed class Graph<TExtension, TMetadata>
        where TMetadata : OrderableMetadata
    {
        public readonly Dictionary<Lazy<TExtension, TMetadata>, Node<TExtension, TMetadata>> Nodes = [];

        public IEnumerable<Lazy<TExtension, TMetadata>> FindExtensions(string name)
        {
            Contract.ThrowIfNull(name);
            return this.Nodes.Keys.Where(k => k.Metadata.Name == name);
        }

        public void CheckForCycles()
        {
            foreach (var node in this.Nodes.Values)
                node.CheckForCycles();
        }

        public ImmutableArray<Lazy<TExtension, TMetadata>> TopologicalSort()
        {
            using var _ = ArrayBuilder<Lazy<TExtension, TMetadata>>.GetInstance(out var result);
            var seenNodes = new HashSet<Node<TExtension, TMetadata>>();

            foreach (var node in this.Nodes.Values)
                Visit(node, result, seenNodes);

            return result.ToImmutableAndClear();
        }

        private static void Visit(
            Node<TExtension, TMetadata> node,
            ArrayBuilder<Lazy<TExtension, TMetadata>> result,
            HashSet<Node<TExtension, TMetadata>> seenNodes)
        {
            if (seenNodes.Add(node))
            {
                foreach (var before in node.ExtensionsBeforeMeSet)
                    Visit(before, result, seenNodes);

                result.Add(node.Extension);
            }
        }
    }
}
