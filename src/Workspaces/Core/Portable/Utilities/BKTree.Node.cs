using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Text;

namespace Roslyn.Utilities
{
    internal partial class BKTree
    {
        private struct Node
        {
            // The string this node corresponds to.  Stored in char[] format so we can easily compute
            // edit distances on it.
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
