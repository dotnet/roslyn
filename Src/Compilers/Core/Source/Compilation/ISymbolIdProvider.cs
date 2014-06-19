namespace Roslyn.Compilers.Common
{
    /// <summary>
    /// Interface used to allow the SymbolId to recreate certain symbols that we do not want exposed
    /// from our public compilation APIs.
    /// </summary>
    internal interface ISymbolIdProvider
    {
        /// <summary>
        /// Returns a new INamedTypeSymbol representing a error type with the given name and arity
        /// in the given optional container.
        /// </summary>
        INamedTypeSymbol CreateErrorTypeSymbol(INamespaceOrTypeSymbol container, string name, int arity);
    }
}