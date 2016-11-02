// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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

            public bool IsRoot => ParentIndex == RootNodeParentIndex;

            private string GetDebuggerDisplay()
            {
                return Name + ", " + ParentIndex;
            }
        }

        [DebuggerDisplay("{GetDebuggerDisplay(),nq}")]
        private struct Node
        {
            /// <summary>
            /// Span in <see cref="_concatenatedNames"/> of the Name of this Node.
            /// </summary>
            public readonly TextSpan NameSpan;

            /// <summary>
            /// Index in <see cref="_nodes"/> of the parent Node of this Node.
            /// Value will be <see cref="RootNodeParentIndex"/> if this is the 
            /// Node corresponding to the root symbol.
            /// </summary>
            public readonly int ParentIndex;

            public Node(TextSpan wordSpan, int parentIndex)
            {
                NameSpan = wordSpan;
                ParentIndex = parentIndex;
            }

            public bool IsRoot => ParentIndex == RootNodeParentIndex;

            public void AssertEquivalentTo(Node node)
            {
                Debug.Assert(node.NameSpan == this.NameSpan);
                Debug.Assert(node.ParentIndex == this.ParentIndex);
            }

            private string GetDebuggerDisplay()
            {
                return NameSpan + ", " + ParentIndex;
            }
        }
    }
}