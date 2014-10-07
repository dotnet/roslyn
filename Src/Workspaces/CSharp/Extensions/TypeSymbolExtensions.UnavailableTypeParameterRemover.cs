using System.Collections.Generic;
using System.Linq;
using Roslyn.Compilers;
using Roslyn.Compilers.Common;
using Roslyn.Compilers.CSharp;

namespace Roslyn.Services.Editor.CSharp.Extensions
{
    internal static partial class TypeSymbolExtensions
    {
        private class UnavailableTypeParameterRemover : SymbolVisitor<object, TypeSymbol>
        {
            private readonly Compilation compilation;
            private readonly ISet<string> availableTypeParameterNames;

            public UnavailableTypeParameterRemover(Compilation compilation, ISet<string> availableTypeParameterNames)
            {
                this.compilation = compilation;
                this.availableTypeParameterNames = availableTypeParameterNames;
            }

            public override TypeSymbol VisitDynamicType(DynamicTypeSymbol symbol, object argument)
            {
                return symbol;
            }

            public override TypeSymbol VisitErrorType(ErrorTypeSymbol symbol, object argument)
            {
                return VisitNamedType(symbol, argument);
            }

            public override TypeSymbol VisitArrayType(ArrayTypeSymbol symbol, object argument)
            {
                var elementType = Visit(symbol.ElementType);
                if (elementType == symbol.ElementType)
                {
                    return symbol;
                }

                // TODO: Code coverage
                return compilation.CreateArrayTypeSymbol(elementType, symbol.Rank);
            }

            public override TypeSymbol VisitNamedType(NamedTypeSymbol symbol, object argument)
            {
                var arguments = symbol.TypeArguments.Select(t => Visit(t)).ToArray();
                if (arguments.SequenceEqual(symbol.TypeArguments.AsEnumerable()))
                {
                    return symbol;
                }

                // TODO: Code coverage
                return symbol.ConstructedFrom.Construct(arguments.ToArray());
            }

            public override TypeSymbol VisitPointerType(PointerTypeSymbol symbol, object argument)
            {
                var elementType = Visit(symbol.PointedAtType);
                if (elementType == symbol.PointedAtType)
                {
                    return symbol;
                }

                return compilation.CreatePointerTypeSymbol(elementType);
            }

            public override TypeSymbol VisitTypeParameter(TypeParameterSymbol symbol, object argument)
            {
                if (availableTypeParameterNames.Contains(symbol.Name))
                {
                    return symbol;
                }

                return compilation.ObjectType;
            }
        }
    }
}
