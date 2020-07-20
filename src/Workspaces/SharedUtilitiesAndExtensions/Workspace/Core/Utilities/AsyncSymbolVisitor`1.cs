// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis
{
    internal abstract class AsyncSymbolVisitor<TResult> : SymbolVisitor<ValueTask<TResult>>
    {
        protected abstract TResult DefaultResult { get; }

        public override ValueTask<TResult> Visit(ISymbol symbol)
        {
#pragma warning disable CA2012 // Use ValueTasks correctly (https://github.com/dotnet/roslyn-analyzers/issues/3384)
            return symbol?.Accept(this) ?? new ValueTask<TResult>(DefaultResult);
#pragma warning restore CA2012 // Use ValueTasks correctly
        }

        public override ValueTask<TResult> DefaultVisit(ISymbol symbol)
            => new ValueTask<TResult>(DefaultResult);
    }
}
