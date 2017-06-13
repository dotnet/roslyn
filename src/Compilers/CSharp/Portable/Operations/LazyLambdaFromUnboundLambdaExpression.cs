using System;
using Microsoft.CodeAnalysis.Semantics;

namespace Microsoft.CodeAnalysis.Semantics
{
    internal sealed class LazyLambdaFromUnboundLambdaExpression : Operation, ILambdaExpression
    {
        private readonly Lazy<ILambdaExpression> _internalLambda;

        public LazyLambdaFromUnboundLambdaExpression(
            Lazy<ILambdaExpression> internalLambda,
            bool isInvalid,
            SyntaxNode syntax,
            ITypeSymbol type,
            Optional<object> constantValue) :
            base(OperationKind.LambdaExpression, isInvalid, syntax, type, constantValue)
        {
            _internalLambda = internalLambda;
        }

        public IMethodSymbol Signature => _internalLambda.Value.Signature;

        public IBlockStatement Body => _internalLambda.Value.Body;

        public override void Accept(OperationVisitor visitor)
        {
            visitor.VisitLambdaExpression(this);
        }

        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitLambdaExpression(this, argument);
        }
    }
}
