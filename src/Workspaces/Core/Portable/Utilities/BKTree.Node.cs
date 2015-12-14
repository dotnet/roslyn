// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Text;

namespace Roslyn.Utilities
{
    internal partial class BKTree
    {
        private struct Node
        {
            // The string this node corresponds to.  Specifically, this span is the range of
            // _allLowerCaseCharacters for that string.
            public readonly TextSpan CharacterSpan;

            // How many children/edges this node has.
            public readonly int EdgeCount;

            // Where the first edge can be found in "_edges".  The edges are in the range:
            // _edges[FirstEdgeIndex, FirstEdgeIndex + EdgeCount)
            public readonly int FirstEdgeIndex;

            public Node(TextSpan characterSpan, int edgeCount, int firstEdgeIndex)
            {
                CharacterSpan = characterSpan;
                EdgeCount = edgeCount;
                FirstEdgeIndex = firstEdgeIndex;
            }

            internal void WriteTo(ObjectWriter writer)
            {
                writer.WriteInt32(CharacterSpan.Start);
                writer.WriteInt32(CharacterSpan.Length);
                writer.WriteInt32(EdgeCount);
                writer.WriteInt32(FirstEdgeIndex);
            }

            internal static Node ReadFrom(ObjectReader reader)
            {
                return new Node(new TextSpan(reader.ReadInt32(), reader.ReadInt32()), reader.ReadInt32(), reader.ReadInt32());
            }
        }
    }
}
