// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.Collections;
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
        private struct LocalSignature : IEquatable<LocalSignature>
        {
            private readonly Cci.ITypeReference Type;
            private readonly LocalSlotConstraints Constraints;

            internal LocalSignature(Cci.ITypeReference valType, LocalSlotConstraints constraints)
            {
                this.Constraints = constraints;
                this.Type = valType;
            }

            public bool Equals(LocalSignature other)
            {
                // ITypeReference does not have object identity.
                // Same type may be represented by multiple instances.
                // Therefore the use of "Equals" here.
                return this.Constraints == other.Constraints &&
                    (this.Type == other.Type || this.Type.Equals(other.Type));
            }

            public override int GetHashCode()
            {
                var code = Hash.Combine(this.Type, (int)this.Constraints);
                return code;
            }

            public override bool Equals(object obj)
            {
                return this.Equals((LocalSignature)obj);
            }
        }

        // maps local identities to locals.
        private Dictionary<ILocalSymbol, LocalDefinition> localMap;

        // pool of free slots partitioned by their signature.
        private KeyedStack<LocalSignature, LocalDefinition> freeSlots;

        // all locals in order
        private ArrayBuilder<Cci.ILocalDefinition> lazyAllLocals;

        // An optional allocator that provides slots for locals.
        // Used when emitting an update to a method body during EnC.
        private readonly VariableSlotAllocator slotAllocatorOpt;

        public LocalSlotManager(VariableSlotAllocator slotAllocatorOpt)
        {
            this.slotAllocatorOpt = slotAllocatorOpt;

            // Add placeholders for pre-allocated locals. 
            // The actual identities are populated if/when the locals are reused.
            if (slotAllocatorOpt != null)
            {
                this.lazyAllLocals = new ArrayBuilder<Cci.ILocalDefinition>();
                slotAllocatorOpt.AddPreviousLocals(this.lazyAllLocals);
            }
        }

        private Dictionary<ILocalSymbol, LocalDefinition> LocalMap
        {
            get
            {
                var map = localMap;
                if (map == null)
                {
                    map = new Dictionary<ILocalSymbol, LocalDefinition>(ReferenceEqualityComparer.Instance);
                    localMap = map;
                }

                return map;
            }
        }

        private KeyedStack<LocalSignature, LocalDefinition> FreeSlots
        {
            get
            {
                var slots = freeSlots;
                if (slots == null)
                {
                    slots = new KeyedStack<LocalSignature, LocalDefinition>();
                    freeSlots = slots;
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
            uint pdbAttributes,
            LocalSlotConstraints constraints,
            bool isDynamic,
            ImmutableArray<TypedConstant> dynamicTransformFlags,
            bool isSlotReusable)
        {
            LocalDefinition local;

            if (!isSlotReusable || !FreeSlots.TryPop(new LocalSignature(type, constraints), out local))
            {
                local = this.DeclareLocalImpl(type, symbol, name, kind, id, pdbAttributes, constraints, isDynamic, dynamicTransformFlags);
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
            ImmutableArray<TypedConstant> dynamicTransformFlags = default(ImmutableArray<TypedConstant>))
        {
            LocalDefinition local;
            if (!FreeSlots.TryPop(new LocalSignature(type, constraints), out local))
            {
                local = this.DeclareLocalImpl(
                    type: type,
                    symbolOpt: null,
                    nameOpt: null,
                    kind: SynthesizedLocalKind.EmitterTemp,
                    id: LocalDebugId.None,
                    pdbAttributes: Cci.PdbWriter.HiddenLocalAttributesValue,
                    constraints: constraints,
                    isDynamic: false,
                    dynamicTransformFlags: dynamicTransformFlags);
            }

            return local;
        }

        private LocalDefinition DeclareLocalImpl(
            Cci.ITypeReference type,
            ILocalSymbolInternal symbolOpt,
            string nameOpt,
            SynthesizedLocalKind kind,
            LocalDebugId id,
            uint pdbAttributes,
            LocalSlotConstraints constraints,
            bool isDynamic,
            ImmutableArray<TypedConstant> dynamicTransformFlags)
        {
            if (this.lazyAllLocals == null)
            {
                this.lazyAllLocals = new ArrayBuilder<Cci.ILocalDefinition>(1);
            }

            LocalDefinition local;

            if (symbolOpt != null && slotAllocatorOpt != null)
            {
                local = this.slotAllocatorOpt.GetPreviousLocal(type, symbolOpt, nameOpt, kind, id, pdbAttributes, constraints, isDynamic, dynamicTransformFlags);
                if (local != null)
                {
                    int slot = local.SlotIndex;
                    this.lazyAllLocals[slot] = local;
                    return local;
                }
            }

            local = new LocalDefinition(
                symbolOpt: symbolOpt,
                nameOpt: nameOpt,
                type: type,
                slot: this.lazyAllLocals.Count,
                synthesizedKind: kind,
                id: id,
                pdbAttributes: pdbAttributes,
                constraints: constraints,
                isDynamic: isDynamic,
                dynamicTransformFlags: dynamicTransformFlags);

            this.lazyAllLocals.Add(local);
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
            if (this.lazyAllLocals == null)
            {
                return ImmutableArray<Cci.ILocalDefinition>.Empty;
            }
            else
            {
                return this.lazyAllLocals.ToImmutable();
            }
        }
    }
}
