// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.Symbols;

namespace Microsoft.CodeAnalysis.CodeGen
{
    internal sealed class VariableSlotAllocator
    {
        private readonly SymbolMatcher symbolMap;
        private readonly Func<SyntaxNode, SyntaxNode> syntaxMap;
        private readonly IReadOnlyDictionary<SyntaxNode, int> previousDeclaratorToOffset;
        private readonly IReadOnlyDictionary<EncLocalInfo, int> previousLocalInfoToSlot;
        private readonly ImmutableArray<EncLocalInfo> previousLocals;

        public VariableSlotAllocator(
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

        public ImmutableArray<EncLocalInfo> PreviousLocals
        {
            get { return previousLocals; }
        }

        public int GetPreviousLocalSlot(ILocalSymbol local, Cci.ITypeReference type, LocalSlotConstraints constraints, CommonSynthesizedLocalKind synthesizedKind)
        {
            var syntaxRefs = local.DeclaringSyntaxReferences;
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
                                return slot;
                            }
                        }
                    }
                }
            }

            return -1;
        }
    }
}
