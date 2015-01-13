// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

namespace Microsoft.CodeAnalysis
{
    internal class CommonModuleCompilationState
    {
        private bool frozen;

        internal void Freeze()
        {
            Debug.Assert(!frozen);

            // make sure that values from all threads are visible:
            Interlocked.MemoryBarrier();

            frozen = true;
        }

        internal bool Frozen
        {
            get { return frozen; }
        }
    }

    internal class ModuleCompilationState<TNamedTypeSymbol, TMethodSymbol> : CommonModuleCompilationState
        where TNamedTypeSymbol : class, Cci.INamespaceTypeDefinition
        where TMethodSymbol : class, Cci.IMethodDefinition
    {
        /// <summary>
        /// Maps an async/iterator method to the synthesized state machine type that implements the method. 
        /// </summary>
        private Dictionary<TMethodSymbol, TNamedTypeSymbol> lazyStateMachineTypes;

        internal void SetStateMachineType(TMethodSymbol method, TNamedTypeSymbol stateMatchineClass)
        {
            Debug.Assert(!Frozen);

            if (lazyStateMachineTypes == null)
            {
                Interlocked.CompareExchange(ref lazyStateMachineTypes, new Dictionary<TMethodSymbol, TNamedTypeSymbol>(), null);
            }

            lock (lazyStateMachineTypes)
            {
                lazyStateMachineTypes.Add(method, stateMatchineClass);
            }
        }

        internal bool TryGetStateMachineType(TMethodSymbol method, out TNamedTypeSymbol stateMachineType)
        {
            Debug.Assert(Frozen);

            stateMachineType = null;
            return lazyStateMachineTypes != null && lazyStateMachineTypes.TryGetValue(method, out stateMachineType);
        }
    }
}
