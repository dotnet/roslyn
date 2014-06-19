// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.Cci;
using Microsoft.CodeAnalysis.CodeGen;
using Roslyn.Utilities;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;

namespace Microsoft.CodeAnalysis.Emit
{
    internal delegate int GetPreviousLocalSlot(object identity, ITypeReference type, LocalSlotConstraints constraints);

    internal sealed class EncLocalSlotManager : LocalSlotManager
    {
        // all locals in order
        private readonly List<ILocalDefinition> allLocals;

        // map locals to local slots from the previous generation of the method
        private readonly GetPreviousLocalSlot getPreviousLocalSlot;

        public EncLocalSlotManager(ImmutableArray<EncLocalInfo> previousLocals, GetPreviousLocalSlot getPreviousLocalSlot)
        {
            // Add placeholders for previous locals. The actual
            // identities are populated if/when the locals are reused.
            this.allLocals = new List<ILocalDefinition>(previousLocals.Select((info, index) => new SignatureOnlyLocalDefinition(info.Signature, index)));
            this.getPreviousLocalSlot = getPreviousLocalSlot;
        }

        public override ImmutableArray<ILocalDefinition> LocalsInOrder()
        {
            return ImmutableArray.CreateRange(this.allLocals);
        }

        protected override LocalDefinition DeclareLocalInternal(
            ITypeReference type,
            object identity,
            string name,
            bool isCompilerGenerated,
            LocalSlotConstraints constraints,
            bool isDynamic,
            ImmutableArray<TypedConstant> dynamicTransformFlags)
        {
            LocalDefinition local;

            if (identity != null)
            {
                int slot = this.getPreviousLocalSlot(identity, type, constraints);
                if (slot >= 0)
                {
                    Debug.Assert(this.allLocals[slot] is SignatureOnlyLocalDefinition);

                    local = new LocalDefinition(
                        identity: identity,
                        name: name,
                        type: type,
                        slot: slot,
                        isCompilerGenerated: isCompilerGenerated,
                        constraints: constraints,
                        isDynamic: isDynamic,
                        dynamicTransformFlags: dynamicTransformFlags);
                    this.allLocals[slot] = local;
                    return local;
                }
            }

            local = new LocalDefinition(
                identity: identity,
                name: name,
                type: type,
                slot: this.allLocals.Count,
                isCompilerGenerated: isCompilerGenerated,
                constraints: constraints,
                isDynamic: isDynamic,
                dynamicTransformFlags: dynamicTransformFlags);
            this.allLocals.Add(local);
            return local;
        }

        /// <summary>
        /// A local from an earlier generation.
        /// </summary>
        private sealed class SignatureOnlyLocalDefinition : ILocalDefinition
        {
            private readonly byte[] signature;
            private readonly int slot;

            internal SignatureOnlyLocalDefinition(byte[] signature, int slot)
            {
                this.signature = signature;
                this.slot = slot;
            }

            public IMetadataConstant CompileTimeValue
            {
                get { throw ExceptionUtilities.Unreachable; }
            }

            public ImmutableArray<ICustomModifier> CustomModifiers
            {
                get { throw ExceptionUtilities.Unreachable; }
            }

            public ImmutableArray<TypedConstant> DynamicTransformFlags
            {
                get { throw ExceptionUtilities.Unreachable; }
            }

            // The placeholder local is marked as compiler-generated
            // so it will be excluded from the PDB and debugger if not
            // replaced by a valid local in DeclareLocalInternal.
            public bool IsCompilerGenerated
            {
                get { return true; }
            }

            public bool IsDynamic
            {
                get { return false; }
            }

            public bool IsPinned
            {
                get { throw ExceptionUtilities.Unreachable; }
            }

            public bool IsReference
            {
                get { throw ExceptionUtilities.Unreachable; }
            }

            public Location Location
            {
                get { return Location.None; }
            }

            public string Name
            {
                get { return null; }
            }

            public int SlotIndex
            {
                get { return this.slot; }
            }

            public ITypeReference Type
            {
                get { throw ExceptionUtilities.Unreachable; }
            }

            public byte[] Signature
            {
                get { return this.signature; }
            }
        }
    }
}
