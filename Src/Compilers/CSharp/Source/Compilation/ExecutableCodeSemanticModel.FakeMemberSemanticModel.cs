using System;
using System.Threading;
using Roslyn.Utilities;

namespace Roslyn.Compilers.CSharp
{
    partial class ExecutableCodeSemanticModel
    {
        protected class FakeMemberSemanticModel : MemberSemanticModel
        {
            private readonly Binder rootBinder;
            private readonly ImmutableMap<SyntaxNode, Binder> map;

            internal FakeMemberSemanticModel(Compilation compilation, SyntaxNode root, SourceMethodSymbol method, Binder rootBinder, ImmutableMap<SyntaxNode, Binder> map)
                : base(compilation, root, method)
            {
                this.rootBinder = rootBinder;
                this.map = map;
            }

            internal override Binder RootBinder
            {
                get { return this.rootBinder; }
            }

            internal override Binder GetBinder(SyntaxNode node)
            {
                Binder binder;
                this.map.TryGetValue(node, out binder);
                return binder;
            }

            internal override BoundNode GetBoundNode(SyntaxNode node)
            {
                throw new NotSupportedException();
            }

            public override SemanticInfo GetSemanticInfo(ExpressionSyntax expression, CancellationToken cancellationToken = default(CancellationToken))
            {
                throw new NotSupportedException();
            }

            public override SemanticInfo GetSemanticInfoInParent(ExpressionSyntax expression)
            {
                throw new NotSupportedException();
            }

            public override SemanticModel GetSpeculativeSemanticModel(int position, ExpressionSyntax expression)
            {
                throw new NotSupportedException();
            }

            public override SemanticModel GetSpeculativeSemanticModel(int position, StatementSyntax statement)
            {
                throw new NotSupportedException();
            }
        }
    }
}