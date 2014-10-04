// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis.CodeGen;
using Microsoft.CodeAnalysis.Symbols;

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
        private readonly IReadOnlyDictionary<EncLocalInfo, int> previousLocalInfoToSlot;
        private readonly ImmutableArray<EncLocalInfo> previousLocals;

        public EncVariableSlotAllocator(
            SymbolMatcher symbolMap,
            Func<SyntaxNode, SyntaxNode> syntaxMapOpt,
            IMethodSymbolInternal previousMethod,
            ImmutableArray<EncLocalInfo> previousLocals)
        {
            Debug.Assert(symbolMap != null);
            Debug.Assert(previousMethod != null);
            Debug.Assert(!previousLocals.IsDefault);

            this.symbolMap = symbolMap;
            this.syntaxMapOpt = syntaxMapOpt;
            this.previousLocals = previousLocals;
            this.previousMethod = previousMethod;

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

            this.previousLocalInfoToSlot = previousLocalInfoToSlot;
        }

        public override void AddPreviousLocals(ArrayBuilder<Cci.ILocalDefinition> builder)
        {
            builder.AddRange(this.previousLocals.Select((info, index) => new SignatureOnlyLocalDefinition(info.Signature, index)));
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
            var currentSyntax = currentLocalSymbol.GetDeclaratorSyntax();
            if (syntaxMapOpt != null)
            {
                SyntaxNode previousSyntax = syntaxMapOpt(currentSyntax);
                if (previousSyntax == null)
                {
                    return null;
                }

                int syntaxOffset = previousMethod.CalculateLocalSyntaxOffset(previousSyntax.SpanStart, previousSyntax.SyntaxTree);
                previousId = new LocalDebugId(syntaxOffset, id.Ordinal, id.Subordinal);
            }
            else
            {
                // no syntax map 
                // => the source of the current method is the same as the source of the previous method 
                // => relative positions are the same 
                // => synthesized ids are the same
                previousId = id;
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
            if (!previousLocalInfoToSlot.TryGetValue(localKey, out slot))
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
    }
}
