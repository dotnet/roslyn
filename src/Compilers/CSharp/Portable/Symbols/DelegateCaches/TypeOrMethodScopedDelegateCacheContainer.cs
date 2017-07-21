// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.Cci;
using Microsoft.CodeAnalysis.CodeGen;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    using FieldSymbolsCollection = SmallDictionary<ValueTuple<NamedTypeSymbol, MethodSymbol>, FieldSymbol>;

    internal class TypeOrMethodScopedDelegateCacheContainer : DelegateCacheContainer
    {
        private readonly Symbol _containingSymbol;

        private readonly string _name;

        private int _delegateFields;
        private readonly FieldSymbolsCollection _delegateFieldsDict = new FieldSymbolsCollection();

        /// <summary>
        /// Contains the mapping from current method's type parameters to this container's type parameters.
        /// Only used by method scoped generic container.
        /// </summary>
        private readonly TypeMap _typeMap;

        private readonly ImmutableArray<TypeParameterSymbol> _typeParameters;

        /// <summary>Creates a type scoped concrete delegate cache container.</summary>
        internal TypeOrMethodScopedDelegateCacheContainer(TypeSymbol containingType, int generation)
            : base(DelegateCacheContainerKind.TypeScopedConcrete)
        {
            Debug.Assert(containingType.IsDefinition);

            _name = GeneratedNames.MakeDelegateCacheContainerName(0, generation, null);
            _containingSymbol = containingType;
            _typeParameters = ImmutableArray<TypeParameterSymbol>.Empty;
        }

        /// <summary>Creates a method scoped generic delegate cache container.</summary>
        internal TypeOrMethodScopedDelegateCacheContainer(MethodSymbol currentMethod, int methodOrdinal, int generation)
            : base(DelegateCacheContainerKind.MethodScopedGeneric)
        {
            Debug.Assert(currentMethod.IsDefinition);

            _name = GeneratedNames.MakeDelegateCacheContainerName(methodOrdinal, generation, currentMethod.Name);
            _containingSymbol = currentMethod.ContainingType;
            _typeMap = TypeMap.Empty.WithAlphaRename(currentMethod, this, out _typeParameters);
        }

        public override Symbol ContainingSymbol => _containingSymbol;

        public override Accessibility DeclaredAccessibility => Accessibility.Private;

        public override string Name => _name;

        public override ImmutableArray<TypeParameterSymbol> TypeParameters => _typeParameters;

        internal override FieldSymbol GetOrAddCacheField(SyntheticBoundNodeFactory factory, NamedTypeSymbol delegateType, MethodSymbol targetMethod)
        {
            var key = ValueTuple.Create(delegateType, targetMethod);

            FieldSymbol field;
            if (_delegateFieldsDict.TryGetValue(key, out field))
            {
                return field;
            }

            var fieldName = GeneratedNames.MakeDelegateCacheContainerFieldName(targetMethod.Name, _delegateFields);
            _delegateFields++;

            TypeSymbol fieldType;
            if (ContainerKind == DelegateCacheContainerKind.TypeScopedConcrete)
            {
                fieldType = delegateType;
            }
            else
            {
                Debug.Assert(ContainerKind == DelegateCacheContainerKind.MethodScopedGeneric);
                fieldType = _typeMap.SubstituteType(delegateType).Type;
            }

            field = new SynthesizedFieldSymbol(this, fieldType, fieldName, isPublic: true, isStatic: true);
            factory.AddField(this, field);

            if (ContainerKind == DelegateCacheContainerKind.MethodScopedGeneric)
            {
                field = field.AsMember(this.Construct(factory.TopLevelMethod.TypeParameters));
            }

            _delegateFieldsDict.Add(key, field);

            return field;
        }

        /// Fields are added using <see cref="SyntheticBoundNodeFactory.AddField(NamedTypeSymbol, FieldSymbol)"/>
        public override ImmutableArray<Symbol> GetMembers() => ImmutableArray<Symbol>.Empty;
    }
}
