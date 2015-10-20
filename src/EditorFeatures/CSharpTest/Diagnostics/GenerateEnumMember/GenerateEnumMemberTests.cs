// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.CodeFixes.GenerateEnumMember;
using Microsoft.CodeAnalysis.Diagnostics;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Diagnostics.GenerateEnumMember
{
    public class GenerateEnumMemberTests : AbstractCSharpDiagnosticProviderBasedUserDiagnosticTest
    {
        internal override Tuple<DiagnosticAnalyzer, CodeFixProvider> CreateDiagnosticProviderAndFixer(Workspace workspace)
        {
            return new Tuple<DiagnosticAnalyzer, CodeFixProvider>(null, new GenerateEnumMemberCodeFixProvider());
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEnumMember)]
        public void TestEmptyEnum()
        {
            Test(
@"class Program { void Main ( ) { Color . [|Red|] ; } } enum Color { } ",
@"class Program { void Main ( ) { Color . Red ; } } enum Color { Red } ");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEnumMember)]
        public void TestWithSingleMember()
        {
            Test(
@"class Program { void Main ( ) { Color . [|Blue|] ; } } enum Color { Red } ",
@"class Program { void Main ( ) { Color . Blue ; } } enum Color { Red , Blue } ");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEnumMember)]
        public void TestWithExistingComma()
        {
            Test(
@"class Program { void Main ( ) { Color . [|Blue|] ; } } enum Color { Red , } ",
@"class Program { void Main ( ) { Color . Blue ; } } enum Color { Red , Blue , } ");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEnumMember)]
        public void TestWithMultipleMembers()
        {
            Test(
@"class Program { void Main ( ) { Color . [|Green|] ; } } enum Color { Red , Blue } ",
@"class Program { void Main ( ) { Color . Green ; } } enum Color { Red , Blue , Green } ");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEnumMember)]
        public void TestWithZero()
        {
            Test(
@"class Program { void Main ( ) { Color . [|Blue|] ; } } enum Color { Red = 0 } ",
@"class Program { void Main ( ) { Color . Blue ; } } enum Color { Red = 0 , Blue = 1 } ");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEnumMember)]
        public void TestWithIntegralValue()
        {
            Test(
@"class Program { void Main ( ) { Color . [|Blue|] ; } } enum Color { Red = 1 } ",
@"class Program { void Main ( ) { Color . Blue ; } } enum Color { Red = 1 , Blue = 2 } ");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEnumMember)]
        public void TestWithSingleBitIntegral()
        {
            Test(
@"class Program { void Main ( ) { Color . [|Blue|] ; } } enum Color { Red = 2 } ",
@"class Program { void Main ( ) { Color . Blue ; } } enum Color { Red = 2 , Blue = 4 } ");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEnumMember)]
        public void TestGenerateIntoGeometricSequence()
        {
            Test(
@"class Program { void Main ( ) { Color . [|Blue|] ; } } enum Color { Red = 1 , Yellow = 2 , Green = 4 }",
@"class Program { void Main ( ) { Color . Blue ; } } enum Color { Red = 1 , Yellow = 2 , Green = 4 , Blue = 8}");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEnumMember)]
        public void TestWithSimpleSequence1()
        {
            Test(
@"class Program { void Main ( ) { Color . [|Blue|] ; } } enum Color { Red = 1 , Green = 2 } ",
@"class Program { void Main ( ) { Color . Blue ; } } enum Color { Red = 1 , Green = 2 , Blue = 3 } ");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEnumMember)]
        public void TestWithSimpleSequence2()
        {
            Test(
@"class Program { void Main ( ) { Color . [|Blue|] ; } } enum Color { Yellow = 0, Red = 1 , Green = 2 } ",
@"class Program { void Main ( ) { Color . Blue ; } } enum Color { Yellow = 0, Red = 1 , Green = 2 , Blue = 3 } ");
        }

        [Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEnumMember)]
        public void TestWithNonZeroInteger()
        {
            Test(
@"class Program { void Main ( ) { Color . [|Blue|] ; } } enum Color { Green = 5 } ",
@"class Program { void Main ( ) { Color . Blue ; } } enum Color { Green = 5 , Blue = 6 } ");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEnumMember)]
        public void TestWithLeftShift0()
        {
            Test(
@"class Program { void Main ( ) { Color . [|Blue|] ; } } enum Color { Green = 1 << 0 } ",
@"class Program { void Main ( ) { Color . Blue ; } } enum Color { Green = 1 << 0 , Blue = 1 << 1 } ");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEnumMember)]
        public void TestWithLeftShift5()
        {
            Test(
@"class Program { void Main ( ) { Color . [|Blue|] ; } } enum Color { Green = 1 << 5 } ",
@"class Program { void Main ( ) { Color . Blue ; } } enum Color { Green = 1 << 5 , Blue = 1 << 6 } ");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEnumMember)]
        public void TestWithDifferentStyles()
        {
            Test(
@"class Program { void Main ( ) { Color . [|Blue|] ; } } enum Color { Red = 2 , Green = 1 << 5 } ",
@"class Program { void Main ( ) { Color . Blue ; } } enum Color { Red = 2 , Green = 1 << 5 , Blue = 33 } ");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEnumMember)]
        public void TestHex1()
        {
            Test(
@"class Program { void Main ( ) { Color . [|Blue|] ; } } enum Color { Red = 0x1 } ",
@"class Program { void Main ( ) { Color . Blue ; } } enum Color { Red = 0x1 , Blue = 0x2 } ");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEnumMember)]
        public void TestHex9()
        {
            Test(
@"class Program { void Main ( ) { Color . [|Blue|] ; } } enum Color { Red = 0x9 } ",
@"class Program { void Main ( ) { Color . Blue ; } } enum Color { Red = 0x9 , Blue = 0xA } ");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEnumMember)]
        public void TestHexF()
        {
            Test(
@"class Program { void Main ( ) { Color . [|Blue|] ; } } enum Color { Red = 0xF } ",
@"class Program { void Main ( ) { Color . Blue ; } } enum Color { Red = 0xF , Blue = 0x10 } ");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEnumMember)]
        public void TestGenerateAfterEnumWithIntegerMaxValue()
        {
            Test(
@"class Program { void Main ( ) { Color . [|Blue|] ; } } enum Color { Red = int.MaxValue } ",
@"class Program { void Main ( ) { Color . Blue ; } } enum Color { Red = int.MaxValue , Blue = int.MinValue } ");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEnumMember)]
        public void TestUnsigned16BitEnums()
        {
            Test(
@"class Program { void Main ( ) { Color . [|Blue|] ; } } enum Color : ushort { Red = 65535 } ",
@"class Program { void Main ( ) { Color . Blue ; } } enum Color : ushort { Red = 65535 , Blue = 0 } ");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEnumMember)]
        public void TestGenerateEnumMemberOfTypeLong()
        {
            Test(
@"class Program { void Main ( ) { Color . [|Blue|] ; } } enum Color : long { Red = long.MaxValue } ",
@"class Program { void Main ( ) { Color . Blue ; } } enum Color : long { Red = long.MaxValue , Blue = long.MinValue } ");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEnumMember)]
        public void TestGenerateAfterEnumWithLongMaxValueInHex()
        {
            Test(
@"class Program { void Main ( ) { Color . [|Blue|] ; } } enum Color : long { Red = 0x7FFFFFFFFFFFFFFF } ",
@"class Program { void Main ( ) { Color . Blue ; } } enum Color : long { Red = 0x7FFFFFFFFFFFFFFF , Blue = 0x8000000000000000 } ");
        }

        [WorkItem(528312)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEnumMember)]
        public void TestGenerateAfterEnumWithLongMinValueInHex()
        {
            Test(
@"class Program { void Main ( ) { Color . [|Blue|] ; } } enum Color : long { Red = 0xFFFFFFFFFFFFFFFF } ",
@"class Program { void Main ( ) { Color . Blue ; } } enum Color : long { Red = 0xFFFFFFFFFFFFFFFF , Blue} ");
        }

        [WorkItem(528312)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEnumMember)]
        public void TestGenerateAfterPositiveLongInHex()
        {
            Test(
@"class Program { void Main ( ) { Color . [|Blue|] ; } } enum Color : long { Red = 0xFFFFFFFFFFFFFFFF , Green = 0x0 } ",
@"class Program { void Main ( ) { Color . Blue ; } } enum Color : long { Red = 0xFFFFFFFFFFFFFFFF , Green = 0x0 , Blue = 0x1 } ");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEnumMember)]
        public void TestGenerateAfterPositiveLongExprInHex()
        {
            Test(
@"class Program { void Main ( ) { Color . [|Blue|] ; } } enum Color : long { Red = 0x414 / 2 } ",
@"class Program { void Main ( ) { Color . Blue ; } } enum Color : long { Red = 0x414 / 2 , Blue = 523 } ");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEnumMember)]
        public void TestGenerateAfterEnumWithULongMaxValue()
        {
            Test(
@"class Program { void Main ( ) { Color . [|Blue|] ; } } enum Color : ulong { Red = ulong.MaxValue } ",
@"class Program { void Main ( ) { Color . Blue ; } } enum Color : ulong { Red = ulong.MaxValue , Blue = 0 } ");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEnumMember)]
        public void TestNegativeRangeIn64BitSignedEnums()
        {
            Test(
@"class Program { void Main ( ) { Color . [|Blue|] ; } } enum Color : long { Red = -10 } ",
@"class Program { void Main ( ) { Color . Blue ; } } enum Color : long { Red = -10 , Blue = -9 } ");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEnumMember)]
        public void TestGenerateWithImplicitValues()
        {
            Test(
@"class Program { void Main ( ) { Color . [|Blue|] ; } } enum Color { Red , Green , Yellow = -1 }",
@"class Program { void Main ( ) { Color . Blue ; } } enum Color { Red , Green , Yellow = -1 , Blue = 2 }");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEnumMember)]
        public void TestGenerateWithImplicitValues2()
        {
            Test(
@"class Program { void Main ( ) { Color . [|Blue|] ; } } enum Color { Red , Green = 10 , Yellow }",
@"class Program { void Main ( ) { Color . Blue ; } } enum Color { Red , Green = 10 , Yellow , Blue }");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEnumMember)]
        public void TestNoExtraneousStatementTerminatorBeforeCommentedMember()
        {
            Test(
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
}",
compareTokens: false);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEnumMember)]
        public void TestNoExtraneousStatementTerminatorBeforeCommentedMember2()
        {
            Test(
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
}",
compareTokens: false);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEnumMember)]
        public void TestGenerateAfterEnumWithMinValue()
        {
            Test(
@"class Program { void Main ( ) { Color . [|Blue|] ; } } enum Color { Red = int.MinValue } ",
@"class Program { void Main ( ) { Color . Blue ; } } enum Color { Red = int.MinValue , Blue = -2147483647 } ");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEnumMember)]
        public void TestGenerateAfterEnumWithMinValuePlusConstant()
        {
            Test(
@"class Program { void Main ( ) { Color . [|Blue|] ; } } enum Color { Red = int.MinValue + 100 } ",
@"class Program { void Main ( ) { Color . Blue ; } } enum Color { Red = int.MinValue + 100 , Blue = -2147483547 } ");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEnumMember)]
        public void TestGenerateAfterEnumWithByteMaxValue()
        {
            Test(
@"class Program { void Main ( ) { Color . [|Blue|] ; } } enum Color : byte { Red = 255 }",
@"class Program { void Main ( ) { Color . Blue ; } } enum Color : byte { Red = 255 , Blue = 0 }");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEnumMember)]
        public void TestGenerateIntoBitshiftEnum1()
        {
            Test(
@"class Program { void Main ( ) { Color . [|Blue|] ; } } enum Color { Red = 1 << 1 , Green = 1 << 2 }",
@"class Program { void Main ( ) { Color . Blue ; } } enum Color { Red = 1 << 1 , Green = 1 << 2 , Blue = 1 << 3 }");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEnumMember)]
        public void TestGenerateIntoBitshiftEnum2()
        {
            Test(
@"class Program { void Main ( ) { Color . [|Blue|] ; } } enum Color { Red = 2 >> 1 }",
@"class Program { void Main ( ) { Color . Blue ; } } enum Color { Red = 2 >> 1 , Blue = 2 }");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEnumMember)]
        public void TestStandaloneReference()
        {
            Test(
@"class Program { void Main ( ) { Color . [|Blue|] ; } } enum Color { Red = int.MinValue , Green = 1 }",
@"class Program { void Main ( ) { Color . Blue ; } } enum Color { Red = int.MinValue , Green = 1 , Blue = 2 }");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEnumMember)]
        public void TestCircularEnumsForErrorTolerance()
        {
            Test(
@"class Program { void Main ( ) { Circular . [|C|] ; } } enum Circular { A = B , B }",
@"class Program { void Main ( ) { Circular . C ; } } enum Circular { A = B , B , C }");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEnumMember)]
        public void TestEnumWithIncorrectValueForErrorTolerance()
        {
            Test(
@"class Program { void Main ( ) { Circular . [|B|] ; } } enum Circular : byte { A = -2 }",
@"class Program { void Main ( ) { Circular . B ; } } enum Circular : byte { A = -2 , B }");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEnumMember)]
        public void TestGenerateIntoNewEnum()
        {
            Test(
@"class B : A { void Main ( ) { BaseColor . [|Blue|] ; } public new enum BaseColor { Yellow = 3 } } class A { public enum BaseColor { Red = 1, Green = 2 } }",
@"class B : A { void Main ( ) { BaseColor . Blue ; } public new enum BaseColor { Yellow = 3 , Blue = 4 } } class A { public enum BaseColor { Red = 1, Green = 2 } }");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEnumMember)]
        public void TestGenerateIntoDerivedEnumMissingNewKeyword()
        {
            Test(
@"class B : A { void Main ( ) { BaseColor . [|Blue|] ; } public enum BaseColor { Yellow = 3 } } class A { public enum BaseColor { Red = 1, Green = 2 } }",
@"class B : A { void Main ( ) { BaseColor . Blue ; } public enum BaseColor { Yellow = 3 , Blue = 4 } } class A { public enum BaseColor { Red = 1, Green = 2 } }");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEnumMember)]
        public void TestGenerateIntoBaseEnum()
        {
            Test(
@"class B : A { void Main ( ) { BaseColor . [|Blue|] ; } } class A { public enum BaseColor { Red = 1, Green = 2 } }",
@"class B : A { void Main ( ) { BaseColor . Blue ; } } class A { public enum BaseColor { Red = 1, Green = 2 , Blue = 3 } }");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEnumMember)]
        public void TestGenerationWhenMembersShareValues()
        {
            Test(
@"class Program { void Main ( ) { Color . [|Blue|] ; } } enum Color { Red , Green , Yellow = Green }",
@"class Program { void Main ( ) { Color . Blue ; } } enum Color { Red , Green , Yellow = Green , Blue = 2 }");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEnumMember)]
        public void TestInvokeFromAddAssignmentStatement()
        {
            Test(
@"class Program { void Main ( ) { int a = 1 ; a += Color . [|Blue|] ; } } enum Color { Red , Green = 10 , Yellow }",
@"class Program { void Main ( ) { int a = 1 ; a += Color . Blue ; } } enum Color { Red , Green = 10 , Yellow , Blue }");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEnumMember)]
        public void TestFormatting()
        {
            Test(
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
}",
compareTokens: false);
        }

        [WorkItem(540919)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEnumMember)]
        public void TestKeyword()
        {
            Test(
@"class Program { static void Main ( string [ ] args ) { Color . [|@enum|] ; } } enum Color { Red } ",
@"class Program { static void Main ( string [ ] args ) { Color . @enum ; } } enum Color { Red , @enum } ");
        }

        [WorkItem(544333)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEnumMember)]
        public void TestNotAfterPointer()
        {
            TestMissing(
@"struct MyStruct { public int MyField ; } class Program { static unsafe void Main ( string [ ] args ) { MyStruct s = new MyStruct ( ) ; MyStruct * ptr = & s ; var i1 = ( ( ) => & s ) -> [|M|] ; } } ");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEnumMember)]
        public void TestMissingOnHiddenEnum()
        {
            TestMissing(
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

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEnumMember)]
        public void TestMissingOnPartiallyHiddenEnum()
        {
            TestMissing(
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

        [WorkItem(545903)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEnumMember)]
        public void TestNoOctal()
        {
            Test(
@"enum E { A = 007 , } class C { E x = E . [|B|] ; } ",
@"enum E { A = 007 , B = 8 , } class C { E x = E . B ; } ");
        }

        [WorkItem(546654)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateEnumMember)]
        public void TestLastValueDoesNotHaveInitializer()
        {
            Test(
@"enum E { A = 1 , B } class Program { void Main ( ) { E . [|C|] } } ",
@"enum E { A = 1 , B , C } class Program { void Main ( ) { E . C } } ");
        }
    }
}
