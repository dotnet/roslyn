// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace Microsoft.CodeAnalysis.LanguageServerIndexFormat.Generator.Graph
{
    /// <summary>
    /// Represents a top-level project. See https://github.com/Microsoft/language-server-protocol/blob/master/indexFormat/specification.md#the-project-vertex for further details.
    /// </summary>
    internal sealed class LsifProject : Vertex
    {
        public string Kind { get; }
        public Uri? Resource { get; }
        public string Name { get; }

        public LsifProject(string kind, Uri? resource, string name, IdFactory idFactory)
            : base(label: "project", idFactory)
        {
            Kind = kind;
            Resource = resource;
            Name = name;
        }
    }
}
