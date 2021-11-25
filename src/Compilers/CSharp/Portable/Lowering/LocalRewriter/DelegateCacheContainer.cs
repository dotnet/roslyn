// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
    internal sealed class DelegateCacheContainer : SynthesizedContainer
    {
        private readonly MethodSymbol _topLevelMethod;

        //private readonly Symbol _containingSymbol;

        //private int _delegateFields;
        //private readonly SmallDictionary<(NamedTypeSymbol, MethodSymbol), FieldSymbol> _delegateFieldsDict = new ();

        ///// <summary>
        ///// Contains the mapping from current method's type parameters to this container's type parameters.
        ///// Only used by method scoped generic container.
        ///// </summary>
        //private readonly TypeMap _typeMap;

        ///// <summary>Creates a type scoped concrete delegate cache container.</summary>
        //internal DelegateCacheContainer(TypeSymbol containingType, int generation)
        //    : base(DelegateCacheContainerKind.TypeScopedConcrete)
        //{
        //    Debug.Assert(containingType.IsDefinition);

        //    _name = GeneratedNames.MakeDelegateCacheContainerName(0, generation, null);
        //    _containingSymbol = containingType;
        //    _typeParameters = ImmutableArray<TypeParameterSymbol>.Empty;
        //}

        ///// <summary>Creates a method scoped generic delegate cache container.</summary>
        //internal DelegateCacheContainer(MethodSymbol currentMethod, int methodOrdinal, int generation)
        //    : base(DelegateCacheContainerKind.MethodScopedGeneric)
        //{
        //    Debug.Assert(currentMethod.IsDefinition);

        //    _name = GeneratedNames.MakeDelegateCacheContainerName(methodOrdinal, generation, currentMethod.Name);
        //    _containingSymbol = currentMethod.ContainingType;
        //    _typeMap = TypeMap.Empty.WithAlphaRename(currentMethod, this, out _typeParameters);
        //}

        public DelegateCacheContainer(string name, MethodSymbol topLevelMethod) : base(name, topLevelMethod)
        {
            _topLevelMethod = topLevelMethod;
        }

        public override Symbol ContainingSymbol => _topLevelMethod.ContainingType;

        public override bool AreLocalsZeroed => throw ExceptionUtilities.Unreachable;

        public override TypeKind TypeKind => TypeKind.Class;

        public override bool IsStatic => true;

        internal override bool IsRecord => false;

        internal override bool IsRecordStruct => false;

        internal override bool HasPossibleWellKnownCloneMethod() => false;

        //internal override FieldSymbol GetOrAddCacheField(SyntheticBoundNodeFactory factory, NamedTypeSymbol delegateType, MethodSymbol targetMethod)
        //{
        //    var key = (delegateType, targetMethod);

        //    FieldSymbol field;
        //    if (_delegateFieldsDict.TryGetValue(key, out field))
        //    {
        //        return field;
        //    }

        //    var fieldName = GeneratedNames.DelegateCacheContainerFieldName(targetMethod.Name, _delegateFields);
        //    _delegateFields++;

        //    TypeSymbol fieldType;
        //    if (TypeParameters.IsEmpty)
        //    {
        //        fieldType = delegateType;
        //    }
        //    else
        //    {
        //        fieldType = _typeMap.SubstituteType(delegateType).Type;
        //    }

        //    field = new SynthesizedFieldSymbol(this, fieldType, fieldName, isPublic: true, isStatic: true);
        //    factory.AddField(this, field);

        //    if (!TypeParameters.IsEmpty)
        //    {
        //        field = field.AsMember(this.Construct(factory.TopLevelMethod.TypeParameters));
        //    }

        //    _delegateFieldsDict.Add(key, field);

        //    return field;
        //}
    }
}
