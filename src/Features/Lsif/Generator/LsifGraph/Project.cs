// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;

namespace Microsoft.CodeAnalysis.Lsif.Generator.LsifGraph
{
    /// <summary>
    /// Represents a top-level project. See https://github.com/Microsoft/language-server-protocol/blob/master/indexFormat/specification.md#the-project-vertex for further details.
    /// </summary>
    internal sealed class Project : Vertex
    {
        public string Kind { get; }
        public Uri? Resource { get; }
        public string? Contents { get; }

        public Project(string kind, Uri? resource = null, string? contents = null)
            : base(label: "project")
        {
            Kind = kind;
            Resource = resource;
            Contents = contents;
        }
    }
}
