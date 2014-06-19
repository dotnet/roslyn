using Rid = System.UInt32;

namespace Roslyn.Compilers.CSharp.Metadata.PE
{
    internal sealed class PEPropertyAccessorSymbol : PEMethodSymbol
    {
        private readonly PEPropertySymbol propertySymbol;

        internal PEPropertyAccessorSymbol(PEModuleSymbol moduleSymbol, PEPropertySymbol propertySymbol, MethodKind methodKind, Rid methodRid) :
            base(moduleSymbol, (PENamedTypeSymbol)propertySymbol.ContainingType, methodKind, methodRid)
        {
            this.propertySymbol = propertySymbol;
        }

        public override Symbol ContainingSymbol
        {
            get { return this.propertySymbol; }
        }

        public override Symbol AssociatedPropertyOrEvent
        {
            get { return this.propertySymbol; }
        }
    }
}
