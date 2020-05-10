// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CSharp.SourceGeneration;
using Roslyn.Test.Utilities;
using Xunit;
using static Microsoft.CodeAnalysis.SourceGeneration.CodeGenerator;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.SourceGeneration
{
    public partial class CodeGenerationTests
    {
        [Fact]
        public void TestEmptyThrowStatement1()
        {
            AssertEx.AreEqual(
@"throw;",
Throw().GenerateStatementString());
        }

        [Fact]
        public void TestThrowStatement1()
        {
            AssertEx.AreEqual(
@"throw null;",
Throw(Literal(null)).GenerateStatementString());
        }

        [Fact]
        public void TestThrowExpression()
        {
            AssertEx.AreEqual(
@"throw null",
Throw(Literal(null)).GenerateExpressionString());
        }

        [Fact]
        public void TestEmptyThrowStatementInBlock1()
        {
            AssertEx.AreEqual(
@"{
    throw;
}",
Block(Throw()).GenerateString());
        }

        [Fact]
        public void TestThrowStatementInBlock1()
        {
            AssertEx.AreEqual(
@"{
    throw null;
}",
Block(Throw(Literal(null))).GenerateString());
        }

        [Fact]
        public void TestThrowExpressionInExpr()
        {
            AssertEx.AreEqual(
@"(throw null)",
Parenthesized(Throw(Literal(null))).GenerateString());
        }
    }
}
