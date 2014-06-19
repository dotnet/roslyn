namespace Roslyn.Compilers.Common
{
    public interface ISymbolVisitor<TArgument, TResult>
    {
        TResult Visit(IAliasSymbol symbol, TArgument argument);
        TResult Visit(IArrayTypeSymbol symbol, TArgument argument);
        TResult Visit(IAssemblySymbol symbol, TArgument argument);
        TResult Visit(IDynamicTypeSymbol symbol, TArgument argument);
        TResult Visit(IFieldSymbol symbol, TArgument argument);
        TResult Visit(ILabelSymbol symbol, TArgument argument);
        TResult Visit(ILocalSymbol symbol, TArgument argument);
        TResult Visit(IMethodSymbol symbol, TArgument argument);
        TResult Visit(IModuleSymbol symbol, TArgument argument);
        TResult Visit(INamedTypeSymbol symbol, TArgument argument);
        TResult Visit(INamespaceSymbol symbol, TArgument argument);
        TResult Visit(IParameterSymbol symbol, TArgument argument);
        TResult Visit(IPointerTypeSymbol symbol, TArgument argument);
        TResult Visit(IPropertySymbol symbol, TArgument argument);
        TResult Visit(IEventSymbol symbol, TArgument argument);
        TResult Visit(ITypeParameterSymbol symbol, TArgument argument);
        TResult Visit(IRangeVariableSymbol symbol, TArgument argument);
    }
}