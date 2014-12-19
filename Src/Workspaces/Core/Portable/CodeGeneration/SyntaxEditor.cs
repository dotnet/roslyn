// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.CodeGeneration
{
    /// <summary>
    /// An editor for making multiple edits to a syntax tree. 
    /// </summary>
    public sealed class SyntaxEditor
    {
        private readonly SyntaxGenerator generator;
        private readonly SyntaxNode root;
        private readonly List<Change> changes;

        private SyntaxEditor(SyntaxNode root, SyntaxGenerator generator)
        {
            this.root = root;
            this.generator = generator;
            this.changes = new List<Change>();
        }

        /// <summary>
        /// Creates a new <see cref="SyntaxEditor"/> instance.
        /// </summary>
        public static SyntaxEditor Create(Workspace workspace, SyntaxNode root)
        {
            if (workspace == null)
            {
                throw new ArgumentNullException(nameof(workspace));
            }

            if (root == null)
            {
                throw new ArgumentNullException(nameof(root));
            }

            return new SyntaxEditor(root, SyntaxGenerator.GetGenerator(workspace, root.Language));
        }

        /// <summary>
        /// The original root node.
        /// </summary>
        public SyntaxNode OriginalRoot
        {
            get { return this.root; }
        }

        /// <summary>
        /// A syntax generator to use to create new node instances.
        /// </summary>
        public SyntaxGenerator Generator
        {
            get { return this.generator; }
        }

        /// <summary>
        /// Returns the changed root node including all the applied edits.
        /// </summary>
        public SyntaxNode GetChangedRoot()
        {
            var nodes = Enumerable.Distinct(this.changes.Select(c => c.Node));
            var newRoot = this.root.TrackNodes(nodes);

            foreach (var change in this.changes)
            {
                newRoot = change.Apply(newRoot, this.generator);
            }

            return newRoot;
        }

        /// <summary>
        /// Makes sure the node is tracked, even if it is not changed.
        /// You do not need to call this method if the node is changed via a Remove, Replace or Insert.
        /// </summary>
        internal void TrackNode(SyntaxNode node)
        {
            this.changes.Add(new NoChange(node));
        }

        /// <summary>
        /// Remove the node from the tree.
        /// </summary>
        /// <param name="node">The node to remove that currently exists as part of the tree.</param>
        public void RemoveNode(SyntaxNode node)
        {
            this.changes.Add(new RemoveChange(node));
        }

        /// <summary>
        /// Replace the specified node with a node produced by the function.
        /// </summary>
        /// <param name="node">The node to replace that already exists in the tree.</param>
        /// <param name="computeReplacement">A function that computes a replacement node. 
        /// The node passed into the compute function includes changes from prior edits. It will not appear as a descendant of the original root.</param>
        public void ReplaceNode(SyntaxNode node, Func<SyntaxNode, SyntaxGenerator, SyntaxNode> computeReplacement)
        {
            this.changes.Add(new ReplaceChange(node, computeReplacement));
        }

        /// <summary>
        /// Replace the specified node with a different node.
        /// </summary>
        /// <param name="node">The node to replace that already exists in the tree.</param>
        /// <param name="newNode">The new node that will be placed into the tree in the existing node's location.</param>
        public void ReplaceNode(SyntaxNode node, SyntaxNode newNode)
        {
            this.ReplaceNode(node, (n, g) => newNode);
        }

        /// <summary>
        /// Insert the new nodes before the specified node already existing in the tree.
        /// </summary>
        /// <param name="node">The node already existing in the tree that the new nodes will be placed before. This must be a node this is contained within a syntax list.</param>
        /// <param name="newNodes">The nodes to place before the existing node. These nodes must be of a compatible type to be placed in the same list containing the existing node.</param>
        public void InsertBefore(SyntaxNode node, IEnumerable<SyntaxNode> newNodes)
        {
            this.changes.Add(new InsertChange(node, newNodes, isBefore: true));
        }

        /// <summary>
        /// Insert the new node before the specified node already existing in the tree.
        /// </summary>
        /// <param name="node">The node already existing in the tree that the new nodes will be placed before. This must be a node this is contained within a syntax list.</param>
        /// <param name="newNode">The node to place before the existing node. This node must be of a compatible type to be placed in the same list containing the existing node.</param>
        public void InsertBefore(SyntaxNode node, SyntaxNode newNode)
        {
            this.InsertBefore(node, new[] { newNode });
        }

        /// <summary>
        /// Insert the new nodes after the specified node already existing in the tree.
        /// </summary>
        /// <param name="node">The node already existing in the tree that the new nodes will be placed after. This must be a node this is contained within a syntax list.</param>
        /// <param name="newNodes">The nodes to place after the existing node. These nodes must be of a compatible type to be placed in the same list containing the existing node.</param>
        public void InsertAfter(SyntaxNode node, IEnumerable<SyntaxNode> newNodes)
        {
            this.changes.Add(new InsertChange(node, newNodes, isBefore: false));
        }

        /// <summary>
        /// Insert the new node after the specified node already existing in the tree.
        /// </summary>
        /// <param name="node">The node already existing in the tree that the new nodes will be placed after. This must be a node this is contained within a syntax list.</param>
        /// <param name="newNode">The node to place after the existing node. This node must be of a compatible type to be placed in the same list containing the existing node.</param>
        public void InsertAfter(SyntaxNode node, SyntaxNode newNode)
        {
            this.InsertBefore(node, new[] { newNode });
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
            public RemoveChange(SyntaxNode node)
                : base(node)
            {
            }

            public override SyntaxNode Apply(SyntaxNode root, SyntaxGenerator generator)
            {
                return generator.RemoveNode(root, root.GetCurrentNode(this.Node));
            }
        }

        private class ReplaceChange : Change
        {
            private Func<SyntaxNode, SyntaxGenerator, SyntaxNode> modifier;

            public ReplaceChange(SyntaxNode node, Func<SyntaxNode, SyntaxGenerator, SyntaxNode> modifier)
                : base(node)
            {
                this.modifier = modifier;
            }

            public override SyntaxNode Apply(SyntaxNode root, SyntaxGenerator generator)
            {
                var current = root.GetCurrentNode(this.Node);
                var newNode = modifier(current, generator);
                return generator.ReplaceNode(root, current, newNode);
            }
        }

        private class InsertChange : Change
        {
            private List<SyntaxNode> newNodes;
            private bool isBefore;

            public InsertChange(SyntaxNode node, IEnumerable<SyntaxNode> newNodes, bool isBefore)
                : base(node)
            {
                this.newNodes = newNodes.ToList();
                this.isBefore = isBefore;
            }

            public override SyntaxNode Apply(SyntaxNode root, SyntaxGenerator generator)
            {
                if (this.isBefore)
                {
                    return generator.InsertNodesBefore(root, root.GetCurrentNode(this.Node), this.newNodes);
                }
                else
                {
                    return generator.InsertNodesAfter(root, root.GetCurrentNode(this.Node), this.newNodes);
                }
            }
        }
    }
}
