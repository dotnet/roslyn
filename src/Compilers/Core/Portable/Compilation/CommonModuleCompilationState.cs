// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using Microsoft.CodeAnalysis.Symbols;

namespace Microsoft.CodeAnalysis
{
    internal class CommonModuleCompilationState
    {
        private bool _frozen;

        internal void Freeze()
        {
            Debug.Assert(!_frozen);

            // make sure that values from all threads are visible:
            Interlocked.MemoryBarrier();

            _frozen = true;
        }

        internal bool Frozen
        {
            get { return _frozen; }
        }
    }

    internal class ModuleCompilationState<TNamedTypeSymbol, TMethodSymbol> : CommonModuleCompilationState
        where TNamedTypeSymbol : class, INamedTypeSymbolInternal
        where TMethodSymbol : class, IMethodSymbolInternal
    {
        /// <summary>
        /// Maps an async/iterator method to the synthesized state machine type that implements the method. 
        /// </summary>
        private Dictionary<TMethodSymbol, TNamedTypeSymbol>? _lazyStateMachineTypes;

        internal void SetStateMachineType(TMethodSymbol method, TNamedTypeSymbol stateMachineClass)
        {
            Debug.Assert(!Frozen);

            if (_lazyStateMachineTypes == null)
            {
                Interlocked.CompareExchange(ref _lazyStateMachineTypes, new Dictionary<TMethodSymbol, TNamedTypeSymbol>(), null);
            }

            lock (_lazyStateMachineTypes)
            {
                _lazyStateMachineTypes.Add(method, stateMachineClass);
            }
        }

        internal bool TryGetStateMachineType(TMethodSymbol method, [NotNullWhen(true)] out TNamedTypeSymbol? stateMachineType)
        {
            Debug.Assert(Frozen);

            stateMachineType = null;
            return _lazyStateMachineTypes != null && _lazyStateMachineTypes.TryGetValue(method, out stateMachineType);
        }
    }
}
