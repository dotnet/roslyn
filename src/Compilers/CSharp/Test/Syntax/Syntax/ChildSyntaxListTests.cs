﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
            ReturnStatementSyntax node1 = SyntaxFactory.ReturnStatement();
            ReturnStatementSyntax node2 = SyntaxFactory.ReturnStatement();

            EqualityTesting.AssertEqual(default(ChildSyntaxList), default(ChildSyntaxList));
            EqualityTesting.AssertEqual(new ChildSyntaxList(node1), new ChildSyntaxList(node1));
        }

        [Fact]
        public void Reverse_Equality()
        {
            ReturnStatementSyntax node1 = SyntaxFactory.ReturnStatement();
            ReturnStatementSyntax node2 = SyntaxFactory.ReturnStatement();

            EqualityTesting.AssertEqual(default(ChildSyntaxList.Reversed), default(ChildSyntaxList.Reversed));
            EqualityTesting.AssertEqual(new ChildSyntaxList(node1).Reverse(), new ChildSyntaxList(node1).Reverse());
        }
    }
}
