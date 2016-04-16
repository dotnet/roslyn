// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp
{
    /// <summary>
    /// The dynamic operation factories below return this struct so that the caller
    /// have the option of separating the call-site initialization from its invocation.
    /// 
    /// Most callers just call <see cref="ToExpression"/> to get the combo but some (object and array initializers) 
    /// hoist all call-site initialization code and emit multiple invocations of the same site.
    /// </summary>
    internal struct LoweredDynamicOperation
    {
        private readonly SyntheticBoundNodeFactory _factory;
        private readonly TypeSymbol _resultType;
        public readonly BoundExpression SiteInitialization;
        public readonly BoundExpression SiteInvocation;

        public LoweredDynamicOperation(SyntheticBoundNodeFactory factory, BoundExpression siteInitialization, BoundExpression siteInvocation, TypeSymbol resultType)
        {
            _factory = factory;
            _resultType = resultType;
            this.SiteInitialization = siteInitialization;
            this.SiteInvocation = siteInvocation;
        }

        public static LoweredDynamicOperation Bad(
            BoundExpression loweredReceiver,
            ImmutableArray<BoundExpression> loweredArguments,
            BoundExpression loweredRight,
            TypeSymbol resultType)
        {
            var children = ArrayBuilder<BoundNode>.GetInstance();
            children.AddOptional(loweredReceiver);
            children.AddRange(loweredArguments);
            children.AddOptional(loweredRight);

            return LoweredDynamicOperation.Bad(resultType, children.ToImmutableAndFree());
        }

        public static LoweredDynamicOperation Bad(TypeSymbol resultType, ImmutableArray<BoundNode> children)
        {
            Debug.Assert(children.Length > 0);
            var bad = new BoundBadExpression(children[0].Syntax, LookupResultKind.Empty, ImmutableArray<Symbol>.Empty, children, resultType);
            return new LoweredDynamicOperation(null, null, bad, resultType);
        }

        public BoundExpression ToExpression()
        {
            if (_factory == null)
            {
                Debug.Assert(SiteInitialization == null && SiteInvocation is BoundBadExpression);
                return SiteInvocation;
            }

            // TODO (tomat): we might be able to using SiteInvocation.Type instead of resultType once we stop using GetLoweredType
            return _factory.Sequence(new[] { SiteInitialization }, SiteInvocation, _resultType);
        }
    }
}
