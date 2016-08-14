// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.Cci;
using Microsoft.CodeAnalysis.CodeGen;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.Symbols;

namespace Microsoft.CodeAnalysis.CSharp.Emit
{
    internal class CSharpEncVariableSlotAllocator : EncVariableSlotAllocator
    {
        public CSharpEncVariableSlotAllocator(
            SymbolMatcher symbolMap,
            Func<SyntaxNode, SyntaxNode> syntaxMapOpt,
            IMethodSymbolInternal previousTopLevelMethod,
            DebugId methodId,
            ImmutableArray<EncLocalInfo> previousLocals,
            IReadOnlyDictionary<int, KeyValuePair<DebugId, int>> lambdaMapOpt,
            IReadOnlyDictionary<int, DebugId> closureMapOpt,
            string stateMachineTypeNameOpt,
            int hoistedLocalSlotCount,
            IReadOnlyDictionary<EncHoistedLocalInfo, int>
            hoistedLocalSlotsOpt, int awaiterCount, IReadOnlyDictionary<ITypeReference, int> awaiterMapOpt) 
            : base(symbolMap, syntaxMapOpt, previousTopLevelMethod, methodId,
                   previousLocals, lambdaMapOpt, closureMapOpt, stateMachineTypeNameOpt,
                   hoistedLocalSlotCount, hoistedLocalSlotsOpt, awaiterCount, awaiterMapOpt)
        {
        }

        protected override SyntaxNode GetLambda(SyntaxNode lambdaOrLambdaBodySyntax)
        {
            return LambdaUtilities.GetLambda(lambdaOrLambdaBodySyntax);
        }

        protected override SyntaxNode TryGetCorrespondingLambdaBody(
            SyntaxNode previousLambdaSyntax,
            SyntaxNode lambdaOrLambdaBodySyntax)
        {
            return LambdaUtilities.TryGetCorrespondingLambdaBody(
                lambdaOrLambdaBodySyntax, previousLambdaSyntax);
        }
    }
}