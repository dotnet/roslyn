// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.CodeAnalysis.CSharp
{
    // Note: this code has a copy-and-paste sibling in AbstractRegionDataFlowPass.
    // Any fix to one should be applied to the other.
    internal class AbstractRegionDataFlowPass : DataFlowPass
    {
        private bool _interactsWithLocalFunction = false;

        internal AbstractRegionDataFlowPass(
            CSharpCompilation compilation,
            Symbol member,
            BoundNode node,
            BoundNode firstInRegion,
            BoundNode lastInRegion,
            HashSet<Symbol> initiallyAssignedVariables = null,
            HashSet<PrefixUnaryExpressionSyntax> unassignedVariableAddressOfSyntaxes = null,
            bool trackUnassignments = false)
            : base(compilation, member, node, firstInRegion, lastInRegion, initiallyAssignedVariables, unassignedVariableAddressOfSyntaxes, trackUnassignments)
        {
        }

        /// <summary>
        /// To scan the whole body, we start outside (before) the region.
        /// </summary>
        protected override ImmutableArray<PendingBranch> Scan(ref bool badRegion)
        {
            SetState(ReachableState());
            MakeSlots(MethodParameters);
            if ((object)MethodThisParameter != null) GetOrCreateSlot(MethodThisParameter);
            var result = base.Scan(ref badRegion);

            // Local functions currently don't implement data-flows-out properly
            // so we currently fail the analysis if we see a local function.
            // See https://github.com/dotnet/roslyn/issues/14214
            _interactsWithLocalFunction |= RegionInsideLocalFunction();
            badRegion = badRegion || _interactsWithLocalFunction;

            return result;
        }

        public override BoundNode VisitLambda(BoundLambda node)
        {
            MakeSlots(node.Symbol.Parameters);
            return base.VisitLambda(node);
        }

        public override BoundNode VisitLocalFunctionStatement(BoundLocalFunctionStatement node)
        {
            if (IsInside)
            {
                _interactsWithLocalFunction = true;
            }
            MakeSlots(node.Symbol.Parameters);
            return base.VisitLocalFunctionStatement(node);
        }

        public override BoundNode VisitConversion(BoundConversion node)
        {
            if (IsInside &&
                node.ConversionKind == ConversionKind.MethodGroup &&
                node.SymbolOpt?.MethodKind == MethodKind.LocalFunction)
            {
                _interactsWithLocalFunction = true;
            }
            return base.VisitConversion(node);
        }

        public override BoundNode VisitCall(BoundCall node)
        {
            if (IsInside && node.Method.MethodKind == MethodKind.LocalFunction)
            {
                _interactsWithLocalFunction = true;
            }
            return base.VisitCall(node);
        }

        public override BoundNode VisitDelegateCreationExpression(BoundDelegateCreationExpression node)
        {
            if (IsInside && node.MethodOpt?.MethodKind == MethodKind.LocalFunction)
            {
                _interactsWithLocalFunction = true;
            }
            return base.VisitDelegateCreationExpression(node);
        }

        public override BoundNode VisitMethodGroup(BoundMethodGroup node)
        {
            if (IsInside &&
                node.Methods.Length == 1 &&
                node.Methods[0].MethodKind == MethodKind.LocalFunction)
            {
                _interactsWithLocalFunction = true;
            }
            return base.VisitMethodGroup(node);
        }

        private bool RegionInsideLocalFunction()
        {
            // Grab the first node and check its ancestors for a local function
            foreach (var ancestor in firstInRegion.Syntax.Ancestors())
            {
                if (ancestor.IsKind(SyntaxKind.LocalFunctionStatement))
                {
                    return true;
                }
            }
            return false;
        }

        private void MakeSlots(ImmutableArray<ParameterSymbol> parameters)
        {
            // assign slots to the parameters
            foreach (var parameter in parameters)
            {
                GetOrCreateSlot(parameter);
            }
        }
    }
}
