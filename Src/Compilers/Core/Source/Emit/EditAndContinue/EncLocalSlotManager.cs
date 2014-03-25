// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CodeGen;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;

namespace Microsoft.CodeAnalysis.Emit
{
    internal delegate int GetPreviousLocalSlot(object identity, Microsoft.Cci.ITypeReference type, LocalSlotConstraints constraints);

    internal sealed class EncLocalSlotManager : LocalSlotManager
    {
        // all locals in order
        private readonly List<LocalDefinition> allLocals;

        // map locals to local slots from the previous generation of the method
        private readonly GetPreviousLocalSlot getPreviousLocalSlot;

        public EncLocalSlotManager(ImmutableArray<EncLocalInfo> previousLocals, GetPreviousLocalSlot getPreviousLocalSlot)
        {
            this.allLocals = new List<LocalDefinition>();
            this.getPreviousLocalSlot = getPreviousLocalSlot;

            // Add placeholders for previous locals. The actual
            // identities are populated if/when the locals are reused.
            for (int i = 0; i < previousLocals.Length; i++)
            {
                var localInfo = previousLocals[i];
                Debug.Assert(localInfo.Type != null);
                var local = new LocalDefinition(
                    identity: null,
                    name: null,
                    type: localInfo.Type,
                    slot: i,
                    isCompilerGenerated: true,
                    // The placeholder local is marked as compiler-generated
                    // so it will be excluded from the PDB and debugger if not
                    // replaced by a valid local in DeclareLocalInternal.
                    constraints: localInfo.Constraints,
                    isDynamic: false,
                    dynamicTransformFlags: default(ImmutableArray<TypedConstant>));
                this.allLocals.Add(local);
            }
        }

        public override ImmutableArray<LocalDefinition> LocalsInOrder()
        {
            return this.allLocals.AsImmutable<LocalDefinition>();
        }

        protected override LocalDefinition DeclareLocalInternal(
            Microsoft.Cci.ITypeReference type,
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
                    Debug.Assert(this.allLocals[slot].Identity == null);

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
    }
}
