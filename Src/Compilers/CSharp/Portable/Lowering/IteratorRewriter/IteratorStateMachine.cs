// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CSharp.Symbols;

namespace Microsoft.CodeAnalysis.CSharp
{
    /// <summary>
    /// The class that represents a translated iterator method.
    /// </summary>
    internal sealed class IteratorStateMachine : SynthesizedContainer, ISynthesizedMethodBodyImplementationSymbol
    {
        private readonly MethodSymbol iteratorMethod;
        private readonly MethodSymbol constructor;
        private readonly ImmutableArray<NamedTypeSymbol> interfaces;

        internal readonly TypeSymbol ElementType;

        public IteratorStateMachine(MethodSymbol iteratorMethod, bool isEnumerable, TypeSymbol elementType, TypeCompilationState compilationState)
            : base(GeneratedNames.MakeStateMachineTypeName(iteratorMethod.Name, compilationState.GenerateTempNumber()), iteratorMethod)
        {
            this.iteratorMethod = iteratorMethod;
            this.ElementType = TypeMap.SubstituteType(elementType);

            var interfaces = ArrayBuilder<NamedTypeSymbol>.GetInstance();
            if (isEnumerable)
            {
                interfaces.Add(ContainingAssembly.GetSpecialType(SpecialType.System_Collections_Generic_IEnumerable_T).Construct(ElementType));
                interfaces.Add(ContainingAssembly.GetSpecialType(SpecialType.System_Collections_IEnumerable));
            }

            interfaces.Add(ContainingAssembly.GetSpecialType(SpecialType.System_Collections_Generic_IEnumerator_T).Construct(ElementType));
            interfaces.Add(ContainingAssembly.GetSpecialType(SpecialType.System_IDisposable));
            interfaces.Add(ContainingAssembly.GetSpecialType(SpecialType.System_Collections_IEnumerator));
            this.interfaces = interfaces.ToImmutableAndFree();

            this.constructor = new IteratorConstructor(this);
        }

        public override TypeKind TypeKind
        {
            get { return TypeKind.Class; }
        }

        public override Symbol ContainingSymbol
        {
            get { return iteratorMethod.ContainingSymbol; }
        }

        internal override MethodSymbol Constructor
        {
            get { return constructor; }
        }

        internal MethodSymbol IteratorMethod
        {
            get { return iteratorMethod; }
        }

        internal override ImmutableArray<NamedTypeSymbol> InterfacesNoUseSiteDiagnostics
        {
            get { return interfaces; }
        }

        internal override NamedTypeSymbol BaseTypeNoUseSiteDiagnostics
        {
            get { return ContainingAssembly.GetSpecialType(SpecialType.System_Object); }
        }

        bool ISynthesizedMethodBodyImplementationSymbol.HasMethodBodyDependency
        {
            get
            {
                // MoveNext method contains user code from the iterator method:
                return true;
            }
        }

        IMethodSymbol ISynthesizedMethodBodyImplementationSymbol.Method
        {
            get { return iteratorMethod; }
        }
    }
}
