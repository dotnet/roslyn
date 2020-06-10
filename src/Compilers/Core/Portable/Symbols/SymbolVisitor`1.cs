// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System.Diagnostics.CodeAnalysis;

namespace Microsoft.CodeAnalysis
{
    public abstract class SymbolVisitor<TResult>
    {
        [return: MaybeNull]
        public virtual TResult Visit(ISymbol? symbol)
        {
            return symbol == null
#pragma warning disable CS8653 // A default expression introduces a null value for a type parameter.
                ? default(TResult)
#pragma warning restore CS8653 // A default expression introduces a null value for a type parameter.
                : symbol.Accept(this);
        }

        [return: MaybeNull]
        public virtual TResult DefaultVisit(ISymbol symbol)
        {
#pragma warning disable CS8653 // A default expression introduces a null value for a type parameter.
            return default(TResult);
#pragma warning restore CS8653 // A default expression introduces a null value for a type parameter.
        }

        [return: MaybeNull]
        public virtual TResult VisitAlias(IAliasSymbol symbol)
        {
            return DefaultVisit(symbol);
        }

        [return: MaybeNull]
        public virtual TResult VisitArrayType(IArrayTypeSymbol symbol)
        {
            return DefaultVisit(symbol);
        }

        [return: MaybeNull]
        public virtual TResult VisitAssembly(IAssemblySymbol symbol)
        {
            return DefaultVisit(symbol);
        }

        [return: MaybeNull]
        public virtual TResult VisitDiscard(IDiscardSymbol symbol)
        {
            return DefaultVisit(symbol);
        }

        [return: MaybeNull]
        public virtual TResult VisitDynamicType(IDynamicTypeSymbol symbol)
        {
            return DefaultVisit(symbol);
        }

        [return: MaybeNull]
        public virtual TResult VisitEvent(IEventSymbol symbol)
        {
            return DefaultVisit(symbol);
        }

        [return: MaybeNull]
        public virtual TResult VisitField(IFieldSymbol symbol)
        {
            return DefaultVisit(symbol);
        }

        [return: MaybeNull]
        public virtual TResult VisitLabel(ILabelSymbol symbol)
        {
            return DefaultVisit(symbol);
        }

        [return: MaybeNull]
        public virtual TResult VisitLocal(ILocalSymbol symbol)
        {
            return DefaultVisit(symbol);
        }

        [return: MaybeNull]
        public virtual TResult VisitMethod(IMethodSymbol symbol)
        {
            return DefaultVisit(symbol);
        }

        [return: MaybeNull]
        public virtual TResult VisitModule(IModuleSymbol symbol)
        {
            return DefaultVisit(symbol);
        }

        [return: MaybeNull]
        public virtual TResult VisitNamedType(INamedTypeSymbol symbol)
        {
            return DefaultVisit(symbol);
        }

        [return: MaybeNull]
        public virtual TResult VisitNamespace(INamespaceSymbol symbol)
        {
            return DefaultVisit(symbol);
        }

        [return: MaybeNull]
        public virtual TResult VisitParameter(IParameterSymbol symbol)
        {
            return DefaultVisit(symbol);
        }

        [return: MaybeNull]
        public virtual TResult VisitPointerType(IPointerTypeSymbol symbol)
        {
            return DefaultVisit(symbol);
        }

        [return: MaybeNull]
        public virtual TResult VisitFunctionPointerType(IFunctionPointerTypeSymbol symbol)
        {
            return DefaultVisit(symbol);
        }

        [return: MaybeNull]
        public virtual TResult VisitProperty(IPropertySymbol symbol)
        {
            return DefaultVisit(symbol);
        }

        [return: MaybeNull]
        public virtual TResult VisitRangeVariable(IRangeVariableSymbol symbol)
        {
            return DefaultVisit(symbol);
        }

        [return: MaybeNull]
        public virtual TResult VisitTypeParameter(ITypeParameterSymbol symbol)
        {
            return DefaultVisit(symbol);
        }
    }
}
