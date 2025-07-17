// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Reflection.Metadata;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Symbols;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CodeGen
{
    /// <summary>
    /// At this level there are two kinds of local variables:
    /// <list type="bullet">
    /// <item>
    /// Locals - have identities by which consuming code refers to them.
    ///     Typical use is a local variable or a compiler generated temp that can be accessed in multiple operations.
    ///     Any object can be used as identity. Reference equality is used.
    /// </item>
    /// <item>
    /// Temps - do not have identity. They are borrowed and returned to the free list.
    ///     Typical use is a scratch temporary or spilling storage.
    /// </item>
    /// </list>
    /// </summary>
    internal sealed class LocalSlotManager
    {
        /// <summary>
        /// Structure that represents a local signature (as in <a href="http://www.ecma-international.org/publications/files/ECMA-ST/ECMA-335.pdf">ECMA-335</a>, Partition I, §8.6.1.3 Local signatures).
        /// </summary>
        private readonly struct LocalSignature : IEquatable<LocalSignature>
        {
            private readonly Cci.ITypeReference _type;
            private readonly LocalSlotConstraints _constraints;

            internal LocalSignature(Cci.ITypeReference valType, LocalSlotConstraints constraints)
            {
                _constraints = constraints;
                _type = valType;
            }

            public bool Equals(LocalSignature other)
            {
                // ITypeReference does not have object identity.
                // Same type may be represented by multiple instances.
                // Therefore the use of "Equals" here.
                return _constraints == other._constraints &&
                    (Cci.SymbolEquivalentEqualityComparer.Instance.Equals(_type, other._type));
            }

            public override int GetHashCode()
                => Hash.Combine(Cci.SymbolEquivalentEqualityComparer.Instance.GetHashCode(_type), (int)_constraints);

            public override bool Equals(object? obj)
                => obj is LocalSignature ls && Equals(ls);
        }

        // maps local identities to locals.
        private Dictionary<ILocalSymbolInternal, LocalDefinition>? _localMap;

        // pool of free slots partitioned by their signature.
        private KeyedStack<LocalSignature, LocalDefinition>? _freeSlots;

        // these locals cannot be added to "FreeSlots"
        private HashSet<LocalDefinition>? _nonReusableLocals;

        // locals whose address has been taken; excludes non-reusable local kinds
        private ArrayBuilder<LocalDefinition>? _addressedLocals;
        private int _addressedLocalScopes;

        // all locals in order
        private ArrayBuilder<Cci.ILocalDefinition>? _lazyAllLocals;

        // An optional allocator that provides slots for locals.
        // Used when emitting an update to a method body during EnC.
        private readonly VariableSlotAllocator? _slotAllocator;

        public LocalSlotManager(VariableSlotAllocator? slotAllocator)
        {
            _slotAllocator = slotAllocator;

            // Add placeholders for pre-allocated locals.
            // The actual identities are populated if/when the locals are reused.
            if (slotAllocator != null)
            {
                _lazyAllLocals = new ArrayBuilder<Cci.ILocalDefinition>();
                slotAllocator.AddPreviousLocals(_lazyAllLocals);
            }
        }

        private Dictionary<ILocalSymbolInternal, LocalDefinition> LocalMap
        {
            get
            {
                var map = _localMap;
                if (map == null)
                {
                    map = new Dictionary<ILocalSymbolInternal, LocalDefinition>(ReferenceEqualityComparer.Instance);
                    _localMap = map;
                }

                return map;
            }
        }

        private KeyedStack<LocalSignature, LocalDefinition> FreeSlots
        {
            get
            {
                var slots = _freeSlots;
                if (slots == null)
                {
                    slots = new KeyedStack<LocalSignature, LocalDefinition>();
                    _freeSlots = slots;
                }

                return slots;
            }
        }

        internal LocalDefinition DeclareLocal(
            Cci.ITypeReference type,
            ILocalSymbolInternal symbol,
            string name,
            SynthesizedLocalKind kind,
            LocalDebugId id,
            LocalVariableAttributes pdbAttributes,
            LocalSlotConstraints constraints,
            ImmutableArray<bool> dynamicTransformFlags,
            ImmutableArray<string> tupleElementNames,
            bool isSlotReusable)
        {
            LocalDefinition? local;

            if (!isSlotReusable || !FreeSlots.TryPop(new LocalSignature(type, constraints), out local))
            {
                local = this.DeclareLocalImpl(type, symbol, name, kind, id, pdbAttributes, constraints, dynamicTransformFlags, tupleElementNames);
            }

            LocalMap.Add(symbol, local);
            return local;
        }

        /// <summary>
        /// Retrieve a local slot by its symbol.
        /// </summary>
        internal LocalDefinition GetLocal(ILocalSymbolInternal symbol)
        {
            return LocalMap[symbol];
        }

        /// <summary>
        /// Release a local slot by its symbol.
        /// Slot is not associated with symbol after this.
        /// </summary>
        internal void FreeLocal(ILocalSymbolInternal symbol)
        {
            var slot = GetLocal(symbol);
            var removed = LocalMap.Remove(symbol);
            Debug.Assert(removed, $"Attempted to free '{symbol}' more than once.");
            FreeSlot(slot);
        }

        /// <summary>
        /// Gets a local slot.
        /// </summary>
        internal LocalDefinition AllocateSlot(
            Cci.ITypeReference type,
            LocalSlotConstraints constraints,
            ImmutableArray<bool> dynamicTransformFlags = default,
            ImmutableArray<string> tupleElementNames = default)
        {
            if (!FreeSlots.TryPop(new LocalSignature(type, constraints), out LocalDefinition? local))
            {
                local = DeclareLocalImpl(
                    type: type,
                    symbol: null,
                    name: null,
                    kind: SynthesizedLocalKind.EmitterTemp,
                    id: LocalDebugId.None,
                    pdbAttributes: LocalVariableAttributes.DebuggerHidden,
                    constraints: constraints,
                    dynamicTransformFlags: dynamicTransformFlags,
                    tupleElementNames: tupleElementNames);
            }

            return local;
        }

        private LocalDefinition DeclareLocalImpl(
            Cci.ITypeReference type,
            ILocalSymbolInternal? symbol,
            string? name,
            SynthesizedLocalKind kind,
            LocalDebugId id,
            LocalVariableAttributes pdbAttributes,
            LocalSlotConstraints constraints,
            ImmutableArray<bool> dynamicTransformFlags,
            ImmutableArray<string> tupleElementNames)
        {
            if (_lazyAllLocals == null)
            {
                _lazyAllLocals = new ArrayBuilder<Cci.ILocalDefinition>(1);
            }

            LocalDefinition? local;

            if (symbol != null && _slotAllocator != null)
            {
                local = _slotAllocator.GetPreviousLocal(
                    type,
                    symbol,
                    name,
                    kind,
                    id,
                    pdbAttributes,
                    constraints,
                    dynamicTransformFlags: dynamicTransformFlags,
                    tupleElementNames: tupleElementNames);

                if (local != null)
                {
                    int slot = local.SlotIndex;
                    _lazyAllLocals[slot] = local;
                    return local;
                }
            }

            local = new LocalDefinition(
                symbolOpt: symbol,
                nameOpt: name,
                type: type,
                slot: _lazyAllLocals.Count,
                synthesizedKind: kind,
                id: id,
                pdbAttributes: pdbAttributes,
                constraints: constraints,
                dynamicTransformFlags: dynamicTransformFlags,
                tupleElementNames: tupleElementNames);

            _lazyAllLocals.Add(local);
            return local;
        }

        /// <summary>
        /// Frees a local slot.
        /// </summary>
        internal void FreeSlot(LocalDefinition slot)
        {
            Debug.Assert(slot.Name == null);

            if (_nonReusableLocals?.Remove(slot) != true)
            {
                FreeSlots.Push(new LocalSignature(slot.Type, slot.Constraints), slot);
            }
        }

        internal int StartScopeOfTrackingAddressedLocals()
        {
            Debug.Assert((_addressedLocals == null) == (_addressedLocalScopes == 0));
            _addressedLocals ??= ArrayBuilder<LocalDefinition>.GetInstance();
            _addressedLocalScopes++;
            return _addressedLocals.Count;
        }

        internal void AddAddressedLocal(LocalDefinition localDef, OptimizationLevel optimizations)
        {
            // No need to add non-reusable local kinds to `_addressedLocals` because that list
            // only contains locals with reusable kinds to mark them as actually non-reusable.
            if (localDef != null && localDef.SymbolOpt?.SynthesizedKind.IsSlotReusable(optimizations) != false)
            {
                _addressedLocals?.Add(localDef);
            }
        }

        internal void EndScopeOfTrackingAddressedLocals(int countBefore, bool markAsNotReusable)
        {
            Debug.Assert(_addressedLocals != null);

            if (markAsNotReusable && countBefore < _addressedLocals.Count)
            {
                _nonReusableLocals ??= new HashSet<LocalDefinition>(ReferenceEqualityComparer.Instance);
                for (var i = countBefore; i < _addressedLocals.Count; i++)
                {
                    _nonReusableLocals.Add(_addressedLocals[i]);
                }
            }

            _addressedLocalScopes--;
            if (_addressedLocalScopes > 0)
            {
                _addressedLocals.Count = countBefore;
            }
            else
            {
                Debug.Assert(_addressedLocalScopes == 0 && countBefore == 0);
                _addressedLocals.Free();
                _addressedLocals = null;
            }
        }

        public ImmutableArray<Cci.ILocalDefinition> LocalsInOrder()
        {
            if (_lazyAllLocals == null)
            {
                return ImmutableArray<Cci.ILocalDefinition>.Empty;
            }
            else
            {
                return _lazyAllLocals.ToImmutable();
            }
        }
    }
}
