using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
namespace Microsoft.CodeAnalysis.CSharp
{
    internal sealed class ScopedExpressionBinder : LocalScopeBinder
    {
        private readonly ExpressionSyntax expression;

        public ScopedExpressionBinder(MethodSymbol owner, Binder enclosing, ExpressionSyntax expression)
            : base(owner, enclosing, enclosing.Flags)
        {
            this.expression = expression;
        }

        protected override ImmutableArray<LocalSymbol> BuildLocals()
        {
            var walker = new BuildLocalsFromDeclarationsWalker(this);

            walker.Visit(expression);

            if (walker.Locals != null)
            {
                return walker.Locals.ToImmutableAndFree();
            }

            return ImmutableArray<LocalSymbol>.Empty;
        }
    }
}
