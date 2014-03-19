namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// Resolves references to metadata specified in the source (#r directives).
    /// </summary>
    public abstract class MetadataReferenceResolver
    {
        public abstract string ResolveReference(string reference, string baseFilePath);
    }
}
