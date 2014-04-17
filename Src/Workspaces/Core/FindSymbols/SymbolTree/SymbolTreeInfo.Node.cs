// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
            private readonly string name;
            private readonly int parentIndex;

            public const int RootNodeParentIndex = -1;

            public Node(string name, int parentIndex)
            {
                this.name = name;
                this.parentIndex = parentIndex;
            }

            public string Name
            {
                get { return this.name; }
            }

            public int ParentIndex
            {
                get { return this.parentIndex; }
            }

            public bool IsRoot
            {
                get { return this.parentIndex == RootNodeParentIndex; }
            }

            public bool IsEquivalent(Node node)
            {
                return (node.Name == this.Name) && (node.ParentIndex == this.ParentIndex);
            }

            private string GetDebuggerDisplay()
            {
                return name + ", " + parentIndex;
            }
        }
    }
}