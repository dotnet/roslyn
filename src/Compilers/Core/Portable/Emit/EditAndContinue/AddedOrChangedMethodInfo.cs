// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Diagnostics;

namespace Microsoft.CodeAnalysis.Emit
{
    internal struct AddedOrChangedMethodInfo
    {
        public readonly ImmutableArray<EncLocalInfo> Locals;

        public readonly string StateMachineTypeNameOpt;
        public readonly ImmutableArray<EncHoistedLocalInfo> StateMachineHoistedLocalSlotsOpt;
        public readonly ImmutableArray<Cci.ITypeReference> StateMachineAwaiterSlotsOpt;

        public AddedOrChangedMethodInfo(
            ImmutableArray<EncLocalInfo> locals, 
            string stateMachineTypeNameOpt,
            ImmutableArray<EncHoistedLocalInfo> stateMachineHoistedLocalSlotsOpt,
            ImmutableArray<Cci.ITypeReference> stateMachineAwaiterSlotsOpt)
        {
            // each state machine has to have awaiters:
            Debug.Assert(stateMachineAwaiterSlotsOpt.IsDefault == (stateMachineTypeNameOpt == null));

            // a state machine might not have hoisted variables:
            Debug.Assert(stateMachineHoistedLocalSlotsOpt.IsDefault || (stateMachineTypeNameOpt != null));

            this.Locals = locals;
            this.StateMachineTypeNameOpt = stateMachineTypeNameOpt;
            this.StateMachineHoistedLocalSlotsOpt = stateMachineHoistedLocalSlotsOpt;
            this.StateMachineAwaiterSlotsOpt = stateMachineAwaiterSlotsOpt;
        }

        public AddedOrChangedMethodInfo MapTypes(SymbolMatcher map)
        {
            var mappedLocals = ImmutableArray.CreateRange(this.Locals, MapLocalInfo, map);
            var mappedHoistedLocalSlots = StateMachineHoistedLocalSlotsOpt.IsDefault ? StateMachineHoistedLocalSlotsOpt : ImmutableArray.CreateRange(StateMachineHoistedLocalSlotsOpt, MapHoistedLocalSlot, map);
            var mappedAwaiterSlots = StateMachineAwaiterSlotsOpt.IsDefault ? StateMachineAwaiterSlotsOpt : ImmutableArray.CreateRange(StateMachineAwaiterSlotsOpt, map.MapReference);

            return new AddedOrChangedMethodInfo(mappedLocals, StateMachineTypeNameOpt, mappedHoistedLocalSlots, mappedAwaiterSlots);
        }

        private static EncLocalInfo MapLocalInfo(EncLocalInfo info, SymbolMatcher map)
        {
            Debug.Assert(!info.IsDefault);
            if (info.IsUnused)
            {
                Debug.Assert(info.Signature != null);
                return info;
            }

            return new EncLocalInfo(info.SlotInfo, map.MapReference(info.Type), info.Constraints, info.Signature);
        }

        private static EncHoistedLocalInfo MapHoistedLocalSlot(EncHoistedLocalInfo info, SymbolMatcher map)
        {
            if (info.IsUnused)
            {
                return info;
            }

            return new EncHoistedLocalInfo(info.SlotInfo, map.MapReference(info.Type));
        }
    }
}
