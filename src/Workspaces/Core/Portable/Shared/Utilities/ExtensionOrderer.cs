// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.CodeAnalysis.Shared.Utilities;

internal static partial class ExtensionOrderer
{
    internal static IList<Lazy<TExtension, TMetadata>> Order<TExtension, TMetadata>(
        IEnumerable<Lazy<TExtension, TMetadata>> extensions)
        where TMetadata : OrderableMetadata
    {
        var graph = GetGraph(extensions);
        return graph.TopologicalSort();
    }

    private static Graph<TExtension, TMetadata> GetGraph<TExtension, TMetadata>(
        IEnumerable<Lazy<TExtension, TMetadata>> extensions)
        where TMetadata : OrderableMetadata
    {
        var list = extensions.ToList();
        var graph = new Graph<TExtension, TMetadata>();

        foreach (var extension in list)
        {
            graph.Nodes.Add(extension, new Node<TExtension, TMetadata>(extension));
        }

        foreach (var extension in list)
        {
            var extensionNode = graph.Nodes[extension];
            foreach (var before in extension.Metadata.BeforeTyped)
            {
                foreach (var beforeExtension in graph.FindExtensions(before))
                {
                    var otherExtensionNode = graph.Nodes[beforeExtension];
                    otherExtensionNode.ExtensionsBeforeMeSet.Add(extensionNode);
                }
            }

            foreach (var after in extension.Metadata.AfterTyped)
            {
                foreach (var afterExtension in graph.FindExtensions(after))
                {
                    var otherExtensionNode = graph.Nodes[afterExtension];
                    extensionNode.ExtensionsBeforeMeSet.Add(otherExtensionNode);
                }
            }
        }

        return graph;
    }

    internal static class TestAccessor
    {
        /// <summary>
        /// Helper for checking whether cycles exist in the extension ordering.
        /// Throws <see cref="ArgumentException"/> if a cycle is detected.
        /// </summary>
        /// <exception cref="ArgumentException">A cycle was detected in the extension ordering.</exception>
        internal static void CheckForCycles<TExtension, TMetadata>(
            IEnumerable<Lazy<TExtension, TMetadata>> extensions)
            where TMetadata : OrderableMetadata
        {
            var graph = GetGraph(extensions);
            graph.CheckForCycles();
        }
    }
}
