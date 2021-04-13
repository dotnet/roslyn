// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.LanguageServerIndexFormat.Generator.Graph
{
    /// <summary>
    /// Represents a single item that points to a range from a result. See https://github.com/Microsoft/language-server-protocol/blob/master/indexFormat/specification.md#request-textdocumentreferences
    /// for an example of item edges.
    /// </summary>
    internal sealed class Item : Edge
    {
        public Id<LsifDocument> Document { get; }
        public string? Property { get; }

        public Item(Id<Vertex> outVertex, Id<Range> range, Id<LsifDocument> document, IdFactory idFactory, string? property = null)
            : base(label: "item", outVertex, new[] { range.As<Range, Vertex>() }, idFactory)
        {
            Document = document;
            Property = property;
        }
    }
}
