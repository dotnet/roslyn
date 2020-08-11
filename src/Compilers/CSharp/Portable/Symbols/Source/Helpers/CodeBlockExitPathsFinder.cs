using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Symbols.Source.Helpers
{
    internal sealed class CodeBlockExitPathsFinder : BoundTreeWalker
    {
        internal static readonly TypeSymbol NoReturnExpression = new UnsupportedMetadataTypeSymbol();

        private readonly ArrayBuilder<(BoundNode, TypeWithAnnotations)> _builder;

        private CodeBlockExitPathsFinder(ArrayBuilder<(BoundNode, TypeWithAnnotations)> builder)
        {
            _builder = builder;
        }

        public static void GetExitPaths(ArrayBuilder<(BoundNode, TypeWithAnnotations)> builder, BoundNode node)
        {
            var visitor = new CodeBlockExitPathsFinder(builder);
            visitor.Visit(node);
        }

        public override BoundNode Visit(BoundNode node)
        {
            if (!(node is BoundExpression))
            {
                return base.Visit(node);
            }

            return null;
        }

        protected override BoundExpression VisitExpressionWithoutStackGuard(BoundExpression node)
        {
            throw ExceptionUtilities.Unreachable;
        }

        public override BoundNode VisitLocalFunctionStatement(BoundLocalFunctionStatement node)
        {
            // Do not recurse into local functions; we don't want their returns.
            return null;
        }

        public override BoundNode VisitReturnStatement(BoundReturnStatement node)
        {
            var expression = node.ExpressionOpt;
            var type = (expression is null) ?
                NoReturnExpression :
                expression.Type?.SetUnknownNullabilityForReferenceTypes();
            _builder.Add((node, TypeWithAnnotations.Create(type)));
            return null;
        }
    }
}
