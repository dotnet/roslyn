// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Roslyn.Utilities
{
    internal partial class BKTree
    {
        private struct Edge
        {
            // The edit distance between the child and parent connected by this edge.
            public readonly int EditDistance;

            // Where the child node can be found on "_nodes"
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
                return new Edge(reader.ReadInt32(), reader.ReadInt32());
            }
        }
    }
}