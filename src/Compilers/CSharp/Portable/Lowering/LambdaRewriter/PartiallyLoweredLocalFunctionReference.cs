using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.Semantics;

namespace Microsoft.CodeAnalysis.CSharp
{
    /// <summary>
    /// This represents a partially lowered local function reference (e.g.,
    /// a local function call or delegate conversion) with relevant proxies
    /// attached. It will later be rewritten by the
    /// <see cref="LambdaRewriter.LocalFunctionReferenceRewriter"/> into a
    /// proper call.
    /// </summary>
    internal class PartiallyLoweredLocalFunctionReference : BoundExpression
    {
        private const BoundKind s_privateKind = (BoundKind)byte.MaxValue;

        public BoundExpression UnderlyingNode { get; }
        public Dictionary<Symbol, CapturedSymbolReplacement> Proxies { get; }

        public PartiallyLoweredLocalFunctionReference(
            BoundExpression underlying,
            Dictionary<Symbol, CapturedSymbolReplacement> proxies)
            : base(s_privateKind, underlying.Syntax, underlying.Type)
        {
            UnderlyingNode = underlying;
            Proxies = proxies;
        }

        public override BoundNode Accept(BoundTreeVisitor visitor) =>
            visitor.Visit(this);

        protected override OperationKind ExpressionKind
        {
            get
            {
                throw new InvalidOperationException();
            }
        }

        public override void Accept(OperationVisitor visitor)
        {
            throw new InvalidOperationException();
        }

        public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            throw new InvalidOperationException();
        }
    }
}
