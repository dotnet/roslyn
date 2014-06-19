namespace Roslyn.Compilers
{
    /// <summary>
    /// This class is used to resolve assembly names to metadata documentation providers.
    /// </summary>
    public class DocumentationResolver
    {
        public static readonly DocumentationResolver Default = new DocumentationResolver();

        protected DocumentationResolver()
        {
        }

        public virtual DocumentationProvider ResolveReference(string assemblyPath)
        {
            return DocumentationProvider.Default;
        }
    }
}
