// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Xunit;

#if false
namespace Roslyn.Services.Editor.UnitTests.CodeGeneration
{
    public class ExpressionPrecedenceGenerationTests : AbstractCodeGenerationTests
    {
        [WpfFact]
        public void TestAddMultiplyPrecedence1()
        {
            TestExpression(
                f => f.CreateMultiplyExpression(
                    f.CreateAddExpression(
                        f.CreateConstantExpression(1),
                        f.CreateConstantExpression(2)),
                    f.CreateConstantExpression(3)),
                cs: "(1 + 2) * 3",
                vb: "(1 + 2) * 3");
        }

        [WpfFact]
        public void TestAddMultiplyPrecedence2()
        {
            TestExpression(
                f => f.CreateAddExpression(
                    f.CreateMultiplyExpression(
                        f.CreateConstantExpression(1),
                        f.CreateConstantExpression(2)),
                    f.CreateConstantExpression(3)),
                cs: "1 * 2 + 3",
                vb: "1 * 2 + 3");
        }

        [WpfFact]
        public void TestAddMultiplyPrecedence3()
        {
            TestExpression(
                f => f.CreateMultiplyExpression(
                    f.CreateConstantExpression(1),
                    f.CreateAddExpression(
                        f.CreateConstantExpression(2),
                        f.CreateConstantExpression(3))),
                cs: "1 * (2 + 3)",
                vb: "1 * (2 + 3)");
        }

        [WpfFact]
        public void TestAddMultiplyPrecedence4()
        {
            TestExpression(
                f => f.CreateAddExpression(
                    f.CreateConstantExpression(1),
                    f.CreateMultiplyExpression(
                        f.CreateConstantExpression(2),
                        f.CreateConstantExpression(3))),
                cs: "1 + 2 * 3",
                vb: "1 + 2 * 3");
        }

        [WpfFact]
        public void TestBinaryAndOrPrecedence1()
        {
            TestExpression(
                f => f.CreateBinaryAndExpression(
                    f.CreateBinaryOrExpression(
                        f.CreateConstantExpression(1),
                        f.CreateConstantExpression(2)),
                    f.CreateConstantExpression(3)),
                cs: "(1 | 2) & 3",
                vb: "(1 Or 2) And 3");
        }

        [WpfFact]
        public void TestBinaryAndOrPrecedence2()
        {
            TestExpression(
                f => f.CreateBinaryOrExpression(
                    f.CreateBinaryAndExpression(
                        f.CreateConstantExpression(1),
                        f.CreateConstantExpression(2)),
                    f.CreateConstantExpression(3)),
                cs: "1 & 2 | 3",
                vb: "1 And 2 Or 3");
        }

        [WpfFact]
        public void TestBinaryAndOrPrecedence3()
        {
            TestExpression(
                f => f.CreateBinaryAndExpression(
                    f.CreateConstantExpression(1),
                    f.CreateBinaryOrExpression(
                        f.CreateConstantExpression(2),
                        f.CreateConstantExpression(3))),
                cs: "1 & (2 | 3)",
                vb: "1 And (2 Or 3)");
        }

        [WpfFact]
        public void TestBinaryAndOrPrecedence4()
        {
            TestExpression(
                f => f.CreateBinaryOrExpression(
                    f.CreateConstantExpression(1),
                    f.CreateBinaryAndExpression(
                        f.CreateConstantExpression(2),
                        f.CreateConstantExpression(3))),
                cs: "1 | 2 & 3",
                vb: "1 Or 2 And 3");
        }

        [WpfFact]
        public void TestLogicalAndOrPrecedence1()
        {
            TestExpression(
                f => f.CreateLogicalAndExpression(
                    f.CreateLogicalOrExpression(
                        f.CreateConstantExpression(1),
                        f.CreateConstantExpression(2)),
                    f.CreateConstantExpression(3)),
                cs: "(1 || 2) && 3",
                vb: "(1 OrElse 2) AndAlso 3");
        }

        [WpfFact]
        public void TestLogicalAndOrPrecedence2()
        {
            TestExpression(
                f => f.CreateLogicalOrExpression(
                    f.CreateLogicalAndExpression(
                        f.CreateConstantExpression(1),
                        f.CreateConstantExpression(2)),
                    f.CreateConstantExpression(3)),
                cs: "1 && 2 || 3",
                vb: "1 AndAlso 2 OrElse 3");
        }

        [WpfFact]
        public void TestLogicalAndOrPrecedence3()
        {
            TestExpression(
                f => f.CreateLogicalAndExpression(
                    f.CreateConstantExpression(1),
                    f.CreateLogicalOrExpression(
                        f.CreateConstantExpression(2),
                        f.CreateConstantExpression(3))),
                cs: "1 && (2 || 3)",
                vb: "1 AndAlso (2 OrElse 3)");
        }

        [WpfFact]
        public void TestLogicalAndOrPrecedence4()
        {
            TestExpression(
                f => f.CreateLogicalOrExpression(
                    f.CreateConstantExpression(1),
                    f.CreateLogicalAndExpression(
                        f.CreateConstantExpression(2),
                        f.CreateConstantExpression(3))),
                cs: "1 || 2 && 3",
                vb: "1 OrElse 2 AndAlso 3");
        }

        [WpfFact]
        public void TestMemberAccessOffOfAdd1()
        {
            TestExpression(
                f => f.CreateMemberAccessExpression(
                    f.CreateAddExpression(
                        f.CreateConstantExpression(1),
                        f.CreateConstantExpression(2)),
                    f.CreateIdentifierName("M")),
                cs: "(1 + 2).M",
                vb: "(1 + 2).M");
        }

        [WpfFact]
        public void TestConditionalExpression1()
        {
            TestExpression(
                f => f.CreateConditionalExpression(
                    f.CreateAssignExpression(
                        f.CreateIdentifierName("E1"),
                        f.CreateIdentifierName("E2")),
                    f.CreateIdentifierName("T"),
                    f.CreateIdentifierName("F")),
                cs: "(E1 = E2) ? T : F");
        }

        [WpfFact]
        public void TestConditionalExpression2()
        {
            TestExpression(
                f => f.CreateAddExpression(
                        f.CreateConditionalExpression(
                            f.CreateIdentifierName("E1"),
                            f.CreateIdentifierName("T1"),
                            f.CreateIdentifierName("F1")),
                        f.CreateConditionalExpression(
                            f.CreateIdentifierName("E2"),
                            f.CreateIdentifierName("T2"),
                            f.CreateIdentifierName("F2"))),
                cs: "(E1 ? T1 : F1) + (E2 ? T2 : F2)");
        }

        [WpfFact]
        public void TestMemberAccessOffOfElementAccess()
        {
            TestExpression(
                f => f.CreateElementAccessExpression(
                    f.CreateAddExpression(
                        f.CreateConstantExpression(1),
                        f.CreateConstantExpression(2)),
                    f.CreateArgument(f.CreateIdentifierName("M"))),
                cs: "(1 + 2)[M]",
                vb: "(1 + 2)(M)");
        }

        [WpfFact]
        public void TestMemberAccessOffOfIsExpression()
        {
            TestExpression(
                f => f.CreateMemberAccessExpression(
                    f.CreateIsExpression(
                        f.CreateIdentifierName("a"),
                        CreateClass("SomeType")),
                    f.CreateIdentifierName("M")),
                cs: "(a is SomeType).M",
                vb: "(TypeOf a Is SomeType).M");
        }

        [WpfFact]
        public void TestIsOfMemberAccessExpression()
        {
            TestExpression(
                f => f.CreateIsExpression(
                    f.CreateMemberAccessExpression(
                        f.CreateIdentifierName("a"),
                        f.CreateIdentifierName("M")),
                    CreateClass("SomeType")),
                cs: "a.M is SomeType",
                vb: "TypeOf a.M Is SomeType");
        }

        [WpfFact]
        public void TestMemberAccessOffOfAsExpression()
        {
            TestExpression(
                f => f.CreateMemberAccessExpression(
                    f.CreateAsExpression(
                        f.CreateIdentifierName("a"),
                        CreateClass("SomeType")),
                    f.CreateIdentifierName("M")),
                cs: "(a as SomeType).M",
                vb: "TryCast(a, SomeType).M");
        }

        [WpfFact]
        public void TestAsOfMemberAccessExpression()
        {
            TestExpression(
                f => f.CreateAsExpression(
                         f.CreateMemberAccessExpression(
                            f.CreateIdentifierName("a"),
                            f.CreateIdentifierName("M")),
                        CreateClass("SomeType")),
                cs: "a.M as SomeType",
                vb: "TryCast(a.M, SomeType)");
        }

        [WpfFact]
        public void TestMemberAccessOffOfNotExpression()
        {
            TestExpression(
                f => f.CreateMemberAccessExpression(
                    f.CreateLogicalNotExpression(
                        f.CreateIdentifierName("a")),
                    f.CreateIdentifierName("M")),
                cs: "(!a).M",
                vb: "(Not a).M");
        }

        [WpfFact]
        public void TestNotOfMemberAccessExpression()
        {
            TestExpression(
                f => f.CreateLogicalNotExpression(
                    f.CreateMemberAccessExpression(
                        f.CreateIdentifierName("a"),
                        f.CreateIdentifierName("M"))),
                cs: "!a.M",
                vb: "Not a.M");
        }

        [WpfFact]
        public void TestMemberAccessOffOfCastExpression()
        {
            TestExpression(
                f => f.CreateMemberAccessExpression(
                    f.CreateCastExpression(
                        CreateClass("SomeType"),
                        f.CreateIdentifierName("a")),
                    f.CreateIdentifierName("M")),
                cs: "((SomeType)a).M",
                vb: "DirectCast(a, SomeType).M");
        }

        [WpfFact]
        public void TestCastOfAddExpression()
        {
            TestExpression(
                f => f.CreateCastExpression(
                    CreateClass("SomeType"),
                    f.CreateAddExpression(
                        f.CreateIdentifierName("a"),
                        f.CreateIdentifierName("b"))),
                cs: "(SomeType)(a + b)",
                vb: "DirectCast(a + b, SomeType)");
        }

        [WpfFact]
        public void TestNegateOfAddExpression()
        {
            TestExpression(
                f => f.CreateNegateExpression(
                    f.CreateAddExpression(
                        f.CreateIdentifierName("a"),
                        f.CreateIdentifierName("b"))),
                cs: "-(a + b)",
                vb: "-(a + b)");
        }

        [WpfFact]
        public void TestMemberAccessOffOfNegate()
        {
            TestExpression(
                f => f.CreateMemberAccessExpression(
                    f.CreateNegateExpression(
                        f.CreateIdentifierName("a")),
                    f.CreateIdentifierName("M")),
                cs: "(-a).M",
                vb: "(-a).M");
        }

        [WpfFact]
        public void TestNegateOfMemberAccess()
        {
            TestExpression(f =>
                f.CreateNegateExpression(
                    f.CreateMemberAccessExpression(
                        f.CreateIdentifierName("a"),
                        f.CreateIdentifierName("M"))),
                cs: "-a.M",
                vb: "-a.M");
        }
    }
}
#endif
