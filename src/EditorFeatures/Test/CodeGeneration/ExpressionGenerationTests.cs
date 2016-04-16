// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using Xunit;

#if false
namespace Roslyn.Services.Editor.UnitTests.CodeGeneration
{
    public class ExpressionGenerationTests : AbstractCodeGenerationTests
    {
        [WpfFact]
        public void TestFalseExpression()
        {
            TestExpression(
                f => f.CreateFalseExpression(),
                cs: "false",
                vb: "False");
        }

        [WpfFact]
        public void TestTrueExpression()
        {
            TestExpression(
                f => f.CreateTrueExpression(),
                cs: "true",
                vb: "True");
        }

        [WpfFact]
        public void TestNullExpression()
        {
            TestExpression(
                f => f.CreateNullExpression(),
                cs: "null",
                vb: "Nothing");
        }

        [WpfFact]
        public void TestThisExpression()
        {
            TestExpression(
                f => f.CreateThisExpression(),
                cs: "this",
                vb: "Me");
        }

        [WpfFact]
        public void TestBaseExpression()
        {
            TestExpression(
                f => f.CreateBaseExpression(),
                cs: "base",
                vb: "MyBase");
        }

        [WpfFact]
        public void TestInt32ConstantExpression0()
        {
            TestExpression(
                f => f.CreateConstantExpression(0),
                cs: "0",
                vb: "0");
        }

        [WpfFact]
        public void TestInt32ConstantExpression1()
        {
            TestExpression(
                f => f.CreateConstantExpression(1),
                cs: "1",
                vb: "1");
        }

        [WpfFact]
        public void TestInt64ConstantExpression0()
        {
            TestExpression(
                f => f.CreateConstantExpression(0L),
                cs: "0L",
                vb: "0&");
        }

        [WpfFact]
        public void TestInt64ConstantExpression1()
        {
            TestExpression(
                f => f.CreateConstantExpression(1L),
                cs: "1L",
                vb: "1&");
        }

        [WpfFact]
        public void TestSingleConstantExpression0()
        {
            TestExpression(
                f => f.CreateConstantExpression(0.0f),
                cs: "0F",
                vb: "0!");
        }

        [WpfFact]
        public void TestSingleConstantExpression1()
        {
            TestExpression(
                f => f.CreateConstantExpression(0.5F),
                cs: "0.5F",
                vb: "0.5!");
        }

        [WpfFact]
        public void TestDoubleConstantExpression0()
        {
            TestExpression(
                f => f.CreateConstantExpression(0.0d),
                cs: "0",
                vb: "0");
        }

        [WpfFact]
        public void TestDoubleConstantExpression1()
        {
            TestExpression(
                f => f.CreateConstantExpression(0.5D),
                cs: "0.5",
                vb: "0.5");
        }

        [WpfFact]
        public void TestAddExpression1()
        {
            TestExpression(
                f => f.CreateAddExpression(
                    f.CreateConstantExpression(1),
                    f.CreateConstantExpression(2)),
                cs: "1 + 2",
                vb: "1 + 2");
        }

        [WpfFact]
        public void TestAddExpression2()
        {
            TestExpression(
                f => f.CreateAddExpression(
                    f.CreateConstantExpression(1),
                    f.CreateAddExpression(
                        f.CreateConstantExpression(2),
                        f.CreateConstantExpression(3))),
                cs: "1 + 2 + 3",
                vb: "1 + 2 + 3");
        }

        [WpfFact]
        public void TestAddExpression3()
        {
            TestExpression(
                f => f.CreateAddExpression(
                    f.CreateAddExpression(
                        f.CreateConstantExpression(1),
                        f.CreateConstantExpression(2)),
                    f.CreateConstantExpression(3)),
                cs: "1 + 2 + 3",
                vb: "1 + 2 + 3");
        }

        [WpfFact]
        public void TestMultiplyExpression1()
        {
            TestExpression(
                f => f.CreateMultiplyExpression(
                    f.CreateConstantExpression(1),
                    f.CreateConstantExpression(2)),
                cs: "1 * 2",
                vb: "1 * 2");
        }

        [WpfFact]
        public void TestMultiplyExpression2()
        {
            TestExpression(
                f => f.CreateMultiplyExpression(
                    f.CreateConstantExpression(1),
                    f.CreateMultiplyExpression(
                        f.CreateConstantExpression(2),
                        f.CreateConstantExpression(3))),
                cs: "1 * 2 * 3",
                vb: "1 * 2 * 3");
        }

        [WpfFact]
        public void TestMultiplyExpression3()
        {
            TestExpression(
                f => f.CreateMultiplyExpression(
                    f.CreateMultiplyExpression(
                        f.CreateConstantExpression(1),
                        f.CreateConstantExpression(2)),
                    f.CreateConstantExpression(3)),
                cs: "1 * 2 * 3",
                vb: "1 * 2 * 3");
        }

        [WpfFact]
        public void TestBinaryAndExpression1()
        {
            TestExpression(
                f => f.CreateBinaryAndExpression(
                    f.CreateConstantExpression(1),
                    f.CreateConstantExpression(2)),
                cs: "1 & 2",
                vb: "1 And 2");
        }

        [WpfFact]
        public void TestBinaryOrExpression1()
        {
            TestExpression(
                f => f.CreateBinaryOrExpression(
                    f.CreateConstantExpression(1),
                    f.CreateConstantExpression(2)),
                cs: "1 | 2",
                vb: "1 Or 2");
        }

        [WpfFact]
        public void TestLogicalAndExpression1()
        {
            TestExpression(
                f => f.CreateLogicalAndExpression(
                    f.CreateConstantExpression(1),
                    f.CreateConstantExpression(2)),
                cs: "1 && 2",
                vb: "1 AndAlso 2");
        }

        [WpfFact]
        public void TestLogicalOrExpression1()
        {
            TestExpression(
                f => f.CreateLogicalOrExpression(
                    f.CreateConstantExpression(1),
                    f.CreateConstantExpression(2)),
                cs: "1 || 2",
                vb: "1 OrElse 2");
        }

        [WpfFact]
        public void TestMemberAccess1()
        {
            TestExpression(
                f => f.CreateMemberAccessExpression(
                    f.CreateIdentifierName("E"),
                    f.CreateIdentifierName("M")),
                cs: "E.M",
                vb: "E.M");
        }

        [WpfFact]
        public void TestConditionalExpression1()
        {
            TestExpression(
                f => f.CreateConditionalExpression(
                    f.CreateIdentifierName("E"),
                    f.CreateIdentifierName("T"),
                    f.CreateIdentifierName("F")),
                cs: "E ? T : F",
                vb: "If(E, T, F)");
        }

        [WpfFact]
        public void TestInvocation1()
        {
            TestExpression(
                f => f.CreateInvocationExpression(
                    f.CreateIdentifierName("E")),
                cs: "E()",
                vb: "E()");
        }

        [WpfFact]
        public void TestInvocation2()
        {
            TestExpression(
                f => f.CreateInvocationExpression(
                    f.CreateIdentifierName("E"),
                    f.CreateArgument(f.CreateIdentifierName("a"))),
                cs: "E(a)",
                vb: "E(a)");
        }

        [WpfFact]
        public void TestInvocation3()
        {
            TestExpression(
                f => f.CreateInvocationExpression(
                    f.CreateIdentifierName("E"),
                    f.CreateArgument("n", RefKind.None, f.CreateIdentifierName("a"))),
                cs: "E(n: a)",
                vb: "E(n:=a)");
        }

        [WpfFact]
        public void TestInvocation4()
        {
            TestExpression(
                f => f.CreateInvocationExpression(
                    f.CreateIdentifierName("E"),
                    f.CreateArgument(null, RefKind.Out, f.CreateIdentifierName("a")),
                    f.CreateArgument(null, RefKind.Ref, f.CreateIdentifierName("b"))),
                cs: "E(out a, ref b)",
                vb: "E(a, b)");
        }

        [WpfFact]
        public void TestInvocation5()
        {
            TestExpression(
                f => f.CreateInvocationExpression(
                    f.CreateIdentifierName("E"),
                    f.CreateArgument("n1", RefKind.Out, f.CreateIdentifierName("a")),
                    f.CreateArgument("n2", RefKind.Ref, f.CreateIdentifierName("b"))),
                cs: "E(n1: out a, n2: ref b)",
                vb: "E(n1:=a, n2:=b)");
        }

        [WpfFact]
        public void TestElementAccess1()
        {
            TestExpression(
                f => f.CreateElementAccessExpression(
                    f.CreateIdentifierName("E")),
                cs: "E[]",
                vb: "E()");
        }

        [WpfFact]
        public void TestElementAccess2()
        {
            TestExpression(
                f => f.CreateElementAccessExpression(
                    f.CreateIdentifierName("E"),
                    f.CreateArgument(f.CreateIdentifierName("a"))),
                cs: "E[a]",
                vb: "E(a)");
        }

        [WpfFact]
        public void TestElementAccess3()
        {
            TestExpression(
                f => f.CreateElementAccessExpression(
                    f.CreateIdentifierName("E"),
                    f.CreateArgument("n", RefKind.None, f.CreateIdentifierName("a"))),
                cs: "E[n: a]",
                vb: "E(n:=a)");
        }

        [WpfFact]
        public void TestElementAccess4()
        {
            TestExpression(
                f => f.CreateElementAccessExpression(
                    f.CreateIdentifierName("E"),
                    f.CreateArgument(null, RefKind.Out, f.CreateIdentifierName("a")),
                    f.CreateArgument(null, RefKind.Ref, f.CreateIdentifierName("b"))),
                cs: "E[out a, ref b]",
                vb: "E(a, b)");
        }

        [WpfFact]
        public void TestElementAccess5()
        {
            TestExpression(
                f => f.CreateElementAccessExpression(
                    f.CreateIdentifierName("E"),
                    f.CreateArgument("n1", RefKind.Out, f.CreateIdentifierName("a")),
                    f.CreateArgument("n2", RefKind.Ref, f.CreateIdentifierName("b"))),
                cs: "E[n1: out a, n2: ref b]",
                vb: "E(n1:=a, n2:=b)");
        }

        [WpfFact]
        public void TestIsExpression()
        {
            TestExpression(
                f => f.CreateIsExpression(
                    f.CreateIdentifierName("a"),
                    CreateClass("SomeType")),
                cs: "a is SomeType",
                vb: "TypeOf a Is SomeType");
        }

        [WpfFact]
        public void TestAsExpression()
        {
            TestExpression(
                f => f.CreateAsExpression(
                    f.CreateIdentifierName("a"),
                    CreateClass("SomeType")),
                cs: "a as SomeType",
                vb: "TryCast(a, SomeType)");
        }

        [WpfFact]
        public void TestNotExpression()
        {
            TestExpression(
                f => f.CreateLogicalNotExpression(
                    f.CreateIdentifierName("a")),
                cs: "!a",
                vb: "Not a");
        }

        [WpfFact]
        public void TestCastExpression()
        {
            TestExpression(
                f => f.CreateCastExpression(
                    CreateClass("SomeType"),
                    f.CreateIdentifierName("a")),
                cs: "(SomeType)a",
                vb: "DirectCast(a, SomeType)");
        }

        [WpfFact]
        public void TestNegateExpression()
        {
            TestExpression(
                f => f.CreateNegateExpression(
                    f.CreateIdentifierName("a")),
                cs: "-a",
                vb: "-a");
        }
    }
}
#endif
