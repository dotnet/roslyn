// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.SyntaxDifferencing
{
    /// <summary>
    /// Represents an edit operation on a tree or a sequence of nodes.
    /// </summary>
    [DebuggerDisplay("{GetDebuggerDisplay(), nq}")]
    internal struct SyntaxEdit : IEquatable<SyntaxEdit>
    {
        private readonly TreeComparer _comparer;
        private readonly SyntaxEditKind _kind;
        private readonly SyntaxNode _oldNode;
        private readonly SyntaxNode _newNode;

        internal SyntaxEdit(
            SyntaxEditKind kind,
            TreeComparer comparer,
            SyntaxNode oldNode,
            SyntaxNode newNode)
        {
            Debug.Assert((oldNode == null || oldNode.Equals(default(SyntaxNode))) == (kind == SyntaxEditKind.Insert));
            Debug.Assert((newNode == null || newNode.Equals(default(SyntaxNode))) == (kind == SyntaxEditKind.Delete));

            Debug.Assert((oldNode == null || oldNode.Equals(default(SyntaxNode))) ||
                         (newNode == null || newNode.Equals(default(SyntaxNode))) ||
                         !comparer.TreesEqual(oldNode, newNode));

            _comparer = comparer;
            _kind = kind;
            _oldNode = oldNode;
            _newNode = newNode;
        }

        public SyntaxEditKind Kind => _kind;

        /// <summary>
        /// Insert: 
        /// default(SyntaxNode).
        /// 
        /// Delete: 
        /// Deleted node.
        /// 
        /// Move, Update: 
        /// Node in the old tree/sequence.
        /// </summary>
        public SyntaxNode OldNode => _oldNode;

        /// <summary>
        /// Insert: 
        /// Inserted node.
        /// 
        /// Delete: 
        /// default(SyntaxNode)
        /// 
        /// Move, Update:
        /// Node in the new tree/sequence.
        /// </summary>
        public SyntaxNode NewNode => _newNode;

        public override bool Equals(object obj)
        {
            return obj is SyntaxEdit && Equals((SyntaxEdit)obj);
        }

        public bool Equals(SyntaxEdit other)
        {
            return _kind == other._kind
                && (_oldNode == null) ? other._oldNode == null : _oldNode.Equals(other._oldNode)
                && (_newNode == null) ? other._newNode == null : _newNode.Equals(other._newNode);
        }

        public override int GetHashCode()
        {
            int hash = (int)_kind;
            if (_oldNode != null)
            {
                hash = Hash.Combine(_oldNode.GetHashCode(), hash);
            }

            if (_newNode != null)
            {
                hash = Hash.Combine(_newNode.GetHashCode(), hash);
            }

            return hash;
        }

        // Has to be 'internal' for now as it's used by EnC test tool
        internal string GetDebuggerDisplay()
        {
            string result = Kind.ToString();
            switch (Kind)
            {
                case SyntaxEditKind.Delete:
                    return result + " [" + _oldNode.ToString() + "]" + DisplayPosition(_oldNode);

                case SyntaxEditKind.Insert:
                    return result + " [" + _newNode.ToString() + "]" + DisplayPosition(_newNode);

                case SyntaxEditKind.Update:
                    return result + " [" + _oldNode.ToString() + "]" + DisplayPosition(_oldNode) + " -> [" + _newNode.ToString() + "]" + DisplayPosition(_newNode);

                case SyntaxEditKind.Move:
                case SyntaxEditKind.Reorder:
                    return result + " [" + _oldNode.ToString() + "]" + DisplayPosition(_oldNode) + " -> " + DisplayPosition(_newNode);
            }

            return result;
        }

        private string DisplayPosition(SyntaxNode node)
        {
            return "@" + _comparer.GetSpan(node).Start;
        }
    }
}