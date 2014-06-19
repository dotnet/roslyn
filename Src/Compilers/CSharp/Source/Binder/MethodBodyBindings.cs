using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading;

namespace Roslyn.Compilers.CSharp
{
    internal abstract class MemberBindings
    {
        internal abstract Symbol Symbol { get; }
        internal abstract TypeSymbol GetExpressionType(ExpressionSyntax syntax);
    }

    internal class MethodBodyBindings : MemberBindings
    {
        private SourceMethodSymbol methodSymbol;
        private BodyInfo bodyInfo;

        internal MethodBodyBindings(SourceMethodSymbol methodSymbol)
        {
            this.methodSymbol = methodSymbol;
        }

        internal override Symbol Symbol
        {
            get { return this.methodSymbol; }
        }

        public SourceMethodSymbol MethodSymbol
        {
            get { return this.methodSymbol; }
        }

        class BodyInfo
        {
            internal readonly BoundBlock BoundBody;
            internal readonly Dictionary<SyntaxNode, BoundNode> NodeMap;
            internal readonly DiagnosticBag Diagnostics;

            internal BodyInfo(BoundBlock boundBody, Dictionary<SyntaxNode, BoundNode> nodeMap, DiagnosticBag diagnostics)
            {
                this.BoundBody = boundBody;
                this.NodeMap = nodeMap;
                this.Diagnostics = diagnostics;
            }
        }

        private BodyInfo BoundInfo
        {
            get
            {
                if (this.bodyInfo == null)
                {
                    var diagnostics = new DiagnosticBag();
                    var nodeMap = new Dictionary<SyntaxNode, BoundNode>();
                    var binder = new SyntaxBinder(this, diagnostics, nodeMap);
                    var body = (BoundBlock)binder.BindStatement(this.methodSymbol.BlockSyntax);
                    var info = new BodyInfo(body, nodeMap, diagnostics);
                    Interlocked.CompareExchange(ref this.bodyInfo, info, null);
                }
                return this.bodyInfo;
            }
        }

        private ImmutableMap<SyntaxNode, BlockBaseBinderContext> blockMap;
        private ImmutableMap<SyntaxNode, BlockBaseBinderContext> MakeBlockMap()
        {
            // TODO: handle partial methods
            var binder = this.methodSymbol.BinderContexts.Single();
            return LocalBinderBuilder.Build(this.methodSymbol);
        }

        public ImmutableMap<SyntaxNode, BlockBaseBinderContext> BlockMap
        {
            get
            {
                if (this.blockMap == null)
                {
                    Interlocked.CompareExchange(ref this.blockMap, MakeBlockMap(), null);
                }
                return this.blockMap;
            }
        }

        private List<LocalSymbol> locals;
        public IList<LocalSymbol> Locals
        {
            get
            {
                if (this.locals == null)
                {
                    Interlocked.CompareExchange(ref this.locals,
                        (from blockContext in this.BlockMap.Values
                         from loc in blockContext.Locals
                         select loc).ToList(),
                         null);
                }

                return this.locals;
            }
        }

        internal override TypeSymbol GetExpressionType(ExpressionSyntax expression)
        {
            BoundNode node;
            if (this.BoundInfo.NodeMap.TryGetValue(expression, out node))
            {
                var expr = node as BoundExpression;
                if (expr != null)
                {
                    return expr.GetExpressionType();
                }
            }
            return null;
        }

        private sealed class LocalBinderBuilder : SyntaxVisitor<BinderContext, int>
        {
            private ImmutableMap<SyntaxNode, BlockBaseBinderContext> map;
            private SourceMethodSymbol method;
            public static ImmutableMap<SyntaxNode, BlockBaseBinderContext> Build(SourceMethodSymbol method)
            {
                // UNDONE: partial methods
                var builder = new LocalBinderBuilder(method);
                builder.Visit(
                    method.BlockSyntax,
                    method.ParameterBinderContext);
                return builder.map;
            }

            private LocalBinderBuilder(SourceMethodSymbol method)
            {
                this.map = ImmutableMap<SyntaxNode, BlockBaseBinderContext>.Empty;
                this.method = method;
            }

            public override int VisitBlock(BlockSyntax node, BinderContext enclosing)
            {
                var context = new BlockBinderContext(this.method, enclosing, node);
                this.map = this.map.Add(node, context);
                return base.VisitBlock(node, context);
            }

            public override int VisitUsingStatement(UsingStatementSyntax node, BinderContext enclosing)
            {
                var context = new UsingStatementBinderContext(this.method, enclosing, node);
                this.map = this.map.Add(node, context);
                return base.VisitUsingStatement(node, context);
            }

            public override int VisitWhileStatement(WhileStatementSyntax node, BinderContext enclosing)
            {
                var context = new WhileBinderContext(this.method, enclosing, node);
                this.map = this.map.Add(node, context);
                return base.VisitWhileStatement(node, context);
            }

            public override int VisitDoStatement(DoStatementSyntax node, BinderContext enclosing)
            {
                var context = new WhileBinderContext(this.method, enclosing, node);
                this.map = this.map.Add(node, context);
                return base.VisitDoStatement(node, context);
            }

            protected override int DefaultVisit(SyntaxNode node, BinderContext arg)
            {
                foreach (var child in node.Children)
                {
                    //we are not interested in terminals
                    if (child.IsNode)
                    {
                        this.Visit(child.AsNode(), arg);
                    }
                }
                return 0;
            }

            // UNDONE: What do we need to do for checked, unchecked, unsafe, label, catch, lock, for, foreach, fixed, try?
        }
    }
}