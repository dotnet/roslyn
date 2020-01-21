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
            if (node == null)
            {
                return null;
            }

            _lazyTrackedNewNodesOpt ??= new HashSet<SyntaxNode>();
            foreach (var descendant in node.DescendantNodesAndSelf())
            {
                _lazyTrackedNewNodesOpt.Add(descendant);
            }

            return node.TrackNodes(node.DescendantNodesAndSelf());
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
        {
            CheckNodeInOriginalTreeOrTracked(node);
            if (computeReplacement == null)
            {
                throw new ArgumentNullException(nameof(computeReplacement));
            }

            _allowEditsOnLazilyCreatedTrackedNewNodes = true;
            _changes.Add(new ReplaceChange(node, computeReplacement, this));
        }

        internal void ReplaceNode(SyntaxNode node, Func<SyntaxNode, SyntaxGenerator, IEnumerable<SyntaxNode>> computeReplacement)
        {
            CheckNodeInOriginalTreeOrTracked(node);
            if (computeReplacement == null)
            {
                throw new ArgumentNullException(nameof(computeReplacement));
            }

            _allowEditsOnLazilyCreatedTrackedNewNodes = true;
            _changes.Add(new ReplaceWithCollectionChange(node, computeReplacement, this));
        }


        internal void ReplaceNode<TArgument>(SyntaxNode node, Func<SyntaxNode, SyntaxGenerator, TArgument, SyntaxNode> computeReplacement, TArgument argument)
        {
            CheckNodeInOriginalTreeOrTracked(node);
            if (computeReplacement == null)
            {
                throw new ArgumentNullException(nameof(computeReplacement));
            }

            _allowEditsOnLazilyCreatedTrackedNewNodes = true;
            _changes.Add(new ReplaceChange<TArgument>(node, computeReplacement, argument, this));
        }

        /// <summary>
        /// Replace the specified node with a different node.
        /// </summary>
        /// <param name="node">The node to replace that already exists in the tree.</param>
        /// <param name="newNode">The new node that will be placed into the tree in the existing node's location.</param>
        public void ReplaceNode(SyntaxNode node, SyntaxNode newNode)
        {
            CheckNodeInOriginalTreeOrTracked(node);
            if (node == newNode)
            {
                return;
            }

            newNode = ApplyTrackingToNewNode(newNode);
            _changes.Add(new ReplaceChange(node, (n, g) => newNode, this));
        }

        /// <summary>
        /// Insert the new nodes before the specified node already existing in the tree.
        /// </summary>
        /// <param name="node">The node already existing in the tree that the new nodes will be placed before. This must be a node this is contained within a syntax list.</param>
        /// <param name="newNodes">The nodes to place before the existing node. These nodes must be of a compatible type to be placed in the same list containing the existing node.</param>
        public void InsertBefore(SyntaxNode node, IEnumerable<SyntaxNode> newNodes)
        {
            CheckNodeInOriginalTreeOrTracked(node);
            if (newNodes == null)
            {
                throw new ArgumentNullException(nameof(newNodes));
            }

            newNodes = ApplyTrackingToNewNodes(newNodes);
            _changes.Add(new InsertChange(node, newNodes, isBefore: true));
        }

        /// <summary>
        /// Insert the new node before the specified node already existing in the tree.
        /// </summary>
        /// <param name="node">The node already existing in the tree that the new nodes will be placed before. This must be a node this is contained within a syntax list.</param>
        /// <param name="newNode">The node to place before the existing node. This node must be of a compatible type to be placed in the same list containing the existing node.</param>
        public void InsertBefore(SyntaxNode node, SyntaxNode newNode)
            => InsertBefore(node, new[] { newNode });

        /// <summary>
        /// Insert the new nodes after the specified node already existing in the tree.
        /// </summary>
        /// <param name="node">The node already existing in the tree that the new nodes will be placed after. This must be a node this is contained within a syntax list.</param>
        /// <param name="newNodes">The nodes to place after the existing node. These nodes must be of a compatible type to be placed in the same list containing the existing node.</param>
        public void InsertAfter(SyntaxNode node, IEnumerable<SyntaxNode> newNodes)
        {
            CheckNodeInOriginalTreeOrTracked(node);
            if (newNodes == null)
            {
                throw new ArgumentNullException(nameof(newNodes));
            }

            newNodes = ApplyTrackingToNewNodes(newNodes);
            _changes.Add(new InsertChange(node, newNodes, isBefore: false));
        }

        /// <summary>
        /// Insert the new node after the specified node already existing in the tree.
        /// </summary>
        /// <param name="node">The node already existing in the tree that the new nodes will be placed after. This must be a node this is contained within a syntax list.</param>
        /// <param name="newNode">The node to place after the existing node. This node must be of a compatible type to be placed in the same list containing the existing node.</param>
        public void InsertAfter(SyntaxNode node, SyntaxNode newNode)
            => this.InsertAfter(node, new[] { newNode });

        private void CheckNodeInOriginalTreeOrTracked(SyntaxNode node)
        {
            if (node == null)
            {
                throw new ArgumentNullException(nameof(node));
            }

            if (OriginalRoot.Contains(node))
            {
                // Node is contained in the original tree.
                return;
            }

            if (_allowEditsOnLazilyCreatedTrackedNewNodes)
            {
                // This could be a node that is handed to us lazily from one of the prior edits
                // which support lazy replacement nodes, we conservatively avoid throwing here.
                // If this was indeed an unsupported node, syntax editor will throw an exception later
                // when attempting to compute changed root.
                return;
            }

            if (_lazyTrackedNewNodesOpt?.Contains(node) == true)
            {
                // Node is one of the new nodes, which is already tracked and supported.
                return;
            }

            throw new ArgumentException(WorkspacesResources.The_node_is_not_part_of_the_tree, nameof(node));
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
            private readonly SyntaxEditor _editor;

            public ReplaceChange(
                SyntaxNode node,
                Func<SyntaxNode, SyntaxGenerator, SyntaxNode> modifier,
                SyntaxEditor editor)
                : base(node)
            {
                _modifier = modifier;
                _editor = editor;
            }

            public override SyntaxNode Apply(SyntaxNode root, SyntaxGenerator generator)
            {
                var current = root.GetCurrentNode(this.Node);
                var newNode = _modifier(current, generator);
                newNode = _editor.ApplyTrackingToNewNode(newNode);
                return generator.ReplaceNode(root, current, newNode);
            }
        }

        private class ReplaceWithCollectionChange : Change
        {
            private readonly Func<SyntaxNode, SyntaxGenerator, IEnumerable<SyntaxNode>> _modifier;
            private readonly SyntaxEditor _editor;

            public ReplaceWithCollectionChange(
                SyntaxNode node,
                Func<SyntaxNode, SyntaxGenerator, IEnumerable<SyntaxNode>> modifier,
                SyntaxEditor editor)
                : base(node)
            {
                _modifier = modifier;
                _editor = editor;
            }

            public override SyntaxNode Apply(SyntaxNode root, SyntaxGenerator generator)
            {
                var current = root.GetCurrentNode(this.Node);
                var newNodes = _modifier(current, generator).ToList();
                for (int i = 0; i < newNodes.Count; i++)
                {
                    newNodes[i] = _editor.ApplyTrackingToNewNode(newNodes[i]);
                }

                return generator.ReplaceNode(root, current, newNodes);
            }
        }

        private class ReplaceChange<TArgument> : Change
        {
            private readonly Func<SyntaxNode, SyntaxGenerator, TArgument, SyntaxNode> _modifier;
            private readonly TArgument _argument;
            private readonly SyntaxEditor _editor;

            public ReplaceChange(
                SyntaxNode node,
                Func<SyntaxNode, SyntaxGenerator, TArgument, SyntaxNode> modifier,
                TArgument argument,
                SyntaxEditor editor)
                : base(node)
            {
                _modifier = modifier;
                _argument = argument;
                _editor = editor;
            }

            public override SyntaxNode Apply(SyntaxNode root, SyntaxGenerator generator)
            {
                var current = root.GetCurrentNode(this.Node);
                var newNode = _modifier(current, generator, _argument);
                newNode = _editor.ApplyTrackingToNewNode(newNode);
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
