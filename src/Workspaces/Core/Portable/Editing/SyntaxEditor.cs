// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.CodeAnalysis.Host;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editing
{
    /// <summary>
    /// An editor for making changes to a syntax tree. 
    /// </summary>
    public class SyntaxEditor
    {
        private readonly SyntaxGenerator _generator;
        private readonly List<Change> _changes = new();

        /// <summary>
        /// Creates a new <see cref="SyntaxEditor"/> instance.
        /// </summary>
        [Obsolete("Use SyntaxEditor(SyntaxNode, HostWorkspaceServices)")]
        public SyntaxEditor(SyntaxNode root, Workspace workspace)
            : this(root, (workspace ?? throw new ArgumentNullException(nameof(workspace))).Services.SolutionServices)
        {
        }

        /// <summary>
        /// Creates a new <see cref="SyntaxEditor"/> instance.
        /// </summary>
        public SyntaxEditor(SyntaxNode root, HostWorkspaceServices services)
            : this(root, (services ?? throw new ArgumentNullException(nameof(services))).SolutionServices)
        {
        }

        /// <summary>
        /// Creates a new <see cref="SyntaxEditor"/> instance.
        /// </summary>
        public SyntaxEditor(SyntaxNode root, SolutionServices services)
            : this(root ?? throw new ArgumentNullException(nameof(root)),
                   SyntaxGenerator.GetGenerator(services ?? throw new ArgumentNullException(nameof(services)), root.Language))
        {
        }

        internal SyntaxEditor(SyntaxNode root, SyntaxGenerator generator)
        {
            OriginalRoot = root;
            _generator = generator;
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
            var nodes = Enumerable.Distinct(_changes.Where(c => OriginalRoot.Contains(c.OriginalNode))
                                                    .Select(c => c.OriginalNode));
            var newRoot = OriginalRoot.TrackNodes(nodes);

            foreach (var change in _changes)
                newRoot = change.Apply(newRoot, _generator);

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
            => RemoveNode(node, SyntaxGenerator.DefaultRemoveOptions);

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
                throw new ArgumentNullException(nameof(computeReplacement));

            _changes.Add(new ReplaceChange(node, computeReplacement));
        }

        internal void ReplaceNode(SyntaxNode node, Func<SyntaxNode, SyntaxGenerator, IEnumerable<SyntaxNode>> computeReplacement)
        {
            CheckNodeInOriginalTreeOrTracked(node);
            if (computeReplacement == null)
                throw new ArgumentNullException(nameof(computeReplacement));

            _changes.Add(new ReplaceWithCollectionChange(node, computeReplacement));
        }

        internal void ReplaceNode<TArgument>(SyntaxNode node, Func<SyntaxNode, SyntaxGenerator, TArgument, SyntaxNode> computeReplacement, TArgument argument)
        {
            CheckNodeInOriginalTreeOrTracked(node);
            if (computeReplacement == null)
                throw new ArgumentNullException(nameof(computeReplacement));

            _changes.Add(new ReplaceChange<TArgument>(node, computeReplacement, argument));
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
                return;

            _changes.Add(new ReplaceChange(node, (n, g) => newNode));
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
                throw new ArgumentNullException(nameof(newNodes));

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
                throw new ArgumentNullException(nameof(newNodes));

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
                throw new ArgumentNullException(nameof(node));

            if (OriginalRoot.Contains(node))
                return;

            throw new ArgumentException(WorkspacesResources.The_node_is_not_part_of_the_tree, nameof(node));
        }

        private abstract class Change
        {
            internal readonly SyntaxNode OriginalNode;

            public Change(SyntaxNode node)
                => OriginalNode = node;

            public SyntaxNode Apply(SyntaxNode root, SyntaxGenerator generator)
            {
                var currentNode = root.GetCurrentNode(OriginalNode);
                if (currentNode is null)
                    Contract.Fail($"GetCurrentNode returned null with the following node: {OriginalNode}");

                return Apply(root, currentNode, generator);
            }

            protected static SyntaxNode ValidateNewRoot(SyntaxNode? root)
                => root ?? throw new InvalidOperationException("Tree root deleted");

            protected abstract SyntaxNode Apply(SyntaxNode root, SyntaxNode currentNode, SyntaxGenerator generator);
        }

        private sealed class NoChange : Change
        {
            public NoChange(SyntaxNode node)
                : base(node)
            {
            }

            protected override SyntaxNode Apply(SyntaxNode root, SyntaxNode currentNode, SyntaxGenerator generator)
                => root;
        }

        private sealed class RemoveChange : Change
        {
            private readonly SyntaxRemoveOptions _options;

            public RemoveChange(SyntaxNode node, SyntaxRemoveOptions options)
                : base(node)
            {
                _options = options;
            }

            protected override SyntaxNode Apply(SyntaxNode root, SyntaxNode currentNode, SyntaxGenerator generator)
                => ValidateNewRoot(generator.RemoveNode(root, currentNode, _options));
        }

        private sealed class ReplaceChange : Change
        {
            private readonly Func<SyntaxNode, SyntaxGenerator, SyntaxNode?> _modifier;

            public ReplaceChange(
                SyntaxNode node,
                Func<SyntaxNode, SyntaxGenerator, SyntaxNode?> modifier)
                : base(node)
            {
                Contract.ThrowIfNull(node);
                _modifier = modifier;
            }

            protected override SyntaxNode Apply(SyntaxNode root, SyntaxNode currentNode, SyntaxGenerator generator)
                => ValidateNewRoot(generator.ReplaceNode(root, currentNode, _modifier(currentNode, generator)));
        }

        private sealed class ReplaceWithCollectionChange : Change
        {
            private readonly Func<SyntaxNode, SyntaxGenerator, IEnumerable<SyntaxNode>> _modifier;

            public ReplaceWithCollectionChange(
                SyntaxNode node,
                Func<SyntaxNode, SyntaxGenerator, IEnumerable<SyntaxNode>> modifier)
                : base(node)
            {
                _modifier = modifier;
            }

            protected override SyntaxNode Apply(SyntaxNode root, SyntaxNode currentNode, SyntaxGenerator generator)
                => SyntaxGenerator.ReplaceNode(root, currentNode, _modifier(currentNode, generator));
        }

        private sealed class ReplaceChange<TArgument> : Change
        {
            private readonly Func<SyntaxNode, SyntaxGenerator, TArgument, SyntaxNode> _modifier;
            private readonly TArgument _argument;

            public ReplaceChange(
                SyntaxNode node,
                Func<SyntaxNode, SyntaxGenerator, TArgument, SyntaxNode> modifier,
                TArgument argument)
                : base(node)
            {
                _modifier = modifier;
                _argument = argument;
            }

            protected override SyntaxNode Apply(SyntaxNode root, SyntaxNode currentNode, SyntaxGenerator generator)
                => ValidateNewRoot(generator.ReplaceNode(root, currentNode, _modifier(currentNode, generator, _argument)));
        }

        private sealed class InsertChange : Change
        {
            private readonly List<SyntaxNode> _newNodes;
            private readonly bool _isBefore;

            public InsertChange(SyntaxNode node, IEnumerable<SyntaxNode> newNodes, bool isBefore)
                : base(node)
            {
                _newNodes = newNodes.ToList();
                _isBefore = isBefore;
            }

            protected override SyntaxNode Apply(SyntaxNode root, SyntaxNode currentNode, SyntaxGenerator generator)
                => _isBefore
                    ? generator.InsertNodesBefore(root, currentNode, _newNodes)
                    : generator.InsertNodesAfter(root, currentNode, _newNodes);
        }
    }
}
