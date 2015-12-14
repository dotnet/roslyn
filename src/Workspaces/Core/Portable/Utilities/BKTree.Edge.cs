// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Roslyn.Utilities
{
    internal partial class BKTree
    {
        private struct Edge
        {
            // The edit distance between the child and parent connected by this edge.
            // The child can be found in _nodes at ChildNodeIndex. 
            public readonly int EditDistance;

            /// <summary>Where the child node can be found in <see cref="_nodes"/>.</summary>
            public readonly int ChildNodeIndex;

            public Edge(int editDistance, int childNodeIndex)
            {
                EditDistance = editDistance;
                ChildNodeIndex = childNodeIndex;
            }

            internal void WriteTo(ObjectWriter writer)
            {
                writer.WriteInt32(EditDistance);
                writer.WriteInt32(ChildNodeIndex);
            }

            internal static Edge ReadFrom(ObjectReader reader)
            {
                return new Edge(editDistance: reader.ReadInt32(), childNodeIndex: reader.ReadInt32());
            }
        }
    }
}