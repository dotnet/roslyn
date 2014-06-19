using Microsoft.Cci;

namespace Microsoft.CodeAnalysis.Emit
{
    /// <summary>
    /// Map definitions and references from one
    /// generation of a compilation to another.
    /// </summary>
    internal abstract class DefinitionAndReferenceMap
    {
        internal abstract IDefinition MapDefinition(IDefinition def);
        internal abstract ITypeReference MapReference(ITypeReference reference);
        internal abstract int GetNextAnonymousTypeIndex();
    }
}
