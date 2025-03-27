// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal abstract class AbstractRegionDataFlowPass : DefiniteAssignmentPass
    {
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
            MakeSlots(MethodParameters);
            if ((object)MethodThisParameter != null) GetOrCreateSlot(MethodThisParameter);
            var result = base.Scan(ref badRegion);
            return result;
        }

        public override BoundNode VisitLambda(BoundLambda node)
        {
            MakeSlots(node.Symbol.Parameters);
            return base.VisitLambda(node);
        }

        public override BoundNode VisitLocalFunctionStatement(BoundLocalFunctionStatement node)
        {
            MakeSlots(node.Symbol.Parameters);
            return base.VisitLocalFunctionStatement(node);
        }

        public override BoundNode VisitNameOfOperator(BoundNameOfOperator node)
        {
            return node;
        }

        private void MakeSlots(ImmutableArray<ParameterSymbol> parameters)
        {
            // assign slots to the parameters
            foreach (var parameter in parameters)
            {
                GetOrCreateSlot(parameter);
            }
        }

        protected override void AfterVisitInlineArrayAccess(BoundInlineArrayAccess node)
        {
        }

        protected override void AfterVisitConversion(BoundConversion node)
        {
        }
    }
}
