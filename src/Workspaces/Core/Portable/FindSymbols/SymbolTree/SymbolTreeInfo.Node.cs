// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.FindSymbols
{
    internal partial class SymbolTreeInfo
    {
        private const int RootNodeParentIndex = -1;

        [DebuggerDisplay("{GetDebuggerDisplay(),nq}")]
        private struct BuilderNode
        {
            public readonly string Name;
            public readonly int ParentIndex;

            public BuilderNode(string name, int parentIndex)
            {
                Name = name;
                ParentIndex = parentIndex;
            }

            public bool IsRoot =>
                ParentIndex == RootNodeParentIndex;

            private string GetDebuggerDisplay()
            {
                return Name + ", " + ParentIndex;
            }
        }

        /// <summary>
        /// A node represents a single unique name in a dotted-name tree.
        /// Uniqueness is always case sensitive.
        /// </summary>
        [DebuggerDisplay("{GetDebuggerDisplay(),nq}")]
        private struct Node
        {
            public readonly TextSpan NameSpan;
            public readonly int ParentIndex;

            public Node(TextSpan wordSpan, int parentIndex)
            {
                NameSpan = wordSpan;
                ParentIndex = parentIndex;
            }

            public bool IsRoot =>
                ParentIndex == RootNodeParentIndex;

            public void AssertEquivalentTo(Node node)
            {
                Debug.Assert(node.NameSpan == this.NameSpan);
                Debug.Assert(node.ParentIndex == this.ParentIndex);
            }
        }
    }
}
