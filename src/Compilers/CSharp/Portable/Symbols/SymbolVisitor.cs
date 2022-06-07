// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using Microsoft.CodeAnalysis.CSharp.Symbols;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal abstract class CSharpSymbolVisitor
    {
        public virtual void Visit(Symbol symbol)
        {
            if ((object)symbol != null)
            {
                symbol.Accept(this);
            }
        }

        public virtual void DefaultVisit(Symbol symbol)
        {
        }

        public virtual void VisitAlias(AliasSymbol symbol)
        {
            DefaultVisit(symbol);
        }

        public virtual void VisitArrayType(ArrayTypeSymbol symbol)
        {
            DefaultVisit(symbol);
        }

        public virtual void VisitAssembly(AssemblySymbol symbol)
        {
            DefaultVisit(symbol);
        }

        public virtual void VisitDynamicType(DynamicTypeSymbol symbol)
        {
            DefaultVisit(symbol);
        }

        public virtual void VisitDiscard(DiscardSymbol symbol)
        {
            DefaultVisit(symbol);
        }

        public virtual void VisitEvent(EventSymbol symbol)
        {
            DefaultVisit(symbol);
        }

        public virtual void VisitField(FieldSymbol symbol)
        {
            DefaultVisit(symbol);
        }

        public virtual void VisitLabel(LabelSymbol symbol)
        {
            DefaultVisit(symbol);
        }

        public virtual void VisitLocal(LocalSymbol symbol)
        {
            DefaultVisit(symbol);
        }

        public virtual void VisitMethod(MethodSymbol symbol)
        {
            DefaultVisit(symbol);
        }

        public virtual void VisitModule(ModuleSymbol symbol)
        {
            DefaultVisit(symbol);
        }

        public virtual void VisitNamedType(NamedTypeSymbol symbol)
        {
            DefaultVisit(symbol);
        }

        public virtual void VisitNamespace(NamespaceSymbol symbol)
        {
            DefaultVisit(symbol);
        }

        public virtual void VisitParameter(ParameterSymbol symbol)
        {
            DefaultVisit(symbol);
        }

        public virtual void VisitPointerType(PointerTypeSymbol symbol)
        {
            DefaultVisit(symbol);
        }

        public virtual void VisitFunctionPointerType(FunctionPointerTypeSymbol symbol)
        {
            DefaultVisit(symbol);
        }

        public virtual void VisitProperty(PropertySymbol symbol)
        {
            DefaultVisit(symbol);
        }

        public virtual void VisitRangeVariable(RangeVariableSymbol symbol)
        {
            DefaultVisit(symbol);
        }

        public virtual void VisitTypeParameter(TypeParameterSymbol symbol)
        {
            DefaultVisit(symbol);
        }
    }
}
