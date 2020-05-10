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
        #region statement

        [Fact]
        public void TestConditionalStatement1()
        {
            AssertEx.AreEqual(
@"if (true)
{
}",
Conditional(Literal(true), Block()).GenerateStatementString());
        }

        [Fact]
        public void TestConditionalStatementInBlock1()
        {
            AssertEx.AreEqual(
@"{
    if (true)
    {
    }
}",
Block(operations: ImmutableArray.Create<IOperation>(
    Conditional(Literal(true), Block()))).GenerateString());
        }

        [Fact]
        public void TestConditionalStatement2()
        {
            AssertEx.AreEqual(
@"if (true)
{
}
else
{
    return;
}",
Conditional(Literal(true), Block(), Return()).GenerateStatementString());
        }

        [Fact]
        public void TestNestedConditionalStatement1()
        {
            AssertEx.AreEqual(
@"if (true)
{
    if (false)
    {
    }
}",
Conditional(Literal(true), Conditional(Literal(false), Block())).GenerateStatementString());
        }

        [Fact]
        public void TestNestedConditionalStatement2()
        {
            AssertEx.AreEqual(
@"if (true)
{
    if (false)
    {
    }
}
else
{
    return;
}",
Conditional(
    Literal(true),
    Conditional(Literal(false), Block()),
    Return()).GenerateStatementString());
        }

        [Fact]
        public void TestNestedConditionalStatement3()
        {
            AssertEx.AreEqual(
@"if ((true) ? (0) : (1))
{
}",
Conditional(
    Conditional(Literal(true), Literal(0), Literal(1)),
    Block()).GenerateStatementString());
        }

        #endregion

        #region expression

        [Fact]
        public void TestConditionalExpression1()
        {
            AssertEx.AreEqual(
@"(true) ? (0) : (1)",
Conditional(Literal(true), Literal(0), Literal(1)).GenerateExpressionString());
        }

        [Fact]
        public void TestNestedConditionalExpression1()
        {
            AssertEx.AreEqual(
@"(true) ? ((false) ? (0) : (1)) : (2)",
Conditional(
    Literal(true),
    Conditional(Literal(false), Literal(0), Literal(1)),
    Literal(2)).GenerateExpressionString());
        }

        #endregion
    }
}
