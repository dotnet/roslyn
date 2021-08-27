﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CodeGen;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Emit
{
    internal readonly struct AddedOrChangedMethodInfo
    {
        public readonly DebugId MethodId;

        // locals:
        public readonly ImmutableArray<EncLocalInfo> Locals;

        // lambdas, closures:
        public readonly ImmutableArray<LambdaDebugInfo> LambdaDebugInfo;
        public readonly ImmutableArray<ClosureDebugInfo> ClosureDebugInfo;

        // state machines:
        public readonly string? StateMachineTypeName;
        public readonly ImmutableArray<EncHoistedLocalInfo> StateMachineHoistedLocalSlotsOpt;
        public readonly ImmutableArray<Cci.ITypeReference?> StateMachineAwaiterSlotsOpt;

        public AddedOrChangedMethodInfo(
            DebugId methodId,
            ImmutableArray<EncLocalInfo> locals,
            ImmutableArray<LambdaDebugInfo> lambdaDebugInfo,
            ImmutableArray<ClosureDebugInfo> closureDebugInfo,
            string? stateMachineTypeName,
            ImmutableArray<EncHoistedLocalInfo> stateMachineHoistedLocalSlotsOpt,
            ImmutableArray<Cci.ITypeReference?> stateMachineAwaiterSlotsOpt)
        {
            // An updated method will carry its id over,
            // an added method id has generation set to the current generation ordinal.
            Debug.Assert(methodId.Generation >= 0);

            // each state machine has to have awaiters:
            Debug.Assert(stateMachineAwaiterSlotsOpt.IsDefault == (stateMachineTypeName == null));

            // a state machine might not have hoisted variables:
            Debug.Assert(stateMachineHoistedLocalSlotsOpt.IsDefault || (stateMachineTypeName != null));

            MethodId = methodId;
            Locals = locals;
            LambdaDebugInfo = lambdaDebugInfo;
            ClosureDebugInfo = closureDebugInfo;
            StateMachineTypeName = stateMachineTypeName;
            StateMachineHoistedLocalSlotsOpt = stateMachineHoistedLocalSlotsOpt;
            StateMachineAwaiterSlotsOpt = stateMachineAwaiterSlotsOpt;
        }

        public AddedOrChangedMethodInfo MapTypes(SymbolMatcher map)
        {
            var mappedLocals = ImmutableArray.CreateRange(Locals, MapLocalInfo, map);

            var mappedHoistedLocalSlots = StateMachineHoistedLocalSlotsOpt.IsDefault ? default :
                ImmutableArray.CreateRange(StateMachineHoistedLocalSlotsOpt, MapHoistedLocalSlot, map);

            var mappedAwaiterSlots = StateMachineAwaiterSlotsOpt.IsDefault ? default :
                ImmutableArray.CreateRange(StateMachineAwaiterSlotsOpt, static (typeRef, map) => (typeRef is null) ? null : map.MapReference(typeRef), map);

            return new AddedOrChangedMethodInfo(MethodId, mappedLocals, LambdaDebugInfo, ClosureDebugInfo, StateMachineTypeName, mappedHoistedLocalSlots, mappedAwaiterSlots);
        }

        private static EncLocalInfo MapLocalInfo(EncLocalInfo info, SymbolMatcher map)
        {
            Debug.Assert(!info.IsDefault);
            if (info.Type is null)
            {
                Debug.Assert(info.Signature != null);
                return info;
            }

            var typeRef = map.MapReference(info.Type);
            RoslynDebug.AssertNotNull(typeRef);

            return new EncLocalInfo(info.SlotInfo, typeRef, info.Constraints, info.Signature);
        }

        private static EncHoistedLocalInfo MapHoistedLocalSlot(EncHoistedLocalInfo info, SymbolMatcher map)
        {
            if (info.Type is null)
            {
                return info;
            }

            var typeRef = map.MapReference(info.Type);
            RoslynDebug.AssertNotNull(typeRef);

            return new EncHoistedLocalInfo(info.SlotInfo, typeRef);
        }
    }
}
