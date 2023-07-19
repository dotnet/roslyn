// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp
{
    /// <summary>
    /// Allows asking semantic questions about any node in a SyntaxTree within a Compilation.
    /// </summary>
    internal partial class SyntaxTreeSemanticModel
    {
        private RegionAnalysisContext RegionAnalysisContext(ExpressionSyntax expression)
        {
            while (expression.Kind() == SyntaxKind.ParenthesizedExpression)
                expression = ((ParenthesizedExpressionSyntax)expression).Expression;

            return RegionAnalysisContext((CSharpSyntaxNode)expression);
        }

        private RegionAnalysisContext RegionAnalysisContext(CSharpSyntaxNode expression)
        {
            var memberModel = GetMemberModel(expression);
            if (memberModel == null)
            {
                // Recover from error cases
                var node = new BoundBadStatement(expression, ImmutableArray<BoundNode>.Empty, hasErrors: true);
                return new RegionAnalysisContext(Compilation, null, node, node, node);
            }

            Symbol member;
            BoundNode boundNode = GetBoundRoot(memberModel, out member);
            var first = memberModel.GetUpperBoundNode(expression, promoteToBindable: true);
            var last = first;
            return new RegionAnalysisContext(this.Compilation, member, boundNode, first, last);
        }

        private RegionAnalysisContext RegionAnalysisContext(StatementSyntax firstStatement, StatementSyntax lastStatement)
        {
            var memberModel = GetMemberModel(firstStatement);
            if (memberModel == null)
            {
                // Recover from error cases
                var node = new BoundBadStatement(firstStatement, ImmutableArray<BoundNode>.Empty, hasErrors: true);
                return new RegionAnalysisContext(Compilation, null, node, node, node);
            }

            Symbol member;
            BoundNode boundNode = GetBoundRoot(memberModel, out member);
            var first = memberModel.GetUpperBoundNode(firstStatement, promoteToBindable: true);
            var last = memberModel.GetUpperBoundNode(lastStatement, promoteToBindable: true);
            return new RegionAnalysisContext(Compilation, member, boundNode, first, last);
        }
    }
}
