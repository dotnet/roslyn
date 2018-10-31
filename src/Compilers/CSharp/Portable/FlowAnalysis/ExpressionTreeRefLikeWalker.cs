using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.CodeAnalysis.CSharp.FlowAnalysis
{
    internal class ExpressionTreeRefLikeWalker : BoundTreeWalker
    {
        private readonly CSharpCompilation _compilation;
        private readonly Symbol _member;
        private readonly BoundBlock _block;
        private readonly DiagnosticBag _diagnostics;
        private bool _expressionTreeContainsRefLikeExpressions = false;

        private ExpressionTreeRefLikeWalker(CSharpCompilation compilation, Symbol member, BoundBlock block, DiagnosticBag diagnostics)
        {
            _compilation = compilation;
            _member = member;
            _block = block;
            _diagnostics = diagnostics;
        }

        protected override BoundExpression VisitExpressionWithoutStackGuard(BoundExpression node)
        {
            return (BoundExpression)base.Visit(node);
        }

        public override BoundNode Visit(BoundNode node)
        {
            if (node is BoundExpression expr && expr.Type.IsByRefLikeType)
            {
                _expressionTreeContainsRefLikeExpressions = true;
                _diagnostics.Add(ErrorCode.ERR_FeatureNotValidInExpressionTree, node.Syntax.Location, "ref struct");
            }
            return base.Visit(node);
        }

        public static bool Analyze(CSharpCompilation compilation, Symbol member, BoundBlock block, DiagnosticBag diagnostics)
        {
            var walker = new ExpressionTreeRefLikeWalker(compilation, member, block, diagnostics);
            walker.Visit(block);
            return walker._expressionTreeContainsRefLikeExpressions;
        }
    }
}
