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
    private readonly Dictionary<(TypeSymbol?, TypeSymbol, MethodSymbol), FieldSymbol> _delegateFields = new(CLRSignatureComparer.Instance);

    /// <summary>Creates a type-scope concrete delegate cache container.</summary>
    internal DelegateCacheContainer(TypeSymbol containingType, int generationOrdinal)
        : base(GeneratedNames.DelegateCacheContainerType(generationOrdinal), containingMethod: null)
    {
        Debug.Assert(containingType.IsDefinition);

        _containingSymbol = containingType;
    }

    /// <summary>Creates a method-scope generic delegate cache container.</summary>
    internal DelegateCacheContainer(MethodSymbol ownerMethod, int topLevelMethodOrdinal, int ownerUniqueId, int generationOrdinal)
        : base(GeneratedNames.DelegateCacheContainerType(generationOrdinal, ownerMethod.Name, topLevelMethodOrdinal, ownerUniqueId), ownerMethod)
    {
        Debug.Assert(ownerMethod.IsDefinition);
        Debug.Assert(ownerMethod.Arity > 0);

        _containingSymbol = ownerMethod.ContainingType;
        _constructedContainer = Construct(ConstructedFromTypeParameters);
    }

    public override Symbol ContainingSymbol => _containingSymbol;

    public override bool AreLocalsZeroed => throw ExceptionUtilities.Unreachable();

    public override TypeKind TypeKind => TypeKind.Class;

    public override bool IsStatic => true;

    internal override bool IsRecord => false;

    internal override bool IsRecordStruct => false;

    internal override bool HasPossibleWellKnownCloneMethod() => false;

    internal FieldSymbol GetOrAddCacheField(SyntheticBoundNodeFactory factory, BoundDelegateCreationExpression boundDelegateCreation)
    {
        var targetMethod = boundDelegateCreation.MethodOpt;
        var delegateType = boundDelegateCreation.Type;

        Debug.Assert(delegateType.IsDelegateType());
        Debug.Assert(targetMethod is { });

        var constrainedToTypeOpt = ((targetMethod.IsAbstract || targetMethod.IsVirtual) && boundDelegateCreation.Argument is BoundTypeExpression typeExpression) ? typeExpression.Type : null;

        if (_delegateFields.TryGetValue((constrainedToTypeOpt, delegateType, targetMethod), out var field))
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

        _delegateFields.Add((constrainedToTypeOpt, delegateType, targetMethod), field);

        return field;
    }

    private sealed class CLRSignatureComparer : IEqualityComparer<(TypeSymbol? constrainedToTypeOpt, TypeSymbol delegateType, MethodSymbol targetMethod)>
    {
        public static readonly CLRSignatureComparer Instance = new();

        public bool Equals((TypeSymbol? constrainedToTypeOpt, TypeSymbol delegateType, MethodSymbol targetMethod) x, (TypeSymbol? constrainedToTypeOpt, TypeSymbol delegateType, MethodSymbol targetMethod) y)
        {
            var symbolComparer = SymbolEqualityComparer.CLRSignature;

            return symbolComparer.Equals(x.delegateType, y.delegateType) &&
                   symbolComparer.Equals(x.targetMethod, y.targetMethod) &&
                   symbolComparer.Equals(x.constrainedToTypeOpt, y.constrainedToTypeOpt);
        }

        public int GetHashCode((TypeSymbol? constrainedToTypeOpt, TypeSymbol delegateType, MethodSymbol targetMethod) conversion)
        {
            var symbolComparer = SymbolEqualityComparer.CLRSignature;

            int hash = Hash.Combine(symbolComparer.GetHashCode(conversion.delegateType), symbolComparer.GetHashCode(conversion.targetMethod));

            if (conversion.constrainedToTypeOpt is { } constrainedToType)
            {
                hash = Hash.Combine(hash, symbolComparer.GetHashCode(constrainedToType));
            }

            return hash;
        }
    }
}
