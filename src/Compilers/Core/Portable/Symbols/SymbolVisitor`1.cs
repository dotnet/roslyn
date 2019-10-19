// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
#pragma warning disable CS8717 // A member returning a [MaybeNull] value introduces a null value when 'TResult' is a non-nullable reference type.
            return DefaultVisit(symbol);
#pragma warning restore CS8717 // A member returning a [MaybeNull] value introduces a null value when 'TResult' is a non-nullable reference type.
        }

        [return: MaybeNull]
        public virtual TResult VisitArrayType(IArrayTypeSymbol symbol)
        {
#pragma warning disable CS8717 // A member returning a [MaybeNull] value introduces a null value when 'TResult' is a non-nullable reference type.
            return DefaultVisit(symbol);
#pragma warning restore CS8717 // A member returning a [MaybeNull] value introduces a null value when 'TResult' is a non-nullable reference type.
        }

        [return: MaybeNull]
        public virtual TResult VisitAssembly(IAssemblySymbol symbol)
        {
#pragma warning disable CS8717 // A member returning a [MaybeNull] value introduces a null value when 'TResult' is a non-nullable reference type.
            return DefaultVisit(symbol);
#pragma warning restore CS8717 // A member returning a [MaybeNull] value introduces a null value when 'TResult' is a non-nullable reference type.
        }

        [return: MaybeNull]
        public virtual TResult VisitDiscard(IDiscardSymbol symbol)
        {
#pragma warning disable CS8717 // A member returning a [MaybeNull] value introduces a null value when 'TResult' is a non-nullable reference type.
            return DefaultVisit(symbol);
#pragma warning restore CS8717 // A member returning a [MaybeNull] value introduces a null value when 'TResult' is a non-nullable reference type.
        }

        [return: MaybeNull]
        public virtual TResult VisitDynamicType(IDynamicTypeSymbol symbol)
        {
#pragma warning disable CS8717 // A member returning a [MaybeNull] value introduces a null value when 'TResult' is a non-nullable reference type.
            return DefaultVisit(symbol);
#pragma warning restore CS8717 // A member returning a [MaybeNull] value introduces a null value when 'TResult' is a non-nullable reference type.
        }

        [return: MaybeNull]
        public virtual TResult VisitEvent(IEventSymbol symbol)
        {
#pragma warning disable CS8717 // A member returning a [MaybeNull] value introduces a null value when 'TResult' is a non-nullable reference type.
            return DefaultVisit(symbol);
#pragma warning restore CS8717 // A member returning a [MaybeNull] value introduces a null value when 'TResult' is a non-nullable reference type.
        }

        [return: MaybeNull]
        public virtual TResult VisitField(IFieldSymbol symbol)
        {
#pragma warning disable CS8717 // A member returning a [MaybeNull] value introduces a null value when 'TResult' is a non-nullable reference type.
            return DefaultVisit(symbol);
#pragma warning restore CS8717 // A member returning a [MaybeNull] value introduces a null value when 'TResult' is a non-nullable reference type.
        }

        [return: MaybeNull]
        public virtual TResult VisitLabel(ILabelSymbol symbol)
        {
#pragma warning disable CS8717 // A member returning a [MaybeNull] value introduces a null value when 'TResult' is a non-nullable reference type.
            return DefaultVisit(symbol);
#pragma warning restore CS8717 // A member returning a [MaybeNull] value introduces a null value when 'TResult' is a non-nullable reference type.
        }

        [return: MaybeNull]
        public virtual TResult VisitLocal(ILocalSymbol symbol)
        {
#pragma warning disable CS8717 // A member returning a [MaybeNull] value introduces a null value when 'TResult' is a non-nullable reference type.
            return DefaultVisit(symbol);
#pragma warning restore CS8717 // A member returning a [MaybeNull] value introduces a null value when 'TResult' is a non-nullable reference type.
        }

        [return: MaybeNull]
        public virtual TResult VisitMethod(IMethodSymbol symbol)
        {
#pragma warning disable CS8717 // A member returning a [MaybeNull] value introduces a null value when 'TResult' is a non-nullable reference type.
            return DefaultVisit(symbol);
#pragma warning restore CS8717 // A member returning a [MaybeNull] value introduces a null value when 'TResult' is a non-nullable reference type.
        }

        [return: MaybeNull]
        public virtual TResult VisitModule(IModuleSymbol symbol)
        {
#pragma warning disable CS8717 // A member returning a [MaybeNull] value introduces a null value when 'TResult' is a non-nullable reference type.
            return DefaultVisit(symbol);
#pragma warning restore CS8717 // A member returning a [MaybeNull] value introduces a null value when 'TResult' is a non-nullable reference type.
        }

        [return: MaybeNull]
        public virtual TResult VisitNamedType(INamedTypeSymbol symbol)
        {
#pragma warning disable CS8717 // A member returning a [MaybeNull] value introduces a null value when 'TResult' is a non-nullable reference type.
            return DefaultVisit(symbol);
#pragma warning restore CS8717 // A member returning a [MaybeNull] value introduces a null value when 'TResult' is a non-nullable reference type.
        }

        [return: MaybeNull]
        public virtual TResult VisitNamespace(INamespaceSymbol symbol)
        {
#pragma warning disable CS8717 // A member returning a [MaybeNull] value introduces a null value when 'TResult' is a non-nullable reference type.
            return DefaultVisit(symbol);
#pragma warning restore CS8717 // A member returning a [MaybeNull] value introduces a null value when 'TResult' is a non-nullable reference type.
        }

        [return: MaybeNull]
        public virtual TResult VisitParameter(IParameterSymbol symbol)
        {
#pragma warning disable CS8717 // A member returning a [MaybeNull] value introduces a null value when 'TResult' is a non-nullable reference type.
            return DefaultVisit(symbol);
#pragma warning restore CS8717 // A member returning a [MaybeNull] value introduces a null value when 'TResult' is a non-nullable reference type.
        }

        [return: MaybeNull]
        public virtual TResult VisitPointerType(IPointerTypeSymbol symbol)
        {
#pragma warning disable CS8717 // A member returning a [MaybeNull] value introduces a null value when 'TResult' is a non-nullable reference type.
            return DefaultVisit(symbol);
#pragma warning restore CS8717 // A member returning a [MaybeNull] value introduces a null value when 'TResult' is a non-nullable reference type.
        }

        [return: MaybeNull]
        public virtual TResult VisitProperty(IPropertySymbol symbol)
        {
#pragma warning disable CS8717 // A member returning a [MaybeNull] value introduces a null value when 'TResult' is a non-nullable reference type.
            return DefaultVisit(symbol);
#pragma warning restore CS8717 // A member returning a [MaybeNull] value introduces a null value when 'TResult' is a non-nullable reference type.
        }

        [return: MaybeNull]
        public virtual TResult VisitRangeVariable(IRangeVariableSymbol symbol)
        {
#pragma warning disable CS8717 // A member returning a [MaybeNull] value introduces a null value when 'TResult' is a non-nullable reference type.
            return DefaultVisit(symbol);
#pragma warning restore CS8717 // A member returning a [MaybeNull] value introduces a null value when 'TResult' is a non-nullable reference type.
        }

        [return: MaybeNull]
        public virtual TResult VisitTypeParameter(ITypeParameterSymbol symbol)
        {
#pragma warning disable CS8717 // A member returning a [MaybeNull] value introduces a null value when 'TResult' is a non-nullable reference type.
            return DefaultVisit(symbol);
#pragma warning restore CS8717 // A member returning a [MaybeNull] value introduces a null value when 'TResult' is a non-nullable reference type.
        }
    }
}
