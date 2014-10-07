// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Composition;
using System.Linq;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Shared.Utilities
{
    internal static partial class ExtensionOrderer
    {
        internal static IList<Lazy<TExtension, TMetadata>> Order<TExtension, TMetadata>(
            IEnumerable<Lazy<TExtension, TMetadata>> extensions)
            where TMetadata : IOrderableMetadata
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
                foreach (var before in Before(extension))
                {
                    foreach (var beforeExtension in graph.FindExtensions(before))
                    {
                        var otherExtensionNode = graph.Nodes[beforeExtension];
                        otherExtensionNode.ExtensionsBeforeMeSet.Add(extensionNode);
                    }
                }

                foreach (var after in After(extension))
                {
                    foreach (var afterExtension in graph.FindExtensions(after))
                    {
                        var otherExtensionNode = graph.Nodes[afterExtension];
                        extensionNode.ExtensionsBeforeMeSet.Add(otherExtensionNode);
                    }
                }
            }

            graph.CheckForCycles();
            return graph.TopologicalSort();
        }

        private static IEnumerable<string> Before<TExtension, TMetadata>(Lazy<TExtension, TMetadata> extension)
            where TMetadata : IOrderableMetadata
        {
            return extension.Metadata.Before ?? SpecializedCollections.EmptyEnumerable<string>();
        }

        private static IEnumerable<string> After<TExtension, TMetadata>(Lazy<TExtension, TMetadata> extension)
            where TMetadata : IOrderableMetadata
        {
            return extension.Metadata.After ?? SpecializedCollections.EmptyEnumerable<string>();
        }
    }
}
