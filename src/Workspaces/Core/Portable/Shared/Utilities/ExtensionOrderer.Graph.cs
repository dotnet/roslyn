// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Shared.Utilities
{
    internal partial class ExtensionOrderer
    {
        private class Graph<TExtension, TMetadata>
            where TMetadata : OrderableMetadata
        {
            public readonly Dictionary<Lazy<TExtension, TMetadata>, Node<TExtension, TMetadata>> Nodes =
                new Dictionary<Lazy<TExtension, TMetadata>, Node<TExtension, TMetadata>>();

            public IEnumerable<Lazy<TExtension, TMetadata>> FindExtensions(string name)
            {
                Contract.ThrowIfNull(name);
                return this.Nodes.Keys.Where(k => k.Metadata.Name == name);
            }

            public void CheckForCycles()
            {
                foreach (var node in this.Nodes.Values)
                {
                    node.CheckForCycles();
                }
            }

            public IList<Lazy<TExtension, TMetadata>> TopologicalSort()
            {
                var result = new List<Lazy<TExtension, TMetadata>>();
                var seenNodes = new HashSet<Node<TExtension, TMetadata>>();

                foreach (var node in this.Nodes.Values)
                {
                    Visit(node, result, seenNodes);
                }

                return result;
            }

            private static void Visit(
                Node<TExtension, TMetadata> node,
                List<Lazy<TExtension, TMetadata>> result,
                HashSet<Node<TExtension, TMetadata>> seenNodes)
            {
                if (seenNodes.Add(node))
                {
                    foreach (var before in node.ExtensionsBeforeMeSet)
                    {
                        Visit(before, result, seenNodes);
                    }

                    result.Add(node.Extension);
                }
            }
        }
    }
}
