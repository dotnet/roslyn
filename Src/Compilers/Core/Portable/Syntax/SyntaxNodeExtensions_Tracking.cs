// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

namespace Microsoft.CodeAnalysis
{
    public static partial class SyntaxNodeExtensions
    {
        private static readonly ConditionalWeakTable<SyntaxNode, SyntaxAnnotation> nodeToIdMap
            = new ConditionalWeakTable<SyntaxNode, SyntaxAnnotation>();

        private static readonly ConditionalWeakTable<SyntaxNode, CurrentNodes> rootToCurrentNodesMap
            = new ConditionalWeakTable<SyntaxNode, CurrentNodes>();

        internal const string IdAnnotationKind = "Id";

        /// <summary>
        /// Creates a new tree of nodes with the specified nodes being tracked.
        /// 
        /// Use GetCurrentNode on the subtree resulting from this operation, or any transformation of it,
        /// to get the current node corresponding to the original tracked node.
        /// </summary>
        /// <param name="root">The root of the subtree containing the nodes to be tracked.</param>
        /// <param name="nodes">One or more nodes that are descendants of the root node.</param>
        public static TRoot TrackNodes<TRoot>(this TRoot root, IEnumerable<SyntaxNode> nodes)
            where TRoot : SyntaxNode
        {
            if (nodes == null)
            {
                throw new ArgumentNullException("nodes");
            }

            // create an id for each node
            foreach (var node in nodes)
            {
                nodeToIdMap.GetValue(node, n => new SyntaxAnnotation(IdAnnotationKind));
            }

            return root.ReplaceNodes(nodes, (n, r) => n.HasAnnotation(GetId(n)) ? r : r.WithAdditionalAnnotations(GetId(n)));
        }

        /// <summary>
        /// Creates a new tree of nodes with the specified nodes being tracked.
        /// 
        /// Use GetCurrentNode on the subtree resulting from this operation, or any transformation of it,
        /// to get the current node corresponding to the original tracked node.
        /// </summary>
        /// <param name="root">The root of the subtree containing the nodes to be tracked.</param>
        /// <param name="nodes">One or more nodes that are descendants of the root node.</param>
        public static TRoot TrackNodes<TRoot>(this TRoot root, params SyntaxNode[] nodes)
            where TRoot : SyntaxNode
        {
            return TrackNodes(root, (IEnumerable<SyntaxNode>)nodes);
        }

        /// <summary>
        /// Gets the nodes within the subtree corresponding to the original tracked node.
        /// Use TrackNodes to start tracking nodes.
        /// </summary>
        /// <param name="root">The root of the subtree containing the current node corresponding to the original tracked node.</param>
        /// <param name="node">The node instance originally tracked.</param>
        public static IEnumerable<TNode> GetCurrentNodes<TNode>(this SyntaxNode root, TNode node)
            where TNode : SyntaxNode
        {
            if (node == null)
            {
                throw new ArgumentNullException("originalNode");
            }

            foreach (var newNode in GetCurrentNodeFromTrueRoots(GetRoot(root), node).OfType<TNode>())
            {
                yield return newNode;
            }
        }

        /// <summary>
        /// Gets the node within the subtree corresponding to the original tracked node.
        /// Use TrackNodes to start tracking nodes.
        /// </summary>
        /// <param name="root">The root of the subtree containing the current node corresponding to the original tracked node.</param>
        /// <param name="node">The node instance originally tracked.</param>
        public static TNode GetCurrentNode<TNode>(this SyntaxNode root, TNode node)
            where TNode : SyntaxNode
        {
            return GetCurrentNodes(root, node).SingleOrDefault();
        }

        /// <summary>
        /// Gets the nodes within the subtree corresponding to the original tracked nodes.
        /// Use TrackNodes to start tracking nodes.
        /// </summary>
        /// <param name="root">The root of the subtree containing the current nodes corresponding to the original tracked nodes.</param>
        /// <param name="nodes">One or more node instances originally tracked.</param>
        public static IEnumerable<TNode> GetCurrentNodes<TNode>(this SyntaxNode root, IEnumerable<TNode> nodes)
            where TNode : SyntaxNode
        {
            if (nodes == null)
            {
                throw new ArgumentNullException("nodes");
            }

            var trueRoot = GetRoot(root);

            foreach (var node in nodes)
            {
                foreach (var newNode in GetCurrentNodeFromTrueRoots(trueRoot, node).OfType<TNode>())
                {
                    yield return newNode;
                }
            }
        }

        private static List<SyntaxNode> GetCurrentNodeFromTrueRoots(SyntaxNode trueRoot, SyntaxNode node)
        {
            var id = GetId(node);
            if (id != null)
            {
                CurrentNodes tracked = rootToCurrentNodesMap.GetValue(trueRoot, r => new CurrentNodes(r));
                return tracked.GetNodes(id);
            }
            else
            {
                return null;
            }
        }

        private static SyntaxAnnotation GetId(SyntaxNode original)
        {
            SyntaxAnnotation id;
            nodeToIdMap.TryGetValue(original, out id);
            return id;
        }

        private static SyntaxNode GetRoot(SyntaxNode node)
        {
            while (true)
            {
                while (node.Parent != null)
                {
                    node = node.Parent;
                }

                if (!node.IsStructuredTrivia)
                {
                    return node;
                }
                else
                {
                    node = ((IStructuredTriviaSyntax)node).ParentTrivia.Token.Parent;
                }
            }
        }

        private class CurrentNodes
        {
            private readonly Dictionary<SyntaxAnnotation, List<SyntaxNode>> idToNodeMap;

            public CurrentNodes(SyntaxNode root)
            {
                // there could be multiple nodes with same annotation if a tree is rewritten with
                // same node injected multiple times.
                var map = new Dictionary<SyntaxAnnotation, List<SyntaxNode>>();

                foreach (var node in root.GetAnnotatedNodesAndTokens(IdAnnotationKind).Select(n => n.AsNode()))
                {
                    foreach (var id in node.GetAnnotations(IdAnnotationKind))
                    {
                        List<SyntaxNode> list;
                        if (!map.TryGetValue(id, out list))
                        {
                            list = new List<SyntaxNode>();
                            map.Add(id, list);
                        }

                        list.Add(node);
                    }
                }

                this.idToNodeMap = map;
            }

            public List<SyntaxNode> GetNodes(SyntaxAnnotation id)
            {
                List<SyntaxNode> node;
                this.idToNodeMap.TryGetValue(id, out node);
                return node;
            }
        }
    }
}