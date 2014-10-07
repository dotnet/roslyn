using System.Collections.Generic;
using System.Linq;
using Roslyn.Compilers;
using Roslyn.Compilers.Common;
using Roslyn.Compilers.CSharp;

namespace Roslyn.Services.Editor.CSharp.Extensions
{
    internal partial class TypeSymbolExtensions
    {
        private class SubstituteTypesVisitor<TType1, TType2> : SymbolVisitor<object, TypeSymbol>
            where TType1 : TypeSymbol
            where TType2 : TypeSymbol
        {
            private readonly Compilation compilation;
            private readonly IDictionary<TType1, TType2> map;

            internal SubstituteTypesVisitor(Compilation compilation, IDictionary<TType1, TType2> map)
            {
                this.compilation = compilation;
                this.map = map;
            }

            private TypeSymbol VisitType(TypeSymbol symbol, object argument)
            {
                TType2 converted;
                if (symbol is TType1 && map.TryGetValue((TType1)symbol, out converted))
                {
                    return converted;
                }

                return symbol;
            }

            public override TypeSymbol VisitErrorType(ErrorTypeSymbol symbol, object argument)
            {
                return VisitType(symbol, argument);
            }

            public override TypeSymbol VisitTypeParameter(TypeParameterSymbol symbol, object argument)
            {
                return VisitType(symbol, argument);
            }

            public override TypeSymbol VisitNamedType(NamedTypeSymbol symbol, object argument)
            {
                if (symbol.TypeArguments.Count == 0)
                {
                    return symbol;
                }

                var substitutedArguments = symbol.TypeArguments.Select(t => Visit(t)).ToArray();
                return ((NamedTypeSymbol)symbol.OriginalDefinition).Construct(substitutedArguments);
            }

            public override TypeSymbol VisitArrayType(ArrayTypeSymbol symbol, object argument)
            {
                return compilation.CreateArrayTypeSymbol(Visit(symbol.ElementType), symbol.Rank);
            }

            public override TypeSymbol VisitPointerType(PointerTypeSymbol symbol, object argument)
            {
                return compilation.CreatePointerTypeSymbol(Visit(symbol.PointedAtType));
            }
        }
    }
}