// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CSharp.Symbols;

namespace Microsoft.CodeAnalysis.CSharp
{
    partial class IteratorRewriter
    {
        /// <summary>
        /// The class that represents a translated iterator method.
        /// </summary>
        internal sealed class IteratorClass : SynthesizedContainer
        {
            private readonly MethodSymbol constructor;
            private readonly ImmutableArray<NamedTypeSymbol> interfaces;

            internal readonly TypeSymbol ElementType;

            public IteratorClass(MethodSymbol method, bool isEnumerable, TypeSymbol elementType, TypeCompilationState compilationState)
                : base(method, GeneratedNames.MakeIteratorOrAsyncDisplayClassName(method.Name, compilationState.GenerateTempNumber()), TypeKind.Class)
            {
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

            internal override MethodSymbol Constructor
            {
                get { return constructor; }
            }

            internal override ImmutableArray<NamedTypeSymbol> InterfacesNoUseSiteDiagnostics
            {
                get { return interfaces; }
            }

            internal override NamedTypeSymbol BaseTypeNoUseSiteDiagnostics
            {
                get { return ContainingAssembly.GetSpecialType(SpecialType.System_Object); }
            }
        }
    }
}
