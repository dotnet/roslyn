// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Reflection.Metadata;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.PooledObjects;
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
        private struct LocalSignature : IEquatable<LocalSignature>
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
                    (_type == other._type || _type.Equals(other._type));
            }

            public override int GetHashCode()
            {
                var code = Hash.Combine(_type, (int)_constraints);
                return code;
            }

            public override bool Equals(object obj)
            {
                return this.Equals((LocalSignature)obj);
            }
        }

        // maps local identities to locals.
        private Dictionary<ILocalSymbol, LocalDefinition>? _localMap;

        // pool of free slots partitioned by their signature.
        private KeyedStack<LocalSignature, LocalDefinition>? _freeSlots;

        // all locals in order
        private ArrayBuilder<Cci.ILocalDefinition>? _lazyAllLocals;

        // An optional allocator that provides slots for locals.
        // Used when emitting an update to a method body during EnC.
        private readonly VariableSlotAllocator? _slotAllocatorOpt;

        public LocalSlotManager(VariableSlotAllocator? slotAllocatorOpt)
        {
            _slotAllocatorOpt = slotAllocatorOpt;

            // Add placeholders for pre-allocated locals. 
            // The actual identities are populated if/when the locals are reused.
            if (slotAllocatorOpt != null)
            {
                _lazyAllLocals = new ArrayBuilder<Cci.ILocalDefinition>();
                slotAllocatorOpt.AddPreviousLocals(_lazyAllLocals);
            }
        }

        private Dictionary<ILocalSymbol, LocalDefinition> LocalMap
        {
            get
            {
                var map = _localMap;
                if (map == null)
                {
                    map = new Dictionary<ILocalSymbol, LocalDefinition>(ReferenceEqualityComparer.Instance);
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
        internal LocalDefinition GetLocal(ILocalSymbol symbol)
        {
            return LocalMap[symbol];
        }

        /// <summary>
        /// Release a local slot by its symbol.
        /// Slot is not associated with symbol after this.
        /// </summary>
        internal void FreeLocal(ILocalSymbol symbol)
        {
            var slot = GetLocal(symbol);
            LocalMap.Remove(symbol);
            FreeSlot(slot);
        }

        /// <summary>
        /// Gets a local slot.
        /// </summary>
        internal LocalDefinition AllocateSlot(
            Cci.ITypeReference type,
            LocalSlotConstraints constraints,
            ImmutableArray<bool> dynamicTransformFlags = default(ImmutableArray<bool>),
            ImmutableArray<string> tupleElementNames = default(ImmutableArray<string>))
        {
            LocalDefinition? local;
            if (!FreeSlots.TryPop(new LocalSignature(type, constraints), out local))
            {
                local = this.DeclareLocalImpl(
                    type: type,
                    symbolOpt: null,
                    nameOpt: null,
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
            ILocalSymbolInternal? symbolOpt,
            string? nameOpt,
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

            LocalDefinition local;

            if (symbolOpt != null && _slotAllocatorOpt != null)
            {
                local = _slotAllocatorOpt.GetPreviousLocal(
                    type,
                    symbolOpt,
                    nameOpt,
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
                symbolOpt: symbolOpt,
                nameOpt: nameOpt,
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
            FreeSlots.Push(new LocalSignature(slot.Type, slot.Constraints), slot);
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
