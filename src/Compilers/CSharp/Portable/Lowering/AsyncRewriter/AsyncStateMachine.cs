// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CodeGen;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    /// <summary>
    /// The class that represents a translated async method.
    /// </summary>
    internal sealed class AsyncStateMachine : StateMachineTypeSymbol
    {
        private readonly TypeKind _typeKind;
        private readonly MethodSymbol _constructor;
        private readonly ImmutableArray<NamedTypeSymbol> _interfaces;

        public AsyncStateMachine(VariableSlotAllocator variableAllocatorOpt, TypeCompilationState compilationState, MethodSymbol asyncMethod, int asyncMethodOrdinal, TypeKind typeKind)
            : base(variableAllocatorOpt, compilationState, asyncMethod, asyncMethodOrdinal)
        {
            // TODO: report use-site errors on these types
            _typeKind = typeKind;
            _interfaces = ImmutableArray.Create(asyncMethod.DeclaringCompilation.GetWellKnownType(WellKnownType.System_Runtime_CompilerServices_IAsyncStateMachine));
            _constructor = new AsyncConstructor(this);
        }

        public override TypeKind TypeKind
        {
            get { return _typeKind; }
        }

        internal override MethodSymbol Constructor
        {
            get { return _constructor; }
        }

        internal override ImmutableArray<NamedTypeSymbol> InterfacesNoUseSiteDiagnostics(ConsList<Symbol> basesBeingResolved)
        {
            return _interfaces;
        }
    }
}
