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
        private readonly SymbolMatcher symbolMap;
        private readonly Func<SyntaxNode, SyntaxNode> syntaxMap;
        private readonly IReadOnlyDictionary<SyntaxNode, int> previousDeclaratorToOffset;
        private readonly IReadOnlyDictionary<EncLocalInfo, int> previousLocalInfoToSlot;
        private readonly ImmutableArray<EncLocalInfo> previousLocals;

        public EncVariableSlotAllocator(
            SymbolMatcher symbolMap,
            Func<SyntaxNode, SyntaxNode> syntaxMap,
            ImmutableArray<SyntaxNode> previousDeclarators,
            ImmutableArray<EncLocalInfo> previousLocals)
        {
            this.symbolMap = symbolMap;
            this.syntaxMap = syntaxMap;
            this.previousLocals = previousLocals;

            // Create a map from declarator to declarator offset.
            var previousDeclaratorToOffset = new Dictionary<SyntaxNode, int>();
            for (int offset = 0; offset < previousDeclarators.Length; offset++)
            {
                previousDeclaratorToOffset.Add(previousDeclarators[offset], offset);
            }

            this.previousDeclaratorToOffset = previousDeclaratorToOffset;

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
            Cci.ITypeReference type,
            ILocalSymbol symbol,
            string nameOpt,
            CommonSynthesizedLocalKind synthesizedKind,
            uint pdbAttributes,
            LocalSlotConstraints constraints,
            bool isDynamic,
            ImmutableArray<TypedConstant> dynamicTransformFlags)
        {
            var syntaxRefs = symbol.DeclaringSyntaxReferences;
            Debug.Assert(!syntaxRefs.IsDefault);

            if (!syntaxRefs.IsDefaultOrEmpty)
            {
                var currentSyntax = syntaxRefs[0].GetSyntax();
                var previousSyntax = syntaxMap(currentSyntax);
                if (previousSyntax != null)
                {
                    int offset;
                    if (previousDeclaratorToOffset.TryGetValue(previousSyntax, out offset))
                    {
                        var previousType = symbolMap.MapReference(type);
                        if (previousType != null)
                        {
                            var localKey = new EncLocalInfo(offset, previousType, constraints, synthesizedKind, signature: null);
                            int slot;
                            // Should report a warning if the type of the local has changed
                            // and the previous value will be dropped. (Bug #781309.)
                            if (previousLocalInfoToSlot.TryGetValue(localKey, out slot))
                            {
                                return new LocalDefinition(
                                    symbol,
                                    nameOpt,
                                    type,
                                    slot,
                                    synthesizedKind,
                                    pdbAttributes,
                                    constraints,
                                    isDynamic,
                                    dynamicTransformFlags);
                            }
                        }
                    }
                }
            }

            return null;
        }
    }
}
