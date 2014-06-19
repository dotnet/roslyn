using Microsoft.Cci;

namespace Roslyn.Compilers.CSharp
{
    /// <summary>
    /// A struct synthesized for an async method.
    /// </summary>
    internal abstract class SynthesizedStruct : SynthesizedContainer
    {
        internal SynthesizedStruct(MethodSymbol topLevelMethod, string name)
            : base(topLevelMethod, name) { }

        internal SynthesizedStruct(NamedTypeSymbol containingType, string name)
            : base(containingType, name) { }

        public override TypeKind TypeKind
        {
            get { return TypeKind.Struct; }
        }

        public override NamedTypeSymbol BaseType
        {
            get { return ContainingAssembly.GetSpecialType(Compilers.SpecialType.System_ValueType); }
        }
    }
}
