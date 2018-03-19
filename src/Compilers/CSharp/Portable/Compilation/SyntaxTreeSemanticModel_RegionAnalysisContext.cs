// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
    internal partial class SyntaxTreeSemanticModel : CSharpSemanticModel
    {
        private RegionAnalysisContext RegionAnalysisContext(ExpressionSyntax expression)
        {
            while (expression.Kind() == SyntaxKind.ParenthesizedExpression)
                expression = ((ParenthesizedExpressionSyntax)expression).Expression;

            MemberSemanticModel memberModel = GetMemberModel(expression);
            if (memberModel == null)
            {
                // Recover from error cases
                var node = new BoundBadStatement(expression, ImmutableArray<BoundNode>.Empty, hasErrors: true);
                return new RegionAnalysisContext(Compilation, null, node, node, node);
            }

            Symbol member;
            BoundNode boundNode = GetBoundRoot(memberModel, out member);
            BoundNode first = memberModel.GetUpperBoundNode(expression, promoteToBindable: true);
            BoundNode last = first;
            return new RegionAnalysisContext(this.Compilation, member, boundNode, first, last);
        }

        private RegionAnalysisContext RegionAnalysisContext(StatementSyntax firstStatement, StatementSyntax lastStatement)
        {
            MemberSemanticModel memberModel = GetMemberModel(firstStatement);
            if (memberModel == null)
            {
                // Recover from error cases
                var node = new BoundBadStatement(firstStatement, ImmutableArray<BoundNode>.Empty, hasErrors: true);
                return new RegionAnalysisContext(Compilation, null, node, node, node);
            }

            Symbol member;
            BoundNode boundNode = GetBoundRoot(memberModel, out member);
            BoundNode first = memberModel.GetUpperBoundNode(firstStatement, promoteToBindable: true);
            BoundNode last = memberModel.GetUpperBoundNode(lastStatement, promoteToBindable: true);
            return new RegionAnalysisContext(Compilation, member, boundNode, first, last);
        }
    }
}
