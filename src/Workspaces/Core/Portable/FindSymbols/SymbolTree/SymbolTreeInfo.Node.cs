// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.FindSymbols
{
    internal partial class SymbolTreeInfo
    {
        private const int RootNodeParentIndex = -1;

        /// <summary>
        /// <see cref="BuilderNode"/>s are produced when initially creating our indices.
        /// They store Names of symbols and the index of their parent symbol.  When we
        /// produce the final <see cref="SymbolTreeInfo"/> though we will then convert
        /// these to <see cref="Node"/>s.  Those nodes will not point to individual 
        /// strings, but will instead point at <see cref="_concatenatedNames"/>.
        /// </summary>
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
        /// <see cref="Node"/>
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
