// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using Microsoft.CodeAnalysis.CSharp.Symbols;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal abstract class CSharpSymbolVisitor<TResult>
    {
        public virtual TResult Visit(Symbol symbol)
        {
            return (object)symbol == null
                ? default(TResult)
                : symbol.Accept(this);
        }

        public virtual TResult DefaultVisit(Symbol symbol)
        {
            return default(TResult);
        }

        public virtual TResult VisitAlias(AliasSymbol symbol)
        {
            return DefaultVisit(symbol);
        }

        public virtual TResult VisitArrayType(ArrayTypeSymbol symbol)
        {
            return DefaultVisit(symbol);
        }

        public virtual TResult VisitAssembly(AssemblySymbol symbol)
        {
            return DefaultVisit(symbol);
        }

        public virtual TResult VisitDynamicType(DynamicTypeSymbol symbol)
        {
            return DefaultVisit(symbol);
        }

        public virtual TResult VisitDiscard(DiscardSymbol symbol)
        {
            return DefaultVisit(symbol);
        }

        public virtual TResult VisitEvent(EventSymbol symbol)
        {
            return DefaultVisit(symbol);
        }

        public virtual TResult VisitField(FieldSymbol symbol)
        {
            return DefaultVisit(symbol);
        }

        public virtual TResult VisitLabel(LabelSymbol symbol)
        {
            return DefaultVisit(symbol);
        }

        public virtual TResult VisitLocal(LocalSymbol symbol)
        {
            return DefaultVisit(symbol);
        }

        public virtual TResult VisitMethod(MethodSymbol symbol)
        {
            return DefaultVisit(symbol);
        }

        public virtual TResult VisitModule(ModuleSymbol symbol)
        {
            return DefaultVisit(symbol);
        }

        public virtual TResult VisitNamedType(NamedTypeSymbol symbol)
        {
            return DefaultVisit(symbol);
        }

        public virtual TResult VisitNamespace(NamespaceSymbol symbol)
        {
            return DefaultVisit(symbol);
        }

        public virtual TResult VisitParameter(ParameterSymbol symbol)
        {
            return DefaultVisit(symbol);
        }

        public virtual TResult VisitPointerType(PointerTypeSymbol symbol)
        {
            return DefaultVisit(symbol);
        }

        public virtual TResult VisitFunctionPointerType(FunctionPointerTypeSymbol symbol)
        {
            return DefaultVisit(symbol);
        }

        public virtual TResult VisitProperty(PropertySymbol symbol)
        {
            return DefaultVisit(symbol);
        }

        public virtual TResult VisitRangeVariable(RangeVariableSymbol symbol)
        {
            return DefaultVisit(symbol);
        }

        public virtual TResult VisitTypeParameter(TypeParameterSymbol symbol)
        {
            return DefaultVisit(symbol);
        }
    }
}
