// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis
{
    public abstract class SymbolVisitor<TArgument, TResult>
    {
        protected abstract TResult DefaultResult { get; }

        public virtual TResult Visit(ISymbol? symbol, TArgument argument)
        {
            if (symbol != null)
            {
                return symbol.Accept(this, argument);
            }

            return DefaultResult;
        }

        public virtual TResult DefaultVisit(ISymbol symbol, TArgument argument)
        {
            return DefaultResult;
        }

        public virtual TResult VisitAlias(IAliasSymbol symbol, TArgument argument)
        {
            return DefaultVisit(symbol, argument);
        }

        public virtual TResult VisitArrayType(IArrayTypeSymbol symbol, TArgument argument)
        {
            return DefaultVisit(symbol, argument);
        }

        public virtual TResult VisitAssembly(IAssemblySymbol symbol, TArgument argument)
        {
            return DefaultVisit(symbol, argument);
        }

        public virtual TResult VisitDiscard(IDiscardSymbol symbol, TArgument argument)
        {
            return DefaultVisit(symbol, argument);
        }

        public virtual TResult VisitDynamicType(IDynamicTypeSymbol symbol, TArgument argument)
        {
            return DefaultVisit(symbol, argument);
        }

        public virtual TResult VisitEvent(IEventSymbol symbol, TArgument argument)
        {
            return DefaultVisit(symbol, argument);
        }

        public virtual TResult VisitField(IFieldSymbol symbol, TArgument argument)
        {
            return DefaultVisit(symbol, argument);
        }

        public virtual TResult VisitLabel(ILabelSymbol symbol, TArgument argument)
        {
            return DefaultVisit(symbol, argument);
        }

        public virtual TResult VisitLocal(ILocalSymbol symbol, TArgument argument)
        {
            return DefaultVisit(symbol, argument);
        }

        public virtual TResult VisitMethod(IMethodSymbol symbol, TArgument argument)
        {
            return DefaultVisit(symbol, argument);
        }

        public virtual TResult VisitModule(IModuleSymbol symbol, TArgument argument)
        {
            return DefaultVisit(symbol, argument);
        }

        public virtual TResult VisitNamedType(INamedTypeSymbol symbol, TArgument argument)
        {
            return DefaultVisit(symbol, argument);
        }

        public virtual TResult VisitNamespace(INamespaceSymbol symbol, TArgument argument)
        {
            return DefaultVisit(symbol, argument);
        }

        public virtual TResult VisitParameter(IParameterSymbol symbol, TArgument argument)
        {
            return DefaultVisit(symbol, argument);
        }

        public virtual TResult VisitPointerType(IPointerTypeSymbol symbol, TArgument argument)
        {
            return DefaultVisit(symbol, argument);
        }

        public virtual TResult VisitFunctionPointerType(IFunctionPointerTypeSymbol symbol, TArgument argument)
        {
            return DefaultVisit(symbol, argument);
        }

        public virtual TResult VisitProperty(IPropertySymbol symbol, TArgument argument)
        {
            return DefaultVisit(symbol, argument);
        }

        public virtual TResult VisitRangeVariable(IRangeVariableSymbol symbol, TArgument argument)
        {
            return DefaultVisit(symbol, argument);
        }

        public virtual TResult VisitTypeParameter(ITypeParameterSymbol symbol, TArgument argument)
        {
            return DefaultVisit(symbol, argument);
        }
    }
}
