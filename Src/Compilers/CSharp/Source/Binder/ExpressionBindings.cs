using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading;

namespace Roslyn.Compilers.CSharp
{
    public class ExpressionBinding : SyntaxBinding
    {
        private BoundExpression boundExpression;
        private Dictionary<SyntaxNode, BoundNode> nodeMap;
        private DiagnosticBag diagnostics;

        internal ExpressionBinding(BoundExpression boundExpression, Dictionary<SyntaxNode, BoundNode> nodeMap, DiagnosticBag diagnostics)
        {
            this.boundExpression = boundExpression;
            this.nodeMap = nodeMap;
            this.diagnostics = diagnostics;
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