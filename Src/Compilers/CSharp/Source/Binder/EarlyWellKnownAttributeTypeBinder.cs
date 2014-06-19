
namespace Roslyn.Compilers.CSharp
{
    /// <summary>
    /// This is a special binder used for decoding some special well-known attribute types very early in the attribute binding phase.
    /// It only binds types that are directly inside a namespace and doesn't bind nested types.
    /// </summary>
    internal sealed class EarlyWellKnownAttributeTypeBinder : Binder
    {
        internal EarlyWellKnownAttributeTypeBinder(Binder binder)
            : base(binder)
        {
        }

        internal override BindingLocation BindingLocation
        {
            get { return BindingLocation.Attribute; }
        }
        
        internal override void LookupSymbols(LookupResult result, string name, int arity, Utilities.ConsList<Symbol> basesBeingResolved, LookupOptions options, bool diagnose)
        {
            options |= LookupOptions.NamespacesOrTypesOnly | LookupOptions.MustNotBeNestedType;
            base.LookupSymbols(result, name, arity, basesBeingResolved, options, diagnose);
        }

        internal override void LookupMembers(LookupResult result, NamespaceOrTypeSymbol nsOrType, string name, int arity, Utilities.ConsList<Symbol> basesBeingResolved, LookupOptions options, Binder originalBinder, bool diagnose)
        {
            options |= LookupOptions.NamespacesOrTypesOnly | LookupOptions.MustNotBeNestedType;
            base.LookupMembers(result, nsOrType, name, arity, basesBeingResolved, options, originalBinder, diagnose);
        }

        internal override bool IsEarlyAttributeBinder
        {
            get
            {
                return true;
            }
        }
    }
}
