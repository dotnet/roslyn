using System.Collections.Generic;
using Roslyn.Compilers.CSharp;

namespace Roslyn.Services.Editor.CSharp.Extensions
{
    internal partial class TypeSymbolExtensions
    {
        private class CollectMethodTypeParameterSymbolsVisitor : SymbolVisitor<IList<TypeParameterSymbol>, object>
        {
            public static readonly SymbolVisitor<IList<TypeParameterSymbol>, object> Instance = new CollectMethodTypeParameterSymbolsVisitor();

            private CollectMethodTypeParameterSymbolsVisitor()
            {
            }

            public override object VisitDynamicType(DynamicTypeSymbol symbol, IList<TypeParameterSymbol> argument)
            {
                return null;
            }

            public override object VisitArrayType(ArrayTypeSymbol symbol, IList<TypeParameterSymbol> arg)
            {
                return this.Visit(symbol.ElementType, arg);
            }

            public override object VisitErrorType(ErrorTypeSymbol symbol, IList<TypeParameterSymbol> arg)
            {
                foreach (var child in symbol.TypeArguments)
                {
                    Visit(child, arg);
                }

                return null;
            }

            public override object VisitNamedType(NamedTypeSymbol symbol, IList<TypeParameterSymbol> arg)
            {
                foreach (var child in symbol.TypeArguments)
                {
                    Visit(child, arg);
                }

                return null;
            }

            public override object VisitPointerType(PointerTypeSymbol symbol, IList<TypeParameterSymbol> arg)
            {
                return Visit(symbol.PointedAtType, arg);
            }

            public override object VisitTypeParameter(TypeParameterSymbol symbol, IList<TypeParameterSymbol> arg)
            {
                if (symbol.ContainingSymbol is MethodSymbol)
                {
                    if (!arg.Contains(symbol))
                    {
                        arg.Add(symbol);
                    }
                }

                return null;
            }
        }
    }
}