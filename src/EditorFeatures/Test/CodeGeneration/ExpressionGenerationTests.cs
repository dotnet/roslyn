// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.CodeGeneration
{
    [Trait(Traits.Feature, Traits.Features.CodeGeneration)]
    public class ExpressionGenerationTests : AbstractCodeGenerationTests
    {
        [Fact]
        public void TestFalseExpression()
        {
            Test(
                f => f.FalseLiteralExpression(),
                cs: "false",
                csSimple: "false",
                vb: "False",
                vbSimple: "False");
        }

        [Fact]
        public void TestTrueExpression()
        {
            Test(
                f => f.TrueLiteralExpression(),
                cs: "true",
                csSimple: "true",
                vb: "True",
                vbSimple: "True");
        }

        [Fact]
        public void TestNullExpression()
        {
            Test(
                f => f.NullLiteralExpression(),
                cs: "null",
                csSimple: "null",
                vb: "Nothing",
                vbSimple: "Nothing");
        }

        [Fact]
        public void TestThisExpression()
        {
            Test(
                f => f.ThisExpression(),
                cs: "this",
                csSimple: "this",
                vb: "Me",
                vbSimple: "Me");
        }

        [Fact]
        public void TestBaseExpression()
        {
            Test(
                f => f.BaseExpression(),
                cs: "base",
                csSimple: "base",
                vb: "MyBase",
                vbSimple: "MyBase");
        }

        [Fact]
        public void TestInt32LiteralExpression0()
        {
            Test(
                f => f.LiteralExpression(0),
                cs: "0",
                csSimple: "0",
                vb: "0",
                vbSimple: "0");
        }

        [Fact]
        public void TestInt32LiteralExpression1()
        {
            Test(
                f => f.LiteralExpression(1),
                cs: "1",
                csSimple: "1",
                vb: "1",
                vbSimple: "1");
        }

        [Fact]
        public void TestInt64LiteralExpression0()
        {
            Test(
                f => f.LiteralExpression(0L),
                cs: "0L",
                csSimple: "0L",
                vb: "0L",
                vbSimple: "0L");
        }

        [Fact]
        public void TestInt64LiteralExpression1()
        {
            Test(
                f => f.LiteralExpression(1L),
                cs: "1L",
                csSimple: "1L",
                vb: "1L",
                vbSimple: "1L");
        }

        [Fact]
        public void TestSingleLiteralExpression0()
        {
            Test(
                f => f.LiteralExpression(0.0f),
                cs: "0F",
                csSimple: "0F",
                vb: "0F",
                vbSimple: "0F");
        }

        [Fact]
        public void TestSingleLiteralExpression1()
        {
            Test(
                f => f.LiteralExpression(0.5F),
                cs: "0.5F",
                csSimple: "0.5F",
                vb: "0.5F",
                vbSimple: "0.5F");
        }

        [Fact]
        public void TestDoubleLiteralExpression0()
        {
            Test(
                f => f.LiteralExpression(0.0d),
                cs: "0D",
                csSimple: "0D",
                vb: "0R",
                vbSimple: "0R");
        }

        [Fact]
        public void TestDoubleLiteralExpression1()
        {
            Test(
                f => f.LiteralExpression(0.5D),
                cs: "0.5D",
                csSimple: "0.5D",
                vb: "0.5R",
                vbSimple: "0.5R");
        }

        [Fact]
        public void TestAddExpression1()
        {
            Test(
                f => f.AddExpression(
                    f.LiteralExpression(1),
                    f.LiteralExpression(2)),
                cs: "(1) + (2)",
                csSimple: "1 + 2",
                vb: "(1) + (2)",
                vbSimple: "1 + 2");
        }

        [Fact]
        public void TestAddExpression2()
        {
            Test(
                f => f.AddExpression(
                    f.LiteralExpression(1),
                    f.AddExpression(
                        f.LiteralExpression(2),
                        f.LiteralExpression(3))),
                cs: "(1) + ((2) + (3))",
                csSimple: "1 + 2 + 3",
                vb: "(1) + ((2) + (3))",
                vbSimple: "1 + (2 + 3)");
        }

        [Fact]
        public void TestAddExpression3()
        {
            Test(
                f => f.AddExpression(
                    f.AddExpression(
                        f.LiteralExpression(1),
                        f.LiteralExpression(2)),
                    f.LiteralExpression(3)),
                cs: "((1) + (2)) + (3)",
                csSimple: "1 + 2 + 3",
                vb: "((1) + (2)) + (3)",
                vbSimple: "1 + 2 + 3");
        }

        [Fact]
        public void TestMultiplyExpression1()
        {
            Test(
                f => f.MultiplyExpression(
                    f.LiteralExpression(1),
                    f.LiteralExpression(2)),
                cs: "(1) * (2)",
                csSimple: "1 * 2",
                vb: "(1) * (2)",
                vbSimple: "1 * 2");
        }

        [Fact]
        public void TestMultiplyExpression2()
        {
            Test(
                f => f.MultiplyExpression(
                    f.LiteralExpression(1),
                    f.MultiplyExpression(
                        f.LiteralExpression(2),
                        f.LiteralExpression(3))),
                cs: "(1) * ((2) * (3))",
                csSimple: "1 * 2 * 3",
                vb: "(1) * ((2) * (3))",
                vbSimple: "1 * (2 * 3)");
        }

        [Fact]
        public void TestMultiplyExpression3()
        {
            Test(
                f => f.MultiplyExpression(
                    f.MultiplyExpression(
                        f.LiteralExpression(1),
                        f.LiteralExpression(2)),
                    f.LiteralExpression(3)),
                cs: "((1) * (2)) * (3)",
                csSimple: "1 * 2 * 3",
                vb: "((1) * (2)) * (3)",
                vbSimple: "1 * 2 * 3");
        }

        [Fact]
        public void TestBinaryAndExpression1()
        {
            Test(
                f => f.BitwiseAndExpression(
                    f.LiteralExpression(1),
                    f.LiteralExpression(2)),
                cs: "(1) & (2)",
                csSimple: "1 & 2",
                vb: "(1) And (2)",
                vbSimple: "1 And 2");
        }

        [Fact]
        public void TestBinaryOrExpression1()
        {
            Test(
                f => f.BitwiseOrExpression(
                    f.LiteralExpression(1),
                    f.LiteralExpression(2)),
                cs: "(1) | (2)",
                csSimple: "1 | 2",
                vb: "(1) Or (2)",
                vbSimple: "1 Or 2");
        }

        [Fact]
        public void TestLogicalAndExpression1()
        {
            Test(
                f => f.LogicalAndExpression(
                    f.LiteralExpression(1),
                    f.LiteralExpression(2)),
                cs: "(1) && (2)",
                csSimple: "1 && 2",
                vb: "(1) AndAlso (2)",
                vbSimple: "1 AndAlso 2");
        }

        [Fact]
        public void TestLogicalOrExpression1()
        {
            Test(
                f => f.LogicalOrExpression(
                    f.LiteralExpression(1),
                    f.LiteralExpression(2)),
                cs: "(1) || (2)",
                csSimple: "1 || 2",
                vb: "(1) OrElse (2)",
                vbSimple: "1 OrElse 2");
        }

        [Fact]
        public void TestMemberAccess1()
        {
            Test(
                f => f.MemberAccessExpression(
                    f.IdentifierName("E"),
                    f.IdentifierName("M")),
                cs: "E.M",
                csSimple: "E.M",
                vb: "E.M",
                vbSimple: "E.M");
        }

        [Fact]
        public void TestConditionalExpression1()
        {
            Test(
                f => f.ConditionalExpression(
                    f.IdentifierName("E"),
                    f.IdentifierName("T"),
                    f.IdentifierName("F")),
                cs: "(E) ? (T) : (F)",
                csSimple: "E ? T : F",
                vb: "If(E, T, F)",
                vbSimple: "If(E, T, F)");
        }

        [Fact]
        public void TestConditionalAccessExpression1()
        {
            Test(
                f => f.ConditionalAccessExpression(
                    f.IdentifierName("E"),
                    f.MemberBindingExpression(
                        f.IdentifierName("T"))),
                cs: "E?.T",
                csSimple: "E?.T",
                vb: "E?.T",
                vbSimple: "E?.T");
        }

        [Fact]
        public void TestConditionalAccessExpression2()
        {
            Test(
                f => f.ConditionalAccessExpression(
                    f.IdentifierName("E"),
                    f.ElementBindingExpression(
                        f.Argument(f.IdentifierName("T")))),
                cs: "E?[T]",
                csSimple: "E?[T]",
                vb: "E?(T)",
                vbSimple: "E?(T)");
        }

        [Fact]
        public void TestInvocation1()
        {
            Test(
                f => f.InvocationExpression(
                    f.IdentifierName("E")),
                cs: "E()",
                csSimple: "E()",
                vb: "E()",
                vbSimple: "E()");
        }

        [Fact]
        public void TestInvocation2()
        {
            Test(
                f => f.InvocationExpression(
                    f.IdentifierName("E"),
                    f.Argument(f.IdentifierName("a"))),
                cs: "E(a)",
                csSimple: "E(a)",
                vb: "E(a)",
                vbSimple: "E(a)");
        }

        [Fact]
        public void TestInvocation3()
        {
            Test(
                f => f.InvocationExpression(
                    f.IdentifierName("E"),
                    f.Argument("n", RefKind.None, f.IdentifierName("a"))),
                cs: "E(n: a)",
                csSimple: "E(n: a)",
                vb: "E(n:=a)",
                vbSimple: "E(n:=a)");
        }

        [Fact]
        public void TestInvocation4()
        {
            Test(
                f => f.InvocationExpression(
                    f.IdentifierName("E"),
                    f.Argument(null, RefKind.Out, f.IdentifierName("a")),
                    f.Argument(null, RefKind.Ref, f.IdentifierName("b"))),
                cs: "E(out a, ref b)",
                csSimple: "E(out a, ref b)",
                vb: "E(a, b)",
                vbSimple: "E(a, b)");
        }

        [Fact]
        public void TestInvocation5()
        {
            Test(
                f => f.InvocationExpression(
                    f.IdentifierName("E"),
                    f.Argument("n1", RefKind.Out, f.IdentifierName("a")),
                    f.Argument("n2", RefKind.Ref, f.IdentifierName("b"))),
                cs: "E(n1: out a, n2: ref b)",
                csSimple: "E(n1: out a, n2: ref b)",
                vb: "E(n1:=a, n2:=b)",
                vbSimple: "E(n1:=a, n2:=b)");
        }

        [Fact]
        public void TestElementAccess1()
        {
            Test(
                f => f.ElementAccessExpression(
                    f.IdentifierName("E")),
                cs: "E[]",
                csSimple: "E[]",
                vb: "E()",
                vbSimple: "E()");
        }

        [Fact]
        public void TestElementAccess2()
        {
            Test(
                f => f.ElementAccessExpression(
                    f.IdentifierName("E"),
                    f.Argument(f.IdentifierName("a"))),
                cs: "E[a]",
                csSimple: "E[a]",
                vb: "E(a)",
                vbSimple: "E(a)");
        }

        [Fact]
        public void TestElementAccess3()
        {
            Test(
                f => f.ElementAccessExpression(
                    f.IdentifierName("E"),
                    f.Argument("n", RefKind.None, f.IdentifierName("a"))),
                cs: "E[n: a]",
                csSimple: "E[n: a]",
                vb: "E(n:=a)",
                vbSimple: "E(n:=a)");
        }

        [Fact]
        public void TestElementAccess4()
        {
            Test(
                f => f.ElementAccessExpression(
                    f.IdentifierName("E"),
                    f.Argument(null, RefKind.Out, f.IdentifierName("a")),
                    f.Argument(null, RefKind.Ref, f.IdentifierName("b"))),
                cs: "E[out a, ref b]",
                csSimple: "E[out a, ref b]",
                vb: "E(a, b)",
                vbSimple: "E(a, b)");
        }

        [Fact]
        public void TestElementAccess5()
        {
            Test(
                f => f.ElementAccessExpression(
                    f.IdentifierName("E"),
                    f.Argument("n1", RefKind.Out, f.IdentifierName("a")),
                    f.Argument("n2", RefKind.Ref, f.IdentifierName("b"))),
                cs: "E[n1: out a, n2: ref b]",
                csSimple: "E[n1: out a, n2: ref b]",
                vb: "E(n1:=a, n2:=b)",
                vbSimple: "E(n1:=a, n2:=b)");
        }

        [Fact]
        public void TestIsExpression()
        {
            Test(
                f => f.IsTypeExpression(
                    f.IdentifierName("a"),
                    CreateClass("SomeType")),
                cs: "(a) is SomeType",
                csSimple: "a is SomeType",
                vb: "TypeOf (a) Is SomeType",
                vbSimple: "TypeOf a Is SomeType");
        }

        [Fact]
        public void TestAsExpression()
        {
            Test(
                f => f.TryCastExpression(
                    f.IdentifierName("a"),
                    CreateClass("SomeType")),
                cs: "(a) as SomeType",
                csSimple: "a as SomeType",
                vb: "TryCast(a, SomeType)",
                vbSimple: "TryCast(a, SomeType)");
        }

        [Fact]
        public void TestNotExpression()
        {
            Test(
                f => f.LogicalNotExpression(
                    f.IdentifierName("a")),
                cs: "!(a)",
                csSimple: "!a",
                vb: "Not (a)",
                vbSimple: "Not a");
        }

        [Fact]
        public void TestCastExpression()
        {
            Test(
                f => f.CastExpression(
                    CreateClass("SomeType"),
                    f.IdentifierName("a")),
                cs: "(SomeType)(a)",
                csSimple: "(SomeType)a",
                vb: "DirectCast(a, SomeType)",
                vbSimple: "DirectCast(a, SomeType)");
        }

        [Fact]
        public void TestNegateExpression()
        {
            Test(
                f => f.NegateExpression(
                    f.IdentifierName("a")),
                cs: "-(a)",
                csSimple: "-a",
                vb: "-(a)",
                vbSimple: "-a");
        }
    }
}
