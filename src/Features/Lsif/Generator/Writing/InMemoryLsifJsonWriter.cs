// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.CodeAnalysis.Lsif.Generator.LsifGraph;

namespace Microsoft.CodeAnalysis.Lsif.Generator.Writing
{
    internal sealed class InMemoryLsifJsonWriter : ILsifJsonWriter
    {
        private readonly List<Vertex> _vertices = new List<Vertex>();
        private readonly List<Edge> _edges = new List<Edge>();

        public void Write(Vertex vertex)
        {
            _vertices.Add(vertex);
        }

        public void Write(Edge edge)
        {
            _edges.Add(edge);
        }

        public void CopyTo(ILsifJsonWriter writer)
        {
            // We always write vertices before edges, as the underlying LSIF format requires that vertices used by an edge must
            // be written before the edge. The easiest way to ensure this is just write all vertices first.
            foreach (var vertex in _vertices)
            {
                writer.Write(vertex);
            }

            foreach (var edge in _edges)
            {
                writer.Write(edge);
            }
        }
    }
}
