// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Text;

namespace Roslyn.Utilities
{
    internal partial class BKTree
    {
        private struct Node
        {
            /// <summary>
            /// The string this node corresponds to.  Specifically, this span is the range of
            /// <see cref="_concatenatedLowerCaseWords"/> for that string.
            /// </summary>
            public readonly TextSpan WordSpan;

            ///<summary>How many child edges this node has.</summary>
            public readonly int EdgeCount;

            ///<summary>Where the first edge can be found in <see cref="_edges"/>.  The edges 
            ///are in the range _edges[FirstEdgeIndex, FirstEdgeIndex + EdgeCount)
            ///</summary>
            public readonly int FirstEdgeIndex;

            public Node(TextSpan wordSpan, int edgeCount, int firstEdgeIndex)
            {
                WordSpan = wordSpan;
                EdgeCount = edgeCount;
                FirstEdgeIndex = firstEdgeIndex;
            }

            internal void WriteTo(ObjectWriter writer)
            {
                writer.WriteInt32(WordSpan.Start);
                writer.WriteInt32(WordSpan.Length);
                writer.WriteInt32(EdgeCount);
                writer.WriteInt32(FirstEdgeIndex);
            }

            internal static Node ReadFrom(ObjectReader reader)
            {
                return new Node(
                    new TextSpan(start: reader.ReadInt32(), length: reader.ReadInt32()),
                    edgeCount: reader.ReadInt32(), firstEdgeIndex: reader.ReadInt32());
            }
        }
    }
}
