using System.Collections.Generic;
using Roslyn.Compilers.Common;

namespace Roslyn.Services.Shared.Extensions
{
    internal partial class ITypeSymbolExtensions
    {
        private class CollectMethodTypeParameterSymbolsVisitor : ISymbolVisitor<IList<ITypeParameterSymbol>, object>
        {
            public static readonly ISymbolVisitor<IList<ITypeParameterSymbol>, object> Instance =
                new CollectMethodTypeParameterSymbolsVisitor();

            private CollectMethodTypeParameterSymbolsVisitor()
            {
            }

            public object Visit(IDynamicTypeSymbol symbol, IList<ITypeParameterSymbol> argument)
            {
                return null;
            }

            public object Visit(IArrayTypeSymbol symbol, IList<ITypeParameterSymbol> arg)
            {
                return symbol.ElementType.Accept(this, arg);
            }

            public object Visit(INamedTypeSymbol symbol, IList<ITypeParameterSymbol> arg)
            {
                foreach (var child in symbol.GetAllTypeArguments())
                {
                    child.Accept(this, arg);
                }

                return null;
            }

            public object Visit(IPointerTypeSymbol symbol, IList<ITypeParameterSymbol> arg)
            {
                return symbol.PointedAtType.Accept(this, arg);
            }

            public object Visit(ITypeParameterSymbol symbol, IList<ITypeParameterSymbol> arg)
            {
                if (symbol.IsMethodTypeParameter)
                {
                    if (!arg.Contains(symbol))
                    {
                        arg.Add(symbol);
                    }
                }

                return null;
            }

            public object Visit(IAliasSymbol aliasSymbol, IList<ITypeParameterSymbol> argument)
            {
                throw new System.NotImplementedException();
            }

            public object Visit(IAssemblySymbol assemblSymboly, IList<ITypeParameterSymbol> argument)
            {
                throw new System.NotImplementedException();
            }

            public object Visit(IFieldSymbol fieldSymbol, IList<ITypeParameterSymbol> argument)
            {
                throw new System.NotImplementedException();
            }

            public object Visit(ILabelSymbol labelSymbol, IList<ITypeParameterSymbol> argument)
            {
                throw new System.NotImplementedException();
            }

            public object Visit(ILocalSymbol localSymbol, IList<ITypeParameterSymbol> argument)
            {
                throw new System.NotImplementedException();
            }

            public object Visit(IMethodSymbol methodSymbol, IList<ITypeParameterSymbol> argument)
            {
                throw new System.NotImplementedException();
            }

            public object Visit(IModuleSymbol moduleSymbol, IList<ITypeParameterSymbol> argument)
            {
                throw new System.NotImplementedException();
            }

            public object Visit(INamespaceSymbol namespaceSymbol, IList<ITypeParameterSymbol> argument)
            {
                throw new System.NotImplementedException();
            }

            public object Visit(IParameterSymbol parameterSymbol, IList<ITypeParameterSymbol> argument)
            {
                throw new System.NotImplementedException();
            }

            public object Visit(IPropertySymbol propertySymbol, IList<ITypeParameterSymbol> argument)
            {
                throw new System.NotImplementedException();
            }

            public object Visit(IEventSymbol eventSymbol, IList<ITypeParameterSymbol> argument)
            {
                throw new System.NotImplementedException();
            }

            public object Visit(IRangeVariableSymbol rangeVariableSymbol, IList<ITypeParameterSymbol> argument)
            {
                throw new System.NotImplementedException();
            }
        }
    }
}