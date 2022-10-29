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

        #region CSharpSyntaxVisitor<TArgument>

        [Fact]
        public void VisitDoesNotThrowOnNullNode_TArgument()
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

        private class DefaultVisitor : InternalSyntax.CSharpSyntaxVisitor
        {

        }

        private class DefaultVisitor<TArgument> : InternalSyntax.CSharpSyntaxVisitor<TArgument>
        {

        }

        private class DefaultVisitor<TArgument, TResult> : InternalSyntax.CSharpSyntaxVisitor<TArgument, TResult>
        {

        }

        #endregion
    }
}
