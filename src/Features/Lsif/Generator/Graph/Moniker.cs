// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.LanguageServerIndexFormat.Generator.Graph
{
    internal sealed class Moniker : Vertex
    {
        public string Scheme { get; }
        public string Identifier { get; }
        public string? Kind { get; }

        public Moniker(string scheme, string identifier, string? kind, IdFactory idFactory)
            : base(label: "moniker", idFactory)
        {
            Scheme = scheme;
            Identifier = identifier;
            Kind = kind;
        }
    }
}
