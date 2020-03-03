// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis
{
    public abstract class AsyncSymbolVisitor<TResult>
    {
        protected abstract TResult DefaultResult { get; }

        public virtual ValueTask<TResult> VisitAsync(ISymbol? symbol)
        {
            return symbol?.AcceptAsync(this) ?? new ValueTask<TResult>(DefaultResult);
        }

        public virtual ValueTask<TResult> DefaultVisitAsync(ISymbol symbol)
        {
            return new ValueTask<TResult>(DefaultResult);
        }

        public virtual ValueTask<TResult> VisitAliasAsync(IAliasSymbol symbol)
        {
            return DefaultVisitAsync(symbol);
        }

        public virtual ValueTask<TResult> VisitArrayTypeAsync(IArrayTypeSymbol symbol)
        {
            return DefaultVisitAsync(symbol);
        }

        public virtual ValueTask<TResult> VisitAssemblyAsync(IAssemblySymbol symbol)
        {
            return DefaultVisitAsync(symbol);
        }

        public virtual ValueTask<TResult> VisitDiscardAsync(IDiscardSymbol symbol)
        {
            return DefaultVisitAsync(symbol);
        }

        public virtual ValueTask<TResult> VisitDynamicTypeAsync(IDynamicTypeSymbol symbol)
        {
            return DefaultVisitAsync(symbol);
        }

        public virtual ValueTask<TResult> VisitEventAsync(IEventSymbol symbol)
        {
            return DefaultVisitAsync(symbol);
        }

        public virtual ValueTask<TResult> VisitFieldAsync(IFieldSymbol symbol)
        {
            return DefaultVisitAsync(symbol);
        }

        public virtual ValueTask<TResult> VisitLabelAsync(ILabelSymbol symbol)
        {
            return DefaultVisitAsync(symbol);
        }

        public virtual ValueTask<TResult> VisitLocalAsync(ILocalSymbol symbol)
        {
            return DefaultVisitAsync(symbol);
        }

        public virtual ValueTask<TResult> VisitMethodAsync(IMethodSymbol symbol)
        {
            return DefaultVisitAsync(symbol);
        }

        public virtual ValueTask<TResult> VisitModuleAsync(IModuleSymbol symbol)
        {
            return DefaultVisitAsync(symbol);
        }

        public virtual ValueTask<TResult> VisitNamedTypeAsync(INamedTypeSymbol symbol)
        {
            return DefaultVisitAsync(symbol);
        }

        public virtual ValueTask<TResult> VisitNamespaceAsync(INamespaceSymbol symbol)
        {
            return DefaultVisitAsync(symbol);
        }

        public virtual ValueTask<TResult> VisitParameterAsync(IParameterSymbol symbol)
        {
            return DefaultVisitAsync(symbol);
        }

        public virtual ValueTask<TResult> VisitPointerTypeAsync(IPointerTypeSymbol symbol)
        {
            return DefaultVisitAsync(symbol);
        }

        public virtual ValueTask<TResult> VisitPropertyAsync(IPropertySymbol symbol)
        {
            return DefaultVisitAsync(symbol);
        }

        public virtual ValueTask<TResult> VisitRangeVariableAsync(IRangeVariableSymbol symbol)
        {
            return DefaultVisitAsync(symbol);
        }

        public virtual ValueTask<TResult> VisitTypeParameterAsync(ITypeParameterSymbol symbol)
        {
            return DefaultVisitAsync(symbol);
        }
    }
}
