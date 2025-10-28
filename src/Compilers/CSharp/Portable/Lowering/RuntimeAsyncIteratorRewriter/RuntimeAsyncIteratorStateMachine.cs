// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CodeGen;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp;

/// <summary>
/// State machine type for async-iterators implemented with runtime-async.
/// <see cref="RuntimeAsyncIteratorRewriter"/>
/// </summary>
internal sealed class RuntimeAsyncIteratorStateMachine : StateMachineTypeSymbol
{
    private readonly MethodSymbol _constructor;
    private readonly ImmutableArray<NamedTypeSymbol> _interfaces;

    internal readonly TypeWithAnnotations ElementType;

    public RuntimeAsyncIteratorStateMachine(
        VariableSlotAllocator? slotAllocatorOpt,
        TypeCompilationState compilationState,
        MethodSymbol kickoffMethod,
        int kickoffMethodOrdinal,
        bool isEnumerable,
        TypeWithAnnotations elementType)
        : base(slotAllocatorOpt, compilationState, kickoffMethod, kickoffMethodOrdinal)
    {
        this.ElementType = TypeMap.SubstituteType(elementType);
        _interfaces = makeInterfaces(isEnumerable);
        _constructor = new IteratorConstructor(this);

        ImmutableArray<NamedTypeSymbol> makeInterfaces(bool isEnumerable)
        {
            var interfaces = ArrayBuilder<NamedTypeSymbol>.GetInstance();
            CSharpCompilation compilation = ContainingAssembly.DeclaringCompilation;
            if (isEnumerable)
            {
                interfaces.Add(compilation.GetWellKnownType(WellKnownType.System_Collections_Generic_IAsyncEnumerable_T).Construct(ElementType.Type));
            }

            interfaces.Add(compilation.GetWellKnownType(WellKnownType.System_Collections_Generic_IAsyncEnumerator_T).Construct(ElementType.Type));
            interfaces.Add(compilation.GetWellKnownType(WellKnownType.System_IAsyncDisposable));

            return interfaces.ToImmutableAndFree();
        }
    }

    public override TypeKind TypeKind
        => TypeKind.Class;

    internal override MethodSymbol Constructor
        => _constructor;

    internal override ImmutableArray<NamedTypeSymbol> InterfacesNoUseSiteDiagnostics(ConsList<TypeSymbol> basesBeingResolved)
        => _interfaces;

    internal override NamedTypeSymbol BaseTypeNoUseSiteDiagnostics
        => ContainingAssembly.GetSpecialType(SpecialType.System_Object);

    internal override bool IsRecord => false;
    internal override bool IsRecordStruct => false;
    internal override bool HasPossibleWellKnownCloneMethod() => false;
}
