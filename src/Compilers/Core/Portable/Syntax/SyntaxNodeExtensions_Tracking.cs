// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using Microsoft.CodeAnalysis.Collections;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    public static partial class SyntaxNodeExtensions
    {
        private static readonly ConditionalWeakTable<SyntaxNode, SyntaxAnnotation> s_nodeToIdMap
            = new ConditionalWeakTable<SyntaxNode, SyntaxAnnotation>();

        private static readonly ConditionalWeakTable<SyntaxNode, CurrentNodes> s_rootToCurrentNodesMap
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
                throw new ArgumentNullException(nameof(nodes));
            }

            // create an id for each node
            foreach (var node in nodes)
            {
                if (!IsDescendant(root, node))
                {
                    throw new ArgumentException(CodeAnalysisResources.InvalidNodeToTrack);
                }

                s_nodeToIdMap.GetValue(node, n => new SyntaxAnnotation(IdAnnotationKind));
            }

            return root.ReplaceNodes(nodes, (n, r) => n.HasAnnotation(GetId(n)!) ? r : r.WithAdditionalAnnotations(GetId(n)!));
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
                throw new ArgumentNullException(nameof(node));
            }

            return GetCurrentNodeFromTrueRoots(GetRoot(root), node).OfType<TNode>();
        }

        /// <summary>
        /// Gets the node within the subtree corresponding to the original tracked node.
        /// Use TrackNodes to start tracking nodes.
        /// </summary>
        /// <param name="root">The root of the subtree containing the current node corresponding to the original tracked node.</param>
        /// <param name="node">The node instance originally tracked.</param>
        public static TNode? GetCurrentNode<TNode>(this SyntaxNode root, TNode node)
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
                throw new ArgumentNullException(nameof(nodes));
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

        private static IReadOnlyList<SyntaxNode> GetCurrentNodeFromTrueRoots(SyntaxNode trueRoot, SyntaxNode node)
        {
            var id = GetId(node);
            if (id is object)
            {
                CurrentNodes tracked = s_rootToCurrentNodesMap.GetValue(trueRoot, r => new CurrentNodes(r));
                return tracked.GetNodes(id);
            }
            else
            {
                return SpecializedCollections.EmptyReadOnlyList<SyntaxNode>();
            }
        }

        private static SyntaxAnnotation? GetId(SyntaxNode original)
        {
            SyntaxAnnotation? id;
            s_nodeToIdMap.TryGetValue(original, out id);
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
                    node = ((IStructuredTriviaSyntax)node).ParentTrivia.Token.Parent!;
                    Debug.Assert(node is object);
                }
            }
        }

        private static bool IsDescendant(SyntaxNode root, SyntaxNode node)
        {
            while (node != null)
            {
                if (node == root)
                {
                    return true;
                }

                if (node.Parent != null)
                {
                    node = node.Parent;
                }
                else if (!node.IsStructuredTrivia)
                {
                    break;
                }
                else
                {
                    node = ((IStructuredTriviaSyntax)node).ParentTrivia.Token.Parent!;
                    Debug.Assert(node is object);
                }
            }

            return false;
        }

        private class CurrentNodes
        {
            [PerformanceSensitive("https://devdiv.visualstudio.com/DevDiv/_workitems/edit/1320760", Constraint = "Avoid large object heap allocations")]
            private readonly ImmutableSegmentedDictionary<SyntaxAnnotation, IReadOnlyList<SyntaxNode>> _idToNodeMap;

            public CurrentNodes(SyntaxNode root)
            {
                // there could be multiple nodes with same annotation if a tree is rewritten with
                // same node injected multiple times.
                var map = new SegmentedDictionary<SyntaxAnnotation, List<SyntaxNode>>();

                foreach (var node in root.GetAnnotatedNodesAndTokens(IdAnnotationKind).Select(n => n.AsNode()!))
                {
                    Debug.Assert(node is object);
                    foreach (var id in node.GetAnnotations(IdAnnotationKind))
                    {
                        List<SyntaxNode>? list;
                        if (!map.TryGetValue(id, out list))
                        {
                            list = new List<SyntaxNode>();
                            map.Add(id, list);
                        }

                        list.Add(node);
                    }
                }

                _idToNodeMap = map.ToImmutableSegmentedDictionary(kv => kv.Key, kv => (IReadOnlyList<SyntaxNode>)ImmutableArray.CreateRange(kv.Value));
            }

            public IReadOnlyList<SyntaxNode> GetNodes(SyntaxAnnotation id)
            {
                IReadOnlyList<SyntaxNode>? nodes;
                if (_idToNodeMap.TryGetValue(id, out nodes))
                {
                    return nodes;
                }
                else
                {
                    return SpecializedCollections.EmptyReadOnlyList<SyntaxNode>();
                }
            }
        }
    }
}
