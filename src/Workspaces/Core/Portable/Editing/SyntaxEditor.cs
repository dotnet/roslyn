// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.CodeAnalysis.Editing
{
    /// <summary>
    /// An editor for making changes to a syntax tree. 
    /// </summary>
    public class SyntaxEditor
    {
        private readonly SyntaxGenerator _generator;
        private readonly List<Change> _changes;
        private bool _allowEditsOnLazilyCreatedTrackedNewNodes;
        private HashSet<SyntaxNode> _lazyTrackedNewNodesOpt;

        /// <summary>
        /// Creates a new <see cref="SyntaxEditor"/> instance.
        /// </summary>
        public SyntaxEditor(SyntaxNode root, Workspace workspace)
        {
            if (workspace == null)
            {
                throw new ArgumentNullException(nameof(workspace));
            }

            OriginalRoot = root ?? throw new ArgumentNullException(nameof(root));
            _generator = SyntaxGenerator.GetGenerator(workspace, root.Language);
            _changes = new List<Change>();
        }

        internal SyntaxEditor(SyntaxNode root, SyntaxGenerator generator)
        {
            OriginalRoot = root ?? throw new ArgumentNullException(nameof(root));
            _generator = generator;
            _changes = new List<Change>();
        }

        private SyntaxNode ApplyTrackingToNewNode(SyntaxNode node)
        {
            _lazyTrackedNewNodesOpt = _lazyTrackedNewNodesOpt ?? new HashSet<SyntaxNode>();
            foreach (var descendant in node.DescendantNodesAndSelf())
            {
                _lazyTrackedNewNodesOpt.Add(descendant);
            }

            return node.TrackNodes(node.DescendantNodesAndSelf());
        }

        private IEnumerable<SyntaxNode> ApplyTrackingToNewNodesIfRequired(IEnumerable<SyntaxNode> nodes, bool trackNodes)
        {
            if (!trackNodes)
            {
                return nodes;
            }

            return ApplyTrackingToNewNodes(nodes);
        }

        private IEnumerable<SyntaxNode> ApplyTrackingToNewNodes(IEnumerable<SyntaxNode> nodes)
        {
            foreach (var node in nodes)
            {
                yield return ApplyTrackingToNewNode(node);
            }
        }

        /// <summary>
        /// The <see cref="SyntaxNode"/> that was specified when the <see cref="SyntaxEditor"/> was constructed.
        /// </summary>
        public SyntaxNode OriginalRoot { get; }

        /// <summary>
        /// A <see cref="SyntaxGenerator"/> to use to create and change <see cref="SyntaxNode"/>'s.
        /// </summary>
        public SyntaxGenerator Generator => _generator;

        /// <summary>
        /// Returns the changed root node.
        /// </summary>
        public SyntaxNode GetChangedRoot()
        {
            var nodes = Enumerable.Distinct(_changes.Where(c => OriginalRoot.Contains(c.Node))
                                                    .Select(c => c.Node));
            var newRoot = OriginalRoot.TrackNodes(nodes);

            foreach (var change in _changes)
            {
                newRoot = change.Apply(newRoot, _generator);
            }

            return newRoot;
        }

        /// <summary>
        /// Makes sure the node is tracked, even if it is not changed.
        /// </summary>
        public void TrackNode(SyntaxNode node)
        {
            CheckNodeInOriginalTreeOrTracked(node);
            _changes.Add(new NoChange(node));
        }

        /// <summary>
        /// Remove the node from the tree.
        /// </summary>
        /// <param name="node">The node to remove that currently exists as part of the tree.</param>
        public void RemoveNode(SyntaxNode node)
        {
            RemoveNode(node, SyntaxGenerator.DefaultRemoveOptions);
        }

        /// <summary>
        /// Remove the node from the tree.
        /// </summary>
        /// <param name="node">The node to remove that currently exists as part of the tree.</param>
        /// <param name="options">Options that affect how node removal works.</param>
        public void RemoveNode(SyntaxNode node, SyntaxRemoveOptions options)
        {
            CheckNodeInOriginalTreeOrTracked(node);
            _changes.Add(new RemoveChange(node, options));
        }

        /// <summary>
        /// Replace the specified node with a node produced by the function.
        /// </summary>
        /// <param name="node">The node to replace that already exists in the tree.</param>
        /// <param name="computeReplacement">A function that computes a replacement node. 
        /// The node passed into the compute function includes changes from prior edits. It will not appear as a descendant of the original root.</param>
        public void ReplaceNode(SyntaxNode node, Func<SyntaxNode, SyntaxGenerator, SyntaxNode> computeReplacement)
            => ReplaceNode(node, computeReplacement, trackNewNode: false);

        /// <summary>
        /// Replace the specified node with a node produced by the function.
        /// </summary>
        /// <param name="node">The node to replace that already exists in the tree.</param>
        /// <param name="computeReplacement">A function that computes a replacement node. 
        /// The node passed into the compute function includes changes from prior edits. It will not appear as a descendant of the original root.</param>
        /// <param name="trackNewNode">Indicates if the replacement node should be tracked for future edits.</param>
        internal void ReplaceNode(SyntaxNode node, Func<SyntaxNode, SyntaxGenerator, SyntaxNode> computeReplacement, bool trackNewNode = false)
        {
            CheckNodeInOriginalTreeOrTracked(node);
            _allowEditsOnLazilyCreatedTrackedNewNodes |= trackNewNode;
            _changes.Add(new ReplaceChange(node, computeReplacement, ApplyTrackingToNewNode, trackNewNode));
        }

        internal void ReplaceNode<TArgument>(SyntaxNode node, Func<SyntaxNode, SyntaxGenerator, TArgument, SyntaxNode> computeReplacement, TArgument argument, bool trackNewNode = false)
        {
            CheckNodeInOriginalTreeOrTracked(node);
            _allowEditsOnLazilyCreatedTrackedNewNodes |= trackNewNode;
            _changes.Add(new ReplaceChange<TArgument>(node, computeReplacement, argument, ApplyTrackingToNewNode, trackNewNode));
        }

        /// <summary>
        /// Replace the specified node with a different node.
        /// </summary>
        /// <param name="node">The node to replace that already exists in the tree.</param>
        /// <param name="newNode">The new node that will be placed into the tree in the existing node's location.</param>
        public void ReplaceNode(SyntaxNode node, SyntaxNode newNode)
            => ReplaceNode(node, newNode, trackNewNode: false);

        /// <summary>
        /// Replace the specified node with a different node.
        /// </summary>
        /// <param name="node">The node to replace that already exists in the tree.</param>
        /// <param name="newNode">The new node that will be placed into the tree in the existing node's location.</param>
        /// <param name="trackNewNode">Flag to indicate if the new node should be tracked for further edits.</param>
        internal void ReplaceNode(SyntaxNode node, SyntaxNode newNode, bool trackNewNode)
        {
            CheckNodeInOriginalTreeOrTracked(node);
            if (node == newNode)
            {
                return;
            }

            if (trackNewNode)
            {
                newNode = ApplyTrackingToNewNode(newNode);
            }

            _changes.Add(new ReplaceChange(node, (n, g) => newNode, ApplyTrackingToNewNode, trackNewNode));
        }

        /// <summary>
        /// Insert the new nodes before the specified node already existing in the tree.
        /// </summary>
        /// <param name="node">The node already existing in the tree that the new nodes will be placed before. This must be a node this is contained within a syntax list.</param>
        /// <param name="newNodes">The nodes to place before the existing node. These nodes must be of a compatible type to be placed in the same list containing the existing node.</param>
        public void InsertBefore(SyntaxNode node, IEnumerable<SyntaxNode> newNodes)
            => InsertBefore(node, newNodes, trackNewNodes: false);

        /// <summary>
        /// Insert the new nodes before the specified node already existing in the tree.
        /// </summary>
        /// <param name="node">The node already existing in the tree that the new nodes will be placed before. This must be a node this is contained within a syntax list.</param>
        /// <param name="newNodes">The nodes to place before the existing node. These nodes must be of a compatible type to be placed in the same list containing the existing node.</param>
        /// <param name="trackNewNodes">Flag to indicate if the new nodes should be tracked for further edits.</param>
        internal void InsertBefore(SyntaxNode node, IEnumerable<SyntaxNode> newNodes, bool trackNewNodes)
        {
            CheckNodeInOriginalTreeOrTracked(node);
            newNodes = ApplyTrackingToNewNodesIfRequired(newNodes, trackNewNodes);
            _changes.Add(new InsertChange(node, newNodes, isBefore: true));
        }

        /// <summary>
        /// Insert the new node before the specified node already existing in the tree.
        /// </summary>
        /// <param name="node">The node already existing in the tree that the new nodes will be placed before. This must be a node this is contained within a syntax list.</param>
        /// <param name="newNode">The node to place before the existing node. This node must be of a compatible type to be placed in the same list containing the existing node.</param>
        public void InsertBefore(SyntaxNode node, SyntaxNode newNode)
            => InsertBefore(node, newNode, trackNewNode: false);

        /// <summary>
        /// Insert the new node before the specified node already existing in the tree.
        /// </summary>
        /// <param name="node">The node already existing in the tree that the new nodes will be placed before. This must be a node this is contained within a syntax list.</param>
        /// <param name="newNode">The node to place before the existing node. This node must be of a compatible type to be placed in the same list containing the existing node.</param>
        /// <param name="trackNewNode">Flag to indicate if the new node should be tracked for further edits.</param>
        internal void InsertBefore(SyntaxNode node, SyntaxNode newNode, bool trackNewNode)
            => this.InsertBefore(node, new[] { newNode }, trackNewNode);

        /// <summary>
        /// Insert the new nodes after the specified node already existing in the tree.
        /// </summary>
        /// <param name="node">The node already existing in the tree that the new nodes will be placed after. This must be a node this is contained within a syntax list.</param>
        /// <param name="newNodes">The nodes to place after the existing node. These nodes must be of a compatible type to be placed in the same list containing the existing node.</param>
        public void InsertAfter(SyntaxNode node, IEnumerable<SyntaxNode> newNodes)
            => InsertAfter(node, newNodes, trackNewNodes: false);

        /// <summary>
        /// Insert the new nodes after the specified node already existing in the tree.
        /// </summary>
        /// <param name="node">The node already existing in the tree that the new nodes will be placed after. This must be a node this is contained within a syntax list.</param>
        /// <param name="newNodes">The nodes to place after the existing node. These nodes must be of a compatible type to be placed in the same list containing the existing node.</param>
        /// <param name="trackNewNodes">Flag to indicate if the new nodes should be tracked for further edits.</param>
        internal void InsertAfter(SyntaxNode node, IEnumerable<SyntaxNode> newNodes, bool trackNewNodes)
        {
            CheckNodeInOriginalTreeOrTracked(node);
            newNodes = ApplyTrackingToNewNodesIfRequired(newNodes, trackNewNodes);
            _changes.Add(new InsertChange(node, newNodes, isBefore: false));
        }

        /// <summary>
        /// Insert the new node after the specified node already existing in the tree.
        /// </summary>
        /// <param name="node">The node already existing in the tree that the new nodes will be placed after. This must be a node this is contained within a syntax list.</param>
        /// <param name="newNode">The node to place after the existing node. This node must be of a compatible type to be placed in the same list containing the existing node.</param>
        public void InsertAfter(SyntaxNode node, SyntaxNode newNode)
            => this.InsertAfter(node, newNode, trackNewNode: false);

        /// <summary>
        /// Insert the new node after the specified node already existing in the tree.
        /// </summary>
        /// <param name="node">The node already existing in the tree that the new nodes will be placed after. This must be a node this is contained within a syntax list.</param>
        /// <param name="newNode">The node to place after the existing node. This node must be of a compatible type to be placed in the same list containing the existing node.</param>
        /// <param name="trackNewNode">Flag to indicate if the new node should be tracked for further edits.</param>
        internal void InsertAfter(SyntaxNode node, SyntaxNode newNode, bool trackNewNode)
            => InsertAfter(node, new[] { newNode }, trackNewNodes: trackNewNode);

        private void ApplyTrackingToNodesToInsert(params SyntaxNode[] nodes)
        {

        }

        private void CheckNodeInOriginalTreeOrTracked(SyntaxNode node)
        {
            if (!OriginalRoot.Contains(node) &&
                !_allowEditsOnLazilyCreatedTrackedNewNodes &&
                (_lazyTrackedNewNodesOpt == null || !_lazyTrackedNewNodesOpt.Contains(node)))
            {
                throw new ArgumentException(WorkspacesResources.The_node_is_not_part_of_the_tree, nameof(node));
            }
        }

        private abstract class Change
        {
            internal readonly SyntaxNode Node;

            public Change(SyntaxNode node)
            {
                this.Node = node;
            }

            public abstract SyntaxNode Apply(SyntaxNode root, SyntaxGenerator generator);
        }

        private class NoChange : Change
        {
            public NoChange(SyntaxNode node)
                : base(node)
            {
            }

            public override SyntaxNode Apply(SyntaxNode root, SyntaxGenerator generator)
            {
                return root;
            }
        }

        private class RemoveChange : Change
        {
            private readonly SyntaxRemoveOptions _options;

            public RemoveChange(SyntaxNode node, SyntaxRemoveOptions options)
                : base(node)
            {
                _options = options;
            }

            public override SyntaxNode Apply(SyntaxNode root, SyntaxGenerator generator)
            {
                return generator.RemoveNode(root, root.GetCurrentNode(this.Node), _options);
            }
        }

        private class ReplaceChange : Change
        {
            private readonly Func<SyntaxNode, SyntaxGenerator, SyntaxNode> _modifier;
            private readonly Func<SyntaxNode, SyntaxNode> _applyTrackingToNewNode;
            private readonly bool _trackNewNode;

            public ReplaceChange(
                SyntaxNode node,
                Func<SyntaxNode, SyntaxGenerator, SyntaxNode> modifier,
                Func<SyntaxNode, SyntaxNode> applyTrackingToNewNode,
                bool trackNewNode)
                : base(node)
            {
                _modifier = modifier;
                _applyTrackingToNewNode = applyTrackingToNewNode;
                _trackNewNode = trackNewNode;
            }

            public override SyntaxNode Apply(SyntaxNode root, SyntaxGenerator generator)
            {
                var current = root.GetCurrentNode(this.Node);
                var newNode = _modifier(current, generator);
                newNode = _trackNewNode ? _applyTrackingToNewNode(newNode) : newNode;
                return generator.ReplaceNode(root, current, newNode);
            }
        }

        private class ReplaceChange<TArgument> : Change
        {
            private readonly Func<SyntaxNode, SyntaxGenerator, TArgument, SyntaxNode> _modifier;
            private readonly TArgument _argument;
            private readonly Func<SyntaxNode, SyntaxNode> _applyTrackingToNewNode;
            private readonly bool _trackNewNode;

            public ReplaceChange(
                SyntaxNode node,
                Func<SyntaxNode, SyntaxGenerator, TArgument, SyntaxNode> modifier,
                TArgument argument,
                Func<SyntaxNode, SyntaxNode> applyTrackingToNewNode,
                bool trackNewNode)
                : base(node)
            {
                _modifier = modifier;
                _argument = argument;
                _applyTrackingToNewNode = applyTrackingToNewNode;
                _trackNewNode = trackNewNode;
            }

            public override SyntaxNode Apply(SyntaxNode root, SyntaxGenerator generator)
            {
                var current = root.GetCurrentNode(this.Node);
                var newNode = _modifier(current, generator, _argument);
                newNode = _trackNewNode ? _applyTrackingToNewNode(newNode) : newNode;
                return generator.ReplaceNode(root, current, newNode);
            }
        }

        private class InsertChange : Change
        {
            private readonly List<SyntaxNode> _newNodes;
            private readonly bool _isBefore;

            public InsertChange(SyntaxNode node, IEnumerable<SyntaxNode> newNodes, bool isBefore)
                : base(node)
            {
                _newNodes = newNodes.ToList();
                _isBefore = isBefore;
            }

            public override SyntaxNode Apply(SyntaxNode root, SyntaxGenerator generator)
            {
                if (_isBefore)
                {
                    return generator.InsertNodesBefore(root, root.GetCurrentNode(this.Node), _newNodes);
                }
                else
                {
                    return generator.InsertNodesAfter(root, root.GetCurrentNode(this.Node), _newNodes);
                }
            }
        }
    }
}
