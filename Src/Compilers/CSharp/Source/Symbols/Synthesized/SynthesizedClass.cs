using Microsoft.Cci;

namespace Roslyn.Compilers.CSharp
{
    /// <summary>
    /// A class synthesized for a lambda, iterator method, or dynamic-sites.
    /// </summary>
    internal class SynthesizedClass : SynthesizedContainer
    {
        internal SynthesizedClass(MethodSymbol topLevelMethod, string name)
            : base(topLevelMethod, name) { }

        internal SynthesizedClass(NamedTypeSymbol containingType, string name)
            : base(containingType, name) { }

        public override TypeKind TypeKind
        {
            get { return TypeKind.Class; }
        }
    }
}
