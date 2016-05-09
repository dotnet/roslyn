// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Diagnostics;

namespace Microsoft.CodeAnalysis.FindSymbols
{
    internal partial class SymbolTreeInfo
    {
        /// <summary>
        /// A node represents a single unique name in a dotted-name tree.
        /// Uniqueness is always case sensitive.
        /// </summary>
        [DebuggerDisplay("{GetDebuggerDisplay(),nq}")]
        private struct Node
        {
            private readonly string _name;
            private readonly int _parentIndex;

            public const int RootNodeParentIndex = -1;

            public Node(string name, int parentIndex)
            {
                _name = name;
                _parentIndex = parentIndex;
            }

            public string Name
            {
                get { return _name; }
            }

            public int ParentIndex
            {
                get { return _parentIndex; }
            }

            public bool IsRoot
            {
                get { return _parentIndex == RootNodeParentIndex; }
            }

            public bool IsEquivalent(Node node)
            {
                return (node.Name == this.Name) && (node.ParentIndex == this.ParentIndex);
            }

            private string GetDebuggerDisplay()
            {
                return _name + ", " + _parentIndex;
            }
        }
    }
}
