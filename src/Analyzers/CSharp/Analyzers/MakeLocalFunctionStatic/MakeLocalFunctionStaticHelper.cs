﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.CSharp.MakeLocalFunctionStatic
{
    internal static class MakeLocalFunctionStaticHelper
    {
        public static bool IsStaticLocalFunctionSupported(SyntaxTree tree)
            => tree.Options is CSharpParseOptions csharpOption && csharpOption.LanguageVersion >= LanguageVersion.CSharp8;

        private static bool TryGetDataFlowAnalysis(LocalFunctionStatementSyntax localFunction, SemanticModel semanticModel, [NotNullWhen(returnValue: true)] out DataFlowAnalysis? dataFlow)
        {
            dataFlow = semanticModel.AnalyzeDataFlow(localFunction);
            return dataFlow is { Succeeded: true };
        }

        private static bool CanBeCalledFromStaticContext(LocalFunctionStatementSyntax localFunction, DataFlowAnalysis dataFlow)
        {
            // If other local functions are called the it can't be made static unles the
            // are static, or the local function is recursive, or its calling a child local function
            return !dataFlow.UsedLocalFunctions.Any(lf => !lf.IsStatic && !IsChildOrSelf(localFunction, lf));

            static bool IsChildOrSelf(LocalFunctionStatementSyntax containingLocalFunction, ISymbol calledLocationFunction)
            {
                var node = calledLocationFunction.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax();
                // Contains also returns true if node is equal to the containing local function
                return containingLocalFunction.Contains(node);
            }
        }

        private static bool HasCapturesThatArentThis(ImmutableArray<ISymbol> captures)
            => !captures.IsEmpty && !captures.Any(s => s.IsThisParameter());

        public static bool CanMakeLocalFunctionStaticBecauseNoCaptures(LocalFunctionStatementSyntax localFunction, SemanticModel semanticModel)
            => TryGetDataFlowAnalysis(localFunction, semanticModel, out var dataFLow)
            && CanBeCalledFromStaticContext(localFunction, dataFLow)
            && dataFLow.CapturedInside.IsEmpty;

        public static bool CanMakeLocalFunctionStaticByRefactoringCaptures(LocalFunctionStatementSyntax localFunction, SemanticModel semanticModel, out ImmutableArray<ISymbol> captures)
        {
            if (TryGetDataFlowAnalysis(localFunction, semanticModel, out var dataFLow) &&
                CanBeCalledFromStaticContext(localFunction, dataFLow) &&
                HasCapturesThatArentThis(dataFLow.CapturedInside))
            {
                captures = dataFLow.CapturedInside;
                return true;
            }

            captures = default;
            return false;
        }
    }
}
