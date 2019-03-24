// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CodeGen;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    /// <summary>
    /// The class that represents a translated async or async-iterator method.
    /// </summary>
    internal sealed class AsyncStateMachine : StateMachineTypeSymbol
    {
        private readonly TypeKind _typeKind;
        private readonly MethodSymbol _constructor;
        private readonly ImmutableArray<NamedTypeSymbol> _interfaces;
        internal readonly TypeSymbol IteratorElementType; // only for async-iterators

        public AsyncStateMachine(VariableSlotAllocator variableAllocatorOpt, TypeCompilationState compilationState, MethodSymbol asyncMethod, int asyncMethodOrdinal, TypeKind typeKind)
            : base(variableAllocatorOpt, compilationState, asyncMethod, asyncMethodOrdinal)
        {
            _typeKind = typeKind;
            CSharpCompilation compilation = asyncMethod.DeclaringCompilation;
            var interfaces = ArrayBuilder<NamedTypeSymbol>.GetInstance();

            bool isIterator = asyncMethod.IsIterator;
            if (isIterator)
            {
                var elementType = TypeMap.SubstituteType(asyncMethod.IteratorElementTypeWithAnnotations).Type;
                this.IteratorElementType = elementType;

                bool isEnumerable = asyncMethod.IsIAsyncEnumerableReturningAsync(compilation);
                if (isEnumerable)
                {
                    // IAsyncEnumerable<TResult>
                    interfaces.Add(compilation.GetWellKnownType(WellKnownType.System_Collections_Generic_IAsyncEnumerable_T).Construct(elementType));
                }

                // IAsyncEnumerator<TResult>
                interfaces.Add(compilation.GetWellKnownType(WellKnownType.System_Collections_Generic_IAsyncEnumerator_T).Construct(elementType));

                // IValueTaskSource<bool>
                interfaces.Add(compilation.GetWellKnownType(WellKnownType.System_Threading_Tasks_Sources_IValueTaskSource_T).Construct(compilation.GetSpecialType(SpecialType.System_Boolean)));

                // IValueTaskSource
                interfaces.Add(compilation.GetWellKnownType(WellKnownType.System_Threading_Tasks_Sources_IValueTaskSource));

                // IAsyncDisposable
                interfaces.Add(compilation.GetWellKnownType(WellKnownType.System_IAsyncDisposable));
            }

            interfaces.Add(compilation.GetWellKnownType(WellKnownType.System_Runtime_CompilerServices_IAsyncStateMachine));
            _interfaces = interfaces.ToImmutableAndFree();

            _constructor = isIterator ? (MethodSymbol)new IteratorConstructor(this) : new AsyncConstructor(this);
        }

        public override TypeKind TypeKind
        {
            get { return _typeKind; }
        }

        internal override MethodSymbol Constructor
        {
            get { return _constructor; }
        }

        internal override ImmutableArray<NamedTypeSymbol> InterfacesNoUseSiteDiagnostics(ConsList<TypeSymbol> basesBeingResolved)
        {
            return _interfaces;
        }
    }
}
