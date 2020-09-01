using System;

namespace RoslynEx
{
    /// <summary>
    /// Applying this attribute on an assembly specifies the execution order of transformers it knows about, including transformers inside the assembly itself.
    /// </summary>
    [AttributeUsage(AttributeTargets.Assembly)]
    public class TransformerOrderAttribute : Attribute
    {
        /// <summary>
        /// Array of namespace-qualified names of transformer types. Their order specifies the execution order of the corresponding transformers.
        /// </summary>
        public string[] TransformerNames { get; }

        /// <param name="transformerNames">Namespace-qualified names of transformer types. Their order specifies the execution order of the corresponding transformers.</param>
        public TransformerOrderAttribute(params string[] transformerNames) => TransformerNames = transformerNames;
    }
}
