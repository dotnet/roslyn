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
            // Pretend address-of is a pure read (i.e. no write) - would we see an 
            // unassigned diagnostic?
            VisitRvalue(node.Operand);
            return null;
        }
    }
}
