// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Symbols;

/// <summary>
/// This type is synthesized to hold the cached delegates that target static method groups.
/// </summary>
internal sealed class DelegateCacheContainer : SynthesizedContainer
{
    private readonly Symbol _containingSymbol;
    private readonly NamedTypeSymbol? _constructedContainer;
    private readonly Dictionary<(TypeSymbol, MethodSymbol), FieldSymbol> _delegateFields = new();

    /// <summary>Creates a type scoped concrete delegate cache container.</summary>
    internal DelegateCacheContainer(TypeSymbol containingType, int generationOrdinal)
        : base(GeneratedNames.DelegateCacheContainerType(generationOrdinal), containingMethod: null)
    {
        Debug.Assert(containingType.IsDefinition);
        _containingSymbol = containingType;
    }

    /// <summary>Creates a method scoped generic delegate cache container.</summary>
    internal DelegateCacheContainer(MethodSymbol containingMethod, int topLevelMethodOrdinal, int ownerUniqueId, int generationOrdinal)
        : base(GeneratedNames.DelegateCacheContainerType(generationOrdinal, containingMethod.Name, topLevelMethodOrdinal, ownerUniqueId), containingMethod)
    {
        Debug.Assert(containingMethod.IsDefinition);
        _containingSymbol = containingMethod.ContainingType;
        _constructedContainer = Construct(ConstructedFromTypeParameters);
    }

    public override Symbol ContainingSymbol => _containingSymbol;

    public override bool AreLocalsZeroed => throw ExceptionUtilities.Unreachable;

    public override TypeKind TypeKind => TypeKind.Class;

    public override bool IsStatic => true;

    internal override bool IsRecord => false;

    internal override bool IsRecordStruct => false;

    internal override bool HasPossibleWellKnownCloneMethod() => false;

    internal FieldSymbol GetOrAddCacheField(SyntheticBoundNodeFactory factory, TypeSymbol delegateType, MethodSymbol targetMethod)
    {
        Debug.Assert(delegateType.IsDelegateType());

        if (_delegateFields.TryGetValue((delegateType, targetMethod), out var field))
        {
            return field;
        }

        var fieldType = TypeParameters.IsEmpty ? delegateType : TypeMap.SubstituteType(delegateType).Type;
        var fieldName = GeneratedNames.DelegateCacheContainerFieldName(_delegateFields.Count, targetMethod.Name);

        field = new SynthesizedFieldSymbol(this, fieldType, fieldName, isPublic: true, isStatic: true);
        factory.AddField(this, field);

        if (!TypeParameters.IsEmpty)
        {
            Debug.Assert(_constructedContainer is { });

            field = field.AsMember(_constructedContainer);
        }

        _delegateFields.Add((delegateType, targetMethod), field);

        return field;
    }
}
