using Microsoft.CodeAnalysis.Common.Symbols;
using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.CodeGen
{
    internal sealed class EncLocalDefinition : LocalDefinition
    {
        private readonly object identity;

        public EncLocalDefinition(
            object identity,
            string name,
            Microsoft.Cci.ITypeReference type,
            int slot,
            bool isCompilerGenerated,
            bool isPinned,
            bool isReference,
            bool isDynamic,
            ImmutableArray<CommonTypedConstant> dynamicTransformFlags) :
            base(name, type, slot, isCompilerGenerated: isCompilerGenerated, isPinned: isPinned, isReference: isReference, isDynamic: isDynamic, dynamicTransformFlags: dynamicTransformFlags)
        {
            this.identity = identity;
        }

        public object Identity
        {
            get { return this.identity; }
        }
    }
}
