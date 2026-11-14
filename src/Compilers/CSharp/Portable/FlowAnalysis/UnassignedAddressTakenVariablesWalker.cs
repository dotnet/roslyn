// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.CodeAnalysis.CSharp
{
    /// <summary>
    /// An analysis that computes all cases where the address is taken of a variable that has not yet been assigned
    /// </summary>
    internal class UnassignedAddressTakenVariablesWalker : DefiniteAssignmentPass
    {
        private UnassignedAddressTakenVariablesWalker(CSharpCompilation compilation, Symbol member, BoundNode node)
            : base(compilation, member, node, strictAnalysis: true)
        {
        }

        internal static HashSet<PrefixUnaryExpressionSyntax> Analyze(CSharpCompilation compilation, Symbol member, BoundNode node)
        {
            var walker = new UnassignedAddressTakenVariablesWalker(compilation, member, node);
            try
            {
                bool badRegion = false;
                var result = walker.Analyze(ref badRegion);
                Debug.Assert(!badRegion);
                return result;
            }
            finally
            {
                walker.Free();
            }
        }

        private readonly HashSet<PrefixUnaryExpressionSyntax> _result = new HashSet<PrefixUnaryExpressionSyntax>();

        private HashSet<PrefixUnaryExpressionSyntax> Analyze(ref bool badRegion)
        {
            // It might seem necessary to clear this.result after each Scan performed by base.Analyze, however,
            // finding new execution paths (via new backwards branches) can only make variables "less" definitely
            // assigned, not more.  That is, if there was formerly a path on which the variable was not definitely
            // assigned, then adding another path will not make the variable definitely assigned.  Therefore,
            // subsequent scans can only re-add elements to this.result, and the HashSet will naturally de-dup.
            base.Analyze(ref badRegion, null);
            return _result;
        }

        protected override void ReportUnassigned(Symbol symbol, SyntaxNode node, int slot, bool skipIfUseBeforeDeclaration)
        {
            if (node.Parent.Kind() == SyntaxKind.AddressOfExpression)
            {
                _result.Add((PrefixUnaryExpressionSyntax)node.Parent);
            }
        }

        public override BoundNode VisitAddressOfOperator(BoundAddressOfOperator node)
        {
            // Inline array reads report unassigned receivers using the receiver's syntax (e.g., `b` in `&b[0]`),
            // unlike field reads which use the field's syntax (e.g., `s.F` in `&s.F`),
            // due to the difference between VisitInlineArrayAccess and VisitFieldAccess,
            // even though both scenarios should be equivalent for the purposes of this analysis.
            // So here we pass the whole address operand's syntax to CheckAssigned (e.g., `b[0]`),
            // so that ReportUnassigned sees the enclosing address-of expression correctly.
            if (node.Syntax is PrefixUnaryExpressionSyntax syntax &&
                node.Operand is BoundInlineArrayAccess
                {
                    GetItemOrSliceHelper: WellKnownMember.System_Span_T__get_Item or WellKnownMember.System_ReadOnlySpan_T__get_Item
                })
            {
                CheckAssigned(node.Operand, syntax.Operand);
            }

            // Pretend address-of is a pure read (i.e. no write) - would we see an 
            // unassigned diagnostic?
            VisitRvalue(node.Operand);
            return null;
        }
    }
}
