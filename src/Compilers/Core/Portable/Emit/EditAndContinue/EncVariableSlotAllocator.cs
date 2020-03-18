﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

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
        private readonly Func<SyntaxNode, SyntaxNode>? _syntaxMap;
        private readonly IMethodSymbolInternal _previousTopLevelMethod;
        private readonly DebugId _methodId;

        // locals:
        private readonly IReadOnlyDictionary<EncLocalInfo, int> _previousLocalSlots;
        private readonly ImmutableArray<EncLocalInfo> _previousLocals;

        // previous state machine:
        private readonly string? _stateMachineTypeName;
        private readonly int _hoistedLocalSlotCount;
        private readonly IReadOnlyDictionary<EncHoistedLocalInfo, int>? _hoistedLocalSlots;
        private readonly int _awaiterCount;
        private readonly IReadOnlyDictionary<Cci.ITypeReference, int>? _awaiterMap;

        // closures:
        private readonly IReadOnlyDictionary<int, KeyValuePair<DebugId, int>>? _lambdaMap; // SyntaxOffset -> (Lambda Id, Closure Ordinal)
        private readonly IReadOnlyDictionary<int, DebugId>? _closureMap; // SyntaxOffset -> Id

        private readonly LambdaSyntaxFacts _lambdaSyntaxFacts;

        public EncVariableSlotAllocator(
            SymbolMatcher symbolMap,
            Func<SyntaxNode, SyntaxNode>? syntaxMap,
            IMethodSymbolInternal previousTopLevelMethod,
            DebugId methodId,
            ImmutableArray<EncLocalInfo> previousLocals,
            IReadOnlyDictionary<int, KeyValuePair<DebugId, int>>? lambdaMap,
            IReadOnlyDictionary<int, DebugId>? closureMap,
            string? stateMachineTypeName,
            int hoistedLocalSlotCount,
            IReadOnlyDictionary<EncHoistedLocalInfo, int>? hoistedLocalSlots,
            int awaiterCount,
            IReadOnlyDictionary<Cci.ITypeReference, int>? awaiterMap,
            LambdaSyntaxFacts lambdaSyntaxFacts)
        {
            RoslynDebug.AssertNotNull(symbolMap);
            RoslynDebug.AssertNotNull(previousTopLevelMethod);
            Debug.Assert(!previousLocals.IsDefault);

            _symbolMap = symbolMap;
            _syntaxMap = syntaxMap;
            _previousLocals = previousLocals;
            _previousTopLevelMethod = previousTopLevelMethod;
            _methodId = methodId;
            _hoistedLocalSlots = hoistedLocalSlots;
            _hoistedLocalSlotCount = hoistedLocalSlotCount;
            _stateMachineTypeName = stateMachineTypeName;
            _awaiterCount = awaiterCount;
            _awaiterMap = awaiterMap;
            _lambdaMap = lambdaMap;
            _closureMap = closureMap;
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
            if (_syntaxMap == null)
            {
                // no syntax map 
                // => the source of the current method is the same as the source of the previous method 
                // => relative positions are the same 
                // => synthesized ids are the same
                previousId = currentId;
                return true;
            }

            SyntaxNode previousDeclarator = _syntaxMap(currentDeclarator);
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

            return _awaiterMap.TryGetValue(_symbolMap.MapReference(currentType), out slotIndex);
        }

        private bool TryGetPreviousSyntaxOffset(SyntaxNode currentSyntax, out int previousSyntaxOffset)
        {
            // no syntax map 
            // => the source of the current method is the same as the source of the previous method 
            // => relative positions are the same 
            // => ids are the same
            SyntaxNode? previousSyntax = _syntaxMap?.Invoke(currentSyntax);
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
            SyntaxNode? previousLambdaSyntax = _syntaxMap?.Invoke(currentLambdaSyntax);
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
            if (_closureMap != null &&
                TryGetPreviousSyntaxOffset(scopeSyntax, out int syntaxOffset) &&
                _closureMap.TryGetValue(syntaxOffset, out closureId))
            {
                return true;
            }

            closureId = default;
            return false;
        }

        public override bool TryGetPreviousLambda(SyntaxNode lambdaOrLambdaBodySyntax, bool isLambdaBody, out DebugId lambdaId)
        {
            if (_lambdaMap != null &&
                TryGetPreviousLambdaSyntaxOffset(lambdaOrLambdaBodySyntax, isLambdaBody, out int syntaxOffset) &&
                _lambdaMap.TryGetValue(syntaxOffset, out var idAndClosureOrdinal))
            {
                lambdaId = idAndClosureOrdinal.Key;
                return true;
            }

            lambdaId = default;
            return false;
        }
    }
}
