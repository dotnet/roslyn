// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.CodeFixes.GenerateEnumMember;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Diagnostics.GenerateEnumMember;

[Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEnumMember)]
public sealed class GenerateEnumMemberTests(ITestOutputHelper logger)
    : AbstractCSharpDiagnosticProviderBasedUserDiagnosticTest_NoEditor(logger)
{
    internal override (DiagnosticAnalyzer?, CodeFixProvider) CreateDiagnosticProviderAndFixer(Workspace workspace)
        => (null, new GenerateEnumMemberCodeFixProvider());

    [Fact]
    public Task TestEmptyEnum()
        => TestInRegularAndScriptAsync(
            """
            class Program
            {
                void Main()
                {
                    Color.[|Red|];
                }
            }

            enum Color
            {
            }
            """,
            """
            class Program
            {
                void Main()
                {
                    Color.Red;
                }
            }

            enum Color
            {
                Red
            }
            """);

    [Fact]
    public Task TestWithSingleMember()
        => TestInRegularAndScriptAsync(
            """
            class Program
            {
                void Main()
                {
                    Color.[|Blue|];
                }
            }

            enum Color
            {
                Red
            }
            """,
            """
            class Program
            {
                void Main()
                {
                    Color.Blue;
                }
            }

            enum Color
            {
                Red,
                Blue
            }
            """);

    [Fact]
    public Task TestWithExistingComma()
        => TestInRegularAndScriptAsync(
            """
            class Program
            {
                void Main()
                {
                    Color.[|Blue|];
                }
            }

            enum Color
            {
                Red,
            }
            """,
            """
            class Program
            {
                void Main()
                {
                    Color.Blue;
                }
            }

            enum Color
            {
                Red,
                Blue,
            }
            """);

    [Fact]
    public Task TestWithMultipleMembers()
        => TestInRegularAndScriptAsync(
            """
            class Program
            {
                void Main()
                {
                    Color.[|Green|];
                }
            }

            enum Color
            {
                Red,
                Blue
            }
            """,
            """
            class Program
            {
                void Main()
                {
                    Color.Green;
                }
            }

            enum Color
            {
                Red,
                Blue,
                Green
            }
            """);

    [Fact]
    public Task TestWithZero()
        => TestInRegularAndScriptAsync(
            """
            class Program
            {
                void Main()
                {
                    Color.[|Blue|];
                }
            }

            enum Color
            {
                Red = 0
            }
            """,
            """
            class Program
            {
                void Main()
                {
                    Color.Blue;
                }
            }

            enum Color
            {
                Red = 0,
                Blue = 1
            }
            """);

    [Fact]
    public Task TestWithIntegralValue()
        => TestInRegularAndScriptAsync(
            """
            class Program
            {
                void Main()
                {
                    Color.[|Blue|];
                }
            }

            enum Color
            {
                Red = 1
            }
            """,
            """
            class Program
            {
                void Main()
                {
                    Color.Blue;
                }
            }

            enum Color
            {
                Red = 1,
                Blue = 2
            }
            """);

    [Fact]
    public Task TestWithSingleBitIntegral()
        => TestInRegularAndScriptAsync(
            """
            class Program
            {
                void Main()
                {
                    Color.[|Blue|];
                }
            }

            enum Color
            {
                Red = 2
            }
            """,
            """
            class Program
            {
                void Main()
                {
                    Color.Blue;
                }
            }

            enum Color
            {
                Red = 2,
                Blue = 4
            }
            """);

    [Fact]
    public Task TestGenerateIntoGeometricSequence()
        => TestInRegularAndScriptAsync(
            """
            class Program
            {
                void Main()
                {
                    Color.[|Blue|];
                }
            }

            enum Color
            {
                Red = 1,
                Yellow = 2,
                Green = 4
            }
            """,
            """
            class Program
            {
                void Main()
                {
                    Color.Blue;
                }
            }

            enum Color
            {
                Red = 1,
                Yellow = 2,
                Green = 4,
                Blue = 8
            }
            """);

    [Fact]
    public Task TestWithSimpleSequence1()
        => TestInRegularAndScriptAsync(
            """
            class Program
            {
                void Main()
                {
                    Color.[|Blue|];
                }
            }

            enum Color
            {
                Red = 1,
                Green = 2
            }
            """,
            """
            class Program
            {
                void Main()
                {
                    Color.Blue;
                }
            }

            enum Color
            {
                Red = 1,
                Green = 2,
                Blue = 3
            }
            """);

    [Fact]
    public Task TestWithSimpleSequence2()
        => TestInRegularAndScriptAsync(
            """
            class Program
            {
                void Main()
                {
                    Color.[|Blue|];
                }
            }

            enum Color
            {
                Yellow = 0,
                Red = 1,
                Green = 2
            }
            """,
            """
            class Program
            {
                void Main()
                {
                    Color.Blue;
                }
            }

            enum Color
            {
                Yellow = 0,
                Red = 1,
                Green = 2,
                Blue = 3
            }
            """);

    [Fact]
    public Task TestWithNonZeroInteger()
        => TestInRegularAndScriptAsync(
            """
            class Program
            {
                void Main()
                {
                    Color.[|Blue|];
                }
            }

            enum Color
            {
                Green = 5
            }
            """,
            """
            class Program
            {
                void Main()
                {
                    Color.Blue;
                }
            }

            enum Color
            {
                Green = 5,
                Blue = 6
            }
            """);

    [Fact]
    public Task TestWithLeftShift0()
        => TestInRegularAndScriptAsync(
            """
            class Program
            {
                void Main()
                {
                    Color.[|Blue|];
                }
            }

            enum Color
            {
                Green = 1 << 0
            }
            """,
            """
            class Program
            {
                void Main()
                {
                    Color.Blue;
                }
            }

            enum Color
            {
                Green = 1 << 0,
                Blue = 1 << 1
            }
            """);

    [Fact]
    public Task TestWithLeftShift5()
        => TestInRegularAndScriptAsync(
            """
            class Program
            {
                void Main()
                {
                    Color.[|Blue|];
                }
            }

            enum Color
            {
                Green = 1 << 5
            }
            """,
            """
            class Program
            {
                void Main()
                {
                    Color.Blue;
                }
            }

            enum Color
            {
                Green = 1 << 5,
                Blue = 1 << 6
            }
            """);

    [Fact]
    public Task TestWithDifferentStyles()
        => TestInRegularAndScriptAsync(
            """
            class Program
            {
                void Main()
                {
                    Color.[|Blue|];
                }
            }

            enum Color
            {
                Red = 2,
                Green = 1 << 5
            }
            """,
            """
            class Program
            {
                void Main()
                {
                    Color.Blue;
                }
            }

            enum Color
            {
                Red = 2,
                Green = 1 << 5,
                Blue = 33
            }
            """);

    [Fact]
    public Task TestBinary()
        => TestInRegularAndScriptAsync(
            """
            class Program
            {
                void Main()
                {
                    Color.[|Blue|];
                }
            }

            enum Color
            {
                Red = 0b01
            }
            """,
            """
            class Program
            {
                void Main()
                {
                    Color.Blue;
                }
            }

            enum Color
            {
                Red = 0b01,
                Blue = 0b10
            }
            """);

    [Fact]
    public Task TestHex1()
        => TestInRegularAndScriptAsync(
            """
            class Program
            {
                void Main()
                {
                    Color.[|Blue|];
                }
            }

            enum Color
            {
                Red = 0x1
            }
            """,
            """
            class Program
            {
                void Main()
                {
                    Color.Blue;
                }
            }

            enum Color
            {
                Red = 0x1,
                Blue = 0x2
            }
            """);

    [Fact]
    public Task TestHex9()
        => TestInRegularAndScriptAsync(
            """
            class Program
            {
                void Main()
                {
                    Color.[|Blue|];
                }
            }

            enum Color
            {
                Red = 0x9
            }
            """,
            """
            class Program
            {
                void Main()
                {
                    Color.Blue;
                }
            }

            enum Color
            {
                Red = 0x9,
                Blue = 0xA
            }
            """);

    [Fact]
    public Task TestHexF()
        => TestInRegularAndScriptAsync(
            """
            class Program
            {
                void Main()
                {
                    Color.[|Blue|];
                }
            }

            enum Color
            {
                Red = 0xF
            }
            """,
            """
            class Program
            {
                void Main()
                {
                    Color.Blue;
                }
            }

            enum Color
            {
                Red = 0xF,
                Blue = 0x10
            }
            """);

    [Fact]
    public Task TestGenerateAfterEnumWithIntegerMaxValue()
        => TestInRegularAndScriptAsync(
            """
            class Program
            {
                void Main()
                {
                    Color.[|Blue|];
                }
            }

            enum Color
            {
                Red = int.MaxValue
            }
            """,
            """
            class Program
            {
                void Main()
                {
                    Color.Blue;
                }
            }

            enum Color
            {
                Red = int.MaxValue,
                Blue = int.MinValue
            }
            """);

    [Fact]
    public Task TestUnsigned16BitEnums()
        => TestInRegularAndScriptAsync(
            """
            class Program
            {
                void Main()
                {
                    Color.[|Blue|];
                }
            }

            enum Color : ushort
            {
                Red = 65535
            }
            """,
            """
            class Program
            {
                void Main()
                {
                    Color.Blue;
                }
            }

            enum Color : ushort
            {
                Red = 65535,
                Blue = 0
            }
            """);

    [Fact]
    public Task TestGenerateEnumMemberOfTypeLong()
        => TestInRegularAndScriptAsync(
            """
            class Program
            {
                void Main()
                {
                    Color.[|Blue|];
                }
            }

            enum Color : long
            {
                Red = long.MaxValue
            }
            """,
            """
            class Program
            {
                void Main()
                {
                    Color.Blue;
                }
            }

            enum Color : long
            {
                Red = long.MaxValue,
                Blue = long.MinValue
            }
            """);

    [Fact]
    public Task TestGenerateAfterEnumWithLongMaxValueInBinary()
        => TestInRegularAndScriptAsync(
            """
            class Program
            {
                void Main()
                {
                    Color.[|Blue|];
                }
            }

            enum Color : long
            {
                Red = 0b0111111111111111111111111111111111111111111111111111111111111111
            }
            """,
            """
            class Program
            {
                void Main()
                {
                    Color.Blue;
                }
            }

            enum Color : long
            {
                Red = 0b0111111111111111111111111111111111111111111111111111111111111111,
                Blue = 0b1000000000000000000000000000000000000000000000000000000000000000
            }
            """);

    [Fact]
    public Task TestGenerateAfterEnumWithLongMaxValueInHex()
        => TestInRegularAndScriptAsync(
            """
            class Program
            {
                void Main()
                {
                    Color.[|Blue|];
                }
            }

            enum Color : long
            {
                Red = 0x7FFFFFFFFFFFFFFF
            }
            """,
            """
            class Program
            {
                void Main()
                {
                    Color.Blue;
                }
            }

            enum Color : long
            {
                Red = 0x7FFFFFFFFFFFFFFF,
                Blue = 0x8000000000000000
            }
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/528312")]
    public Task TestGenerateAfterEnumWithLongMinValueInHex()
        => TestInRegularAndScriptAsync(
            """
            class Program
            {
                void Main()
                {
                    Color.[|Blue|];
                }
            }

            enum Color : long
            {
                Red = 0xFFFFFFFFFFFFFFFF
            }
            """,
            """
            class Program
            {
                void Main()
                {
                    Color.Blue;
                }
            }

            enum Color : long
            {
                Red = 0xFFFFFFFFFFFFFFFF,
                Blue
            }
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/528312")]
    public Task TestGenerateAfterPositiveLongInHex()
        => TestInRegularAndScriptAsync(
            """
            class Program
            {
                void Main()
                {
                    Color.[|Blue|];
                }
            }

            enum Color : long
            {
                Red = 0xFFFFFFFFFFFFFFFF,
                Green = 0x0
            }
            """,
            """
            class Program
            {
                void Main()
                {
                    Color.Blue;
                }
            }

            enum Color : long
            {
                Red = 0xFFFFFFFFFFFFFFFF,
                Green = 0x0,
                Blue = 0x1
            }
            """);

    [Fact]
    public Task TestGenerateAfterPositiveLongExprInHex()
        => TestInRegularAndScriptAsync(
            """
            class Program
            {
                void Main()
                {
                    Color.[|Blue|];
                }
            }

            enum Color : long
            {
                Red = 0x414 / 2
            }
            """,
            """
            class Program
            {
                void Main()
                {
                    Color.Blue;
                }
            }

            enum Color : long
            {
                Red = 0x414 / 2,
                Blue = 523
            }
            """);

    [Fact]
    public Task TestGenerateAfterEnumWithULongMaxValue()
        => TestInRegularAndScriptAsync(
            """
            class Program
            {
                void Main()
                {
                    Color.[|Blue|];
                }
            }

            enum Color : ulong
            {
                Red = ulong.MaxValue
            }
            """,
            """
            class Program
            {
                void Main()
                {
                    Color.Blue;
                }
            }

            enum Color : ulong
            {
                Red = ulong.MaxValue,
                Blue = 0
            }
            """);

    [Fact]
    public Task TestNegativeRangeIn64BitSignedEnums()
        => TestInRegularAndScriptAsync(
            """
            class Program
            {
                void Main()
                {
                    Color.[|Blue|];
                }
            }

            enum Color : long
            {
                Red = -10
            }
            """,
            """
            class Program
            {
                void Main()
                {
                    Color.Blue;
                }
            }

            enum Color : long
            {
                Red = -10,
                Blue = -9
            }
            """);

    [Fact]
    public Task TestGenerateWithImplicitValues()
        => TestInRegularAndScriptAsync(
            """
            class Program
            {
                void Main()
                {
                    Color.[|Blue|];
                }
            }

            enum Color
            {
                Red,
                Green,
                Yellow = -1
            }
            """,
            """
            class Program
            {
                void Main()
                {
                    Color.Blue;
                }
            }

            enum Color
            {
                Red,
                Green,
                Yellow = -1,
                Blue = 2
            }
            """);

    [Fact]
    public Task TestGenerateWithImplicitValues2()
        => TestInRegularAndScriptAsync(
            """
            class Program
            {
                void Main()
                {
                    Color.[|Blue|];
                }
            }

            enum Color
            {
                Red,
                Green = 10,
                Yellow
            }
            """,
            """
            class Program
            {
                void Main()
                {
                    Color.Blue;
                }
            }

            enum Color
            {
                Red,
                Green = 10,
                Yellow,
                Blue
            }
            """);

    [Fact]
    public Task TestNoExtraneousStatementTerminatorBeforeCommentedMember()
        => TestInRegularAndScriptAsync(
            """
            class Program
            {
                static void Main(string[] args)
                {
                    Color . [|Blue|] ;
                }
            }

            enum Color 
            {
                Red
                //Blue
            }
            """,
            """
            class Program
            {
                static void Main(string[] args)
                {
                    Color . Blue ;
                }
            }

            enum Color 
            {
                Red,
                Blue
                //Blue
            }
            """);

    [Fact]
    public Task TestNoExtraneousStatementTerminatorBeforeCommentedMember2()
        => TestInRegularAndScriptAsync(
            """
            class Program
            {
                static void Main(string[] args)
                {
                    Color . [|Blue|] ;
                }
            }

            enum Color 
            {
                Red
                /*Blue*/
            }
            """,
            """
            class Program
            {
                static void Main(string[] args)
                {
                    Color . Blue ;
                }
            }

            enum Color 
            {
                Red,
                Blue
                /*Blue*/
            }
            """);

    [Fact]
    public Task TestGenerateAfterEnumWithMinValue()
        => TestInRegularAndScriptAsync(
            """
            class Program
            {
                void Main()
                {
                    Color.[|Blue|];
                }
            }

            enum Color
            {
                Red = int.MinValue
            }
            """,
            """
            class Program
            {
                void Main()
                {
                    Color.Blue;
                }
            }

            enum Color
            {
                Red = int.MinValue,
                Blue = -2147483647
            }
            """);

    [Fact]
    public Task TestGenerateAfterEnumWithMinValuePlusConstant()
        => TestInRegularAndScriptAsync(
            """
            class Program
            {
                void Main()
                {
                    Color.[|Blue|];
                }
            }

            enum Color
            {
                Red = int.MinValue + 100
            }
            """,
            """
            class Program
            {
                void Main()
                {
                    Color.Blue;
                }
            }

            enum Color
            {
                Red = int.MinValue + 100,
                Blue = -2147483547
            }
            """);

    [Fact]
    public Task TestGenerateAfterEnumWithByteMaxValue()
        => TestInRegularAndScriptAsync(
            """
            class Program
            {
                void Main()
                {
                    Color.[|Blue|];
                }
            }

            enum Color : byte
            {
                Red = 255
            }
            """,
            """
            class Program
            {
                void Main()
                {
                    Color.Blue;
                }
            }

            enum Color : byte
            {
                Red = 255,
                Blue = 0
            }
            """);

    [Fact]
    public Task TestGenerateIntoBitshiftEnum1()
        => TestInRegularAndScriptAsync(
            """
            class Program
            {
                void Main()
                {
                    Color.[|Blue|];
                }
            }

            enum Color
            {
                Red = 1 << 1,
                Green = 1 << 2
            }
            """,
            """
            class Program
            {
                void Main()
                {
                    Color.Blue;
                }
            }

            enum Color
            {
                Red = 1 << 1,
                Green = 1 << 2,
                Blue = 1 << 3
            }
            """);

    [Fact]
    public Task TestGenerateIntoBitshiftEnum2()
        => TestInRegularAndScriptAsync(
            """
            class Program
            {
                void Main()
                {
                    Color.[|Blue|];
                }
            }

            enum Color
            {
                Red = 2 >> 1
            }
            """,
            """
            class Program
            {
                void Main()
                {
                    Color.Blue;
                }
            }

            enum Color
            {
                Red = 2 >> 1,
                Blue = 2
            }
            """);

    [Fact]
    public Task TestStandaloneReference()
        => TestInRegularAndScriptAsync(
            """
            class Program
            {
                void Main()
                {
                    Color.[|Blue|];
                }
            }

            enum Color
            {
                Red = int.MinValue,
                Green = 1
            }
            """,
            """
            class Program
            {
                void Main()
                {
                    Color.Blue;
                }
            }

            enum Color
            {
                Red = int.MinValue,
                Green = 1,
                Blue = 2
            }
            """);

    [Fact]
    public Task TestCircularEnumsForErrorTolerance()
        => TestInRegularAndScriptAsync(
            """
            class Program
            {
                void Main()
                {
                    Circular.[|C|];
                }
            }

            enum Circular
            {
                A = B,
                B
            }
            """,
            """
            class Program
            {
                void Main()
                {
                    Circular.C;
                }
            }

            enum Circular
            {
                A = B,
                B,
                C
            }
            """);

    [Fact]
    public Task TestEnumWithIncorrectValueForErrorTolerance()
        => TestInRegularAndScriptAsync(
            """
            class Program
            {
                void Main()
                {
                    Circular.[|B|];
                }
            }

            enum Circular : byte
            {
                A = -2
            }
            """,
            """
            class Program
            {
                void Main()
                {
                    Circular.B;
                }
            }

            enum Circular : byte
            {
                A = -2,
                B
            }
            """);

    [Fact]
    public Task TestGenerateIntoNewEnum()
        => TestInRegularAndScriptAsync(
            """
            class B : A
            {
                void Main()
                {
                    BaseColor.[|Blue|];
                }

                public new enum BaseColor
                {
                    Yellow = 3
                }
            }

            class A
            {
                public enum BaseColor
                {
                    Red = 1,
                    Green = 2
                }
            }
            """,
            """
            class B : A
            {
                void Main()
                {
                    BaseColor.Blue;
                }

                public new enum BaseColor
                {
                    Yellow = 3,
                    Blue = 4
                }
            }

            class A
            {
                public enum BaseColor
                {
                    Red = 1,
                    Green = 2
                }
            }
            """);

    [Fact]
    public Task TestGenerateIntoDerivedEnumMissingNewKeyword()
        => TestInRegularAndScriptAsync(
            """
            class B : A
            {
                void Main()
                {
                    BaseColor.[|Blue|];
                }

                public enum BaseColor
                {
                    Yellow = 3
                }
            }

            class A
            {
                public enum BaseColor
                {
                    Red = 1,
                    Green = 2
                }
            }
            """,
            """
            class B : A
            {
                void Main()
                {
                    BaseColor.Blue;
                }

                public enum BaseColor
                {
                    Yellow = 3,
                    Blue = 4
                }
            }

            class A
            {
                public enum BaseColor
                {
                    Red = 1,
                    Green = 2
                }
            }
            """);

    [Fact]
    public Task TestGenerateIntoBaseEnum()
        => TestInRegularAndScriptAsync(
            """
            class B : A
            {
                void Main()
                {
                    BaseColor.[|Blue|];
                }
            }

            class A
            {
                public enum BaseColor
                {
                    Red = 1,
                    Green = 2
                }
            }
            """,
            """
            class B : A
            {
                void Main()
                {
                    BaseColor.Blue;
                }
            }

            class A
            {
                public enum BaseColor
                {
                    Red = 1,
                    Green = 2,
                    Blue = 3
                }
            }
            """);

    [Fact]
    public Task TestGenerationWhenMembersShareValues()
        => TestInRegularAndScriptAsync(
            """
            class Program
            {
                void Main()
                {
                    Color.[|Blue|];
                }
            }

            enum Color
            {
                Red,
                Green,
                Yellow = Green
            }
            """,
            """
            class Program
            {
                void Main()
                {
                    Color.Blue;
                }
            }

            enum Color
            {
                Red,
                Green,
                Yellow = Green,
                Blue = 2
            }
            """);

    [Fact]
    public Task TestInvokeFromAddAssignmentStatement()
        => TestInRegularAndScriptAsync(
            """
            class Program
            {
                void Main()
                {
                    int a = 1;
                    a += Color.[|Blue|];
                }
            }

            enum Color
            {
                Red,
                Green = 10,
                Yellow
            }
            """,
            """
            class Program
            {
                void Main()
                {
                    int a = 1;
                    a += Color.Blue;
                }
            }

            enum Color
            {
                Red,
                Green = 10,
                Yellow,
                Blue
            }
            """);

    [Fact]
    public Task TestFormatting()
        => TestInRegularAndScriptAsync(
            """
            class Program
            {
                static void Main(string[] args)
                {
                    Weekday.[|Tuesday|];
                }
            }
            enum Weekday
            {
                Monday
            }
            """,
            """
            class Program
            {
                static void Main(string[] args)
                {
                    Weekday.Tuesday;
                }
            }
            enum Weekday
            {
                Monday,
                Tuesday
            }
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540919")]
    public Task TestKeyword()
        => TestInRegularAndScriptAsync(
            """
            class Program
            {
                static void Main(string[] args)
                {
                    Color.[|@enum|];
                }
            }

            enum Color
            {
                Red
            }
            """,
            """
            class Program
            {
                static void Main(string[] args)
                {
                    Color.@enum;
                }
            }

            enum Color
            {
                Red,
                @enum
            }
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544333")]
    public Task TestNotAfterPointer()
        => TestMissingInRegularAndScriptAsync(
            """
            struct MyStruct
            {
                public int MyField;
            }

            class Program
            {
                static unsafe void Main(string[] args)
                {
                    MyStruct s = new MyStruct();
                    MyStruct* ptr = &s;
                    var i1 = (() => &s)->[|M|];
                }
            }
            """);

    [Fact]
    public Task TestMissingOnHiddenEnum()
        => TestMissingInRegularAndScriptAsync(
            """
            using System;

            enum E
            {
            #line hidden
            }
            #line default

            class Program
            {
                void Main()
                {
                    Console.WriteLine(E.[|x|]);
                }
            }
            """);

    [Fact]
    public Task TestMissingOnPartiallyHiddenEnum()
        => TestMissingInRegularAndScriptAsync(
            """
            using System;

            enum E
            {
                A,
                B,
                C,
            #line hidden
            }
            #line default

            class Program
            {
                void Main()
                {
                    Console.WriteLine(E.[|x|]);
                }
            }
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545903")]
    public Task TestNoOctal()
        => TestInRegularAndScriptAsync(
            """
            enum E
            {
                A = 007,
            }

            class C
            {
                E x = E.[|B|];
            }
            """,
            """
            enum E
            {
                A = 007,
                B = 8,
            }

            class C
            {
                E x = E.B;
            }
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546654")]
    public Task TestLastValueDoesNotHaveInitializer()
        => TestInRegularAndScriptAsync(
            """
            enum E
            {
                A = 1,
                B
            }

            class Program
            {
                void Main()
                {
                    E.[|C|] }
            }
            """,
            """
            enum E
            {
                A = 1,
                B,
                C
            }

            class Program
            {
                void Main()
                {
                    E.C }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/49679")]
    public Task TestWithLeftShift_Long()
        => TestInRegularAndScriptAsync(
            """
            class Program
            {
                void Main()
                {
                    Color.[|Blue|];
                }
            }

            enum Color : long
            {
                Green = 1L << 0
            }
            """,
            """
            class Program
            {
                void Main()
                {
                    Color.Blue;
                }
            }

            enum Color : long
            {
                Green = 1L << 0,
                Blue = 1L << 1
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/49679")]
    public Task TestWithLeftShift_UInt()
        => TestInRegularAndScriptAsync(
            """
            class Program
            {
                void Main()
                {
                    Color.[|Blue|];
                }
            }

            enum Color : uint
            {
                Green = 1u << 0
            }
            """,
            """
            class Program
            {
                void Main()
                {
                    Color.Blue;
                }
            }

            enum Color : uint
            {
                Green = 1u << 0,
                Blue = 1u << 1
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/49679")]
    public Task TestWithLeftShift_ULong()
        => TestInRegularAndScriptAsync(
            """
            class Program
            {
                void Main()
                {
                    Color.[|Blue|];
                }
            }

            enum Color : ulong
            {
                Green = 1UL << 0
            }
            """,
            """
            class Program
            {
                void Main()
                {
                    Color.Blue;
                }
            }

            enum Color : ulong
            {
                Green = 1UL << 0,
                Blue = 1UL << 1
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/5468")]
    public Task TestWithColorColorConflict1()
        => TestInRegularAndScriptAsync(
            """
            enum Color
            {
                Blue,
                Green
            }

            class Sample1
            {
                Color Color => Color.[|Red|];

                void Method()
                {
                    if (Color == Color.Red) { } 
                }
            }
            """,
            """
            enum Color
            {
                Blue,
                Green,
                Red
            }

            class Sample1
            {
                Color Color => Color.Red;

                void Method()
                {
                    if (Color == Color.Red) { } 
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/5468")]
    public Task TestWithColorColorConflict2()
        => TestInRegularAndScriptAsync(
            """
            enum Color
            {
                Blue,
                Green
            }

            class Sample1
            {
                Color Color => Color.Red;

                void Method()
                {
                    if (Color == Color.[|Red|]) { } 
                }
            }
            """,
            """
            enum Color
            {
                Blue,
                Green,
                Red
            }

            class Sample1
            {
                Color Color => Color.Red;

                void Method()
                {
                    if (Color == Color.Red) { } 
                }
            }
            """);
}
