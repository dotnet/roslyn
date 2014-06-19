using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Roslyn.Compilers.CSharp
{
    /// <summary>
    /// This is a placeholder for the binder for the fixed statement.  It is not implemented
    /// any more than necessary to prevent assertion failures during compilation.
    /// </summary>
    internal sealed class FixedBinder : LocalScopeBinder
    {
        private readonly FixedStatementSyntax syntax;

        public FixedBinder(FixedStatementSyntax syntax, MethodSymbol owner, Binder enclosing)
            : base(owner, enclosing)
        {
            this.syntax = syntax;
        }

        override protected ReadOnlyArray<LocalSymbol> BuildLocals()
        {
            var declaration = this.syntax.Declaration;
            if (declaration == null)
            {
                return ReadOnlyArray<LocalSymbol>.Empty;
            }

            var locals = ArrayBuilder<LocalSymbol>.GetInstance();
            foreach (var variable in declaration.Variables)
            {
                var localSymbol = SourceLocalSymbol.MakeLocal(
                    this.Owner,
                    this,
                    declaration.Type,
                    variable.Identifier,
                    variable.Initializer,
                    LocalDeclarationKind.For);
                locals.Add(localSymbol);
            }
            return locals.ToReadOnlyAndFree();
        }
    }
}
