// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis
{
    public abstract class SymbolVisitor<TResult>
    {
        public virtual TResult? Visit(ISymbol? symbol)
        {
            return symbol == null
                ? default(TResult?)
                : symbol.Accept(this);
        }

        public virtual TResult? DefaultVisit(ISymbol symbol)
        {
            return default(TResult?);
        }

        public virtual TResult? VisitAlias(IAliasSymbol symbol)
        {
            return DefaultVisit(symbol);
        }

        public virtual TResult? VisitArrayType(IArrayTypeSymbol symbol)
        {
            return DefaultVisit(symbol);
        }

        public virtual TResult? VisitAssembly(IAssemblySymbol symbol)
        {
            return DefaultVisit(symbol);
        }

        public virtual TResult? VisitDiscard(IDiscardSymbol symbol)
        {
            return DefaultVisit(symbol);
        }

        public virtual TResult? VisitDynamicType(IDynamicTypeSymbol symbol)
        {
            return DefaultVisit(symbol);
        }

        public virtual TResult? VisitEvent(IEventSymbol symbol)
        {
            return DefaultVisit(symbol);
        }

        public virtual TResult? VisitField(IFieldSymbol symbol)
        {
            return DefaultVisit(symbol);
        }

        public virtual TResult? VisitLabel(ILabelSymbol symbol)
        {
            return DefaultVisit(symbol);
        }

        public virtual TResult? VisitLocal(ILocalSymbol symbol)
        {
            return DefaultVisit(symbol);
        }

        public virtual TResult? VisitMethod(IMethodSymbol symbol)
        {
            return DefaultVisit(symbol);
        }

        public virtual TResult? VisitModule(IModuleSymbol symbol)
        {
            return DefaultVisit(symbol);
        }

        public virtual TResult? VisitNamedType(INamedTypeSymbol symbol)
        {
            return DefaultVisit(symbol);
        }

        public virtual TResult? VisitNamespace(INamespaceSymbol symbol)
        {
            return DefaultVisit(symbol);
        }

        public virtual TResult? VisitParameter(IParameterSymbol symbol)
        {
            return DefaultVisit(symbol);
        }

        public virtual TResult? VisitPointerType(IPointerTypeSymbol symbol)
        {
            return DefaultVisit(symbol);
        }

        public virtual TResult? VisitFunctionPointerType(IFunctionPointerTypeSymbol symbol)
        {
            return DefaultVisit(symbol);
        }

        public virtual TResult? VisitProperty(IPropertySymbol symbol)
        {
            return DefaultVisit(symbol);
        }

        public virtual TResult? VisitRangeVariable(IRangeVariableSymbol symbol)
        {
            return DefaultVisit(symbol);
        }

        public virtual TResult? VisitTypeParameter(ITypeParameterSymbol symbol)
        {
            return DefaultVisit(symbol);
        }
    }
}
