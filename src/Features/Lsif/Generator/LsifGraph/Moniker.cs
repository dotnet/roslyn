// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis.Lsif.Generator.LsifGraph
{
    internal sealed class Moniker : Vertex
    {
        public string Scheme { get; }
        public string Identifier { get; }
        public string? Kind { get; }

        public Moniker(string scheme, string identifier, string? kind = null)
            : base(label: "moniker")
        {
            Scheme = scheme;
            Identifier = identifier;
            Kind = kind;
        }
    }
}
