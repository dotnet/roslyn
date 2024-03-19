// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Newtonsoft.Json;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.LanguageServerIndexFormat.Generator.Graph
{
    /// <summary>
    /// Represents an edge from one vertex to another.
    /// </summary>
    internal class Edge : Element
    {
        [JsonProperty("outV")]
        public Id<Vertex> OutVertex { get; }

        // LSIF edges can have either an inV for a one-to-one edge, or an inVs for a one-to-many edge. We'll represent
        // this by having two properties here, and exactly one of them will always be non-null.

        [JsonProperty("inV", NullValueHandling = NullValueHandling.Ignore)]
        public Id<Vertex>? InVertex { get; }

        [JsonProperty("inVs", NullValueHandling = NullValueHandling.Ignore)]
        public Id<Vertex>[]? InVertices { get; }

        public IEnumerable<Id<Vertex>> GetInVerticies() => InVertices ?? SpecializedCollections.SingletonEnumerable(InVertex!.Value);

        public Edge(string label, Id<Vertex> outVertex, Id<Vertex> inVertex, IdFactory idFactory)
            : base(type: "edge", label: label, idFactory)
        {
            // We'll be strict and assert that label types that are one-to-many must always use inVs
            Contract.ThrowIfTrue(IsEdgeLabelOneToMany(label));
            OutVertex = outVertex;
            InVertex = inVertex;
        }

        public Edge(string label, Id<Vertex> outVertex, Id<Vertex>[] inVertices, IdFactory idFactory)
            : base(type: "edge", label: label, idFactory)
        {
            Contract.ThrowIfFalse(IsEdgeLabelOneToMany(label));
            OutVertex = outVertex;
            InVertices = inVertices;
        }

        private static bool IsEdgeLabelOneToMany(string label) => label is "contains" or "item";

        public static Edge Create<TOutVertex, TInVertex>(string label, Id<TOutVertex> outVertex, Id<TInVertex> inVertex, IdFactory idFactory) where TOutVertex : Vertex where TInVertex : Vertex
        {
            return new Edge(label, outVertex.As<TOutVertex, Vertex>(), inVertex.As<TInVertex, Vertex>(), idFactory);
        }

        public static Edge Create<TOutVertex, TInVertex>(string label, Id<TOutVertex> outVertex, IList<Id<TInVertex>> inVertices, IdFactory idFactory) where TOutVertex : Vertex where TInVertex : Vertex
        {
            var inVerticesArray = new Id<Vertex>[inVertices.Count];

            // Note: this is ultimately just an array copy, but in a strongly-typed way. The JIT might see through this as a memory copy,
            // but might require some more explicit code if not.
            for (var i = 0; i < inVertices.Count; i++)
            {
                inVerticesArray[i] = inVertices[i].As<TInVertex, Vertex>();
            }

            return new Edge(label, outVertex.As<TOutVertex, Vertex>(), inVerticesArray, idFactory);
        }

        public override string ToString()
        {
            return $"{Label} edge from {OutVertex} to {string.Join(", ", GetInVerticies())}";
        }
    }
}
