// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using Newtonsoft.Json;

namespace Microsoft.CodeAnalysis.Lsif.Generator.LsifGraph
{
    /// <summary>
    /// Represents an edge from one vertex to another.
    /// </summary>
    internal class Edge : Element
    {
        [JsonProperty("outV")]
        public Id<Vertex> OutVertex { get; }

        // The LSIF format allows both for a one-to-one edge and a one-to-many edge. We'll just always use a one-to-many edge when
        // emitting since the format isn't really that much larger but keeps everything much simpler.
        [JsonProperty("inVs")]
        public Id<Vertex>[] InVertices { get; }

        public Edge(string label, Id<Vertex> outVertex, Id<Vertex>[] inVertices)
            : base(type: "edge", label: label)
        {
            OutVertex = outVertex;
            InVertices = inVertices;
        }

        public static Edge Create<TOutVertex, TInVertex>(string label, Id<TOutVertex> outVertex, Id<TInVertex> inVertex) where TOutVertex : Vertex where TInVertex : Vertex
        {
            var inVerticesArray = new Id<Vertex>[1];
            inVerticesArray[0] = inVertex.As<TInVertex, Vertex>();

            return new Edge(label, outVertex.As<TOutVertex, Vertex>(), inVerticesArray);
        }

        public static Edge Create<TOutVertex, TInVertex>(string label, Id<TOutVertex> outVertex, IList<Id<TInVertex>> inVertices) where TOutVertex : Vertex where TInVertex : Vertex
        {
            var inVerticesArray = new Id<Vertex>[inVertices.Count];

            // Note: this is ultimately just an array copy, but in a strongly-typed way. The JIT might see through this as a memory copy,
            // but might require some more explicit code if not.
            for (int i = 0; i < inVertices.Count; i++)
            {
                inVerticesArray[i] = inVertices[i].As<TInVertex, Vertex>();
            }

            return new Edge(label, outVertex.As<TOutVertex, Vertex>(), inVerticesArray);
        }
    }
}
