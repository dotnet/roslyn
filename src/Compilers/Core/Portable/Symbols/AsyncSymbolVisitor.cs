// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis
{
    public abstract class AsyncSymbolVisitor
    {
        public virtual ValueTask VisitAsync(ISymbol? symbol)
        {
            return symbol?.AcceptAsync(this) ?? new ValueTask();
        }

        public virtual ValueTask DefaultVisitAsync(ISymbol symbol)
        {
            return new ValueTask();
        }

        public virtual ValueTask VisitAliasAsync(IAliasSymbol symbol)
        {
            return DefaultVisitAsync(symbol);
        }

        public virtual ValueTask VisitArrayTypeAsync(IArrayTypeSymbol symbol)
        {
            return DefaultVisitAsync(symbol);
        }

        public virtual ValueTask VisitAssemblyAsync(IAssemblySymbol symbol)
        {
            return DefaultVisitAsync(symbol);
        }

        public virtual ValueTask VisitDiscardAsync(IDiscardSymbol symbol)
        {
            return DefaultVisitAsync(symbol);
        }

        public virtual ValueTask VisitDynamicTypeAsync(IDynamicTypeSymbol symbol)
        {
            return DefaultVisitAsync(symbol);
        }

        public virtual ValueTask VisitEventAsync(IEventSymbol symbol)
        {
            return DefaultVisitAsync(symbol);
        }

        public virtual ValueTask VisitFieldAsync(IFieldSymbol symbol)
        {
            return DefaultVisitAsync(symbol);
        }

        public virtual ValueTask VisitLabelAsync(ILabelSymbol symbol)
        {
            return DefaultVisitAsync(symbol);
        }

        public virtual ValueTask VisitLocalAsync(ILocalSymbol symbol)
        {
            return DefaultVisitAsync(symbol);
        }

        public virtual ValueTask VisitMethodAsync(IMethodSymbol symbol)
        {
            return DefaultVisitAsync(symbol);
        }

        public virtual ValueTask VisitModuleAsync(IModuleSymbol symbol)
        {
            return DefaultVisitAsync(symbol);
        }

        public virtual ValueTask VisitNamedTypeAsync(INamedTypeSymbol symbol)
        {
            return DefaultVisitAsync(symbol);
        }

        public virtual ValueTask VisitNamespaceAsync(INamespaceSymbol symbol)
        {
            return DefaultVisitAsync(symbol);
        }

        public virtual ValueTask VisitParameterAsync(IParameterSymbol symbol)
        {
            return DefaultVisitAsync(symbol);
        }

        public virtual ValueTask VisitPointerTypeAsync(IPointerTypeSymbol symbol)
        {
            return DefaultVisitAsync(symbol);
        }

        public virtual ValueTask VisitPropertyAsync(IPropertySymbol symbol)
        {
            return DefaultVisitAsync(symbol);
        }

        public virtual ValueTask VisitRangeVariableAsync(IRangeVariableSymbol symbol)
        {
            return DefaultVisitAsync(symbol);
        }

        public virtual ValueTask VisitTypeParameterAsync(ITypeParameterSymbol symbol)
        {
            return DefaultVisitAsync(symbol);
        }
    }
}
