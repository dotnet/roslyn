// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.CodeFixes.GenerateEnumMember;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Diagnostics.GenerateEnumMember
{
    [Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEnumMember)]
    public class GenerateEnumMemberTests : AbstractCSharpDiagnosticProviderBasedUserDiagnosticTest
    {
        public GenerateEnumMemberTests(ITestOutputHelper logger)
           : base(logger)
        {
        }

        internal override (DiagnosticAnalyzer, CodeFixProvider) CreateDiagnosticProviderAndFixer(Workspace workspace)
            => (null, new GenerateEnumMemberCodeFixProvider());

        [Fact]
        public async Task TestEmptyEnum()
        {
            await TestInRegularAndScriptAsync(
@"class Program
{
    void Main()
    {
        Color.[|Red|];
    }
}

enum Color
{
}",
@"class Program
{
    void Main()
    {
        Color.Red;
    }
}

enum Color
{
    Red
}");
        }

        [Fact]
        public async Task TestWithSingleMember()
        {
            await TestInRegularAndScriptAsync(
@"class Program
{
    void Main()
    {
        Color.[|Blue|];
    }
}

enum Color
{
    Red
}",
@"class Program
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
}");
        }

        [Fact]
        public async Task TestWithExistingComma()
        {
            await TestInRegularAndScriptAsync(
@"class Program
{
    void Main()
    {
        Color.[|Blue|];
    }
}

enum Color
{
    Red,
}",
@"class Program
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
}");
        }

        [Fact]
        public async Task TestWithMultipleMembers()
        {
            await TestInRegularAndScriptAsync(
@"class Program
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
}",
@"class Program
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
}");
        }

        [Fact]
        public async Task TestWithZero()
        {
            await TestInRegularAndScriptAsync(
@"class Program
{
    void Main()
    {
        Color.[|Blue|];
    }
}

enum Color
{
    Red = 0
}",
@"class Program
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
}");
        }

        [Fact]
        public async Task TestWithIntegralValue()
        {
            await TestInRegularAndScriptAsync(
@"class Program
{
    void Main()
    {
        Color.[|Blue|];
    }
}

enum Color
{
    Red = 1
}",
@"class Program
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
}");
        }

        [Fact]
        public async Task TestWithSingleBitIntegral()
        {
            await TestInRegularAndScriptAsync(
@"class Program
{
    void Main()
    {
        Color.[|Blue|];
    }
}

enum Color
{
    Red = 2
}",
@"class Program
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
}");
        }

        [Fact]
        public async Task TestGenerateIntoGeometricSequence()
        {
            await TestInRegularAndScriptAsync(
@"class Program
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
}",
@"class Program
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
}");
        }

        [Fact]
        public async Task TestWithSimpleSequence1()
        {
            await TestInRegularAndScriptAsync(
@"class Program
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
}",
@"class Program
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
}");
        }

        [Fact]
        public async Task TestWithSimpleSequence2()
        {
            await TestInRegularAndScriptAsync(
@"class Program
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
}",
@"class Program
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
}");
        }

        [Fact]
        public async Task TestWithNonZeroInteger()
        {
            await TestInRegularAndScriptAsync(
@"class Program
{
    void Main()
    {
        Color.[|Blue|];
    }
}

enum Color
{
    Green = 5
}",
@"class Program
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
}");
        }

        [Fact]
        public async Task TestWithLeftShift0()
        {
            await TestInRegularAndScriptAsync(
@"class Program
{
    void Main()
    {
        Color.[|Blue|];
    }
}

enum Color
{
    Green = 1 << 0
}",
@"class Program
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
}");
        }

        [Fact]
        public async Task TestWithLeftShift5()
        {
            await TestInRegularAndScriptAsync(
@"class Program
{
    void Main()
    {
        Color.[|Blue|];
    }
}

enum Color
{
    Green = 1 << 5
}",
@"class Program
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
}");
        }

        [Fact]
        public async Task TestWithDifferentStyles()
        {
            await TestInRegularAndScriptAsync(
@"class Program
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
}",
@"class Program
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
}");
        }

        [Fact]
        public async Task TestBinary()
        {
            await TestInRegularAndScriptAsync(
@"class Program
{
    void Main()
    {
        Color.[|Blue|];
    }
}

enum Color
{
    Red = 0b01
}",
@"class Program
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
}");
        }

        [Fact]
        public async Task TestHex1()
        {
            await TestInRegularAndScriptAsync(
@"class Program
{
    void Main()
    {
        Color.[|Blue|];
    }
}

enum Color
{
    Red = 0x1
}",
@"class Program
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
}");
        }

        [Fact]
        public async Task TestHex9()
        {
            await TestInRegularAndScriptAsync(
@"class Program
{
    void Main()
    {
        Color.[|Blue|];
    }
}

enum Color
{
    Red = 0x9
}",
@"class Program
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
}");
        }

        [Fact]
        public async Task TestHexF()
        {
            await TestInRegularAndScriptAsync(
@"class Program
{
    void Main()
    {
        Color.[|Blue|];
    }
}

enum Color
{
    Red = 0xF
}",
@"class Program
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
}");
        }

        [Fact]
        public async Task TestGenerateAfterEnumWithIntegerMaxValue()
        {
            await TestInRegularAndScriptAsync(
@"class Program
{
    void Main()
    {
        Color.[|Blue|];
    }
}

enum Color
{
    Red = int.MaxValue
}",
@"class Program
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
}");
        }

        [Fact]
        public async Task TestUnsigned16BitEnums()
        {
            await TestInRegularAndScriptAsync(
@"class Program
{
    void Main()
    {
        Color.[|Blue|];
    }
}

enum Color : ushort
{
    Red = 65535
}",
@"class Program
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
}");
        }

        [Fact]
        public async Task TestGenerateEnumMemberOfTypeLong()
        {
            await TestInRegularAndScriptAsync(
@"class Program
{
    void Main()
    {
        Color.[|Blue|];
    }
}

enum Color : long
{
    Red = long.MaxValue
}",
@"class Program
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
}");
        }

        [Fact]
        public async Task TestGenerateAfterEnumWithLongMaxValueInBinary()
        {
            await TestInRegularAndScriptAsync(
@"class Program
{
    void Main()
    {
        Color.[|Blue|];
    }
}

enum Color : long
{
    Red = 0b0111111111111111111111111111111111111111111111111111111111111111
}",
@"class Program
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
}");
        }

        [Fact]
        public async Task TestGenerateAfterEnumWithLongMaxValueInHex()
        {
            await TestInRegularAndScriptAsync(
@"class Program
{
    void Main()
    {
        Color.[|Blue|];
    }
}

enum Color : long
{
    Red = 0x7FFFFFFFFFFFFFFF
}",
@"class Program
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
}");
        }

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/528312")]
        public async Task TestGenerateAfterEnumWithLongMinValueInHex()
        {
            await TestInRegularAndScriptAsync(
@"class Program
{
    void Main()
    {
        Color.[|Blue|];
    }
}

enum Color : long
{
    Red = 0xFFFFFFFFFFFFFFFF
}",
@"class Program
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
}");
        }

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/528312")]
        public async Task TestGenerateAfterPositiveLongInHex()
        {
            await TestInRegularAndScriptAsync(
@"class Program
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
}",
@"class Program
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
}");
        }

        [Fact]
        public async Task TestGenerateAfterPositiveLongExprInHex()
        {
            await TestInRegularAndScriptAsync(
@"class Program
{
    void Main()
    {
        Color.[|Blue|];
    }
}

enum Color : long
{
    Red = 0x414 / 2
}",
@"class Program
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
}");
        }

        [Fact]
        public async Task TestGenerateAfterEnumWithULongMaxValue()
        {
            await TestInRegularAndScriptAsync(
@"class Program
{
    void Main()
    {
        Color.[|Blue|];
    }
}

enum Color : ulong
{
    Red = ulong.MaxValue
}",
@"class Program
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
}");
        }

        [Fact]
        public async Task TestNegativeRangeIn64BitSignedEnums()
        {
            await TestInRegularAndScriptAsync(
@"class Program
{
    void Main()
    {
        Color.[|Blue|];
    }
}

enum Color : long
{
    Red = -10
}",
@"class Program
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
}");
        }

        [Fact]
        public async Task TestGenerateWithImplicitValues()
        {
            await TestInRegularAndScriptAsync(
@"class Program
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
}",
@"class Program
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
}");
        }

        [Fact]
        public async Task TestGenerateWithImplicitValues2()
        {
            await TestInRegularAndScriptAsync(
@"class Program
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
}",
@"class Program
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
}");
        }

        [Fact]
        public async Task TestNoExtraneousStatementTerminatorBeforeCommentedMember()
        {
            await TestInRegularAndScriptAsync(
@"class Program
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
}",
@"class Program
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
}");
        }

        [Fact]
        public async Task TestNoExtraneousStatementTerminatorBeforeCommentedMember2()
        {
            await TestInRegularAndScriptAsync(
@"class Program
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
}",
@"class Program
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
}");
        }

        [Fact]
        public async Task TestGenerateAfterEnumWithMinValue()
        {
            await TestInRegularAndScriptAsync(
@"class Program
{
    void Main()
    {
        Color.[|Blue|];
    }
}

enum Color
{
    Red = int.MinValue
}",
@"class Program
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
}");
        }

        [Fact]
        public async Task TestGenerateAfterEnumWithMinValuePlusConstant()
        {
            await TestInRegularAndScriptAsync(
@"class Program
{
    void Main()
    {
        Color.[|Blue|];
    }
}

enum Color
{
    Red = int.MinValue + 100
}",
@"class Program
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
}");
        }

        [Fact]
        public async Task TestGenerateAfterEnumWithByteMaxValue()
        {
            await TestInRegularAndScriptAsync(
@"class Program
{
    void Main()
    {
        Color.[|Blue|];
    }
}

enum Color : byte
{
    Red = 255
}",
@"class Program
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
}");
        }

        [Fact]
        public async Task TestGenerateIntoBitshiftEnum1()
        {
            await TestInRegularAndScriptAsync(
@"class Program
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
}",
@"class Program
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
}");
        }

        [Fact]
        public async Task TestGenerateIntoBitshiftEnum2()
        {
            await TestInRegularAndScriptAsync(
@"class Program
{
    void Main()
    {
        Color.[|Blue|];
    }
}

enum Color
{
    Red = 2 >> 1
}",
@"class Program
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
}");
        }

        [Fact]
        public async Task TestStandaloneReference()
        {
            await TestInRegularAndScriptAsync(
@"class Program
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
}",
@"class Program
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
}");
        }

        [Fact]
        public async Task TestCircularEnumsForErrorTolerance()
        {
            await TestInRegularAndScriptAsync(
@"class Program
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
}",
@"class Program
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
}");
        }

        [Fact]
        public async Task TestEnumWithIncorrectValueForErrorTolerance()
        {
            await TestInRegularAndScriptAsync(
@"class Program
{
    void Main()
    {
        Circular.[|B|];
    }
}

enum Circular : byte
{
    A = -2
}",
@"class Program
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
}");
        }

        [Fact]
        public async Task TestGenerateIntoNewEnum()
        {
            await TestInRegularAndScriptAsync(
@"class B : A
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
}",
@"class B : A
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
}");
        }

        [Fact]
        public async Task TestGenerateIntoDerivedEnumMissingNewKeyword()
        {
            await TestInRegularAndScriptAsync(
@"class B : A
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
}",
@"class B : A
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
}");
        }

        [Fact]
        public async Task TestGenerateIntoBaseEnum()
        {
            await TestInRegularAndScriptAsync(
@"class B : A
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
}",
@"class B : A
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
}");
        }

        [Fact]
        public async Task TestGenerationWhenMembersShareValues()
        {
            await TestInRegularAndScriptAsync(
@"class Program
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
}",
@"class Program
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
}");
        }

        [Fact]
        public async Task TestInvokeFromAddAssignmentStatement()
        {
            await TestInRegularAndScriptAsync(
@"class Program
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
}",
@"class Program
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
}");
        }

        [Fact]
        public async Task TestFormatting()
        {
            await TestInRegularAndScriptAsync(
@"class Program
{
    static void Main(string[] args)
    {
        Weekday.[|Tuesday|];
    }
}
enum Weekday
{
    Monday
}",
@"class Program
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
}");
        }

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540919")]
        public async Task TestKeyword()
        {
            await TestInRegularAndScriptAsync(
@"class Program
{
    static void Main(string[] args)
    {
        Color.[|@enum|];
    }
}

enum Color
{
    Red
}",
@"class Program
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
}");
        }

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544333")]
        public async Task TestNotAfterPointer()
        {
            await TestMissingInRegularAndScriptAsync(
@"struct MyStruct
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
}");
        }

        [Fact]
        public async Task TestMissingOnHiddenEnum()
        {
            await TestMissingInRegularAndScriptAsync(
@"using System;

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
}");
        }

        [Fact]
        public async Task TestMissingOnPartiallyHiddenEnum()
        {
            await TestMissingInRegularAndScriptAsync(
@"using System;

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
}");
        }

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545903")]
        public async Task TestNoOctal()
        {
            await TestInRegularAndScriptAsync(
@"enum E
{
    A = 007,
}

class C
{
    E x = E.[|B|];
}",
@"enum E
{
    A = 007,
    B = 8,
}

class C
{
    E x = E.B;
}");
        }

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546654")]
        public async Task TestLastValueDoesNotHaveInitializer()
        {
            await TestInRegularAndScriptAsync(
@"enum E
{
    A = 1,
    B
}

class Program
{
    void Main()
    {
        E.[|C|] }
}",
@"enum E
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
}");
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/49679")]
        public async Task TestWithLeftShift_Long()
        {
            await TestInRegularAndScriptAsync(
@"class Program
{
    void Main()
    {
        Color.[|Blue|];
    }
}

enum Color : long
{
    Green = 1L << 0
}",
@"class Program
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
}");
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/49679")]
        public async Task TestWithLeftShift_UInt()
        {
            await TestInRegularAndScriptAsync(
@"class Program
{
    void Main()
    {
        Color.[|Blue|];
    }
}

enum Color : uint
{
    Green = 1u << 0
}",
@"class Program
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
}");
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/49679")]
        public async Task TestWithLeftShift_ULong()
        {
            await TestInRegularAndScriptAsync(
@"class Program
{
    void Main()
    {
        Color.[|Blue|];
    }
}

enum Color : ulong
{
    Green = 1UL << 0
}",
@"class Program
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
}");
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/5468")]
        public async Task TestWithColorColorConflict1()
        {
            await TestInRegularAndScriptAsync(
@"enum Color
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
",
@"enum Color
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
");
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/5468")]
        public async Task TestWithColorColorConflict2()
        {
            await TestInRegularAndScriptAsync(
@"enum Color
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
",
@"enum Color
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
");
        }
    }
}
