// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

namespace Microsoft.CodeAnalysis
{
    public abstract class SymbolVisitor
    {
        public virtual void Visit(ISymbol? symbol)
        {
            symbol?.Accept(this);
        }

        public virtual void DefaultVisit(ISymbol symbol)
        {
        }

        public virtual void VisitAlias(IAliasSymbol symbol)
        {
            DefaultVisit(symbol);
        }

        public virtual void VisitArrayType(IArrayTypeSymbol symbol)
        {
            DefaultVisit(symbol);
        }

        public virtual void VisitAssembly(IAssemblySymbol symbol)
        {
            DefaultVisit(symbol);
        }

        public virtual void VisitDiscard(IDiscardSymbol symbol)
        {
            DefaultVisit(symbol);
        }

        public virtual void VisitDynamicType(IDynamicTypeSymbol symbol)
        {
            DefaultVisit(symbol);
        }

        public virtual void VisitEvent(IEventSymbol symbol)
        {
            DefaultVisit(symbol);
        }

        public virtual void VisitField(IFieldSymbol symbol)
        {
            DefaultVisit(symbol);
        }

        public virtual void VisitLabel(ILabelSymbol symbol)
        {
            DefaultVisit(symbol);
        }

        public virtual void VisitLocal(ILocalSymbol symbol)
        {
            DefaultVisit(symbol);
        }

        public virtual void VisitMethod(IMethodSymbol symbol)
        {
            DefaultVisit(symbol);
        }

        public virtual void VisitModule(IModuleSymbol symbol)
        {
            DefaultVisit(symbol);
        }

        public virtual void VisitNamedType(INamedTypeSymbol symbol)
        {
            DefaultVisit(symbol);
        }

        public virtual void VisitNamespace(INamespaceSymbol symbol)
        {
            DefaultVisit(symbol);
        }

        public virtual void VisitParameter(IParameterSymbol symbol)
        {
            DefaultVisit(symbol);
        }

        public virtual void VisitPointerType(IPointerTypeSymbol symbol)
        {
            DefaultVisit(symbol);
        }

        public virtual void VisitProperty(IPropertySymbol symbol)
        {
            DefaultVisit(symbol);
        }

        public virtual void VisitRangeVariable(IRangeVariableSymbol symbol)
        {
            DefaultVisit(symbol);
        }

        public virtual void VisitTypeParameter(ITypeParameterSymbol symbol)
        {
            DefaultVisit(symbol);
        }
    }
}
