// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using System;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public class ChildSyntaxListTests : CSharpTestBase
    {
        [Fact]
        public void Equality()
        {
            var node1 = SyntaxFactory.ReturnStatement();
            var node2 = SyntaxFactory.ReturnStatement();

            EqualityTesting.AssertEqual(default(ChildSyntaxList), default(ChildSyntaxList));
            EqualityTesting.AssertEqual(new ChildSyntaxList(node1), new ChildSyntaxList(node1));
        }

        [Fact]
        public void Reverse_Equality()
        {
            var node1 = SyntaxFactory.ReturnStatement();
            var node2 = SyntaxFactory.ReturnStatement();

            EqualityTesting.AssertEqual(default(ChildSyntaxList.Reversed), default(ChildSyntaxList.Reversed));
            EqualityTesting.AssertEqual(new ChildSyntaxList(node1).Reverse(), new ChildSyntaxList(node1).Reverse());
        }
    }
}
