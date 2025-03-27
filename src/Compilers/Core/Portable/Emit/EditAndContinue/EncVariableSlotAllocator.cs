// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Reflection.Metadata;
using Microsoft.CodeAnalysis.CodeGen;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Symbols;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Emit
{
    internal sealed class EncVariableSlotAllocator : VariableSlotAllocator
    {
        // symbols:
        private readonly SymbolMatcher _symbolMap;

        // syntax:
        private readonly EncMappedMethod _mappedMethod;
        private readonly DebugId? _methodId;

        // locals:
        private readonly IReadOnlyDictionary<EncLocalInfo, int> _previousLocalSlots;
        private readonly ImmutableArray<EncLocalInfo> _previousLocals;

        // previous state machine:
        private readonly string? _stateMachineTypeName;
        private readonly int _hoistedLocalSlotCount;
        private readonly IReadOnlyDictionary<EncHoistedLocalInfo, int>? _hoistedLocalSlots;
        private readonly int _awaiterCount;
        private readonly IReadOnlyDictionary<Cci.ITypeReference, int>? _awaiterMap;
        private readonly IReadOnlyDictionary<(int syntaxOffset, AwaitDebugId awaitId), StateMachineState>? _stateMachineStateMap;
        private readonly StateMachineState? _firstUnusedDecreasingStateMachineState;
        private readonly StateMachineState? _firstUnusedIncreasingStateMachineState;

        // closures (keyed by syntax offset):
        private readonly IReadOnlyDictionary<int, EncLambdaMapValue>? _lambdaMap;
        private readonly IReadOnlyDictionary<int, EncClosureMapValue>? _closureMap;

        private readonly LambdaSyntaxFacts _lambdaSyntaxFacts;

        public EncVariableSlotAllocator(
            SymbolMatcher symbolMap,
            EncMappedMethod mappedMethod,
            DebugId? methodId,
            ImmutableArray<EncLocalInfo> previousLocals,
            IReadOnlyDictionary<int, EncLambdaMapValue>? lambdaMap,
            IReadOnlyDictionary<int, EncClosureMapValue>? closureMap,
            string? stateMachineTypeName,
            int hoistedLocalSlotCount,
            IReadOnlyDictionary<EncHoistedLocalInfo, int>? hoistedLocalSlots,
            int awaiterCount,
            IReadOnlyDictionary<Cci.ITypeReference, int>? awaiterMap,
            IReadOnlyDictionary<(int syntaxOffset, AwaitDebugId awaitId), StateMachineState>? stateMachineStateMap,
            StateMachineState? firstUnusedIncreasingStateMachineState,
            StateMachineState? firstUnusedDecreasingStateMachineState,
            LambdaSyntaxFacts lambdaSyntaxFacts)
        {
            Debug.Assert(!previousLocals.IsDefault);

            _symbolMap = symbolMap;
            _mappedMethod = mappedMethod;
            _previousLocals = previousLocals;
            _methodId = methodId;
            _hoistedLocalSlots = hoistedLocalSlots;
            _hoistedLocalSlotCount = hoistedLocalSlotCount;
            _stateMachineTypeName = stateMachineTypeName;
            _awaiterCount = awaiterCount;
            _awaiterMap = awaiterMap;
            _stateMachineStateMap = stateMachineStateMap;
            _lambdaMap = lambdaMap;
            _closureMap = closureMap;
            _lambdaSyntaxFacts = lambdaSyntaxFacts;
            _firstUnusedIncreasingStateMachineState = firstUnusedIncreasingStateMachineState;
            _firstUnusedDecreasingStateMachineState = firstUnusedDecreasingStateMachineState;

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
            return _mappedMethod.PreviousMethod.CalculateLocalSyntaxOffset(_lambdaSyntaxFacts.GetDeclaratorPosition(node), node.SyntaxTree);
        }

        public override void AddPreviousLocals(ArrayBuilder<Cci.ILocalDefinition> builder)
        {
            builder.AddRange(_previousLocals.Select((info, index) =>
            {
                RoslynDebug.AssertNotNull(info.Signature);
                return new SignatureOnlyLocalDefinition(info.Signature, index);
            }));
        }

        private bool TryGetPreviousLocalId(SyntaxNode currentDeclarator, LocalDebugId currentId, out LocalDebugId previousId)
        {
            if (_mappedMethod.SyntaxMap == null)
            {
                // no syntax map 
                // => the source of the current method is the same as the source of the previous method 
                // => relative positions are the same 
                // => synthesized ids are the same
                previousId = currentId;
                return true;
            }

            SyntaxNode? previousDeclarator = _mappedMethod.SyntaxMap(currentDeclarator);
            if (previousDeclarator == null)
            {
                previousId = default;
                return false;
            }

            int syntaxOffset = CalculateSyntaxOffsetInPreviousMethod(previousDeclarator);
            previousId = new LocalDebugId(syntaxOffset, currentId.Ordinal);
            return true;
        }

        public override LocalDefinition? GetPreviousLocal(
            Cci.ITypeReference currentType,
            ILocalSymbolInternal currentLocalSymbol,
            string? name,
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

            if (!TryGetPreviousLocalId(currentLocalSymbol.GetDeclaratorSyntax(), id, out LocalDebugId previousId))
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

            if (!_previousLocalSlots.TryGetValue(localKey, out int slot))
            {
                return null;
            }

            return new LocalDefinition(
                currentLocalSymbol,
                name,
                currentType,
                slot,
                kind,
                id,
                pdbAttributes,
                constraints,
                dynamicTransformFlags,
                tupleElementNames);
        }

        public override string? PreviousStateMachineTypeName => _stateMachineTypeName;

        public override bool TryGetPreviousHoistedLocalSlotIndex(
            SyntaxNode currentDeclarator,
            Cci.ITypeReference currentType,
            SynthesizedLocalKind synthesizedKind,
            LocalDebugId currentId,
            DiagnosticBag diagnostics,
            out int slotIndex)
        {
            // The previous method was not a state machine (it is allowed to change non-state machine to a state machine):
            if (_hoistedLocalSlots == null)
            {
                slotIndex = -1;
                return false;
            }

            if (!TryGetPreviousLocalId(currentDeclarator, currentId, out LocalDebugId previousId))
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

            return _hoistedLocalSlots.TryGetValue(localKey, out slotIndex);
        }

        public override int PreviousHoistedLocalSlotCount => _hoistedLocalSlotCount;
        public override int PreviousAwaiterSlotCount => _awaiterCount;

        public override bool TryGetPreviousAwaiterSlotIndex(Cci.ITypeReference currentType, DiagnosticBag diagnostics, out int slotIndex)
        {
            // The previous method was not a state machine (it is allowed to change non-state machine to a state machine):
            if (_awaiterMap == null)
            {
                slotIndex = -1;
                return false;
            }

            var typeRef = _symbolMap.MapReference(currentType);
            RoslynDebug.AssertNotNull(typeRef);

            return _awaiterMap.TryGetValue(typeRef, out slotIndex);
        }

        private bool TryGetPreviousSyntaxOffset(SyntaxNode currentSyntax, out int previousSyntaxOffset)
        {
            // no syntax map 
            // => the source of the current method is the same as the source of the previous method 
            // => relative positions are the same 
            // => ids are the same
            SyntaxNode? previousSyntax = _mappedMethod.SyntaxMap?.Invoke(currentSyntax);
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
            SyntaxNode? previousLambdaSyntax = _mappedMethod.SyntaxMap?.Invoke(currentLambdaSyntax);
            if (previousLambdaSyntax == null)
            {
                previousSyntaxOffset = 0;
                return false;
            }

            SyntaxNode? previousSyntax;
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

        public override bool TryGetPreviousClosure(
            SyntaxNode scopeSyntax,
            DebugId? parentClosureId,
            ImmutableArray<string> structCaptures,
            out DebugId closureId,
            out RuntimeRudeEdit? runtimeRudeEdit)
        {
            if (_closureMap != null && TryGetPreviousSyntaxOffset(scopeSyntax, out int syntaxOffset))
            {
                if (_closureMap.TryGetValue(syntaxOffset, out var closureMapValue) &&
                    closureMapValue.IsCompatibleWith(parentClosureId, structCaptures))
                {
                    closureId = closureMapValue.Id;
                    runtimeRudeEdit = _mappedMethod.RuntimeRudeEdit?.Invoke(scopeSyntax);
                    return true;
                }

                // closure shape changed:
                closureId = default;
                runtimeRudeEdit = new RuntimeRudeEdit(HotReloadExceptionCode.UnsupportedChangeToCapturedVariables);
                return false;
            }

            // closure added:
            closureId = default;
            runtimeRudeEdit = null;
            return false;
        }

        public override bool TryGetPreviousLambda(SyntaxNode lambdaOrLambdaBodySyntax, bool isLambdaBody, int closureOrdinal, ImmutableArray<DebugId> structClosureIds, out DebugId lambdaId, out RuntimeRudeEdit? runtimeRudeEdit)
        {
            Debug.Assert(closureOrdinal >= LambdaDebugInfo.MinClosureOrdinal);

            if (_lambdaMap != null && TryGetPreviousLambdaSyntaxOffset(lambdaOrLambdaBodySyntax, isLambdaBody, out int syntaxOffset))
            {
                if (_lambdaMap.TryGetValue(syntaxOffset, out var lambdaMapValue) && lambdaMapValue.IsCompatibleWith(closureOrdinal, structClosureIds))
                {
                    // Rude edit map contains mapping for lambdas, but not their bodies. 
                    runtimeRudeEdit = _mappedMethod.RuntimeRudeEdit?.Invoke(isLambdaBody ? _lambdaSyntaxFacts.GetLambda(lambdaOrLambdaBodySyntax) : lambdaOrLambdaBodySyntax);

                    lambdaId = lambdaMapValue.Id;
                    return true;
                }

                // lambda closure changed:
                lambdaId = default;
                runtimeRudeEdit = new RuntimeRudeEdit(HotReloadExceptionCode.UnsupportedChangeToCapturedVariables);
                return false;
            }

            // lambda added:
            lambdaId = default;
            runtimeRudeEdit = null;
            return false;
        }

        public override StateMachineState? GetFirstUnusedStateMachineState(bool increasing)
            => increasing ? _firstUnusedIncreasingStateMachineState : _firstUnusedDecreasingStateMachineState;

        public override bool TryGetPreviousStateMachineState(SyntaxNode syntax, AwaitDebugId awaitId, out StateMachineState state)
        {
            if (_stateMachineStateMap != null &&
                TryGetPreviousSyntaxOffset(syntax, out int syntaxOffset) &&
                _stateMachineStateMap.TryGetValue((syntaxOffset, awaitId), out state))
            {
                return true;
            }

            state = default;
            return false;
        }
    }
}
