// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis.Lsif.Generator.LsifGraph
{
    /// <summary>
    /// The base class of any vertex in the graph.
    /// </summary>
    internal abstract class Vertex : Element
    {
        protected Vertex(string label)
            : base(type: "vertex", label)
        {
        }
    }
}
