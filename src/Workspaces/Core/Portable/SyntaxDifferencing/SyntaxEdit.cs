// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using Microsoft.CodeAnalysis.Differencing;

namespace Microsoft.CodeAnalysis.SyntaxDifferencing
{
    /// <summary>
    /// Represents an edit operation on a tree or a sequence of nodes.
    /// </summary>
    [DebuggerDisplay("{GetDebuggerDisplay(), nq}")]
    public struct SyntaxEdit : IEquatable<SyntaxEdit>
    {
        private readonly Edit<SyntaxNode> _edit;

        internal SyntaxEdit(Edit<SyntaxNode> edit)
        {
            _edit = edit;
        }

        public SyntaxEditKind Kind => (SyntaxEditKind)_edit.Kind;

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
        public SyntaxNode OldNode => _edit.OldNode;

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
        public SyntaxNode NewNode => _edit.NewNode;

        public override bool Equals(object obj)
        {
            return obj is SyntaxEdit && Equals((SyntaxEdit)obj);
        }

        public bool Equals(SyntaxEdit other)
        {
            return _edit.Equals(other._edit);
        }

        public override int GetHashCode()
        {
            return _edit.GetHashCode();
        }

        // Has to be 'internal' for now as it's used by EnC test tool
        internal string GetDebuggerDisplay()
        {
            return _edit.GetDebuggerDisplay();
        }
    }
}