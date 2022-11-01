// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;
using InternalSyntax = Microsoft.CodeAnalysis.CSharp.Syntax.InternalSyntax;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public partial class GreenVisitorTests
    {
        #region CSharpSyntaxVisitor

        [Fact]
        public void VisitDoesNotThrowOnNullNode()
        {
            var visitor = new DefaultVisitor();
            visitor.Visit(null);
        }

        #endregion

        #region CSharpSyntaxVisitor<TResult>

        [Fact]
        public void VisitDoesNotThrowOnNullNode_TResult()
        {
            var visitor = new DefaultVisitor<object?>();
            visitor.Visit(null);
        }

        #endregion

        #region CSharpSyntaxVisitor<TArgument, TResult>

        [Fact]
        public void VisitDoesNotThrowOnNullNode_TArgument_TResult()
        {
            var visitor = new DefaultVisitor<object?, object?>();
            visitor.Visit(null, null);
        }

        #endregion

        #region Misc

        internal class DefaultVisitor : InternalSyntax.CSharpSyntaxVisitor
        {
            public bool DefaultVisitWasCalled { get; private set; }

            public override void DefaultVisit(InternalSyntax.CSharpSyntaxNode node)
            {
                DefaultVisitWasCalled = true;
            }
        }

        internal class DefaultVisitor<TResult> : InternalSyntax.CSharpSyntaxVisitor<TResult>
        {
            public bool DefaultVisitWasCalled { get; private set; }

            private readonly TResult _returnValue;

            public DefaultVisitor(TResult returnValue = default)
            {
                _returnValue = returnValue;
            }

            protected override TResult DefaultVisit(InternalSyntax.CSharpSyntaxNode node)
            {
                DefaultVisitWasCalled = true;
                return _returnValue;
            }
        }

        internal class DefaultVisitor<TArgument, TResult> : InternalSyntax.CSharpSyntaxVisitor<TArgument, TResult>
        {
            public bool DefaultVisitWasCalled { get; private set; }

            private readonly TResult _returnValue;

            public DefaultVisitor(TResult returnValue = default)
            {
                _returnValue = returnValue;
            }

            protected override TResult DefaultVisit(InternalSyntax.CSharpSyntaxNode node, TArgument argument)
            {
                DefaultVisitWasCalled = true;
                return _returnValue;
            }
        }

        #endregion
    }
}
