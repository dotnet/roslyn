// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CodeGen;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    /// <summary>
    /// The class that represents a translated iterator method.
    /// </summary>
    internal sealed class IteratorStateMachine : StateMachineTypeSymbol
    {
        private readonly MethodSymbol _constructor;
        private readonly ImmutableArray<NamedTypeSymbol> _interfaces;

        internal readonly TypeWithAnnotations ElementType;

        public IteratorStateMachine(VariableSlotAllocator slotAllocatorOpt, TypeCompilationState compilationState, MethodSymbol iteratorMethod, int iteratorMethodOrdinal, bool isEnumerable, TypeWithAnnotations elementType)
            : base(slotAllocatorOpt, compilationState, iteratorMethod, iteratorMethodOrdinal)
        {
            this.ElementType = TypeMap.SubstituteType(elementType);

            var interfaces = ArrayBuilder<NamedTypeSymbol>.GetInstance();
            if (isEnumerable)
            {
                interfaces.Add(ContainingAssembly.GetSpecialType(SpecialType.System_Collections_Generic_IEnumerable_T).Construct(ElementType.Type));
                interfaces.Add(ContainingAssembly.GetSpecialType(SpecialType.System_Collections_IEnumerable));
            }

            interfaces.Add(ContainingAssembly.GetSpecialType(SpecialType.System_Collections_Generic_IEnumerator_T).Construct(ElementType.Type));
            interfaces.Add(ContainingAssembly.GetSpecialType(SpecialType.System_IDisposable));
            interfaces.Add(ContainingAssembly.GetSpecialType(SpecialType.System_Collections_IEnumerator));
            _interfaces = interfaces.ToImmutableAndFree();

            _constructor = new IteratorConstructor(this);
        }

        public override TypeKind TypeKind
        {
            get { return TypeKind.Class; }
        }

        internal override MethodSymbol Constructor
        {
            get { return _constructor; }
        }

        internal override ImmutableArray<NamedTypeSymbol> InterfacesNoUseSiteDiagnostics(ConsList<TypeSymbol> basesBeingResolved)
        {
            return _interfaces;
        }

        internal override NamedTypeSymbol BaseTypeNoUseSiteDiagnostics => ContainingAssembly.GetSpecialType(SpecialType.System_Object);
    }
}
