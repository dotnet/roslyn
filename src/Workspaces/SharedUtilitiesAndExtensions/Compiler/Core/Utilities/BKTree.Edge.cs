// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.Utilities;

internal readonly partial struct BKTree
{
    private readonly struct Edge(int editDistance, int childNodeIndex)
    {
        // The edit distance between the child and parent connected by this edge.
        // The child can be found in _nodes at ChildNodeIndex. 
        public readonly int EditDistance = editDistance;

        /// <summary>Where the child node can be found in <see cref="_nodes"/>.</summary>
        public readonly int ChildNodeIndex = childNodeIndex;

        internal void WriteTo(ObjectWriter writer)
        {
            writer.WriteInt32(EditDistance);
            writer.WriteInt32(ChildNodeIndex);
        }

        internal static Edge ReadFrom(ObjectReader reader)
            => new(editDistance: reader.ReadInt32(), childNodeIndex: reader.ReadInt32());
    }
}
