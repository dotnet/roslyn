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

        /// <summary>
        /// Corresponds to the uniqueness level of a moniker, per https://microsoft.github.io/language-server-protocol/specifications/lsp/3.17/specification/#uniquenessLevel.
        /// </summary>
        public string? Unique { get; }

        public Moniker(string scheme, string identifier, string? kind, string? unique, IdFactory idFactory)
            : base(label: "moniker", idFactory)
        {
            Scheme = scheme;
            Identifier = identifier;
            Kind = kind;
            Unique = unique;
        }
    }
}
