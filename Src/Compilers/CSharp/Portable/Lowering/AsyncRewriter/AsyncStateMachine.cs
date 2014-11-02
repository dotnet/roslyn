// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CodeGen;
using Microsoft.CodeAnalysis.CSharp.Symbols;

namespace Microsoft.CodeAnalysis.CSharp
{
    /// <summary>
    /// The class that represents a translated async method.
    /// </summary>
    internal sealed class AsyncStateMachine : StateMachineTypeSymbol
    {
        private readonly TypeKind typeKind;
        private readonly MethodSymbol constructor;
        private readonly ImmutableArray<NamedTypeSymbol> interfaces;

        public AsyncStateMachine(VariableSlotAllocator variableAllocatorOpt, MethodSymbol asyncMethod, TypeKind typeKind)
            : base(variableAllocatorOpt, asyncMethod)
        {
            // TODO: report use-site errors on these types
            this.typeKind = typeKind;
            this.interfaces = ImmutableArray.Create(asyncMethod.DeclaringCompilation.GetWellKnownType(WellKnownType.System_Runtime_CompilerServices_IAsyncStateMachine));
            this.constructor = new AsyncConstructor(this);
        }

        public override TypeKind TypeKind
        {
            get { return typeKind; }
        }

        internal override MethodSymbol Constructor
        {
            get { return constructor; }
        }

        internal override ImmutableArray<NamedTypeSymbol> InterfacesNoUseSiteDiagnostics
        {
            get { return interfaces; }
        }
    }
}