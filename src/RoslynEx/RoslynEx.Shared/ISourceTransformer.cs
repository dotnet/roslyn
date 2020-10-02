using Microsoft.CodeAnalysis;

namespace RoslynEx
{
    /// <summary>
    /// The interface required to implement a source transformer.
    /// </summary>
    public interface ISourceTransformer
    {
        /// <summary>
        /// Called to perform source transformation.
        /// </summary>
        Compilation Execute(TransformerContext context);
    }
}
