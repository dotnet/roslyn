using Microsoft.CodeAnalysis.CSharp.Symbols;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal sealed class CatchFilterBinder : LocalScopeBinder
    {
        public CatchFilterBinder(MethodSymbol owner, Binder enclosing)
            : base(owner, enclosing, enclosing.Flags | BinderFlags.InCatchFilter)
        {
        }
    }
}