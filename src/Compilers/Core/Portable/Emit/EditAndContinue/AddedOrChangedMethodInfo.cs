// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CodeGen;

namespace Microsoft.CodeAnalysis.Emit
{
    internal struct AddedOrChangedMethodInfo
    {
        public readonly DebugId MethodId;

        // locals:
        public readonly ImmutableArray<EncLocalInfo> Locals;

        // lambdas, closures:
        public readonly ImmutableArray<LambdaDebugInfo> LambdaDebugInfo;
        public readonly ImmutableArray<ClosureDebugInfo> ClosureDebugInfo;

        // state machines:
        public readonly string StateMachineTypeNameOpt;
        public readonly ImmutableArray<EncHoistedLocalInfo> StateMachineHoistedLocalSlotsOpt;
        public readonly ImmutableArray<Cci.ITypeReference> StateMachineAwaiterSlotsOpt;

        public AddedOrChangedMethodInfo(
            DebugId methodId,
            ImmutableArray<EncLocalInfo> locals,
            ImmutableArray<LambdaDebugInfo> lambdaDebugInfo,
            ImmutableArray<ClosureDebugInfo> closureDebugInfo,
            string stateMachineTypeNameOpt,
            ImmutableArray<EncHoistedLocalInfo> stateMachineHoistedLocalSlotsOpt,
            ImmutableArray<Cci.ITypeReference> stateMachineAwaiterSlotsOpt)
        {
            // An updated method will carry its id over,
            // an added method id has generation set to the current generation ordinal.
            Debug.Assert(methodId.Generation >= 0);

            // each state machine has to have awaiters:
            Debug.Assert(stateMachineAwaiterSlotsOpt.IsDefault == (stateMachineTypeNameOpt == null));

            // a state machine might not have hoisted variables:
            Debug.Assert(stateMachineHoistedLocalSlotsOpt.IsDefault || (stateMachineTypeNameOpt != null));

            this.MethodId = methodId;
            this.Locals = locals;
            this.LambdaDebugInfo = lambdaDebugInfo;
            this.ClosureDebugInfo = closureDebugInfo;
            this.StateMachineTypeNameOpt = stateMachineTypeNameOpt;
            this.StateMachineHoistedLocalSlotsOpt = stateMachineHoistedLocalSlotsOpt;
            this.StateMachineAwaiterSlotsOpt = stateMachineAwaiterSlotsOpt;
        }

        public AddedOrChangedMethodInfo MapTypes(SymbolMatcher map)
        {
            var mappedLocals = ImmutableArray.CreateRange(this.Locals, MapLocalInfo, map);
            var mappedHoistedLocalSlots = StateMachineHoistedLocalSlotsOpt.IsDefault ? StateMachineHoistedLocalSlotsOpt : ImmutableArray.CreateRange(StateMachineHoistedLocalSlotsOpt, MapHoistedLocalSlot, map);
            var mappedAwaiterSlots = StateMachineAwaiterSlotsOpt.IsDefault ? StateMachineAwaiterSlotsOpt : ImmutableArray.CreateRange(StateMachineAwaiterSlotsOpt, map.MapReference);

            return new AddedOrChangedMethodInfo(this.MethodId, mappedLocals, LambdaDebugInfo, ClosureDebugInfo, StateMachineTypeNameOpt, mappedHoistedLocalSlots, mappedAwaiterSlots);
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
