// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.Collections;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CodeGen
{
    [Flags]
    internal enum LocalSlotConstraints : byte
    {
        None = 0,
        ByRef = 1,
        Pinned = 2,
    }

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
    internal abstract class LocalSlotManager
    {
        /// <summary>
        /// Structure that represents a local signature (as in <a href="http://www.ecma-international.org/publications/files/ECMA-ST/ECMA-335.pdf">ECMA-335</a>, Partition I, §8.6.1.3 Local signatures).
        /// </summary>
        private struct LocalSignature : IEquatable<LocalSignature>
        {
            private readonly Microsoft.Cci.ITypeReference Type;
            private readonly LocalSlotConstraints Constraints;

            internal LocalSignature(Microsoft.Cci.ITypeReference valType, LocalSlotConstraints constraints)
            {
                this.Constraints = constraints;
                this.Type = valType;
            }

            public bool Equals(LocalSignature other)
            {
                // Microsoft.Cci.ITypeReference does not have object identity.
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
        private Dictionary<object, LocalDefinition> localMap;

        // pool of free slots partitioned by their signature.
        private KeyedStack<LocalSignature, LocalDefinition> freeSlots;

        private Dictionary<object, LocalDefinition> LocalMap
        {
            get
            {
                var map = localMap;
                if (map == null)
                {
                    map = new Dictionary<object, LocalDefinition>(ReferenceEqualityComparer.Instance);
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
            Microsoft.Cci.ITypeReference type,
            object identity,
            string name,
            bool isCompilerGenerated,
            LocalSlotConstraints constraints,
            bool isDynamic,
            ImmutableArray<TypedConstant> dynamicTransformFlags)
        {
            LocalDefinition local;

            if ((name != null) || !FreeSlots.TryPop(new LocalSignature(type, constraints), out local))
            {
                local = this.DeclareLocalInternal(type, identity, name, isCompilerGenerated, constraints, isDynamic, dynamicTransformFlags);
            }

            LocalMap.Add(identity, local);
            return local;
        }

        /// <summary>
        /// Retrieve a local slot by its identity.
        /// </summary>
        internal LocalDefinition GetLocal(object identity)
        {
            return LocalMap[identity];
        }

        /// <summary>
        /// Release a local slot by its identity.
        /// Slot is not associated with identity after this.
        /// </summary>
        internal void FreeLocal(object identity)
        {
            var slot = GetLocal(identity);
            LocalMap.Remove(identity);
            FreeSlot(slot);
        }

        /// <summary>
        /// Gets a local slot.
        /// </summary>
        internal LocalDefinition AllocateSlot(
            Microsoft.Cci.ITypeReference type,
            LocalSlotConstraints constraints,
            ImmutableArray<TypedConstant> dynamicTransformFlags = default(ImmutableArray<TypedConstant>))
        {
            LocalDefinition local;
            if (!FreeSlots.TryPop(new LocalSignature(type, constraints), out local))
            {
                local = this.DeclareLocalInternal(
                    type: type,
                    identity: null,
                    name: null,
                    isCompilerGenerated: true,
                    constraints: constraints,
                    isDynamic: false,
                    dynamicTransformFlags: dynamicTransformFlags);
            }
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

        public abstract ImmutableArray<LocalDefinition> LocalsInOrder();

        protected abstract LocalDefinition DeclareLocalInternal(
            Microsoft.Cci.ITypeReference type,
            object identity,
            string name,
            bool isCompilerGenerated,
            LocalSlotConstraints constraints,
            bool isDynamic,
            ImmutableArray<TypedConstant> dynamicTransformFlags);
    }
}
