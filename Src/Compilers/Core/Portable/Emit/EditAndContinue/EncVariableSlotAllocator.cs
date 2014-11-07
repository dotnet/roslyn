// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis.CodeGen;
using Microsoft.CodeAnalysis.Symbols;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Emit
{
    internal sealed class EncVariableSlotAllocator : VariableSlotAllocator
    {
        // symbols:
        private readonly SymbolMatcher symbolMap;

        // syntax:
        private readonly Func<SyntaxNode, SyntaxNode> syntaxMapOpt;
        private readonly IMethodSymbolInternal previousMethod;

        // locals:
        private readonly IReadOnlyDictionary<EncLocalInfo, int> previousLocalSlots;
        private readonly ImmutableArray<EncLocalInfo> previousLocals;

        // state machine:
        private readonly string previousStateMachineTypeNameOpt;
        private readonly int hoistedLocalSlotCount;
        private readonly IReadOnlyDictionary<EncLocalInfo, string> previousHoistedLocalSlotsOpt;
        private readonly int awaiterCount;
        private readonly IReadOnlyDictionary<Cci.ITypeReference, string> awaiterMapOpt;

        public EncVariableSlotAllocator(
            SymbolMatcher symbolMap,
            Func<SyntaxNode, SyntaxNode> syntaxMapOpt,
            IMethodSymbolInternal previousMethod,
            ImmutableArray<EncLocalInfo> previousLocals,
            string previousStateMachineTypeNameOpt,
            int hoistedLocalSlotCount,
            IReadOnlyDictionary<EncLocalInfo, string> previousHoistedLocalSlotsOpt,
            int awaiterCount,
            IReadOnlyDictionary<Cci.ITypeReference, string> awaiterMapOpt)
        {
            Debug.Assert(symbolMap != null);
            Debug.Assert(previousMethod != null);
            Debug.Assert(!previousLocals.IsDefault);

            this.symbolMap = symbolMap;
            this.syntaxMapOpt = syntaxMapOpt;
            this.previousLocals = previousLocals;
            this.previousMethod = previousMethod;
            this.previousHoistedLocalSlotsOpt = previousHoistedLocalSlotsOpt;
            this.hoistedLocalSlotCount = hoistedLocalSlotCount;
            this.previousStateMachineTypeNameOpt = previousStateMachineTypeNameOpt;
            this.awaiterCount = awaiterCount;
            this.awaiterMapOpt = awaiterMapOpt;

            // Create a map from local info to slot.
            var previousLocalInfoToSlot = new Dictionary<EncLocalInfo, int>();
            for (int slot = 0; slot < previousLocals.Length; slot++)
            {
                var localInfo = previousLocals[slot];
                Debug.Assert(!localInfo.IsDefault);
                if (localInfo.IsInvalid)
                {
                    // Unrecognized or deleted local.
                    continue;
                }

                previousLocalInfoToSlot.Add(localInfo, slot);
            }

            this.previousLocalSlots = previousLocalInfoToSlot;
        }

        public override void AddPreviousLocals(ArrayBuilder<Cci.ILocalDefinition> builder)
        {
            builder.AddRange(this.previousLocals.Select((info, index) => new SignatureOnlyLocalDefinition(info.Signature, index)));
        }

        private bool TryGetPreviousLocalId(SyntaxNode currentDeclarator, LocalDebugId currentId, out LocalDebugId previousId)
        {
            if (syntaxMapOpt == null)
            {
                // no syntax map 
                // => the source of the current method is the same as the source of the previous method 
                // => relative positions are the same 
                // => synthesized ids are the same
                previousId = currentId;
                return true;
            }

            SyntaxNode previousDeclarator = syntaxMapOpt(currentDeclarator);
            if (previousDeclarator == null)
            {
                previousId = default(LocalDebugId);
                return false;
            }

            int syntaxOffset = previousMethod.CalculateLocalSyntaxOffset(previousDeclarator.SpanStart, previousDeclarator.SyntaxTree);
            previousId = new LocalDebugId(syntaxOffset, currentId.Ordinal);
            return true;
        }

        public override LocalDefinition GetPreviousLocal(
            Cci.ITypeReference currentType,
            ILocalSymbolInternal currentLocalSymbol,
            string nameOpt,
            SynthesizedLocalKind kind,
            LocalDebugId id,
            uint pdbAttributes,
            LocalSlotConstraints constraints,
            bool isDynamic,
            ImmutableArray<TypedConstant> dynamicTransformFlags)
        {
            if (id.IsNone)
            {
                return null;
            }

            LocalDebugId previousId;
            if (!TryGetPreviousLocalId(currentLocalSymbol.GetDeclaratorSyntax(), id, out previousId))
            {
                return null;
            }

            var previousType = symbolMap.MapReference(currentType);
            if (previousType == null)
            {
                return null;
            }

            // TODO (bug #781309): Should report a warning if the type of the local has changed
            // and the previous value will be dropped.
            var localKey = new EncLocalInfo(previousId, previousType, constraints, kind, signature: null);

            int slot;
            if (!previousLocalSlots.TryGetValue(localKey, out slot))
            {
                return null;
            }

            return new LocalDefinition(
                currentLocalSymbol,
                nameOpt,
                currentType,
                slot,
                kind,
                id,
                pdbAttributes,
                constraints,
                isDynamic,
                dynamicTransformFlags);
        }

        public override string GetPreviousHoistedLocal(SyntaxNode currentDeclarator, Cci.ITypeReference currentType, SynthesizedLocalKind synthesizedKind, LocalDebugId currentId)
        {
            Debug.Assert(previousHoistedLocalSlotsOpt != null);

            LocalDebugId previousId;
            if (!TryGetPreviousLocalId(currentDeclarator, currentId, out previousId))
            {
                return null;
            }

            var previousType = symbolMap.MapReference(currentType);
            if (previousType == null)
            {
                return null;
            }

            // TODO (bug #781309): Should report a warning if the type of the local has changed
            // and the previous value will be dropped.
            var localKey = new EncLocalInfo(previousId, previousType, LocalSlotConstraints.None, synthesizedKind, signature: null);

            string fieldName;
            if (!previousHoistedLocalSlotsOpt.TryGetValue(localKey, out fieldName))
            {
                return null;
            }

            return fieldName;
        }


        public override string PreviousStateMachineTypeName
        {
            get { return previousStateMachineTypeNameOpt; }
        }

        public override int PreviousHoistedLocalSlotCount
        {
            get { return hoistedLocalSlotCount; }
        }

        public override int PreviousAwaiterSlotCount
        {
            get { return awaiterCount; }
        }

        public override string GetPreviousAwaiter(Cci.ITypeReference currentType)
        {
            Debug.Assert(awaiterMapOpt != null);

            string name;
            return awaiterMapOpt.TryGetValue(symbolMap.MapReference(currentType), out name) ? name : null;
        }
    }
}