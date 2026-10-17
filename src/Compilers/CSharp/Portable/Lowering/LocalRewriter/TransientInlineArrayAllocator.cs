// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp;

internal sealed class TransientInlineArrayAllocator
{
    private static readonly ObjectPool<Dictionary<TypeSymbol, int>> s_currentlyAllocatedSpacePool =
        new ObjectPool<Dictionary<TypeSymbol, int>>(() => new Dictionary<TypeSymbol, int>(comparer: Symbols.SymbolEqualityComparer.IgnoringTupleNamesAndNullability), 10);

    private static readonly ObjectPool<Dictionary<TypeSymbol, TransientInlineArrayInfo>> s_allocatedSpacePool =
        new ObjectPool<Dictionary<TypeSymbol, TransientInlineArrayInfo>>(() => new Dictionary<TypeSymbol, TransientInlineArrayInfo>(comparer: Symbols.SymbolEqualityComparer.IgnoringTupleNamesAndNullability), 10);

    private readonly Dictionary<TypeSymbol, int> _currentlyAllocatedSpace;
    private readonly Dictionary<TypeSymbol, TransientInlineArrayInfo> _allocatedSpace;
    public TransientInlineArrayAllocator()
    {
        _currentlyAllocatedSpace = s_currentlyAllocatedSpacePool.Allocate();
        _allocatedSpace = s_allocatedSpacePool.Allocate();
    }

    public void AllocateInlineArray(TypeSymbol elementType, int length, bool isReadOnly)
    {
        if (_allocatedSpace.TryGetValue(elementType, out var allocatedSpace))
        {
            Debug.Assert(_currentlyAllocatedSpace.ContainsKey(elementType));
            var currentSpace = _currentlyAllocatedSpace[elementType];
            currentSpace += length;
            _allocatedSpace[elementType] = allocatedSpace.AddUse(currentSpace, isReadOnly);
            _currentlyAllocatedSpace[elementType] = currentSpace;
        }
        else
        {
            Debug.Assert(!_currentlyAllocatedSpace.ContainsKey(elementType));
            _allocatedSpace[elementType] = new TransientInlineArrayInfo(elementType, length, numUses: 1, isReadOnly);
            _currentlyAllocatedSpace[elementType] = length;
        }
    }

    public void ReturnInlineArray(TypeSymbol elementType, int length)
    {
        Debug.Assert(_currentlyAllocatedSpace.ContainsKey(elementType));
        var currentSpace = _currentlyAllocatedSpace[elementType];
        currentSpace -= length;
        Debug.Assert(currentSpace >= 0);
        _currentlyAllocatedSpace[elementType] = currentSpace;
    }

    public ImmutableArray<TransientInlineArrayInfo> ToAllocatedArraysAndFree()
    {
        Debug.Assert(_currentlyAllocatedSpace.All(kv => kv.Value == 0), "All allocated space should have been returned.");
        _currentlyAllocatedSpace.Clear();
        s_currentlyAllocatedSpacePool.Free(_currentlyAllocatedSpace);
        var result = _allocatedSpace.SelectAsArray(kv =>
        {
            Debug.Assert(kv.Key.Equals(kv.Value.ElementType, TypeCompareKind.IgnoreTupleNames | TypeCompareKind.IgnoreNullableModifiersForReferenceTypes));
            return kv.Value;
        });
        _allocatedSpace.Clear();
        s_allocatedSpacePool.Free(_allocatedSpace);
        return result;
    }
}

internal readonly struct TransientInlineArrayInfo(TypeSymbol elementType, int maxSpace, int numUses, bool needsReadOnly, bool needsMutable = false)
{
    public readonly TypeSymbol ElementType = elementType;
    public readonly int MaxSpace = maxSpace;
    public readonly int NumUses = numUses;
    public readonly bool NeedsReadOnly = needsReadOnly;
    public readonly bool NeedsMutable = needsMutable;

    public TransientInlineArrayInfo(TypeSymbol elementType, int maxSpace, int numUses, bool isReadOnly)
        : this(elementType, maxSpace, numUses, needsReadOnly: isReadOnly, needsMutable: !isReadOnly)
    {
    }

    public TransientInlineArrayInfo AddUse(int currentlyUsedSpace, bool isReadOnly)
    {
        var needsReadonly = NeedsReadOnly || isReadOnly;
        var needsMutable = NeedsMutable || !isReadOnly;
        return new TransientInlineArrayInfo(
            ElementType,
            Math.Max(MaxSpace, currentlyUsedSpace),
            NumUses + 1,
            needsReadonly,
            needsMutable);
    }
}
