using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;

namespace Roslyn.Compilers.CSharp.Symbols.Source
{
    /// <summary>
    /// A context for binding type parameter symbols.
    /// Note: This will go away once we change method declarations to switch
    /// from TypeArgumentSyntax to TypeParameterSyntax
    /// </summary>
    internal class OldTypeArgumentBinderContext : BinderContext
    {
        private readonly TypeArgumentSyntax declaration;
        private readonly Symbol genericSymbol;
        private readonly WithTypeParametersBaseBinderContext enclosing;

        internal OldTypeArgumentBinderContext(TypeArgumentSyntax declaration, Symbol genericSymbol, WithTypeParametersBaseBinderContext next)
            : base(next.Location(declaration), genericSymbol, next)
        {
            this.declaration = declaration;
            this.genericSymbol = genericSymbol;
            this.enclosing = next;
        }

        internal TypeParameterSymbol MakeSymbol(Symbol owner, int ordinal, IEnumerable<OldTypeArgumentBinderContext> binders)
        {
            Debug.Assert(Object.ReferenceEquals(genericSymbol, owner));
            return new SourceTypeArgumentSymbol(owner, ordinal, binders);
        }

        internal TypeArgumentSyntax Declaration
        {
            get
            {
                return declaration;
            }
        }
    }
}