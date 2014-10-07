using Roslyn.Compilers.CSharp;

namespace Roslyn.Services.Editor.CSharp.Extensions
{
    internal partial class SymbolExtensions
    {
        private class IsUnsafeVisitor : SymbolVisitor<object, bool>
        {
            internal static readonly IsUnsafeVisitor Instance = new IsUnsafeVisitor();

            private IsUnsafeVisitor()
            {
            }

            public override bool VisitArrayType(ArrayTypeSymbol symbol, object argument)
            {
                return Visit(symbol.ElementType, argument);
            }

            public override bool VisitErrorType(ErrorTypeSymbol symbol, object argument)
            {
                return symbol.TypeArguments.Any(ts => Visit(ts, argument));
            }

            public override bool VisitField(FieldSymbol symbol, object argument)
            {
                return Visit(symbol.Type, argument);
            }

            public override bool VisitNamedType(NamedTypeSymbol symbol, object argument)
            {
                return symbol.TypeArguments.Any(ts => Visit(ts, argument));
            }

            public override bool VisitPointerType(PointerTypeSymbol symbol, object argument)
            {
                return true;
            }

            public override bool VisitProperty(PropertySymbol symbol, object argument)
            {
                return
                    Visit(symbol.Type, argument) ||
                    symbol.Parameters.Any(p => Visit(p.Type, argument));
            }

            public override bool VisitTypeParameter(TypeParameterSymbol symbol, object argument)
            {
                return symbol.ConstraintTypes.Any(ts => Visit(ts, argument));
            }

            public override bool VisitMethod(MethodSymbol symbol, object argument)
            {
                return
                    Visit(symbol.ReturnType, argument) ||
                    symbol.Parameters.Any(p => Visit(p, argument)) ||
                    symbol.TypeParameters.Any(tp => Visit(tp, argument));
            }

            public override bool VisitParameter(ParameterSymbol symbol, object argument)
            {
                return Visit(symbol.Type, argument);
            }

            public override bool VisitParameter(ParameterSymbol symbol, object arg)
            {
                return Visit(symbol.Type, argument);
            }
        }
    }
}
