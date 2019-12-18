namespace Microsoft.CodeAnalysis.Lsif.Generator.LsifGraph
{
    /// <summary>
    /// Represents a single item that points to a range from a result. See https://github.com/Microsoft/language-server-protocol/blob/master/indexFormat/specification.md#request-textdocumentreferences
    /// for an example of item edges.
    /// </summary>
    internal sealed class Item : Edge
    {
        public Id<Document> Document { get; }
        public string Property { get; }

        public Item(Id<Vertex> outVertex, Id<Range> range, Id<Document> document, string property)
            : base(label: "item", outVertex, new[] { range.As<Range, Vertex>() })
        {
            Document = document;
            Property = property;
        }
    }
}
