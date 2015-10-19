// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public class ConstantTests : CompilingTestBase
    {
        [Fact]
        public void TestConstantFolding()
        {
            // TODO: explicit conversions
            // TODO: constants from metadata
            // TODO: char, byte, sbyte, short, ushort, long, ulong

            var source =
@"using System;
class C
{
    void M()
    {
        const int x = ((1 + 2 * 3) / (4 >> 1) ) << (5 % 3);
        const double y = (1.0 + (11.0 % (2.0 * 3.0))) / 4.0;
        const bool z = (1 < 2) & (3 > 2) & (10.0 < 11.0) & (120m >= 13m) & (3 != 4) & (""hello"" == ""hello"");

        const char c = (char)('a' + 2);

        const long lng1 = -2147483648U;
        const long lng2 = -2147483648u;
        const long lng3 = -2147483648L;
        const long lng4 = -2147483648l;
        const int minint = -2147483648 + 0;
        const long minlong = -9223372036854775808 + 0;

        const string s1 = ""hello"" + ""goodbye"";
        string s2 = ""abc"" + 123; // This is NOT a constant because it involves a boxing conversion.
        const string s3 = ""not null"" + null;
        const bool b = (null == null) & (null != null); 
        const bool b2 = b & !b;
        const string s4 = s1 + s3;

        // According to the spec these are not constants but according to the native compiler
        // they are; we preserve this bug in Roslyn.

        const byte zero1 = new byte();
        const ushort zero2 = new ushort();
        const uint zero3 = new uint();
        const ulong zero4 = new ulong();
        const sbyte zero5 = new sbyte();
        const short zero6 = new short();
        const int zero7 = new int();
        const long zero8 = new long();
        const decimal zero9 = new decimal();
        const double zero10 = new double();
        const float zero11 = new float();
        const char zero12 = new char();
        const DayOfWeek zero13 = new DayOfWeek();

        const long negUnsigned = -2u;

        const decimal dec1 = 100.0m % 3m;
        const decimal dec2 = 100.0m / 3m;
        const decimal dec3 = 100.0m - 50.00m;
        const double unplus1 = +123.4d;
        const float unplus2 = +123.4f;
        const decimal unplus3 = +123.4m;
    }
";
            var actual = ParseAndGetConstantFoldingSteps(source);

            var expected =
@"((1 + 2 * 3) / (4 >> 1) ) << (5 % 3) --> 12
(1 + 2 * 3) / (4 >> 1) --> 3
1 + 2 * 3 --> 7
2 * 3 --> 6
4 >> 1 --> 2
5 % 3 --> 2
(1.0 + (11.0 % (2.0 * 3.0))) / 4.0 --> 1.5
1.0 + (11.0 % (2.0 * 3.0)) --> 6
11.0 % (2.0 * 3.0) --> 5
2.0 * 3.0 --> 6
(1 < 2) & (3 > 2) & (10.0 < 11.0) & (120m >= 13m) & (3 != 4) & (""hello"" == ""hello"") --> True
(1 < 2) & (3 > 2) & (10.0 < 11.0) & (120m >= 13m) & (3 != 4) --> True
(1 < 2) & (3 > 2) & (10.0 < 11.0) & (120m >= 13m) --> True
(1 < 2) & (3 > 2) & (10.0 < 11.0) --> True
(1 < 2) & (3 > 2) --> True
1 < 2 --> True
3 > 2 --> True
10.0 < 11.0 --> True
120m >= 13m --> True
3 != 4 --> True
""hello"" == ""hello"" --> True
(char)('a' + 2) --> c
'a' + 2 --> 99
'a' --> 97
-2147483648U --> -2147483648
2147483648U --> 2147483648
-2147483648u --> -2147483648
2147483648u --> 2147483648
-2147483648L --> -2147483648
-2147483648l --> -2147483648
-2147483648 + 0 --> -2147483648
-9223372036854775808 + 0 --> -9223372036854775808
0 --> 0
""hello"" + ""goodbye"" --> hellogoodbye
""not null"" + null --> not null
null --> null
(null == null) & (null != null) --> False
b & !b --> False
!b --> True
s1 + s3 --> hellogoodbyenot null
new byte() --> 0
new ushort() --> 0
new uint() --> 0
new ulong() --> 0
new sbyte() --> 0
new short() --> 0
new int() --> 0
new long() --> 0
new decimal() --> 0
new double() --> 0
new float() --> 0
new char() --> control character
new DayOfWeek() --> 0
-2u --> -2
2u --> 2
100.0m % 3m --> 1.0
100.0m / 3m --> 33.333333333333333333333333333
100.0m - 50.00m --> 50.00
+123.4d --> 123.4
+123.4f --> 123.4
+123.4m --> 123.4";
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void ParameterlessCtorsInStructs()
        {
            var source = @"

struct S1
{

}

struct S2
{
    public S2()
    {
    }
}

class Program
{
    static void Main(string[] args)
    {
    }

    static void Foo(S1 s = new S1())
    {

    }

    static void Foo(S2 s = new S2())
    {

    }
}
";
            var comp = CreateExperimentalCompilationWithMscorlib45(source);
            comp.VerifyDiagnostics(
    // (10,12): error CS0568: Structs cannot contain explicit parameterless constructors
    //     public S2()
    Diagnostic(ErrorCode.ERR_StructsCantContainDefaultConstructor, "S2").WithLocation(10, 12),
    // (26,28): error CS1736: Default parameter value for 's' must be a compile-time constant
    //     static void Foo(S2 s = new S2())
    Diagnostic(ErrorCode.ERR_DefaultValueMustBeConstant, "new S2()").WithArguments("s").WithLocation(26, 28)
);
        }

        [Fact]
        public void TestConstantInt32Comparisons()
        {
            var source =
@"class C
{
    void M()
    {
        const bool comp1 = 1 < 2;
        const bool comp2 = 1 <= 2;
        const bool comp3 = 1 > 2;
        const bool comp4 = 1 >= 2;
        const bool comp5 = 1 == 2;
        const bool comp6 = 1 != 2;
    }
";
            var actual = ParseAndGetConstantFoldingSteps(source);

            var expected =
@"1 < 2 --> True
1 <= 2 --> True
1 > 2 --> False
1 >= 2 --> False
1 == 2 --> False
1 != 2 --> True";

            Assert.Equal(expected, actual);
        }

        [Fact]
        public void TestConstantUInt32Comparisons()
        {
            var source =
@"class C
{
    void M()
    {
        const bool comp1 = 1u < 2u;
        const bool comp2 = 1u <= 2u;
        const bool comp3 = 1u > 2u;
        const bool comp4 = 1u >= 2u;
        const bool comp5 = 1u == 2u;
        const bool comp6 = 1u != 2u;
    }
";
            var actual = ParseAndGetConstantFoldingSteps(source);

            var expected =
@"1u < 2u --> True
1u <= 2u --> True
1u > 2u --> False
1u >= 2u --> False
1u == 2u --> False
1u != 2u --> True";

            Assert.Equal(expected, actual);
        }

        [Fact]
        public void TestConstantInt64Comparisons()
        {
            var source =
@"class C
{
    void M()
    {
        const bool comp1 = 1L < 2L;
        const bool comp2 = 1L <= 2L;
        const bool comp3 = 1L > 2L;
        const bool comp4 = 1L >= 2L;
        const bool comp5 = 1L == 2L;
        const bool comp6 = 1L != 2L;
    }
";
            var actual = ParseAndGetConstantFoldingSteps(source);

            var expected =
@"1L < 2L --> True
1L <= 2L --> True
1L > 2L --> False
1L >= 2L --> False
1L == 2L --> False
1L != 2L --> True";

            Assert.Equal(expected, actual);
        }

        [Fact]
        public void TestConstantUInt64Comparisons()
        {
            var source =
@"class C
{
    void M()
    {
        const bool comp1 = 1UL < 2UL;
        const bool comp2 = 1UL <= 2UL;
        const bool comp3 = 1UL > 2UL;
        const bool comp4 = 1UL >= 2UL;
        const bool comp5 = 1UL == 2UL;
        const bool comp6 = 1UL != 2UL;
    }
";
            var actual = ParseAndGetConstantFoldingSteps(source);

            var expected =
@"1UL < 2UL --> True
1UL <= 2UL --> True
1UL > 2UL --> False
1UL >= 2UL --> False
1UL == 2UL --> False
1UL != 2UL --> True";

            Assert.Equal(expected, actual);
        }

        [Fact]
        public void TestConstantFloatComparisons()
        {
            var source =
@"class C
{
    void M()
    {
        const bool comp1 = 1f < 2f;
        const bool comp2 = 1f <= 2f;
        const bool comp3 = 1f > 2f;
        const bool comp4 = 1f >= 2f;
        const bool comp5 = 1f == 2f;
        const bool comp6 = 1f != 2f;
    }
";
            var actual = ParseAndGetConstantFoldingSteps(source);

            var expected =
@"1f < 2f --> True
1f <= 2f --> True
1f > 2f --> False
1f >= 2f --> False
1f == 2f --> False
1f != 2f --> True";

            Assert.Equal(expected, actual);
        }

        [Fact]
        public void TestConstantDecimalComparisons01()
        {
            var source =
@"class C
{
    void M()
    {
        const bool comp1 = 1m < 2m;
        const bool comp2 = 1m <= 2m;
        const bool comp3 = 1m > 2m;
        const bool comp4 = 1m >= 2m;
        const bool comp5 = 1m == 2m;
        const bool comp6 = 1m != 2m;
    }
}";
            var actual = ParseAndGetConstantFoldingSteps(source);

            var expected =
@"1m < 2m --> True
1m <= 2m --> True
1m > 2m --> False
1m >= 2m --> False
1m == 2m --> False
1m != 2m --> True";

            Assert.Equal(expected, actual);
        }

        [Fact]
        public void TestConstantDecimalComparisons02()
        {
            var source =
@"class C
{
    void M()
    {
        const bool comp1 = 0m < -0m;
        const bool comp2 = 0m <= -0m;
        const bool comp3 = 0m > -0m;
        const bool comp4 = 0m >= -0m;
        const bool comp5 = 0m == -0m;
        const bool comp6 = 0m != -0m;
    }
}";
            var actual = ParseAndGetConstantFoldingSteps(source);

            var expected =
@"0m < -0m --> False
-0m --> 0
0m <= -0m --> True
-0m --> 0
0m > -0m --> False
-0m --> 0
0m >= -0m --> True
-0m --> 0
0m == -0m --> True
-0m --> 0
0m != -0m --> False
-0m --> 0";

            Assert.Equal(expected, actual);
        }

        [Fact]
        public void TestConstantDoubleComparisons()
        {
            var source =
@"class C
{
    void M()
    {
        const bool comp1 = 1d < 2d;
        const bool comp2 = 1d <= 2d;
        const bool comp3 = 1d > 2d;
        const bool comp4 = 1d >= 2d;
        const bool comp5 = 1d == 2d;
        const bool comp6 = 1d != 2d;
    }
";
            var actual = ParseAndGetConstantFoldingSteps(source);

            var expected =
@"1d < 2d --> True
1d <= 2d --> True
1d > 2d --> False
1d >= 2d --> False
1d == 2d --> False
1d != 2d --> True";

            Assert.Equal(expected, actual);
        }

        private static readonly string[] s_enumTypeQualifiers =
            {
                "",
                " : sbyte",
                " : byte",
                " : short",
                " : ushort",
                " : int",
                " : uint",
                " : long",
                " : ulong",
            };

        [Fact]
        public void TestExplicitEnumIntConversions()
        {
            foreach (var typeQualifier in s_enumTypeQualifiers)
            {
                var source =
@"enum E" + typeQualifier + @" { A, B = 64, C }
class C
{
    static void F(E e) { }
    static void M()
    {
        const E e = E.C;
        const sbyte s8 = (sbyte)e;
        const byte u8 = (byte)e;
        const short s16 = (short)e;
        const ushort u16 = (ushort)e;
        const int s32 = (int)e;
        const uint u32 = (uint)e;
        const long s64 = (long)e;
        const ulong u64 = (ulong)e;
        const char c = (char)e;
        const float f = (float)e;
        const double d = (double)e;
        const decimal dec = (decimal)e;
        F((E)s8);
        F((E)u8);
        F((E)s16);
        F((E)u16);
        F((E)s32);
        F((E)u32);
        F((E)s64);
        F((E)u64);
        F((E)c);
        F((E)f);
        F((E)d);
        F((E)dec);
    }
}";
                var actual = ParseAndGetConstantFoldingSteps(source);
                var expected =
@"E.C --> 65
(sbyte)e --> 65
(byte)e --> 65
(short)e --> 65
(ushort)e --> 65
(int)e --> 65
(uint)e --> 65
(long)e --> 65
(ulong)e --> 65
(char)e --> A
(float)e --> 65
(double)e --> 65
(decimal)e --> 65
(E)s8 --> 65
(E)u8 --> 65
(E)s16 --> 65
(E)u16 --> 65
(E)s32 --> 65
(E)u32 --> 65
(E)s64 --> 65
(E)u64 --> 65
(E)c --> 65
(E)f --> 65
(E)d --> 65
(E)dec --> 65";
                Assert.Equal(expected, actual);
            }
        }

        [Fact]
        public void TestExplicitEnumEnumConversions()
        {
            foreach (var typeQualifier in s_enumTypeQualifiers)
            {
                var source =
@"enum E" + typeQualifier + @" { A, B = 3, C }
enum S8 : sbyte { A, B, C }
enum U8 : byte { A, B, C }
enum S16 : short { A, B, C }
enum U16 : ushort { A, B, C }
enum S32 : int { A, B, C }
enum U32 : uint { A, B, C }
enum S64 : long { A, B, C }
enum U64 : ulong { A, B, C }
class C
{
    static void M()
    {
        const E e = E.C;
        S8 s8 = (S8)e;
        U8 u8 = (U8)e;
        S16 s16 = (S16)e;
        U16 u16 = (U16)e;
        S32 s32 = (S32)e;
        U32 u32 = (U32)e;
        S64 s64 = (S64)e;
        U64 u64 = (U64)e;
    }
}";
                var actual = ParseAndGetConstantFoldingSteps(source);
                var expected =
@"E.C --> 4
(S8)e --> 4
(U8)e --> 4
(S16)e --> 4
(U16)e --> 4
(S32)e --> 4
(U32)e --> 4
(S64)e --> 4
(U64)e --> 4";
                Assert.Equal(expected, actual);
            }
        }

        [Fact]
        public void TestConstantEnumOperations()
        {
            foreach (var typeQualifier in s_enumTypeQualifiers)
            {
                var source =
@"enum E" + typeQualifier + @" { A, B, C }
class C
{
    static void M()
    {
        E add1 = E.B + 1;
        E add2 = 2 + E.C;
        var sub1 = E.C - E.B;
        E sub2 = E.C - 2;
        bool comp1 = E.A == E.B;
        bool comp2 = E.A != E.B;
        bool comp3 = E.A == 0;
        bool comp4 = 0 != E.B;
        bool comp5 = E.A < E.A;
        bool comp6 = E.B > E.A;
        bool comp7 = E.A <= E.A;
        bool comp8 = E.B >= E.A;
        bool comp9 = 0 <= E.A;
        bool comp10 = E.B >= 0;
        E logical1 = E.B & E.C;
        E logical2 = E.B | E.C;
        E logical3 = E.B ^ E.C;
    }
}";

                var actual = ParseAndGetConstantFoldingSteps(source, node => node.Kind == BoundKind.BinaryOperator);
                var expected =
@"E.B + 1 --> 2
2 + E.C --> 4
E.C - E.B --> 1
E.C - 2 --> 0
E.A == E.B --> False
E.A != E.B --> True
E.A == 0 --> True
0 != E.B --> True
E.A < E.A --> False
E.B > E.A --> True
E.A <= E.A --> True
E.B >= E.A --> True
0 <= E.A --> True
E.B >= 0 --> True
E.B & E.C --> 0
E.B | E.C --> 3
E.B ^ E.C --> 3";
                Assert.Equal(expected, actual);
            }
        }

        [Fact]
        public void TestConstantEnumBitwiseComplement()
        {
            var source =
@"enum S8 : sbyte { A, B, C }
enum U8 : byte { A, B, C }
enum S16 : short { A, B, C }
enum U16 : ushort { A, B, C }
enum S32 : int { A, B, C }
enum U32 : uint { A, B, C }
enum S64 : long { A, B, C }
enum U64 : ulong { A, B, C }
class C
{
    static void M()
    {
        const S8 s8 = ~S8.A;
        const U8 u8 = ~U8.B;
        const S16 s16 = ~S16.A;
        const U16 u16 = ~U16.B;
        const S32 s32 = ~S32.B;
        const U32 u32 = ~U32.C;
        const S64 s64 = ~S64.B;
        const U64 u64 = ~U64.C;
    }
}";
            var actual = ParseAndGetConstantFoldingSteps(source, node => node.Kind == BoundKind.UnaryOperator);
            var expected =
@"~S8.A --> -1
~U8.B --> 254
~S16.A --> -1
~U16.B --> 65534
~S32.B --> -2
~U32.C --> 4294967293
~S64.B --> -2
~U64.C --> 18446744073709551613";
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void TestConstantBooleanOperations()
        {
            var source =
@"class C
{
    void M()
    {
        const bool op1 = !true;
        const bool op2 = true && false;
        const bool op3 = true || false;
        const bool op4 = true & false;
        const bool op5 = true | false;
        const bool op6 = true == false;
        const bool op7 = true != false;
    }
";
            var actual = ParseAndGetConstantFoldingSteps(source);

            var expected =
@"!true --> False
true && false --> False
true || false --> True
true & false --> False
true | false --> True
true == false --> False
true != false --> True";

            Assert.Equal(expected, actual);
        }

        [Fact]
        public void TestEnumOverflowErrors()
        {
            TestErrors(
@"enum S8 : sbyte { Min = sbyte.MinValue, MinPlusOne, Max = sbyte.MaxValue }
enum U8 : byte { Min = byte.MinValue, MinPlusOne, Max = byte.MaxValue }
enum S16 : short { Min = short.MinValue, MinPlusOne, Max = short.MaxValue }
enum U16 : ushort { Min = ushort.MinValue, MinPlusOne, Max = ushort.MaxValue }
enum S32 : int { Min = int.MinValue, MinPlusOne, Max = int.MaxValue }
enum U32 : uint { Min = uint.MinValue, MinPlusOne, Max = uint.MaxValue }
enum S64 : long { Min = long.MinValue, MinPlusOne, Max = long.MaxValue }
enum U64 : ulong { Min = ulong.MinValue, MinPlusOne, Max = ulong.MaxValue }

class C
{
    static void F(S8 x) { }
    static void F(U8 x) { }
    static void F(S16 x) { }
    static void F(U16 x) { }
    static void F(S32 x) { }
    static void F(U32 x) { }
    static void F(S64 x) { }
    static void F(U64 x) { }
    static void F(sbyte x) { }
    static void F(byte x) { }
    static void F(short x) { }
    static void F(ushort x) { }
    static void F(int x) { }
    static void F(uint x) { }
    static void F(long x) { }
    static void F(ulong x) { }

    static void M()
    {
        // E + U
        F(S8.Max + 1); // 128 cannot be converted to ...
        F(U8.Max + 1); // 256 cannot be converted to ...
        F(S16.Max + 1); // 32768 cannot be converted to ...
        F(U16.Max + 1); // 65536 cannot be converted to ...
        F(S32.Max + 1); // overflows at compile time in checked mode
        F(U32.Max + 1); // overflows at compile time in checked mode
        F(S64.Max + 1); // overflows at compile time in checked mode
        F(U64.Max + 1); // overflows at compile time in checked mode

        // U + E
        F(2 + S8.Max); // 129 cannot be converted to ...
        F(2 + U8.Max); // 257 cannot be converted to ...
        F(2 + S16.Max); // 32769 cannot be converted to ...
        F(2 + U16.Max); // 65537 cannot be converted to ...
        F(2 + S32.Max); // overflows at compile time in checked mode
        F(2 + U32.Max); // overflows at compile time in checked mode
        F(2 + S64.Max); // overflows at compile time in checked mode
        F(2 + U64.Max); // overflows at compile time in checked mode

        // E - E
        F(S8.Min - S8.MinPlusOne); // no error
        F(U8.Min - U8.MinPlusOne); // -1 cannot be converted to ...
        F(S16.Min - S16.MinPlusOne); // no error
        F(U16.Min - U16.MinPlusOne); // -1 cannot be converted to ...
        F(S32.Min - S32.MinPlusOne); // no error
        F(U32.Min - U32.MinPlusOne); // overflows at compile time in checked mode
        F(S64.Min - S64.MinPlusOne); // no error
        F(U64.Min - U64.MinPlusOne); // overflows at compile time in checked mode

        // E - E
        F(S8.Min - S8.Max); // -255 cannot be converted to ...
        F(U8.Min - U8.Max); // -255 cannot be converted to ...
        F(S16.Min - S16.Max); // -65535 cannot be converted to ...
        F(U16.Min - U16.Max); // -65535 cannot be converted to ...
        F(S32.Min - S32.Max); // overflows at compile time in checked mode
        F(U32.Min - U32.Max); // overflows at compile time in checked mode
        F(S64.Min - S64.Max); // overflows at compile time in checked mode
        F(U64.Min - U64.Max); // overflows at compile time in checked mode

        // E - E
        F(S8.Max - S8.Min); // 255 cannot be converted to ...
        F(U8.Max - U8.Min); // no error
        F(S16.Max - S16.Min); // 65535 cannot be converted to ...
        F(U16.Max - U16.Min); // no error
        F(S32.Max - S32.Min); // overflows at compile time in checked mode
        F(U32.Max - U32.Min); // no error
        F(S64.Max - S64.Min); // overflows at compile time in checked mode
        F(U64.Max - U64.Min); // no error

        // E - U
        F(S8.Min - 2); // -130 cannot be converted to ...
        F(U8.Min - 2); // -2 cannot be converted to ...
        F(S16.Min - 2); // -32770 cannot be converted to ...
        F(U16.Min - 2); // -2 cannot be converted to ...
        F(S32.Min - 2); // overflows at compile time in checked mode
        F(U32.Min - 2); // overflows at compile time in checked mode
        F(S64.Min - 2); // overflows at compile time in checked mode
        F(U64.Min - 2); // overflows at compile time in checked mode
    }
}",
                "'S8.Max + 1' error CS0221: Constant value '128' cannot be converted to a 'S8' (use 'unchecked' syntax to override)",
                "'U8.Max + 1' error CS0221: Constant value '256' cannot be converted to a 'U8' (use 'unchecked' syntax to override)",
                "'S16.Max + 1' error CS0221: Constant value '32768' cannot be converted to a 'S16' (use 'unchecked' syntax to override)",
                "'U16.Max + 1' error CS0221: Constant value '65536' cannot be converted to a 'U16' (use 'unchecked' syntax to override)",
                "'S32.Max + 1' error CS0220: The operation overflows at compile time in checked mode",
                "'U32.Max + 1' error CS0220: The operation overflows at compile time in checked mode",
                "'S64.Max + 1' error CS0220: The operation overflows at compile time in checked mode",
                "'U64.Max + 1' error CS0220: The operation overflows at compile time in checked mode",
                "'2 + S8.Max' error CS0221: Constant value '129' cannot be converted to a 'S8' (use 'unchecked' syntax to override)",
                "'2 + U8.Max' error CS0221: Constant value '257' cannot be converted to a 'U8' (use 'unchecked' syntax to override)",
                "'2 + S16.Max' error CS0221: Constant value '32769' cannot be converted to a 'S16' (use 'unchecked' syntax to override)",
                "'2 + U16.Max' error CS0221: Constant value '65537' cannot be converted to a 'U16' (use 'unchecked' syntax to override)",
                "'2 + S32.Max' error CS0220: The operation overflows at compile time in checked mode",
                "'2 + U32.Max' error CS0220: The operation overflows at compile time in checked mode",
                "'2 + S64.Max' error CS0220: The operation overflows at compile time in checked mode",
                "'2 + U64.Max' error CS0220: The operation overflows at compile time in checked mode",
                "'U8.Min - U8.MinPlusOne' error CS0221: Constant value '-1' cannot be converted to a 'byte' (use 'unchecked' syntax to override)",
                "'U16.Min - U16.MinPlusOne' error CS0221: Constant value '-1' cannot be converted to a 'ushort' (use 'unchecked' syntax to override)",
                "'U32.Min - U32.MinPlusOne' error CS0220: The operation overflows at compile time in checked mode",
                "'U64.Min - U64.MinPlusOne' error CS0220: The operation overflows at compile time in checked mode",
                "'S8.Min - S8.Max' error CS0221: Constant value '-255' cannot be converted to a 'sbyte' (use 'unchecked' syntax to override)",
                "'U8.Min - U8.Max' error CS0221: Constant value '-255' cannot be converted to a 'byte' (use 'unchecked' syntax to override)",
                "'S16.Min - S16.Max' error CS0221: Constant value '-65535' cannot be converted to a 'short' (use 'unchecked' syntax to override)",
                "'U16.Min - U16.Max' error CS0221: Constant value '-65535' cannot be converted to a 'ushort' (use 'unchecked' syntax to override)",
                "'S32.Min - S32.Max' error CS0220: The operation overflows at compile time in checked mode",
                "'U32.Min - U32.Max' error CS0220: The operation overflows at compile time in checked mode",
                "'S64.Min - S64.Max' error CS0220: The operation overflows at compile time in checked mode",
                "'U64.Min - U64.Max' error CS0220: The operation overflows at compile time in checked mode",
                "'S8.Max - S8.Min' error CS0221: Constant value '255' cannot be converted to a 'sbyte' (use 'unchecked' syntax to override)",
                "'S16.Max - S16.Min' error CS0221: Constant value '65535' cannot be converted to a 'short' (use 'unchecked' syntax to override)",
                "'S32.Max - S32.Min' error CS0220: The operation overflows at compile time in checked mode",
                "'S64.Max - S64.Min' error CS0220: The operation overflows at compile time in checked mode",
                "'S8.Min - 2' error CS0221: Constant value '-130' cannot be converted to a 'S8' (use 'unchecked' syntax to override)",
                "'U8.Min - 2' error CS0221: Constant value '-2' cannot be converted to a 'U8' (use 'unchecked' syntax to override)",
                "'S16.Min - 2' error CS0221: Constant value '-32770' cannot be converted to a 'S16' (use 'unchecked' syntax to override)",
                "'U16.Min - 2' error CS0221: Constant value '-2' cannot be converted to a 'U16' (use 'unchecked' syntax to override)",
                "'S32.Min - 2' error CS0220: The operation overflows at compile time in checked mode",
                "'U32.Min - 2' error CS0220: The operation overflows at compile time in checked mode",
                "'S64.Min - 2' error CS0220: The operation overflows at compile time in checked mode",
                "'U64.Min - 2' error CS0220: The operation overflows at compile time in checked mode");
        }

        [Fact, WorkItem(528727, "DevDiv")]
        public void TestConstantNumericConversionsNotOverflow()
        {
            var source = @"
using System;
class C
{
    void M() // Test helper requires Method 'M' (default)
    {
    const sbyte S8_Max = (sbyte)(sbyte.MaxValue + 0.1);
    const byte U8_Max = (byte)(byte.MaxValue + 0.1);
    const short S16_Max = (short)(short.MaxValue + 0.1);
    const ushort U16_Max = (ushort)(ushort.MaxValue + 0.1);
    const int S32_Max = (int)(int.MaxValue + 0.1);
    const uint U32_Max = (uint)(uint.MaxValue + 0.1);

    const sbyte S8_Min = (sbyte)(sbyte.MinValue - 0.1);
    const byte U8_Min = (byte)(byte.MinValue - 0.1);
    const short S16_Min = (short)(short.MinValue - 0.1);
    const ushort U16_Min = (ushort)(ushort.MinValue - 0.1);
    const int S32_Min = (int)(int.MinValue - 0.1);
    const uint U32_Min = (uint)(uint.MinValue - 0.1);
    const long S64_Min = (long)(long.MinValue - 0.1);
    const ulong U64_Min = (ulong)(ulong.MinValue - 0.1);
    }
}";

            var actual = ParseAndGetConstantFoldingSteps(source);

            var expected =
@"(sbyte)(sbyte.MaxValue + 0.1) --> 127
sbyte.MaxValue + 0.1 --> 127.1
sbyte.MaxValue --> 127
sbyte.MaxValue --> 127
(byte)(byte.MaxValue + 0.1) --> 255
byte.MaxValue + 0.1 --> 255.1
byte.MaxValue --> 255
byte.MaxValue --> 255
(short)(short.MaxValue + 0.1) --> 32767
short.MaxValue + 0.1 --> 32767.1
short.MaxValue --> 32767
short.MaxValue --> 32767
(ushort)(ushort.MaxValue + 0.1) --> 65535
ushort.MaxValue + 0.1 --> 65535.1
ushort.MaxValue --> 65535
ushort.MaxValue --> 65535
(int)(int.MaxValue + 0.1) --> 2147483647
int.MaxValue + 0.1 --> 2147483647.1
int.MaxValue --> 2147483647
int.MaxValue --> 2147483647
(uint)(uint.MaxValue + 0.1) --> 4294967295
uint.MaxValue + 0.1 --> 4294967295.1
uint.MaxValue --> 4294967295
uint.MaxValue --> 4294967295
(sbyte)(sbyte.MinValue - 0.1) --> -128
sbyte.MinValue - 0.1 --> -128.1
sbyte.MinValue --> -128
sbyte.MinValue --> -128
(byte)(byte.MinValue - 0.1) --> 0
byte.MinValue - 0.1 --> -0.1
byte.MinValue --> 0
byte.MinValue --> 0
(short)(short.MinValue - 0.1) --> -32768
short.MinValue - 0.1 --> -32768.1
short.MinValue --> -32768
short.MinValue --> -32768
(ushort)(ushort.MinValue - 0.1) --> 0
ushort.MinValue - 0.1 --> -0.1
ushort.MinValue --> 0
ushort.MinValue --> 0
(int)(int.MinValue - 0.1) --> -2147483648
int.MinValue - 0.1 --> -2147483648.1
int.MinValue --> -2147483648
int.MinValue --> -2147483648
(uint)(uint.MinValue - 0.1) --> 0
uint.MinValue - 0.1 --> -0.1
uint.MinValue --> 0
uint.MinValue --> 0
(long)(long.MinValue - 0.1) --> -9223372036854775808
long.MinValue - 0.1 --> -9.22337203685478E+18
long.MinValue --> -9.22337203685478E+18
long.MinValue --> -9223372036854775808
(ulong)(ulong.MinValue - 0.1) --> 0
ulong.MinValue - 0.1 --> -0.1
ulong.MinValue --> 0
ulong.MinValue --> 0";

            Assert.Equal(expected, actual);
        }

        [Fact]
        public void TestConstantNumericConversions()
        {
            var source =
@"class C
{
    void M()
    {
        const int i = 65;
        const uint u = 65U;
        const long l = 65L;
        const ulong ul = 65UL;
        const float f = 65F;
        const decimal m = 65M;
        const double d = 65D;
        const byte b = (byte)65;
        const sbyte sb = (sbyte)65;
        const short s = (short)65;
        const ushort us = (ushort)65;
        const char c = 'A';

        const int int01 = (int)i;
        const int int02 = (int)u;
        const int int03 = (int)l;
        const int int04 = (int)ul;
        const int int05 = (int)f;
        const int int06 = (int)m;
        const int int07 = (int)d;
        const int int08 = (int)b;
        const int int09 = (int)sb;
        const int int10 = (int)s;
        const int int11 = (int)us;
        const int int12 = (int)c;

        const uint uint01 = (uint)i;
        const uint uint02 = (uint)u;
        const uint uint03 = (uint)l;
        const uint uint04 = (uint)ul;
        const uint uint05 = (uint)f;
        const uint uint06 = (uint)m;
        const uint uint07 = (uint)d;
        const uint uint08 = (uint)b;
        const uint uint09 = (uint)sb;
        const uint uint10 = (uint)s;
        const uint uint11 = (uint)us;
        const uint uint12 = (uint)c;

        const long long01 = (long)i;
        const long long02 = (long)u;
        const long long03 = (long)l;
        const long long04 = (long)ul;
        const long long05 = (long)f;
        const long long06 = (long)m;
        const long long07 = (long)d;
        const long long08 = (long)b;
        const long long09 = (long)sb;
        const long long10 = (long)s;
        const long long11 = (long)us;
        const long long12 = (long)c;

        const ulong ulong01 = (ulong)i;
        const ulong ulong02 = (ulong)u;
        const ulong ulong03 = (ulong)l;
        const ulong ulong04 = (ulong)ul;
        const ulong ulong05 = (ulong)f;
        const ulong ulong06 = (ulong)m;
        const ulong ulong07 = (ulong)d;
        const ulong ulong08 = (ulong)b;
        const ulong ulong09 = (ulong)sb;
        const ulong ulong10 = (ulong)s;
        const ulong ulong11 = (ulong)us;
        const ulong ulong12 = (ulong)c;

        const float float01 = (float)i;
        const float float02 = (float)u;
        const float float03 = (float)l;
        const float float04 = (float)ul;
        const float float05 = (float)f;
        const float float06 = (float)m;
        const float float07 = (float)d;
        const float float08 = (float)b;
        const float float09 = (float)sb;
        const float float10 = (float)s;
        const float float11 = (float)us;
        const float float12 = (float)c;

        const decimal decimal01 = (decimal)i;
        const decimal decimal02 = (decimal)u;
        const decimal decimal03 = (decimal)l;
        const decimal decimal04 = (decimal)ul;
        const decimal decimal05 = (decimal)f;
        const decimal decimal06 = (decimal)m;
        const decimal decimal07 = (decimal)d;
        const decimal decimal08 = (decimal)b;
        const decimal decimal09 = (decimal)sb;
        const decimal decimal10 = (decimal)s;
        const decimal decimal11 = (decimal)us;
        const decimal decimal12 = (decimal)c;

        const double double01 = (double)i;
        const double double02 = (double)u;
        const double double03 = (double)l;
        const double double04 = (double)ul;
        const double double05 = (double)f;
        const double double06 = (double)m;
        const double double07 = (double)d;
        const double double08 = (double)b;
        const double double09 = (double)sb;
        const double double10 = (double)s;
        const double double11 = (double)us;
        const double double12 = (double)c;

        const byte byte01 = (byte)i;
        const byte byte02 = (byte)u;
        const byte byte03 = (byte)l;
        const byte byte04 = (byte)ul;
        const byte byte05 = (byte)f;
        const byte byte06 = (byte)m;
        const byte byte07 = (byte)d;
        const byte byte08 = (byte)b;
        const byte byte09 = (byte)sb;
        const byte byte10 = (byte)s;
        const byte byte11 = (byte)us;
        const byte byte12 = (byte)c;

        const sbyte sbyte01 = (sbyte)i;
        const sbyte sbyte02 = (sbyte)u;
        const sbyte sbyte03 = (sbyte)l;
        const sbyte sbyte04 = (sbyte)ul;
        const sbyte sbyte05 = (sbyte)f;
        const sbyte sbyte06 = (sbyte)m;
        const sbyte sbyte07 = (sbyte)d;
        const sbyte sbyte08 = (sbyte)b;
        const sbyte sbyte09 = (sbyte)sb;
        const sbyte sbyte10 = (sbyte)s;
        const sbyte sbyte11 = (sbyte)us;
        const sbyte sbyte12 = (sbyte)c;

        const short short01 = (short)i;
        const short short02 = (short)u;
        const short short03 = (short)l;
        const short short04 = (short)ul;
        const short short05 = (short)f;
        const short short06 = (short)m;
        const short short07 = (short)d;
        const short short08 = (short)b;
        const short short09 = (short)sb;
        const short short10 = (short)s;
        const short short11 = (short)us;
        const short short12 = (short)c;

        const ushort ushort01 = (ushort)i;
        const ushort ushort02 = (ushort)u;
        const ushort ushort03 = (ushort)l;
        const ushort ushort04 = (ushort)ul;
        const ushort ushort05 = (ushort)f;
        const ushort ushort06 = (ushort)m;
        const ushort ushort07 = (ushort)d;
        const ushort ushort08 = (ushort)b;
        const ushort ushort09 = (ushort)sb;
        const ushort ushort10 = (ushort)s;
        const ushort ushort11 = (ushort)us;
        const ushort ushort12 = (ushort)c;

        const char char01 = (char)i;
        const char char02 = (char)u;
        const char char03 = (char)l;
        const char char04 = (char)ul;
        const char char05 = (char)f;
        const char char06 = (char)m;
        const char char07 = (char)d;
        const char char08 = (char)b;
        const char char09 = (char)sb;
        const char char10 = (char)s;
        const char char11 = (char)us;
        const char char12 = (char)c;
    }
";
            var actual = ParseAndGetConstantFoldingSteps(source);

            //the first four are for the constants
            //no entries for identity conversions
            var expected =
@"(byte)65 --> 65
(sbyte)65 --> 65
(short)65 --> 65
(ushort)65 --> 65
(int)i --> 65
(int)u --> 65
(int)l --> 65
(int)ul --> 65
(int)f --> 65
(int)m --> 65
(int)d --> 65
(int)b --> 65
(int)sb --> 65
(int)s --> 65
(int)us --> 65
(int)c --> 65
(uint)i --> 65
(uint)u --> 65
(uint)l --> 65
(uint)ul --> 65
(uint)f --> 65
(uint)m --> 65
(uint)d --> 65
(uint)b --> 65
(uint)sb --> 65
(uint)s --> 65
(uint)us --> 65
(uint)c --> 65
(long)i --> 65
(long)u --> 65
(long)l --> 65
(long)ul --> 65
(long)f --> 65
(long)m --> 65
(long)d --> 65
(long)b --> 65
(long)sb --> 65
(long)s --> 65
(long)us --> 65
(long)c --> 65
(ulong)i --> 65
(ulong)u --> 65
(ulong)l --> 65
(ulong)ul --> 65
(ulong)f --> 65
(ulong)m --> 65
(ulong)d --> 65
(ulong)b --> 65
(ulong)sb --> 65
(ulong)s --> 65
(ulong)us --> 65
(ulong)c --> 65
(float)i --> 65
(float)u --> 65
(float)l --> 65
(float)ul --> 65
(float)f --> 65
(float)m --> 65
(float)d --> 65
(float)b --> 65
(float)sb --> 65
(float)s --> 65
(float)us --> 65
(float)c --> 65
(decimal)i --> 65
(decimal)u --> 65
(decimal)l --> 65
(decimal)ul --> 65
(decimal)f --> 65
(decimal)m --> 65
(decimal)d --> 65
(decimal)b --> 65
(decimal)sb --> 65
(decimal)s --> 65
(decimal)us --> 65
(decimal)c --> 65
(double)i --> 65
(double)u --> 65
(double)l --> 65
(double)ul --> 65
(double)f --> 65
(double)m --> 65
(double)d --> 65
(double)b --> 65
(double)sb --> 65
(double)s --> 65
(double)us --> 65
(double)c --> 65
(byte)i --> 65
(byte)u --> 65
(byte)l --> 65
(byte)ul --> 65
(byte)f --> 65
(byte)m --> 65
(byte)d --> 65
(byte)b --> 65
(byte)sb --> 65
(byte)s --> 65
(byte)us --> 65
(byte)c --> 65
(sbyte)i --> 65
(sbyte)u --> 65
(sbyte)l --> 65
(sbyte)ul --> 65
(sbyte)f --> 65
(sbyte)m --> 65
(sbyte)d --> 65
(sbyte)b --> 65
(sbyte)sb --> 65
(sbyte)s --> 65
(sbyte)us --> 65
(sbyte)c --> 65
(short)i --> 65
(short)u --> 65
(short)l --> 65
(short)ul --> 65
(short)f --> 65
(short)m --> 65
(short)d --> 65
(short)b --> 65
(short)sb --> 65
(short)s --> 65
(short)us --> 65
(short)c --> 65
(ushort)i --> 65
(ushort)u --> 65
(ushort)l --> 65
(ushort)ul --> 65
(ushort)f --> 65
(ushort)m --> 65
(ushort)d --> 65
(ushort)b --> 65
(ushort)sb --> 65
(ushort)s --> 65
(ushort)us --> 65
(ushort)c --> 65
(char)i --> A
(char)u --> A
(char)l --> A
(char)ul --> A
(char)f --> A
(char)m --> A
(char)d --> A
(char)b --> A
(char)sb --> A
(char)s --> A
(char)us --> A
(char)c --> A";

            Assert.Equal(expected, actual);
        }

        [Fact]
        public void TestConstantFoldingOperations()
        {
            // TODO: char, byte, sbyte, short, ushort

            var source =
@"class C
{
    void M()
    {
        const int intOps = (1 - 2) + (3 / 4) + (5 % 6) + (7 * 8) + (9 << 10) + (11 >> 12) + (13 ^ 14) + (15 | 16) + (17 & 18) + (~19) + (-20) + (+21);
        const long longOps = (1L - 2L) + (3L / 4L) + (5L % 6L) + (7L * 8L) + (9L << 10) + (11L >> 12) + (13L ^ 14L) + (15L | 16L) + (17L & 18L) + (~19L) + (-20L) + (+21L);
        const uint uintOps = (20U - 19U) + (18U / 17U) + (16U % 15U) + (14U * 13U) + (12U << 11) + (10U >> 9) + (8U ^ 7U) + (6U | 5U) + (4U & 3U) + (+2U) & (~1U);
        const ulong ulongOps = (20UL - 19UL) + (18UL / 17UL) + (16UL % 15UL) + (14UL * 13UL) + (12UL << 11) + (10UL >> 9) + (8UL ^ 7UL) + (6UL | 5UL) + (4UL & 3UL) + (+2UL) & (~1UL);
    }
";
            var actual = ParseAndGetConstantFoldingSteps(source);

            var expected =
@"(1 - 2) + (3 / 4) + (5 % 6) + (7 * 8) + (9 << 10) + (11 >> 12) + (13 ^ 14) + (15 | 16) + (17 & 18) + (~19) + (-20) + (+21) --> 9307
(1 - 2) + (3 / 4) + (5 % 6) + (7 * 8) + (9 << 10) + (11 >> 12) + (13 ^ 14) + (15 | 16) + (17 & 18) + (~19) + (-20) --> 9286
(1 - 2) + (3 / 4) + (5 % 6) + (7 * 8) + (9 << 10) + (11 >> 12) + (13 ^ 14) + (15 | 16) + (17 & 18) + (~19) --> 9306
(1 - 2) + (3 / 4) + (5 % 6) + (7 * 8) + (9 << 10) + (11 >> 12) + (13 ^ 14) + (15 | 16) + (17 & 18) --> 9326
(1 - 2) + (3 / 4) + (5 % 6) + (7 * 8) + (9 << 10) + (11 >> 12) + (13 ^ 14) + (15 | 16) --> 9310
(1 - 2) + (3 / 4) + (5 % 6) + (7 * 8) + (9 << 10) + (11 >> 12) + (13 ^ 14) --> 9279
(1 - 2) + (3 / 4) + (5 % 6) + (7 * 8) + (9 << 10) + (11 >> 12) --> 9276
(1 - 2) + (3 / 4) + (5 % 6) + (7 * 8) + (9 << 10) --> 9276
(1 - 2) + (3 / 4) + (5 % 6) + (7 * 8) --> 60
(1 - 2) + (3 / 4) + (5 % 6) --> 4
(1 - 2) + (3 / 4) --> -1
1 - 2 --> -1
3 / 4 --> 0
5 % 6 --> 5
7 * 8 --> 56
9 << 10 --> 9216
11 >> 12 --> 0
13 ^ 14 --> 3
15 | 16 --> 31
17 & 18 --> 16
~19 --> -20
-20 --> -20
+21 --> 21
(1L - 2L) + (3L / 4L) + (5L % 6L) + (7L * 8L) + (9L << 10) + (11L >> 12) + (13L ^ 14L) + (15L | 16L) + (17L & 18L) + (~19L) + (-20L) + (+21L) --> 9307
(1L - 2L) + (3L / 4L) + (5L % 6L) + (7L * 8L) + (9L << 10) + (11L >> 12) + (13L ^ 14L) + (15L | 16L) + (17L & 18L) + (~19L) + (-20L) --> 9286
(1L - 2L) + (3L / 4L) + (5L % 6L) + (7L * 8L) + (9L << 10) + (11L >> 12) + (13L ^ 14L) + (15L | 16L) + (17L & 18L) + (~19L) --> 9306
(1L - 2L) + (3L / 4L) + (5L % 6L) + (7L * 8L) + (9L << 10) + (11L >> 12) + (13L ^ 14L) + (15L | 16L) + (17L & 18L) --> 9326
(1L - 2L) + (3L / 4L) + (5L % 6L) + (7L * 8L) + (9L << 10) + (11L >> 12) + (13L ^ 14L) + (15L | 16L) --> 9310
(1L - 2L) + (3L / 4L) + (5L % 6L) + (7L * 8L) + (9L << 10) + (11L >> 12) + (13L ^ 14L) --> 9279
(1L - 2L) + (3L / 4L) + (5L % 6L) + (7L * 8L) + (9L << 10) + (11L >> 12) --> 9276
(1L - 2L) + (3L / 4L) + (5L % 6L) + (7L * 8L) + (9L << 10) --> 9276
(1L - 2L) + (3L / 4L) + (5L % 6L) + (7L * 8L) --> 60
(1L - 2L) + (3L / 4L) + (5L % 6L) --> 4
(1L - 2L) + (3L / 4L) --> -1
1L - 2L --> -1
3L / 4L --> 0
5L % 6L --> 5
7L * 8L --> 56
9L << 10 --> 9216
11L >> 12 --> 0
13L ^ 14L --> 3
15L | 16L --> 31
17L & 18L --> 16
~19L --> -20
-20L --> -20
+21L --> 21
(20U - 19U) + (18U / 17U) + (16U % 15U) + (14U * 13U) + (12U << 11) + (10U >> 9) + (8U ^ 7U) + (6U | 5U) + (4U & 3U) + (+2U) & (~1U) --> 24784
(20U - 19U) + (18U / 17U) + (16U % 15U) + (14U * 13U) + (12U << 11) + (10U >> 9) + (8U ^ 7U) + (6U | 5U) + (4U & 3U) + (+2U) --> 24785
(20U - 19U) + (18U / 17U) + (16U % 15U) + (14U * 13U) + (12U << 11) + (10U >> 9) + (8U ^ 7U) + (6U | 5U) + (4U & 3U) --> 24783
(20U - 19U) + (18U / 17U) + (16U % 15U) + (14U * 13U) + (12U << 11) + (10U >> 9) + (8U ^ 7U) + (6U | 5U) --> 24783
(20U - 19U) + (18U / 17U) + (16U % 15U) + (14U * 13U) + (12U << 11) + (10U >> 9) + (8U ^ 7U) --> 24776
(20U - 19U) + (18U / 17U) + (16U % 15U) + (14U * 13U) + (12U << 11) + (10U >> 9) --> 24761
(20U - 19U) + (18U / 17U) + (16U % 15U) + (14U * 13U) + (12U << 11) --> 24761
(20U - 19U) + (18U / 17U) + (16U % 15U) + (14U * 13U) --> 185
(20U - 19U) + (18U / 17U) + (16U % 15U) --> 3
(20U - 19U) + (18U / 17U) --> 2
20U - 19U --> 1
18U / 17U --> 1
16U % 15U --> 1
14U * 13U --> 182
12U << 11 --> 24576
10U >> 9 --> 0
8U ^ 7U --> 15
6U | 5U --> 7
4U & 3U --> 0
+2U --> 2
~1U --> 4294967294
(20UL - 19UL) + (18UL / 17UL) + (16UL % 15UL) + (14UL * 13UL) + (12UL << 11) + (10UL >> 9) + (8UL ^ 7UL) + (6UL | 5UL) + (4UL & 3UL) + (+2UL) & (~1UL) --> 24784
(20UL - 19UL) + (18UL / 17UL) + (16UL % 15UL) + (14UL * 13UL) + (12UL << 11) + (10UL >> 9) + (8UL ^ 7UL) + (6UL | 5UL) + (4UL & 3UL) + (+2UL) --> 24785
(20UL - 19UL) + (18UL / 17UL) + (16UL % 15UL) + (14UL * 13UL) + (12UL << 11) + (10UL >> 9) + (8UL ^ 7UL) + (6UL | 5UL) + (4UL & 3UL) --> 24783
(20UL - 19UL) + (18UL / 17UL) + (16UL % 15UL) + (14UL * 13UL) + (12UL << 11) + (10UL >> 9) + (8UL ^ 7UL) + (6UL | 5UL) --> 24783
(20UL - 19UL) + (18UL / 17UL) + (16UL % 15UL) + (14UL * 13UL) + (12UL << 11) + (10UL >> 9) + (8UL ^ 7UL) --> 24776
(20UL - 19UL) + (18UL / 17UL) + (16UL % 15UL) + (14UL * 13UL) + (12UL << 11) + (10UL >> 9) --> 24761
(20UL - 19UL) + (18UL / 17UL) + (16UL % 15UL) + (14UL * 13UL) + (12UL << 11) --> 24761
(20UL - 19UL) + (18UL / 17UL) + (16UL % 15UL) + (14UL * 13UL) --> 185
(20UL - 19UL) + (18UL / 17UL) + (16UL % 15UL) --> 3
(20UL - 19UL) + (18UL / 17UL) --> 2
20UL - 19UL --> 1
18UL / 17UL --> 1
16UL % 15UL --> 1
14UL * 13UL --> 182
12UL << 11 --> 24576
10UL >> 9 --> 0
8UL ^ 7UL --> 15
6UL | 5UL --> 7
4UL & 3UL --> 0
+2UL --> 2
~1UL --> 18446744073709551614";
            Assert.Equal(expected, actual);
        }

        private static string ParseAndGetConstantFoldingSteps(string source)
        {
            return ParseAndGetConstantFoldingSteps(source, node => node.Kind != BoundKind.Literal && node.Kind != BoundKind.Local);
        }

        private static string ParseAndGetConstantFoldingSteps(string source, Func<BoundNode, bool> predicate)
        {
            var block = ParseAndBindMethodBody(source);
            var constants = BoundTreeSequencer.GetNodes(block).
                Where(predicate).
                OfType<BoundExpression>().
                Where(node => node.ConstantValue != null).
                Select(node => node.Syntax.ToFullString().Trim() + " --> " + ExtractValue(node.ConstantValue));
            var result = string.Join(Environment.NewLine, constants);
            return result;
        }

        private static object ExtractValue(ConstantValue constantValue)
        {
            if (constantValue.IsBad)
            {
                return "BAD";
            }

            if (constantValue.IsChar && char.IsControl(constantValue.CharValue))
            {
                return "control character";
            }

            // return constantValue.Value ?? "null";
            if (constantValue.Value == null)
                return "null";

            return TestHelpers.GetCultureInvariantString(constantValue.Value);
        }

        /// <summary>
        /// Breaking change from the native compiler for
        /// certain constant expressions involving +0m and -0m.
        /// </summary>
        [WorkItem(529730, "DevDiv")]
        [WorkItem(1043494, "DevDiv")]
        [Fact(Skip = "1043494")]
        public void TestConstantFoldingDecimalOperations01()
        {
            var source =
@"using System;
using System.Globalization;
class C
{
    static void Main()
    {
        // +
        Console.WriteLine(""1 / (double)(0m + 0m) = {0}"", (1 / (double)(0m + 0m)).ToString(CultureInfo.InvariantCulture));  // Dev11: Infinity
        Console.WriteLine(""1 / (double)(0m + -0m) = {0}"", (1 / (double)(0m + -0m)).ToString(CultureInfo.InvariantCulture));  // Dev11: -Infinity
        Console.WriteLine(""1 / (double)(-0m + 0m) = {0}"", (1 / (double)(-0m + 0m)).ToString(CultureInfo.InvariantCulture));  // Dev11: Infinity
        Console.WriteLine(""1 / (double)(-0m + -0m) = {0}"", (1 / (double)(-0m + -0m)).ToString(CultureInfo.InvariantCulture));  // Dev11: -Infinity
        Console.WriteLine();
        // -
        Console.WriteLine(""1 / (double)(0m - 0m) = {0}"", (1 / (double)(0m - 0m)).ToString(CultureInfo.InvariantCulture));  // Dev11: -Infinity
        Console.WriteLine(""1 / (double)(0m - -0m) = {0}"", (1 / (double)(0m - -0m)).ToString(CultureInfo.InvariantCulture));  // Dev11: Infinity
        Console.WriteLine(""1 / (double)(-0m - 0m) = {0}"", (1 / (double)(-0m - 0m)).ToString(CultureInfo.InvariantCulture));  // Dev11: -Infinity
        Console.WriteLine(""1 / (double)(-0m - -0m) = {0}"", (1 / (double)(-0m - -0m)).ToString(CultureInfo.InvariantCulture));  // Dev11: Infinity
        Console.WriteLine();
        // *
        Console.WriteLine(""1 / (double)(0m * 1m) = {0}"", (1 / (double)(0m * 1m)).ToString(CultureInfo.InvariantCulture));  // Dev11: Infinity
        Console.WriteLine(""1 / (double)(1m * 0m) = {0}"", (1 / (double)(1m * 0m)).ToString(CultureInfo.InvariantCulture));  // Dev11: Infinity
        Console.WriteLine(""1 / (double)(0m * -1m) = {0}"", (1 / (double)(0m * -1m)).ToString(CultureInfo.InvariantCulture));  // Dev11: -Infinity
        Console.WriteLine(""1 / (double)(-1m * 0m) = {0}"", (1 / (double)(-1m * 0m)).ToString(CultureInfo.InvariantCulture));  // Dev11: -Infinity
        Console.WriteLine(""1 / (double)(0m * 1m) = {0}"", (1 / (double)(-0m * 1m)).ToString(CultureInfo.InvariantCulture));  // Dev11: -Infinity
        Console.WriteLine(""1 / (double)(1m * -0m) = {0}"", (1 / (double)(1m * -0m)).ToString(CultureInfo.InvariantCulture));  // Dev11: -Infinity
        Console.WriteLine(""1 / (double)(0m * -1m) = {0}"", (1 / (double)(-0m * -1m)).ToString(CultureInfo.InvariantCulture));  // Dev11: Infinity
        Console.WriteLine(""1 / (double)(-1m * -0m) = {0}"", (1 / (double)(-1m * -0m)).ToString(CultureInfo.InvariantCulture));  // Dev11: Infinity
    }
}";
            CompileAndVerify(source, expectedOutput:
@"1 / (double)(0m + 0m) = Infinity
1 / (double)(0m + -0m) = Infinity
1 / (double)(-0m + 0m) = -Infinity
1 / (double)(-0m + -0m) = -Infinity

1 / (double)(0m - 0m) = Infinity
1 / (double)(0m - -0m) = Infinity
1 / (double)(-0m - 0m) = -Infinity
1 / (double)(-0m - -0m) = -Infinity

1 / (double)(0m * 1m) = Infinity
1 / (double)(1m * 0m) = Infinity
1 / (double)(0m * -1m) = -Infinity
1 / (double)(-1m * 0m) = -Infinity
1 / (double)(0m * 1m) = -Infinity
1 / (double)(1m * -0m) = -Infinity
1 / (double)(0m * -1m) = Infinity
1 / (double)(-1m * -0m) = Infinity");
        }

        /// <summary>
        /// Breaking change from the native compiler for
        /// certain constant expressions involving +0m and -0m.
        /// </summary>
        [WorkItem(529730, "DevDiv")]
        [Fact]
        public void TestConstantFoldingDecimalOperations02()
        {
            var source =
@"using System;
using System.Linq;
class C
{
    static void Main()
    {
        var positiveZero = 0e-1m;
        var negativeZero = -0e-1m;
        Console.WriteLine(ToHexString(0e-1m));
        Console.WriteLine(ToHexString(-0e-1m));
        Console.WriteLine();
        Console.WriteLine(ToHexString(0e-1m + 0e-1m)); // Dev11: 0e-1m, Roslyn: same
        Console.WriteLine(ToHexString(0e-1m + -0e-1m)); // Dev11: -0e-1m, Roslyn: 0e-1m
        Console.WriteLine(ToHexString(-0e-1m + 0e-1m)); // Dev11: 0e-1m, Roslyn: -0e-1m
        Console.WriteLine(ToHexString(-0e-1m + -0e-1m)); // Dev11: -0e-1m, Roslyn: same
        Console.WriteLine();
        Console.WriteLine(ToHexString(0e-1m - 0e-1m)); // Dev11: -0e-1m, Roslyn: 0e-1m
        Console.WriteLine(ToHexString(0e-1m - -0e-1m)); // Dev11: 0e-1m, Roslyn: same
        Console.WriteLine(ToHexString(-0e-1m - 0e-1m)); // Dev11: -0e-1m, Roslyn: same
        Console.WriteLine(ToHexString(-0e-1m - -0e-1m)); // Dev11: 0e-1m, Roslyn: -0e-1m
        Console.WriteLine();
        Console.WriteLine(ToHexString(positiveZero + negativeZero)); // Dev11: 0e-1m, Roslyn: same
        Console.WriteLine(ToHexString(negativeZero + positiveZero)); // Dev11: -0e-1m, Roslyn: same
        Console.WriteLine(ToHexString(positiveZero - positiveZero)); // Dev11: 0e-1m, Roslyn: same
        Console.WriteLine(ToHexString(negativeZero - negativeZero)); // Dev11: -0e-1m, Roslyn: same
    }
    static string ToHexString(decimal d)
    {
        return string.Join("""", decimal.GetBits(d).Select(word => string.Format(""{0:x8}"", word)));
    }
}";
            var compilation = CreateCompilationWithMscorlibAndSystemCore(source, options: TestOptions.ReleaseExe);
            CompileAndVerify(compilation, expectedOutput:
@"00000000000000000000000000010000
00000000000000000000000080010000

00000000000000000000000000010000
00000000000000000000000000010000
00000000000000000000000080010000
00000000000000000000000080010000

00000000000000000000000000010000
00000000000000000000000000010000
00000000000000000000000080010000
00000000000000000000000080010000

00000000000000000000000000010000
00000000000000000000000080010000
00000000000000000000000000010000
00000000000000000000000080010000");
        }

        [Fact]
        public void TestConstantFoldingOperationBoundaries()
        {
            var source =
@"class C
{
    void M()
    {
        const int intOpBoundary1 = 2147483647 & -2147483648;
        const int intOpBoundary2 = 2147483647 | -2147483648;
        const int intOpBoundary3 = 2147483647 ^ -2147483648;
        const int intOpBoundary4 = 2147483647 + -2147483648;

        const int intOpBoundary5 = 2147483647 / -2147483648;
        const int intOpBoundary6 = 2147483647 % -2147483648;
        const int intOpBoundary7 = 2147483647 << -2147483648;
        const int intOpBoundary8 = 2147483647 >> -2147483648;
        
        const int intOpBoundary9 = -2147483648 / 2147483647;
        const int intOpBoundary10 = -2147483648 % 2147483647;
        const int intOpBoundary11 = -2147483648 << 2147483647;
        const int intOpBoundary12 = -2147483648 >> 2147483647;

        const long longOpBoundary1 = 9223372036854775807 & -9223372036854775808;
        const long longOpBoundary2 = 9223372036854775807 | -9223372036854775808;
        const long longOpBoundary3 = 9223372036854775807 ^ -9223372036854775808;
        const long longOpBoundary4 = 9223372036854775807 + -9223372036854775808;

        const long longOpBoundary5 = 9223372036854775807 / -9223372036854775808;
        const long longOpBoundary6 = 9223372036854775807 % -9223372036854775808;
        const long longOpBoundary7 = 9223372036854775807 << -2147483648;
        const long longOpBoundary8 = 9223372036854775807 >> -2147483648;
        
        const long longOpBoundary9 = -9223372036854775808 / 9223372036854775807;
        const long longOpBoundary10 = -9223372036854775808 % 9223372036854775807;
        const long longOpBoundary11 = -9223372036854775808 << 2147483647;
        const long longOpBoundary12 = -9223372036854775808 >> 2147483647;
    }
";
            var actual = ParseAndGetConstantFoldingSteps(source);

            var expected =
@"2147483647 & -2147483648 --> 0
2147483647 | -2147483648 --> -1
2147483647 ^ -2147483648 --> -1
2147483647 + -2147483648 --> -1
2147483647 / -2147483648 --> 0
2147483647 % -2147483648 --> 2147483647
2147483647 << -2147483648 --> 2147483647
2147483647 >> -2147483648 --> 2147483647
-2147483648 / 2147483647 --> -1
-2147483648 % 2147483647 --> -1
-2147483648 << 2147483647 --> 0
-2147483648 >> 2147483647 --> -1
9223372036854775807 & -9223372036854775808 --> 0
9223372036854775807 | -9223372036854775808 --> -1
9223372036854775807 ^ -9223372036854775808 --> -1
9223372036854775807 + -9223372036854775808 --> -1
9223372036854775807 / -9223372036854775808 --> 0
9223372036854775807 % -9223372036854775808 --> 9223372036854775807
9223372036854775807 << -2147483648 --> 9223372036854775807
9223372036854775807 >> -2147483648 --> 9223372036854775807
-9223372036854775808 / 9223372036854775807 --> -1
-9223372036854775808 % 9223372036854775807 --> -1
-9223372036854775808 << 2147483647 --> 0
-9223372036854775808 >> 2147483647 --> -1";
            Assert.Equal(expected, actual);
        }

        [WorkItem(538179, "DevDiv")]
        [Fact]
        public void TestConstantErrors()
        {
            // UNDONE: Extend this to test all the constant out-of-bounds errors
            // UNDONE: Test unchecked contexts
            var source = @"
class C
{
    struct S {}
    void M()
    {
        const S s = new S();
        const double ul1 = -9223372036854775808UL + 0;
        const double ul2 = -9223372036854775808ul + 0;

        string s1 = null;
        const string s2 = s1; // Not a constant

        const object o1 = ""hello""; // Constants of ref type other than string must be null.

        int y = (1 / 0) + (1L/0L) + (1UL/0UL) + (1M/0M) + (-79228162514264337593543950335m - 1m);

        const int z = 1 + (z + 1);
        
        int intConversion = (int)0x8888888888888888;
        uint uintConversion = (uint)0x8888888888888888;
        long longConversion = (long)0x8888888888888888;
        ulong ulongConversion = (ulong)1E50;
        
        int intOverflow = int.MaxValue + 1;
        uint uintOverflow = uint.MaxValue + 1;
        long longOverflow = long.MaxValue + 1;
        ulong ulongOverflow = ulong.MaxValue + 1;
        
        int intUnderflow = int.MinValue - 1;
        uint uintUnderflow = uint.MinValue - 1;
        long longUnderflow = long.MinValue - 1;
        ulong ulongUnderflow = ulong.MinValue - 1;
    }
}";
            CreateCompilationWithMscorlib(source).VerifyDiagnostics(
    // (7,15): error CS0283: The type 'C.S' cannot be declared const
    //         const S s = new S();
    Diagnostic(ErrorCode.ERR_BadConstType, "S").WithArguments("C.S").WithLocation(7, 15),
    // (8,28): error CS0023: Operator '-' cannot be applied to operand of type 'ulong'
    //         const double ul1 = -9223372036854775808UL + 0;
    Diagnostic(ErrorCode.ERR_BadUnaryOp, "-9223372036854775808UL").WithArguments("-", "ulong").WithLocation(8, 28),
    // (9,28): error CS0023: Operator '-' cannot be applied to operand of type 'ulong'
    //         const double ul2 = -9223372036854775808ul + 0;
    Diagnostic(ErrorCode.ERR_BadUnaryOp, "-9223372036854775808ul").WithArguments("-", "ulong").WithLocation(9, 28),
    // (12,27): error CS0133: The expression being assigned to 's2' must be constant
    //         const string s2 = s1; // Not a constant
    Diagnostic(ErrorCode.ERR_NotConstantExpression, "s1").WithArguments("s2").WithLocation(12, 27),
    // (14,27): error CS0134: 'o1' is of type 'object'. A const field of a reference type other than string can only be initialized with null.
    //         const object o1 = "hello"; // Constants of ref type other than string must be null.
    Diagnostic(ErrorCode.ERR_NotNullConstRefField, @"""hello""").WithArguments("o1", "object").WithLocation(14, 27),
    // (16,60): error CS0463: Evaluation of the decimal constant expression failed
    //         int y = (1 / 0) + (1L/0L) + (1UL/0UL) + (1M/0M) + (-79228162514264337593543950335m - 1m);
    Diagnostic(ErrorCode.ERR_DecConstError, "-79228162514264337593543950335m - 1m").WithLocation(16, 60),
    // (16,50): error CS0020: Division by constant zero
    //         int y = (1 / 0) + (1L/0L) + (1UL/0UL) + (1M/0M) + (-79228162514264337593543950335m - 1m);
    Diagnostic(ErrorCode.ERR_IntDivByZero, "1M/0M").WithLocation(16, 50),
    // (16,38): error CS0020: Division by constant zero
    //         int y = (1 / 0) + (1L/0L) + (1UL/0UL) + (1M/0M) + (-79228162514264337593543950335m - 1m);
    Diagnostic(ErrorCode.ERR_IntDivByZero, "1UL/0UL").WithLocation(16, 38),
    // (16,28): error CS0020: Division by constant zero
    //         int y = (1 / 0) + (1L/0L) + (1UL/0UL) + (1M/0M) + (-79228162514264337593543950335m - 1m);
    Diagnostic(ErrorCode.ERR_IntDivByZero, "1L/0L").WithLocation(16, 28),
    // (16,18): error CS0020: Division by constant zero
    //         int y = (1 / 0) + (1L/0L) + (1UL/0UL) + (1M/0M) + (-79228162514264337593543950335m - 1m);
    Diagnostic(ErrorCode.ERR_IntDivByZero, "1 / 0").WithLocation(16, 18),
    // (18,28): error CS0110: The evaluation of the constant value for 'z' involves a circular definition
    //         const int z = 1 + (z + 1);
    Diagnostic(ErrorCode.ERR_CircConstValue, "z").WithArguments("z").WithLocation(18, 28),
    // (20,29): error CS0221: Constant value '9838263505978427528' cannot be converted to a 'int' (use 'unchecked' syntax to override)
    //         int intConversion = (int)0x8888888888888888;
    Diagnostic(ErrorCode.ERR_ConstOutOfRangeChecked, "(int)0x8888888888888888").WithArguments("9838263505978427528", "int").WithLocation(20, 29),
    // (21,31): error CS0221: Constant value '9838263505978427528' cannot be converted to a 'uint' (use 'unchecked' syntax to override)
    //         uint uintConversion = (uint)0x8888888888888888;
    Diagnostic(ErrorCode.ERR_ConstOutOfRangeChecked, "(uint)0x8888888888888888").WithArguments("9838263505978427528", "uint").WithLocation(21, 31),
    // (22,31): error CS0221: Constant value '9838263505978427528' cannot be converted to a 'long' (use 'unchecked' syntax to override)
    //         long longConversion = (long)0x8888888888888888;
    Diagnostic(ErrorCode.ERR_ConstOutOfRangeChecked, "(long)0x8888888888888888").WithArguments("9838263505978427528", "long").WithLocation(22, 31),
    // (23,33): error CS0221: Constant value '1E+50' cannot be converted to a 'ulong' (use 'unchecked' syntax to override)
    //         ulong ulongConversion = (ulong)1E50;
    Diagnostic(ErrorCode.ERR_ConstOutOfRangeChecked, "(ulong)1E50").WithArguments("1E+50", "ulong").WithLocation(23, 33),
    // (25,27): error CS0220: The operation overflows at compile time in checked mode
    //         int intOverflow = int.MaxValue + 1;
    Diagnostic(ErrorCode.ERR_CheckedOverflow, "int.MaxValue + 1").WithLocation(25, 27),
    // (26,29): error CS0220: The operation overflows at compile time in checked mode
    //         uint uintOverflow = uint.MaxValue + 1;
    Diagnostic(ErrorCode.ERR_CheckedOverflow, "uint.MaxValue + 1").WithLocation(26, 29),
    // (27,29): error CS0220: The operation overflows at compile time in checked mode
    //         long longOverflow = long.MaxValue + 1;
    Diagnostic(ErrorCode.ERR_CheckedOverflow, "long.MaxValue + 1").WithLocation(27, 29),
    // (28,31): error CS0220: The operation overflows at compile time in checked mode
    //         ulong ulongOverflow = ulong.MaxValue + 1;
    Diagnostic(ErrorCode.ERR_CheckedOverflow, "ulong.MaxValue + 1").WithLocation(28, 31),
    // (30,28): error CS0220: The operation overflows at compile time in checked mode
    //         int intUnderflow = int.MinValue - 1;
    Diagnostic(ErrorCode.ERR_CheckedOverflow, "int.MinValue - 1").WithLocation(30, 28),
    // (31,30): error CS0220: The operation overflows at compile time in checked mode
    //         uint uintUnderflow = uint.MinValue - 1;
    Diagnostic(ErrorCode.ERR_CheckedOverflow, "uint.MinValue - 1").WithLocation(31, 30),
    // (32,30): error CS0220: The operation overflows at compile time in checked mode
    //         long longUnderflow = long.MinValue - 1;
    Diagnostic(ErrorCode.ERR_CheckedOverflow, "long.MinValue - 1").WithLocation(32, 30),
    // (33,32): error CS0220: The operation overflows at compile time in checked mode
    //         ulong ulongUnderflow = ulong.MinValue - 1;
    Diagnostic(ErrorCode.ERR_CheckedOverflow, "ulong.MinValue - 1").WithLocation(33, 32),
    // (7,17): warning CS0219: The variable 's' is assigned but its value is never used
    //         const S s = new S();
    Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "s").WithArguments("s").WithLocation(7, 17),
    // (20,13): warning CS0219: The variable 'intConversion' is assigned but its value is never used
    //         int intConversion = (int)0x8888888888888888;
    Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "intConversion").WithArguments("intConversion").WithLocation(20, 13),
    // (21,14): warning CS0219: The variable 'uintConversion' is assigned but its value is never used
    //         uint uintConversion = (uint)0x8888888888888888;
    Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "uintConversion").WithArguments("uintConversion").WithLocation(21, 14),
    // (22,14): warning CS0219: The variable 'longConversion' is assigned but its value is never used
    //         long longConversion = (long)0x8888888888888888;
    Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "longConversion").WithArguments("longConversion").WithLocation(22, 14),
    // (23,15): warning CS0219: The variable 'ulongConversion' is assigned but its value is never used
    //         ulong ulongConversion = (ulong)1E50;
    Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "ulongConversion").WithArguments("ulongConversion").WithLocation(23, 15)
                );
        }

        [Fact]
        public void TestDynamicConstantError()
        {
            var source = @"
class C
{
    const int d0 = default(dynamic);
    const int d1 = (dynamic)1;
    const int d2 = (int)(dynamic)1;
    const int d3 = 1 + (int)(dynamic)1;
}";
            CreateCompilationWithMscorlib(source).VerifyDiagnostics(
                // (4,20): error CS0133: The expression being assigned to 'C.d0' must be constant
                Diagnostic(ErrorCode.ERR_NotConstantExpression, "default(dynamic)").WithArguments("C.d0"),
                // (5,20): error CS0133: The expression being assigned to 'C.d1' must be constant
                Diagnostic(ErrorCode.ERR_NotConstantExpression, "(dynamic)1").WithArguments("C.d1"),
                // (6,20): error CS0133: The expression being assigned to 'C.d2' must be constant
                Diagnostic(ErrorCode.ERR_NotConstantExpression, "(int)(dynamic)1").WithArguments("C.d2"),
                // (7,20): error CS0133: The expression being assigned to 'C.d3' must be constant
                Diagnostic(ErrorCode.ERR_NotConstantExpression, "1 + (int)(dynamic)1").WithArguments("C.d3"));
        }

        [Fact]
        public void FoldingUnaryOperators()
        {
            var source = @"
using System;

class C
{
    const int implicit_int = -Int32.MinValue;
    const long implicit_long = -Int64.MinValue;

    const int checked_int = checked(-Int32.MinValue);
    const long checked_long = checked(-Int64.MinValue);

    const int unchecked_int = unchecked(-Int32.MinValue);
    const long unchecked_long = unchecked(-Int64.MinValue);
}
";
            CreateCompilationWithMscorlib(source).VerifyDiagnostics(
                // (6,30): error CS0220: The operation overflows at compile time in checked mode
                Diagnostic(ErrorCode.ERR_CheckedOverflow, "-Int32.MinValue"),
                // (7,32): error CS0220: The operation overflows at compile time in checked mode
                Diagnostic(ErrorCode.ERR_CheckedOverflow, "-Int64.MinValue"),
                // (9,37): error CS0220: The operation overflows at compile time in checked mode
                Diagnostic(ErrorCode.ERR_CheckedOverflow, "-Int32.MinValue"),
                // (10,39): error CS0220: The operation overflows at compile time in checked mode
                Diagnostic(ErrorCode.ERR_CheckedOverflow, "-Int64.MinValue"));
        }

        [ClrOnlyFact]
        public void FoldingRemDivOperators()
        {
            var source = @"
class C
{
    void M()
    {
        const int crem = checked(int.MinValue % (-1));
        const int urem = unchecked(int.MinValue % (-1));

        const long creml = checked(long.MinValue % (-1));
        const long ureml = unchecked(long.MinValue % (-1));

        const int cdiv = checked(int.MinValue / (-1));
        const int udiv = unchecked(int.MinValue / (-1));

        const long cdivl = checked(long.MinValue / (-1));
        const long udivl = unchecked(long.MinValue / (-1));

        System.Console.WriteLine(null, crem, urem, creml, ureml, cdiv, udiv, cdivl, udivl);
    }
}
";
            CreateCompilationWithMscorlib(source).VerifyDiagnostics(
                // (12,34): error CS0220: The operation overflows at compile time in checked mode
                Diagnostic(ErrorCode.ERR_CheckedOverflow, "int.MinValue / (-1)"),
                // (15,36): error CS0220: The operation overflows at compile time in checked mode
                Diagnostic(ErrorCode.ERR_CheckedOverflow, "long.MinValue / (-1)"));

            var actual = ParseAndGetConstantFoldingSteps(source);
            var expected =
@"int.MinValue % (-1) --> 0
int.MinValue --> -2147483648
-1 --> -1
int.MinValue % (-1) --> 0
int.MinValue --> -2147483648
-1 --> -1
long.MinValue % (-1) --> 0
long.MinValue --> -9223372036854775808
-1 --> -1
-1 --> -1
long.MinValue % (-1) --> 0
long.MinValue --> -9223372036854775808
-1 --> -1
-1 --> -1
int.MinValue / (-1) --> BAD
int.MinValue --> -2147483648
-1 --> -1
int.MinValue / (-1) --> -2147483648
int.MinValue --> -2147483648
-1 --> -1
long.MinValue / (-1) --> BAD
long.MinValue --> -9223372036854775808
-1 --> -1
-1 --> -1
long.MinValue / (-1) --> -9223372036854775808
long.MinValue --> -9223372036854775808
-1 --> -1
-1 --> -1
null --> null
cdiv --> BAD
cdivl --> BAD";

            Assert.Equal(expected, actual);
        }

        [Fact]
        public void CS0220ERR_CheckedOverflow01()
        {
            var text =
@"class TestClass
{
    const int x = 1000000;
    const int y = 1000000;

    public int MethodCh()
    {
        int z = (x * y);   // CS0220
        return z;
    }

    public int MethodUnCh()
    {
        unchecked
        {
            int z = (x * y);
            return z;
        }
    }
}
";
            CreateCompilationWithMscorlib(text).VerifyDiagnostics(
                // (8,18): error CS0220: The operation overflows at compile time in checked mode
                Diagnostic(ErrorCode.ERR_CheckedOverflow, "x * y"));
        }

        [Fact]
        public void CS0220ERR_CheckedOverflow02()
        {
            string text =
@"enum E : uint { A, B = A - 1 }
";
            CreateCompilationWithMscorlib(text).VerifyDiagnostics(
                // (1,24): error CS0220: The operation overflows at compile time in checked mode
                Diagnostic(ErrorCode.ERR_CheckedOverflow, "A - 1"));
        }

        [Fact]
        public void CS0220ERR_CheckedOverflow_Enums()
        {
            string text =
@"enum E : uint { A = uint.MaxValue }
class C
{
    const uint F = (uint)(E.A + 1); // CS0220
    const uint G = unchecked((uint)(E.A + 2));
    const uint H = checked((uint)(E.A + 3)); // CS0220
}
";
            CreateCompilationWithMscorlib(text).VerifyDiagnostics(
                // (4,27): error CS0220: The operation overflows at compile time in checked mode
                Diagnostic(ErrorCode.ERR_CheckedOverflow, "E.A + 1"),
                // (6,35): error CS0220: The operation overflows at compile time in checked mode
                Diagnostic(ErrorCode.ERR_CheckedOverflow, "E.A + 3"));
        }

        [Fact]
        public void MultiplyingConstantsInCheckedStatement()
        {
            // multiplying constants in checked statement that causes overflow behaves like unchecked

            var source = @"
public class foo
{
    const int i = 1000000;
    const int j = 1000000;

    public static void Main()
    {
        checked
        {
            int k = i * j;
        }
    }
}
";
            var comp = CreateCompilationWithMscorlib(source);
            comp.VerifyDiagnostics(
                // (11,21): error CS0220: The operation overflows at compile time in checked mode
                //             int k = i * j;
                Diagnostic(ErrorCode.ERR_CheckedOverflow, "i * j").WithLocation(11, 21));
        }

        [Fact]
        public void ExpressionsInDefaultCheckedContext()
        {
            // Expressions which are in unchecked statement are in explicitly unchecked context.
            // Expressions which are out of unchecked statement are in default checked context.

            var source = @"
class Program
{
    static void Main()
    {
        int r = 0;

        r = int.MaxValue + 1;

        unchecked { r = int.MaxValue + 1; };
    }
}
";
            var comp = CreateCompilationWithMscorlib(source);
            comp.VerifyDiagnostics(
                // (8,13): error CS0220: The operation overflows at compile time in checked mode
                //         r = int.MaxValue + 1;
                Diagnostic(ErrorCode.ERR_CheckedOverflow, "int.MaxValue + 1"));
        }

        [Fact]
        public void MethodInvocationExpressionInCheckedOrUncheckedStatement()
        {
            // Overflow checking context with use method invocation expression in checked/unchecked statement

            var source = @"
class Program
{
    static int M1(int i)
    {
        int r = int.MaxValue + 1;
        checked
        {
            r = int.MaxValue + 1;
        }
        return r;
    }

    static void Main()
    {
        int r = 0;
        r = int.MaxValue + 1;
        unchecked
        {
            r = M1(int.MaxValue + 1);
        }

        checked
        {
            r = M1(int.MaxValue + 1);
        }
    }
}
";
            var comp = CreateCompilationWithMscorlib(source);
            comp.VerifyDiagnostics(
                    // (6,17): error CS0220: The operation overflows at compile time in checked mode
                    //         int r = int.MaxValue + 1;
                    Diagnostic(ErrorCode.ERR_CheckedOverflow, "int.MaxValue + 1"),
                    // (9,17): error CS0220: The operation overflows at compile time in checked mode
                    //         r = int.MaxValue + 1;
                    Diagnostic(ErrorCode.ERR_CheckedOverflow, "int.MaxValue + 1"),
                    // (17,13): error CS0220: The operation overflows at compile time in checked mode
                    //         r = int.MaxValue + 1;
                    Diagnostic(ErrorCode.ERR_CheckedOverflow, "int.MaxValue + 1"),
                    // (25,20): error CS0220: The operation overflows at compile time in checked mode
                    //         r = M1(int.MaxValue + 1);
                    Diagnostic(ErrorCode.ERR_CheckedOverflow, "int.MaxValue + 1"));
        }

        [Fact]
        public void AnonymousFunctionAndLambdaExpressionInUncheckedStatement()
        {
            // Overflow checking context with use anonymous function expression in unchecked statement

            var source = @"
class Program
{
    delegate int D1(int i);
    static void Main()
    {
        int r = 0;
        D1 d1;
        r = int.MaxValue + 1;
        unchecked
        {
            r = int.MaxValue + 1;
            d1 = delegate (int i)
            {
                int r1 = int.MaxValue + 1;
                checked { r1 = int.MaxValue + 1; }
                return r1;
            };
        }
        unchecked
        {
            r = int.MaxValue + 1;
            d1 = i => int.MaxValue + 1 + checked(0 + 0);
        }
        unchecked
        {
            r = int.MaxValue + 1;
            d1 = i => 0 + 0 + checked(int.MaxValue + 1);
        }

        unchecked
        {
            r = int.MaxValue + 1;
            d1 = new D1(delegate (int i)
            {
                int r1 = int.MaxValue + 1;
                checked { r1 = int.MaxValue + 1; }
                return r1;
            });
        }
        unchecked
        {
            r = int.MaxValue + 1;
            d1 = new D1(i => int.MaxValue + 1 + checked(0 + 0));
        }
        unchecked
        {
            r = int.MaxValue + 1;
            d1 = new D1(i => 0 + 0 + checked(int.MaxValue + 1));
        }
    }
}
";
            var comp = CreateCompilationWithMscorlib(source);
            comp.VerifyDiagnostics(
                // (9,13): error CS0220: The operation overflows at compile time in checked mode
                //         r = int.MaxValue + 1;
                Diagnostic(ErrorCode.ERR_CheckedOverflow, "int.MaxValue + 1"),
                // (16,32): error CS0220: The operation overflows at compile time in checked mode
                //         checked { r1 = int.MaxValue + 1; }
                Diagnostic(ErrorCode.ERR_CheckedOverflow, "int.MaxValue + 1"),
                // (28,39): error CS0220: The operation overflows at compile time in checked mode
                //         d1 = i => 0 + 0 + checked(int.MaxValue + 1);
                Diagnostic(ErrorCode.ERR_CheckedOverflow, "int.MaxValue + 1"),
                // (37,32): error CS0220: The operation overflows at compile time in checked mode
                //         checked { r1 = int.MaxValue + 1; }
                Diagnostic(ErrorCode.ERR_CheckedOverflow, "int.MaxValue + 1"),
                // (49,46): error CS0220: The operation overflows at compile time in checked mode
                //         d1 = new D1(i => 0 + 0 + checked(int.MaxValue + 1));
                Diagnostic(ErrorCode.ERR_CheckedOverflow, "int.MaxValue + 1"));
        }

        [Fact]
        public void AnonymousFunctionAndLambdaExpressionInCheckedStatement()
        {
            // Overflow checking context with use anonymous function expression in checked statement

            var source = @"
class Program
{
    delegate int D1(int i);
    static void Main()
    {
        int r = 0;
        D1 d1;
        checked
        {
            r = int.MaxValue + 1;
            d1 = i => int.MaxValue + 1 + unchecked(0 + 0);
        }
        checked
        {
            r = int.MaxValue + 1;
            d1 = i => 0 + 0 + unchecked(int.MaxValue + 1);
        }
        checked
        {
            r = int.MaxValue + 1;
            d1 = new D1(i => int.MaxValue + 1 + unchecked(0 + 0));
        }
        checked
        {
            r = int.MaxValue + 1;
            d1 = new D1(i => 0 + 0 + unchecked(int.MaxValue + 1));
        }
    }
}
";
            var comp = CreateCompilationWithMscorlib(source);
            comp.VerifyDiagnostics(
                // (11,17): error CS0220: The operation overflows at compile time in checked mode
                //             r = int.MaxValue + 1;
                Diagnostic(ErrorCode.ERR_CheckedOverflow, "int.MaxValue + 1"),
                // (12,23): error CS0220: The operation overflows at compile time in checked mode
                //             d1 = i => int.MaxValue + 1 + unchecked(0 + 0);
                Diagnostic(ErrorCode.ERR_CheckedOverflow, "int.MaxValue + 1"),
                // (16,17): error CS0220: The operation overflows at compile time in checked mode
                //             r = int.MaxValue + 1;
                Diagnostic(ErrorCode.ERR_CheckedOverflow, "int.MaxValue + 1"),
                // (21,17): error CS0220: The operation overflows at compile time in checked mode
                //             r = int.MaxValue + 1;
                Diagnostic(ErrorCode.ERR_CheckedOverflow, "int.MaxValue + 1"),
                // (22,30): error CS0220: The operation overflows at compile time in checked mode
                //             d1 = new D1(i => int.MaxValue + 1 + unchecked(0 + 0));
                Diagnostic(ErrorCode.ERR_CheckedOverflow, "int.MaxValue + 1"),
                // (26,17): error CS0220: The operation overflows at compile time in checked mode
                //             r = int.MaxValue + 1;
                Diagnostic(ErrorCode.ERR_CheckedOverflow, "int.MaxValue + 1"));
        }

        [Fact]
        public void TestConstantFields()
        {
            var source =
@"class C
{
    const int x = 1;
    const int y = x + 1;

    void M()
    {
        const int z = x + y;
    }
";
            var actual = ParseAndGetConstantFoldingSteps(source);

            var expected =
@"x + y --> 3
x --> 1
y --> 2";
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void TestBadConstantValue()
        {
            var source =
@"class C
{
    const int x = x;

    void M()
    {
        const int z = ((short)(+x)) + 1;
    }
";
            var actual = ParseAndGetConstantFoldingSteps(source);

            //the cast line is duplicated because there's also an implicit conversions
            var expected =
@"((short)(+x)) + 1 --> BAD
(short)(+x) --> BAD
(short)(+x) --> BAD
+x --> BAD
x --> BAD";
            Assert.Equal(expected, actual);
        }

        // Technically, these are binary operators, but it's clearer to test them separately.
        [Fact]
        public void TestLiftedEquality()
        {
            var source =
@"class C
{
    void M()
    {
        const bool a = 1 == null;
        const bool b = 1 != null;
        const bool c = null == (1 == null);
        const bool d = null != (1 != null);
    }
}";
            var actual = ParseAndGetConstantFoldingSteps(source);

            //the identity lines are implicit conversions
            var expected =
@"1 == null --> False
null --> null
1 != null --> True
null --> null
null == (1 == null) --> False
null --> null
1 == null --> False
null --> null
null != (1 != null) --> True
null --> null
1 != null --> True
null --> null";
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void ConstantEquals1()
        {
            ConstantValue nonNullValue = ConstantValue.Create(10);
            ConstantValue nullValue = null;

            ConstantEquals(nullValue, nullValue);
            ConstantEquals(nullValue, nonNullValue);
            ConstantEquals(nonNullValue, nullValue);
            ConstantEquals(nonNullValue, nonNullValue);
        }

        private static void ConstantEquals(ConstantValue a, ConstantValue b)
        {
            var same = object.ReferenceEquals(a, b);
            Assert.Equal(a == b, same);
            Assert.Equal(a != b, !same);
        }

        [Fact]
        public void TestConstantConditional()
        {
            var source =
@"class C
{
    const bool b = true ? false : true;

    void M()
    {
        const int z = b ? 1 + 2 : (int)4u;
    }
";
            var actual = ParseAndGetConstantFoldingSteps(source);

            var expected =
@"b ? 1 + 2 : (int)4u --> 4
b --> False
1 + 2 --> 3
(int)4u --> 4";
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void TestConstantConditionalBadValue()
        {
            var source =
@"class C
{
    const int i = i;
    const bool b = b;

    void M()
    {
        const int z = true ? 1 + i : (int)4u;
        const int z = b ? (uint)1 : (byte)2;
    }
";
            var actual = ParseAndGetConstantFoldingSteps(source);

            // Confirm that both branches are evaluated, even if the value is Bad
            // Duplicate "(byte)2" is because there's an implicit conversion to uint.
            var expected =
@"true ? 1 + i : (int)4u --> BAD
1 + i --> BAD
i --> BAD
(int)4u --> 4
b ? (uint)1 : (byte)2 --> BAD
b --> BAD
(uint)1 --> 1
(byte)2 --> 2
(byte)2 --> 2";
            Assert.Equal(expected, actual);
        }

        // Constant fields should be bound in the declaration phase.
        [Fact]
        public void TestConstantEvalAtDeclarationPhase()
        {
            var source =
@"class C
{
    const string F = F;
}";
            var compilation = CreateCompilationWithMscorlib(source);
            compilation.GetDeclarationDiagnostics().Verify(
                // (3,18): error CS0110: The evaluation of the constant value for 'C.F' involves a circular definition
                Diagnostic(CSharp.ErrorCode.ERR_CircConstValue, "F").WithArguments("C.F").WithLocation(3, 18));
        }

        [Fact]
        public void TestConstantEvalAcrossCompilations()
        {
            var source1 =
@"public class A
{
    public const string A1 = null;
}
public class B
{
    public const string B1 = A.A1;
}
public class C
{
    public const string C1 = B.B1;
}";
            var source2 =
@"public class D
{
    public const string D1 = E.E1;
}
public class E
{
    public const string E1 = C.C1;
}";
            var compilation1 = CreateCompilationWithMscorlib(source1);
            var compilation2 = CreateCompilationWithMscorlib(source2, new MetadataReference[] { new CSharpCompilationReference(compilation1) });
            compilation2.VerifyDiagnostics();
            compilation1.VerifyDiagnostics();
        }

        [Fact]
        public void TestCyclicConstantEvalAcrossCompilations()
        {
            var source1 =
@"public class A
{
    public const string A1 = B.B1;
}
public class B
{
    public const string B1 = A.A1;
}
public class C
{
    public const string C1 = B.B1;
}";
            var source2 =
@"public class D
{
    public const string D1 = D1;
}";
            var source3 =
@"public class E
{
    public const string E1 = F.F1;
}
public class F
{
    public const string F1 = C.C1;
}";
            var source4 =
@"public class G
{
    public const string G1 = F.F1 + D.D1;
}";
            var compilation1 = CreateCompilationWithMscorlib(source1);
            var reference1 = new CSharpCompilationReference(compilation1);
            var compilation2 = CreateCompilationWithMscorlib(source2);
            var reference2 = new CSharpCompilationReference(compilation2);
            var compilation3 = CreateCompilationWithMscorlib(source3, new MetadataReference[] { reference1 });
            var reference3 = new CSharpCompilationReference(compilation3);
            var compilation4 = CreateCompilationWithMscorlib(source4, new MetadataReference[] { reference2, reference3 });
            compilation4.VerifyDiagnostics();
            compilation3.VerifyDiagnostics();
            compilation2.VerifyDiagnostics(
                // (3,25): error CS0110: The evaluation of the constant value for 'D.D1' involves a circular definition
                //     public const string D1 = D1;
                Diagnostic(ErrorCode.ERR_CircConstValue, "D1").WithArguments("D.D1").WithLocation(3, 25));
            compilation1.VerifyDiagnostics(
                // (3,25): error CS0110: The evaluation of the constant value for 'A.A1' involves a circular definition
                //     public const string A1 = B.B1;
                Diagnostic(ErrorCode.ERR_CircConstValue, "A1").WithArguments("A.A1").WithLocation(3, 25));
        }

        [Fact]
        public void TestConstantValueInsideAttributes()
        {
            var tree = SyntaxFactory.ParseSyntaxTree(@"
class c1
{
    const int A = 1;
    const int B = 2;

    class MyAttribute : Attribute
    {
        MyAttribute(int i) { }
    }

    [MyAttribute(A + B + 3)]
    void Foo()
    {
    }
}");
            var expr = tree.GetCompilationUnitRoot().DescendantNodes().OfType<BinaryExpressionSyntax>().First();
            var comp = CreateCompilationWithMscorlib(tree);
            var constantValue = comp.GetSemanticModel(tree).GetConstantValue(expr);
            Assert.True(constantValue.HasValue);
            Assert.Equal(constantValue.Value, 6);
        }

        [WorkItem(544620, "DevDiv")]
        [Fact]
        public void NoConstantValueForOverflows()
        {
            var tree = SyntaxFactory.ParseSyntaxTree(@"
class c1
{
    const byte Z1 = 300;
    const byte Z2 = (byte)300;
}");

            var compilation = CreateCompilationWithMscorlib(tree);
            compilation.VerifyDiagnostics(
                // (4,21): error CS0031: Constant value '300' cannot be converted to a 'byte'
                //     const byte Z1 = 300;
                Diagnostic(ErrorCode.ERR_ConstOutOfRange, "300").WithArguments("300", "byte"),
                // (5,21): error CS0221: Constant value '300' cannot be converted to a 'byte' (use 'unchecked' syntax to override)
                //     const byte Z2 = (byte)300;
                Diagnostic(ErrorCode.ERR_ConstOutOfRangeChecked, "(byte)300").WithArguments("300", "byte")
                );

            var symbol = compilation.GlobalNamespace.GetTypeMembers("c1").First().GetMembers("Z1").First();
            Assert.False(((FieldSymbol)symbol).HasConstantValue);

            symbol = compilation.GlobalNamespace.GetTypeMembers("c1").First().GetMembers("Z2").First();
            Assert.False(((FieldSymbol)symbol).HasConstantValue);
        }

        [WorkItem(545965, "DevDiv")]
        [Fact]
        public void CircularConstantReportingRace()
        {
            const int numConstants = 5;

            var template = @"
class C{0}
{{
    public const int X = C{1}.X;
}}";
            var range = Enumerable.Range(0, numConstants);

            var source = string.Join(Environment.NewLine, range.Select(i =>
                string.Format(template, i, (i + 1) % numConstants)));

            var compilation = CreateCompilationWithMscorlib(source);
            var global = compilation.GlobalNamespace;

            var types = range.Select(i => global.GetMember<NamedTypeSymbol>("C" + i));

            // Complete all the types at the same time.
            Parallel.ForEach(types, t => t.ForceComplete(null, default(CancellationToken)));

            compilation.VerifyDiagnostics(
                // (4,22): error CS0110: The evaluation of the constant value for 'C0.X' involves a circular definition
                //     public const int X = C1.X;
                Diagnostic(ErrorCode.ERR_CircConstValue, "X").WithArguments("C0.X"));
        }

        [WorkItem(545965, "DevDiv")]
        [Fact]
        public void MultiCircularConstantReportingRace()
        {
            const int numConstants = 10;

            var template = @"
class C{0}
{{
    public const int X = C{1}.X + C{2}.X;
}}";
            var range = Enumerable.Range(0, numConstants);

            var source = string.Join(Environment.NewLine, range.Select(i =>
                i == 0 ? string.Format(template, i, i + 1, i + 1) :
                i == (numConstants - 1) ? string.Format(template, i, i - 1, i - 1) :
                string.Format(template, i, i - 1, i + 1)));

            var compilation = CreateCompilationWithMscorlib(source);
            var global = compilation.GlobalNamespace;

            var types = range.Select(i => global.GetMember<NamedTypeSymbol>("C" + i));

            // Complete all the types at the same time.
            Parallel.ForEach(types, t => t.ForceComplete(null, default(CancellationToken)));

            // All but C9.X, which is not (lexically) first in any cycle.
            compilation.VerifyDiagnostics(
                // (4,22): error CS0110: The evaluation of the constant value for 'C0.X' involves a circular definition
                //     public const int X = C1.X + C1.X;
                Diagnostic(ErrorCode.ERR_CircConstValue, "X").WithArguments("C0.X"),
                // (9,22): error CS0110: The evaluation of the constant value for 'C1.X' involves a circular definition
                //     public const int X = C0.X + C2.X;
                Diagnostic(ErrorCode.ERR_CircConstValue, "X").WithArguments("C1.X"),
                // (14,22): error CS0110: The evaluation of the constant value for 'C2.X' involves a circular definition
                //     public const int X = C1.X + C3.X;
                Diagnostic(ErrorCode.ERR_CircConstValue, "X").WithArguments("C2.X"),
                // (19,22): error CS0110: The evaluation of the constant value for 'C3.X' involves a circular definition
                //     public const int X = C2.X + C4.X;
                Diagnostic(ErrorCode.ERR_CircConstValue, "X").WithArguments("C3.X"),
                // (24,22): error CS0110: The evaluation of the constant value for 'C4.X' involves a circular definition
                //     public const int X = C3.X + C5.X;
                Diagnostic(ErrorCode.ERR_CircConstValue, "X").WithArguments("C4.X"),
                // (29,22): error CS0110: The evaluation of the constant value for 'C5.X' involves a circular definition
                //     public const int X = C4.X + C6.X;
                Diagnostic(ErrorCode.ERR_CircConstValue, "X").WithArguments("C5.X"),
                // (34,22): error CS0110: The evaluation of the constant value for 'C6.X' involves a circular definition
                //     public const int X = C5.X + C7.X;
                Diagnostic(ErrorCode.ERR_CircConstValue, "X").WithArguments("C6.X"),
                // (39,22): error CS0110: The evaluation of the constant value for 'C7.X' involves a circular definition
                //     public const int X = C6.X + C8.X;
                Diagnostic(ErrorCode.ERR_CircConstValue, "X").WithArguments("C7.X"),
                // (44,22): error CS0110: The evaluation of the constant value for 'C8.X' involves a circular definition
                //     public const int X = C7.X + C9.X;
                Diagnostic(ErrorCode.ERR_CircConstValue, "X").WithArguments("C8.X"));
        }

        [WorkItem(545965, "DevDiv")]
        [Fact]
        public void CircularEnumReportingRace()
        {
            const int numConstants = 5;

            var template = @"
enum E{0}
{{
    X = E{1}.X
}}";
            var range = Enumerable.Range(0, numConstants);

            var source = string.Join(Environment.NewLine, range.Select(i =>
                string.Format(template, i, (i + 1) % numConstants)));

            var compilation = CreateCompilationWithMscorlib(source);
            var global = compilation.GlobalNamespace;

            var types = range.Select(i => global.GetMember<NamedTypeSymbol>("E" + i));

            // Complete all the types at the same time.
            Parallel.ForEach(types, t => t.ForceComplete(null, default(CancellationToken)));

            compilation.VerifyDiagnostics(
                // (4,5): error CS0110: The evaluation of the constant value for 'E0.X' involves a circular definition
                //     X = E1.X
                Diagnostic(ErrorCode.ERR_CircConstValue, "X").WithArguments("E0.X"));
        }

        [WorkItem(545965, "DevDiv")]
        [Fact]
        public void MultiCircularEnumReportingRace()
        {
            const int numConstants = 10;

            var template = @"
enum E{0}
{{
    X = E{1}.X | E{2}.X
}}";
            var range = Enumerable.Range(0, numConstants);

            var source = string.Join(Environment.NewLine, range.Select(i =>
                i == 0 ? string.Format(template, i, i + 1, i + 1) :
                i == (numConstants - 1) ? string.Format(template, i, i - 1, i - 1) :
                string.Format(template, i, i - 1, i + 1)));

            var compilation = CreateCompilationWithMscorlib(source);
            var global = compilation.GlobalNamespace;

            var types = range.Select(i => global.GetMember<NamedTypeSymbol>("E" + i));

            // Complete all the types at the same time.
            Parallel.ForEach(types, t => t.ForceComplete(null, default(CancellationToken)));

            // All but E9.X, which is not (lexically) first in any cycle.
            compilation.VerifyDiagnostics(
                // (4,5): error CS0110: The evaluation of the constant value for 'E0.X' involves a circular definition
                //     X = E1.X | E1.X
                Diagnostic(ErrorCode.ERR_CircConstValue, "X").WithArguments("E0.X").WithLocation(4, 5),
                // (9,5): error CS0110: The evaluation of the constant value for 'E1.X' involves a circular definition
                //     X = E0.X | E2.X
                Diagnostic(ErrorCode.ERR_CircConstValue, "X").WithArguments("E1.X").WithLocation(9, 5),
                // (14,5): error CS0110: The evaluation of the constant value for 'E2.X' involves a circular definition
                //     X = E1.X | E3.X
                Diagnostic(ErrorCode.ERR_CircConstValue, "X").WithArguments("E2.X").WithLocation(14, 5),
                // (19,5): error CS0110: The evaluation of the constant value for 'E3.X' involves a circular definition
                //     X = E2.X | E4.X
                Diagnostic(ErrorCode.ERR_CircConstValue, "X").WithArguments("E3.X").WithLocation(19, 5),
                // (24,5): error CS0110: The evaluation of the constant value for 'E4.X' involves a circular definition
                //     X = E3.X | E5.X
                Diagnostic(ErrorCode.ERR_CircConstValue, "X").WithArguments("E4.X").WithLocation(24, 5),
                // (29,5): error CS0110: The evaluation of the constant value for 'E5.X' involves a circular definition
                //     X = E4.X | E6.X
                Diagnostic(ErrorCode.ERR_CircConstValue, "X").WithArguments("E5.X").WithLocation(29, 5),
                // (34,5): error CS0110: The evaluation of the constant value for 'E6.X' involves a circular definition
                //     X = E5.X | E7.X
                Diagnostic(ErrorCode.ERR_CircConstValue, "X").WithArguments("E6.X").WithLocation(34, 5),
                // (39,5): error CS0110: The evaluation of the constant value for 'E7.X' involves a circular definition
                //     X = E6.X | E8.X
                Diagnostic(ErrorCode.ERR_CircConstValue, "X").WithArguments("E7.X").WithLocation(39, 5),
                // (44,5): error CS0110: The evaluation of the constant value for 'E8.X' involves a circular definition
                //     X = E7.X | E9.X
                Diagnostic(ErrorCode.ERR_CircConstValue, "X").WithArguments("E8.X").WithLocation(44, 5));
        }

        [WorkItem(545965, "DevDiv")]
        [Fact]
        public void CircularImplicitEnumReportingRace()
        {
            const int numEnums = 5;

            var template = @"
enum E{0}
{{
    A = E{1}.D,
    B,
    C,
    D,
}}";
            var range = Enumerable.Range(0, numEnums);

            var source = string.Join(Environment.NewLine, range.Select(i =>
                string.Format(template, i, (i + 1) % numEnums)));

            var compilation = CreateCompilationWithMscorlib(source);
            var global = compilation.GlobalNamespace;

            var types = range.Select(i => global.GetMember<NamedTypeSymbol>("E" + i));

            // Complete all the types at the same time.
            Parallel.ForEach(types, t => t.ForceComplete(null, default(CancellationToken)));

            compilation.VerifyDiagnostics(
                // (4,5): error CS0110: The evaluation of the constant value for 'E0.A' involves a circular definition
                //     A = E1.D,
                Diagnostic(ErrorCode.ERR_CircConstValue, "A").WithArguments("E0.A"));
        }

        [WorkItem(545965, "DevDiv")]
        [Fact]
        public void MultiCircularImplicitEnumReportingRace()
        {
            const int numEnums = 10;

            var template = @"
enum E{0}
{{
    A = E{1}.D | E{2}.D,
    B,
    C,
    D,
}}";
            var range = Enumerable.Range(0, numEnums);

            var source = string.Join(Environment.NewLine, range.Select(i =>
                i == 0 ? string.Format(template, i, i + 1, i + 1) :
                i == (numEnums - 1) ? string.Format(template, i, i - 1, i - 1) :
                string.Format(template, i, i - 1, i + 1)));

            var compilation = CreateCompilationWithMscorlib(source);
            var global = compilation.GlobalNamespace;

            var types = range.Select(i => global.GetMember<NamedTypeSymbol>("E" + i));

            // Complete all the types at the same time.
            Parallel.ForEach(types, t => t.ForceComplete(null, default(CancellationToken)));

            // All but E9.X, which is not (lexically) first in any cycle.
            compilation.VerifyDiagnostics(
                // (4,5): error CS0110: The evaluation of the constant value for 'E0.A' involves a circular definition
                //     A = E1.D | E1.D,
                Diagnostic(ErrorCode.ERR_CircConstValue, "A").WithArguments("E0.A"),
                // (12,5): error CS0110: The evaluation of the constant value for 'E1.A' involves a circular definition
                //     A = E0.D | E2.D,
                Diagnostic(ErrorCode.ERR_CircConstValue, "A").WithArguments("E1.A"),
                // (20,5): error CS0110: The evaluation of the constant value for 'E2.A' involves a circular definition
                //     A = E1.D | E3.D,
                Diagnostic(ErrorCode.ERR_CircConstValue, "A").WithArguments("E2.A"),
                // (28,5): error CS0110: The evaluation of the constant value for 'E3.A' involves a circular definition
                //     A = E2.D | E4.D,
                Diagnostic(ErrorCode.ERR_CircConstValue, "A").WithArguments("E3.A"),
                // (36,5): error CS0110: The evaluation of the constant value for 'E4.A' involves a circular definition
                //     A = E3.D | E5.D,
                Diagnostic(ErrorCode.ERR_CircConstValue, "A").WithArguments("E4.A"),
                // (44,5): error CS0110: The evaluation of the constant value for 'E5.A' involves a circular definition
                //     A = E4.D | E6.D,
                Diagnostic(ErrorCode.ERR_CircConstValue, "A").WithArguments("E5.A"),
                // (52,5): error CS0110: The evaluation of the constant value for 'E6.A' involves a circular definition
                //     A = E5.D | E7.D,
                Diagnostic(ErrorCode.ERR_CircConstValue, "A").WithArguments("E6.A"),
                // (60,5): error CS0110: The evaluation of the constant value for 'E7.A' involves a circular definition
                //     A = E6.D | E8.D,
                Diagnostic(ErrorCode.ERR_CircConstValue, "A").WithArguments("E7.A"),
                // (68,5): error CS0110: The evaluation of the constant value for 'E8.A' involves a circular definition
                //     A = E7.D | E9.D,
                Diagnostic(ErrorCode.ERR_CircConstValue, "A").WithArguments("E8.A"));
        }

        [Fact, WorkItem(544941, "DevDiv")]
        public static void ConstantNullNotObject()
        {
            var source =
@"class MyTest { }
class MyClass
{
    const MyTest test = null;
    const bool b = test == null;
    public static int Main()
    {
        const bool bb = test != null;
        return bb ? 1 : 0;
    }
}";
            CreateCompilationWithMscorlib(source).VerifyDiagnostics();
        }

        // We used to return constant zero in unchecked conversion from a floating-point type to an integral type
        // in a constant expression if the value being converted is not within the target type.
        // The C# spec says that in this case the result of the conversion is an unspecified value of the destination type.
        // Zero is a perfectly valid unspecified value, so that behavior was formally correct.
        // But it did not agree with the behavior of the native C# compiler, that apparently returned a value that
        // would resulted from a runtime conversion with normal CLR overflow behavior.
        // To avoid breaking programs that might accidentally rely on that unspecified behavior
        // we now match the native compiler behavior, and we are going to keep this behavior for compatibility.
        [Fact, WorkItem(1020273, "DevDiv")]
        public void Bug1020273()
        {
            string source = @"
using System;
 
class Program
{
    static void Main()
    {
        double aslocal = 65535.17567;
        Console.WriteLine(""As local: {0}"", unchecked((short)aslocal));
        Console.WriteLine(""Inline  : {0}"", unchecked((short)65535.17567));
    }
}";
            string expectedOutput = @"As local: -1
Inline  : -1";

            CompileAndVerify(source, expectedOutput: expectedOutput);
        }

        [Fact, WorkItem(1098197, "DevDiv")]
        public static void Bug1098197_01()
        {
            var source =
@"
class Program
{
    static void Main(string[] args)
    {
        void f() { if () const int i = 0; }
    }
}
";
            CreateCompilationWithMscorlib(source).VerifyDiagnostics(
    // (6,9): error CS8058: Feature 'local functions' is only available in 'experimental' language version.
    //         void f() { if () const int i = 0; }
    Diagnostic(ErrorCode.ERR_FeatureIsExperimental, "void f() { if () const int i = 0; }").WithArguments("local functions").WithLocation(6, 9),
    // (6,24): error CS1525: Invalid expression term ')'
    //         void f() { if () const int i = 0; }
    Diagnostic(ErrorCode.ERR_InvalidExprTerm, ")").WithArguments(")").WithLocation(6, 24),
    // (6,26): error CS1023: Embedded statement cannot be a declaration or labeled statement
    //         void f() { if () const int i = 0; }
    Diagnostic(ErrorCode.ERR_BadEmbeddedStmt, "const int i = 0;").WithLocation(6, 26),
    // (6,14): warning CS0168: The variable 'f' is declared but never used
    //         void f() { if () const int i = 0; }
    Diagnostic(ErrorCode.WRN_UnreferencedVar, "f").WithArguments("f").WithLocation(6, 14)
                );
        }

        [Fact]
        public static void DoubleRecursiveConst()
        {
            var source =
@"using System;
class C
{
    public static void Main()
    {
        const Func<int> a = () => { const int b = a(); return 1; };
    }
}";
            CreateCompilationWithMscorlib(source).VerifyDiagnostics(
                // (6,51): error CS0133: The expression being assigned to 'b' must be constant
                //         const Func<int> a = () => { const int b = a(); return 1; };
                Diagnostic(ErrorCode.ERR_NotConstantExpression, "a()").WithArguments("b").WithLocation(6, 51)
                );
        }

        [Fact]
        public static void RecursiveConst()
        {
            var source =
@"class C
{
    public static void Main()
    {
        const int z = 1 + z + 1;
    }
}";
            CreateCompilationWithMscorlib(source).VerifyDiagnostics(
    // (5,27): error CS0110: The evaluation of the constant value for 'z' involves a circular definition
    //         const int z = 1 + z + 1;
    Diagnostic(ErrorCode.ERR_CircConstValue, "z").WithArguments("z").WithLocation(5, 27)
                );
        }

        [Fact, WorkItem(1098197, "DevDiv")]
        public static void Bug1098197_02()
        {
            var source =
@"
void f() { if () const int i = 0; }
";
            CreateCompilationWithMscorlib(source).VerifyDiagnostics(
    // (2,16): error CS1525: Invalid expression term ')'
    // void f() { if () const int i = 0; }
    Diagnostic(ErrorCode.ERR_InvalidExprTerm, ")").WithArguments(")").WithLocation(2, 16),
    // (2,18): error CS1023: Embedded statement cannot be a declaration or labeled statement
    // void f() { if () const int i = 0; }
    Diagnostic(ErrorCode.ERR_BadEmbeddedStmt, "const int i = 0;").WithLocation(2, 18)
                );
        }

        [Fact, WorkItem(1098605, "DevDiv")]
        public static void Bug1098605_01()
        {
            var source =
@"
 class C
 {
     static void Main(string[] args)
     {
        const string x1 = (string)(object)null;
        const string y1 = (string)(object)""y"";

        const string x2 = (object)null;
        const string y2 = (object)""y"";

        const object x3 = (string)null;
        const object y3 = ""y"";

        switch (args[0])
        {
            case (string)(object)null:
                break;
            case (string)(object)""b"":
                break;
            case (object)null:
                break;
            case (object)""b"":
                break;
        }

        System.Console.WriteLine("""", x1, x2, x3, y1, y2, y3);
        }
    }
";
            CreateCompilationWithMscorlib(source).VerifyDiagnostics(
    // (7,27): error CS0133: The expression being assigned to 'y1' must be constant
    //         const string y1 = (string)(object)"y";
    Diagnostic(ErrorCode.ERR_NotConstantExpression, @"(string)(object)""y""").WithArguments("y1").WithLocation(7, 27),
    // (9,27): error CS0266: Cannot implicitly convert type 'object' to 'string'. An explicit conversion exists (are you missing a cast?)
    //         const string x2 = (object)null;
    Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "(object)null").WithArguments("object", "string").WithLocation(9, 27),
    // (10,27): error CS0266: Cannot implicitly convert type 'object' to 'string'. An explicit conversion exists (are you missing a cast?)
    //         const string y2 = (object)"y";
    Diagnostic(ErrorCode.ERR_NoImplicitConvCast, @"(object)""y""").WithArguments("object", "string").WithLocation(10, 27),
    // (13,27): error CS0134: 'y3' is of type 'object'. A const field of a reference type other than string can only be initialized with null.
    //         const object y3 = "y";
    Diagnostic(ErrorCode.ERR_NotNullConstRefField, @"""y""").WithArguments("y3", "object").WithLocation(13, 27),
    // (19,13): error CS0150: A constant value is expected
    //             case (string)(object)"b":
    Diagnostic(ErrorCode.ERR_ConstantExpected, @"case (string)(object)""b"":").WithLocation(19, 13),
    // (21,18): error CS0266: Cannot implicitly convert type 'object' to 'string'. An explicit conversion exists (are you missing a cast?)
    //             case (object)null:
    Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "(object)null").WithArguments("object", "string").WithLocation(21, 18),
    // (23,18): error CS0266: Cannot implicitly convert type 'object' to 'string'. An explicit conversion exists (are you missing a cast?)
    //             case (object)"b":
    Diagnostic(ErrorCode.ERR_NoImplicitConvCast, @"(object)""b""").WithArguments("object", "string").WithLocation(23, 18)
                );
        }
    }


    internal sealed class BoundTreeSequencer : BoundTreeWalkerWithStackGuard
    {
        public static IEnumerable<BoundNode> GetNodes(BoundNode root)
        {
            var s = new BoundTreeSequencer();
            s.Visit(root);
            foreach (var node in s._list)
                yield return node;
        }

        private readonly List<BoundNode> _list;

        private BoundTreeSequencer()
        {
            _list = new List<BoundNode>();
        }

        public override BoundNode Visit(BoundNode node)
        {
            if (node != null) //e.g. static method invocations have null receivers
            {
                _list.Add(node);
            }
            return base.Visit(node);
        }

        protected override bool ConvertInsufficientExecutionStackExceptionToCancelledByStackGuardException()
        {
            return false;
        }
    }
}
