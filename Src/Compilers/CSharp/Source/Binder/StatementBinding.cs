using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading;

namespace Roslyn.Compilers.CSharp
{
    public class StatementBinding : LocalBinding
    {
        private BoundStatement boundStatement;
        private ImmutableMap<SyntaxNode, BlockBaseBinderContext> blockMap;
        private IList<LocalSymbol> locals;
        private Dictionary<SyntaxNode, BoundNode> nodeMap;
        private DiagnosticBag diagnostics;

        internal StatementBinding(
            BoundStatement boundStatement,
            ImmutableMap<SyntaxNode, BlockBaseBinderContext> blockMap,
            IList<LocalSymbol> locals,
            Dictionary<SyntaxNode, BoundNode> nodeMap,
            DiagnosticBag diagnostics)
        {
            this.boundStatement = boundStatement;
            this.blockMap = blockMap;
            this.locals = locals;
            this.nodeMap = nodeMap;
            this.diagnostics = diagnostics;
        }

        public override IList<LocalSymbol> Locals
        {
            get { return this.locals; }
        }

        internal override ImmutableMap<SyntaxNode, BlockBaseBinderContext> BlockMap
        {
            get { return this.blockMap; }
        }

        public override SymbolInfo LookupType(TypeSyntax type, int arity = 0)
        {
            throw new NotImplementedException();
        }

        public override TypeSymbol GetExpressionType(ExpressionSyntax expression)
        {
            BoundNode boundNode;
            if (this.nodeMap.TryGetValue(expression, out boundNode))
            {
                var boundExpression = boundNode as BoundExpression;
                if (boundExpression != null)
                {
                    return boundExpression.GetExpressionType();
                }
            }
            return null;
        }

        public override Symbol GetExpressionSymbol(ExpressionSyntax expression)
        {
            BoundNode boundNode;
            if (this.nodeMap.TryGetValue(expression, out boundNode))
            {
                var boundExpression = boundNode as BoundExpression;
                if (boundExpression != null)
                {
                    return boundExpression.GetExpressionSymbol();
                }
            }
            return null;
        }

        public bool HasDiagnostics
        {
            get { return !this.diagnostics.IsEmpty; }
        }

        public IEnumerable<IDiagnostic> GetDiagnostics()
        {
            return this.diagnostics;
        }
    }
}