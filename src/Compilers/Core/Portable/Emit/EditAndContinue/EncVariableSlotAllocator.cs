// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Reflection.Metadata;
using Microsoft.CodeAnalysis.CodeGen;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Symbols;

namespace Microsoft.CodeAnalysis.Emit
{
    internal sealed class EncVariableSlotAllocator : VariableSlotAllocator
    {
        // symbols:
        private readonly SymbolMatcher _symbolMap;

        // syntax:
        private readonly Func<SyntaxNode, SyntaxNode> _syntaxMapOpt;
        private readonly IMethodSymbolInternal _previousTopLevelMethod;
        private readonly DebugId _methodId;

        // locals:
        private readonly IReadOnlyDictionary<EncLocalInfo, int> _previousLocalSlots;
        private readonly ImmutableArray<EncLocalInfo> _previousLocals;

        // previous state machine:
        private readonly string _stateMachineTypeNameOpt;
        private readonly int _hoistedLocalSlotCount;
        private readonly IReadOnlyDictionary<EncHoistedLocalInfo, int> _hoistedLocalSlotsOpt;
        private readonly int _awaiterCount;
        private readonly IReadOnlyDictionary<Cci.ITypeReference, int> _awaiterMapOpt;

        // closures:
        private readonly IReadOnlyDictionary<int, KeyValuePair<DebugId, int>> _lambdaMapOpt; // SyntaxOffset -> (Lambda Id, Closure Ordinal)
        private readonly IReadOnlyDictionary<int, DebugId> _closureMapOpt; // SyntaxOffset -> Id

        private readonly LambdaSyntaxFacts _lambdaSyntaxFacts;

        public EncVariableSlotAllocator(
            SymbolMatcher symbolMap,
            Func<SyntaxNode, SyntaxNode> syntaxMapOpt,
            IMethodSymbolInternal previousTopLevelMethod,
            DebugId methodId,
            ImmutableArray<EncLocalInfo> previousLocals,
            IReadOnlyDictionary<int, KeyValuePair<DebugId, int>> lambdaMapOpt,
            IReadOnlyDictionary<int, DebugId> closureMapOpt,
            string stateMachineTypeNameOpt,
            int hoistedLocalSlotCount,
            IReadOnlyDictionary<EncHoistedLocalInfo, int> hoistedLocalSlotsOpt,
            int awaiterCount,
            IReadOnlyDictionary<Cci.ITypeReference, int> awaiterMapOpt,
            LambdaSyntaxFacts lambdaSyntaxFacts)
        {
            Debug.Assert(symbolMap != null);
            Debug.Assert(previousTopLevelMethod != null);
            Debug.Assert(!previousLocals.IsDefault);

            _symbolMap = symbolMap;
            _syntaxMapOpt = syntaxMapOpt;
            _previousLocals = previousLocals;
            _previousTopLevelMethod = previousTopLevelMethod;
            _methodId = methodId;
            _hoistedLocalSlotsOpt = hoistedLocalSlotsOpt;
            _hoistedLocalSlotCount = hoistedLocalSlotCount;
            _stateMachineTypeNameOpt = stateMachineTypeNameOpt;
            _awaiterCount = awaiterCount;
            _awaiterMapOpt = awaiterMapOpt;
            _lambdaMapOpt = lambdaMapOpt;
            _closureMapOpt = closureMapOpt;
            _lambdaSyntaxFacts = lambdaSyntaxFacts;

            // Create a map from local info to slot.
            var previousLocalInfoToSlot = new Dictionary<EncLocalInfo, int>();
            for (int slot = 0; slot < previousLocals.Length; slot++)
            {
                var localInfo = previousLocals[slot];
                Debug.Assert(!localInfo.IsDefault);
                if (localInfo.IsUnused)
                {
                    // Unrecognized or deleted local.
                    continue;
                }

                previousLocalInfoToSlot.Add(localInfo, slot);
            }

            _previousLocalSlots = previousLocalInfoToSlot;
        }

        public override DebugId? MethodId => _methodId;

        private int CalculateSyntaxOffsetInPreviousMethod(SyntaxNode node)
        {
            // Note that syntax offset of a syntax node contained in a lambda body is calculated by the containing top-level method,
            // not by the lambda method. The offset is thus relative to the top-level method body start. We can thus avoid mapping 
            // the current lambda symbol or body to the corresponding previous lambda symbol or body, which is non-trivial. 
            return _previousTopLevelMethod.CalculateLocalSyntaxOffset(_lambdaSyntaxFacts.GetDeclaratorPosition(node), node.SyntaxTree);
        }

        public override void AddPreviousLocals(ArrayBuilder<Cci.ILocalDefinition> builder)
        {
            builder.AddRange(_previousLocals.Select((info, index) => new SignatureOnlyLocalDefinition(info.Signature, index)));
        }

        private bool TryGetPreviousLocalId(SyntaxNode currentDeclarator, LocalDebugId currentId, out LocalDebugId previousId)
        {
            if (_syntaxMapOpt == null)
            {
                // no syntax map 
                // => the source of the current method is the same as the source of the previous method 
                // => relative positions are the same 
                // => synthesized ids are the same
                previousId = currentId;
                return true;
            }

            SyntaxNode previousDeclarator = _syntaxMapOpt(currentDeclarator);
            if (previousDeclarator == null)
            {
                previousId = default;
                return false;
            }

            int syntaxOffset = CalculateSyntaxOffsetInPreviousMethod(previousDeclarator);
            previousId = new LocalDebugId(syntaxOffset, currentId.Ordinal);
            return true;
        }

        public override LocalDefinition GetPreviousLocal(
            Cci.ITypeReference currentType,
            ILocalSymbolInternal currentLocalSymbol,
            string nameOpt,
            SynthesizedLocalKind kind,
            LocalDebugId id,
            LocalVariableAttributes pdbAttributes,
            LocalSlotConstraints constraints,
            ImmutableArray<bool> dynamicTransformFlags,
            ImmutableArray<string> tupleElementNames)
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

            var previousType = _symbolMap.MapReference(currentType);
            if (previousType == null)
            {
                return null;
            }

            // TODO (bug #781309): Should report a warning if the type of the local has changed
            // and the previous value will be dropped.
            var localKey = new EncLocalInfo(new LocalSlotDebugInfo(kind, previousId), previousType, constraints, signature: null);

            int slot;
            if (!_previousLocalSlots.TryGetValue(localKey, out slot))
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
                dynamicTransformFlags,
                tupleElementNames);
        }

        public override string PreviousStateMachineTypeName => _stateMachineTypeNameOpt;

        public override bool TryGetPreviousHoistedLocalSlotIndex(
            SyntaxNode currentDeclarator,
            Cci.ITypeReference currentType,
            SynthesizedLocalKind synthesizedKind,
            LocalDebugId currentId,
            DiagnosticBag diagnostics,
            out int slotIndex)
        {
            // The previous method was not a state machine (it is allowed to change non-state machine to a state machine):
            if (_hoistedLocalSlotsOpt == null)
            {
                slotIndex = -1;
                return false;
            }

            LocalDebugId previousId;
            if (!TryGetPreviousLocalId(currentDeclarator, currentId, out previousId))
            {
                slotIndex = -1;
                return false;
            }

            var previousType = _symbolMap.MapReference(currentType);
            if (previousType == null)
            {
                slotIndex = -1;
                return false;
            }

            // TODO (bug #781309): Should report a warning if the type of the local has changed
            // and the previous value will be dropped.
            var localKey = new EncHoistedLocalInfo(new LocalSlotDebugInfo(synthesizedKind, previousId), previousType);

            return _hoistedLocalSlotsOpt.TryGetValue(localKey, out slotIndex);
        }

        public override int PreviousHoistedLocalSlotCount => _hoistedLocalSlotCount;
        public override int PreviousAwaiterSlotCount => _awaiterCount;

        public override bool TryGetPreviousAwaiterSlotIndex(Cci.ITypeReference currentType, DiagnosticBag diagnostics, out int slotIndex)
        {
            // The previous method was not a state machine (it is allowed to change non-state machine to a state machine):
            if (_awaiterMapOpt == null)
            {
                slotIndex = -1;
                return false;
            }

            return _awaiterMapOpt.TryGetValue(_symbolMap.MapReference(currentType), out slotIndex);
        }

        private bool TryGetPreviousSyntaxOffset(SyntaxNode currentSyntax, out int previousSyntaxOffset)
        {
            // no syntax map 
            // => the source of the current method is the same as the source of the previous method 
            // => relative positions are the same 
            // => ids are the same
            SyntaxNode previousSyntax = _syntaxMapOpt?.Invoke(currentSyntax);
            if (previousSyntax == null)
            {
                previousSyntaxOffset = 0;
                return false;
            }

            previousSyntaxOffset = CalculateSyntaxOffsetInPreviousMethod(previousSyntax);
            return true;
        }

        private bool TryGetPreviousLambdaSyntaxOffset(SyntaxNode lambdaOrLambdaBodySyntax, bool isLambdaBody, out int previousSyntaxOffset)
        {
            // Syntax map contains mapping for lambdas, but not their bodies. 
            // Map the lambda first and then determine the corresponding body.
            var currentLambdaSyntax = isLambdaBody
                ? _lambdaSyntaxFacts.GetLambda(lambdaOrLambdaBodySyntax)
                : lambdaOrLambdaBodySyntax;

            // no syntax map 
            // => the source of the current method is the same as the source of the previous method 
            // => relative positions are the same 
            // => ids are the same
            SyntaxNode previousLambdaSyntax = _syntaxMapOpt?.Invoke(currentLambdaSyntax);
            if (previousLambdaSyntax == null)
            {
                previousSyntaxOffset = 0;
                return false;
            }

            SyntaxNode previousSyntax;
            if (isLambdaBody)
            {
                previousSyntax = _lambdaSyntaxFacts.TryGetCorrespondingLambdaBody(previousLambdaSyntax, lambdaOrLambdaBodySyntax);
                if (previousSyntax == null)
                {
                    previousSyntaxOffset = 0;
                    return false;
                }
            }
            else
            {
                previousSyntax = previousLambdaSyntax;
            }

            previousSyntaxOffset = CalculateSyntaxOffsetInPreviousMethod(previousSyntax);
            return true;
        }

        public override bool TryGetPreviousClosure(SyntaxNode scopeSyntax, out DebugId closureId)
        {
            int syntaxOffset;
            if (_closureMapOpt != null &&
                TryGetPreviousSyntaxOffset(scopeSyntax, out syntaxOffset) &&
                _closureMapOpt.TryGetValue(syntaxOffset, out closureId))
            {
                return true;
            }

            closureId = default(DebugId);
            return false;
        }

        public override bool TryGetPreviousLambda(SyntaxNode lambdaOrLambdaBodySyntax, bool isLambdaBody, out DebugId lambdaId)
        {
            KeyValuePair<DebugId, int> idAndClosureOrdinal;

            int syntaxOffset;
            if (_lambdaMapOpt != null &&
                TryGetPreviousLambdaSyntaxOffset(lambdaOrLambdaBodySyntax, isLambdaBody, out syntaxOffset) &&
                _lambdaMapOpt.TryGetValue(syntaxOffset, out idAndClosureOrdinal))
            {
                lambdaId = idAndClosureOrdinal.Key;
                return true;
            }

            lambdaId = default(DebugId);
            return false;
        }
    }
}
