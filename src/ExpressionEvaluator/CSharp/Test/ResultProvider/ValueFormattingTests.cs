// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Globalization;
using Microsoft.CodeAnalysis.ExpressionEvaluator;
using Microsoft.VisualStudio.Debugger.Evaluation;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.ExpressionEvaluator.UnitTests
{
    public class ValueFormattingTests : CSharpResultProviderTestBase
    {
        [Fact]
        public void IntegralPrimitives()
        {
            // only testing a couple simple cases here...more tests live in ObjectDisplayTests...
            unchecked
            {
                Assert.Equal("1", FormatValue((ushort)1));
                Assert.Equal("65535", FormatValue((ushort)-1));
                Assert.Equal("1", FormatValue((int)1));
                Assert.Equal("-1", FormatValue((int)-1));

                Assert.Equal("0x01", FormatValue((sbyte)1, useHexadecimal: true));
                Assert.Equal("0xffffffff", FormatValue((sbyte)-1, useHexadecimal: true)); // As in dev11.
                Assert.Equal("0x0001", FormatValue((short)1, useHexadecimal: true));
                Assert.Equal("0xffffffff", FormatValue((short)-1, useHexadecimal: true)); // As in dev11.
                Assert.Equal("0x0000000000000001", FormatValue((ulong)1, useHexadecimal: true));
                Assert.Equal("0xffffffffffffffff", FormatValue((ulong)-1, useHexadecimal: true));
            }
        }

        [Fact]
        public void Double()
        {
            Assert.Equal("-1.7976931348623157E+308", FormatValue(double.MinValue));
            Assert.Equal("-1.1", FormatValue((double)-1.1));
            Assert.Equal("0", FormatValue((double)0));
            Assert.Equal("1.1", FormatValue((double)1.1));
            Assert.Equal("1.7976931348623157E+308", FormatValue(double.MaxValue));

            Assert.Equal("-Infinity", FormatValue(double.NegativeInfinity));
            Assert.Equal("Infinity", FormatValue(double.PositiveInfinity));
            Assert.Equal("NaN", FormatValue(double.NaN));
            Assert.Equal("4.94065645841247E-324", FormatValue(double.Epsilon));
        }

        [Fact]
        public void Float()
        {
            Assert.Equal("-3.40282347E+38", FormatValue(float.MinValue));
            Assert.Equal("-1.1", FormatValue((float)-1.1));
            Assert.Equal("0", FormatValue((float)0));
            Assert.Equal("1.1", FormatValue((float)1.1));
            Assert.Equal("3.40282347E+38", FormatValue(float.MaxValue));

            Assert.Equal("-Infinity", FormatValue(float.NegativeInfinity));
            Assert.Equal("Infinity", FormatValue(float.PositiveInfinity));
            Assert.Equal("NaN", FormatValue(float.NaN));
            Assert.Equal("1.401298E-45", FormatValue(float.Epsilon));
        }

        [Fact]
        public void Decimal()
        {
            Assert.Equal("-79228162514264337593543950335", FormatValue(decimal.MinValue));
            Assert.Equal("-1.1", FormatValue((decimal)-1.1));
            Assert.Equal("0", FormatValue((decimal)0));
            Assert.Equal("1.1", FormatValue((decimal)1.1));
            Assert.Equal("79228162514264337593543950335", FormatValue(decimal.MaxValue));
        }

        [Fact]
        public void Bool()
        {
            Assert.Equal("true", FormatValue(true));
            Assert.Equal("false", FormatValue(false));
        }

        [Fact]
        public void Char()
        {
            // We'll exhaustively test the first 256 code points (single-byte characters) as well
            // as a few double-byte characters.  Testing all possible characters takes too long.
            const string format = "{0} '{1}'";
            const string formatUsingHex = "0x{0:x4} '{1}'";
            char ch;
            for (ch = (char)0; ch < 0xff; ch++)
            {
                string expected;
                switch (ch)
                {
                    case '\0':
                        expected = "\\0";
                        break;
                    case '\t':
                        expected = "\\t";
                        break;
                    case '\f':
                        expected = "\\f";
                        break;
                    case '\r':
                        expected = "\\r";
                        break;
                    case '\n':
                        expected = "\\n";
                        break;
                    case '\a':
                        expected = "\\a";
                        break;
                    case '\b':
                        expected = "\\b";
                        break;
                    case '\v':
                        expected = "\\v";
                        break;
                    case '\'':
                        expected = "\\'";
                        break;
                    case '\\':
                        expected = "\\\\";
                        break;
                    default:
                        expected = FormatStringChar(ch);
                        break;
                }
                Assert.Equal(string.Format(format, (int)ch, expected), FormatValue(ch));
                Assert.Equal(string.Format(formatUsingHex, (int)ch, expected), FormatValue(ch, useHexadecimal: true));
            }

            ch = (char)0xabcd;
            Assert.Equal(string.Format(format, (int)ch, ch), FormatValue(ch));
            Assert.Equal(string.Format(formatUsingHex, (int)ch, ch), FormatValue(ch, useHexadecimal: true));

            ch = (char)0xfeef;
            Assert.Equal(string.Format(format, (int)ch, ch), FormatValue(ch));
            Assert.Equal(string.Format(formatUsingHex, (int)ch, ch), FormatValue(ch, useHexadecimal: true));

            ch = (char)0xffef;
            Assert.Equal("65519 '\\uffef'", FormatValue(ch));
            Assert.Equal("0xffef '\\uffef'", FormatValue(ch, useHexadecimal: true));

            ch = char.MaxValue;
            Assert.Equal("65535 '\\uffff'", FormatValue(ch));
            Assert.Equal("0xffff '\\uffff'", FormatValue(ch, useHexadecimal: true));
        }

        [Fact]
        public void String()
        {
            Assert.Equal("null", FormatNull<string>());
            Assert.Equal("null", FormatNull<string>(useHexadecimal: true));

            // We'll exhaustively test the first 256 code points (single-byte characters) as well
            // as a few multi-byte characters.  Testing all possible characters takes too long.
            string format = "\"{0}\"";
            for (char ch = (char)0; ch < 0xff; ch++)
            {
                string expected;
                switch (ch)
                {
                    case '\0':
                        expected = "\\0";
                        break;
                    case '\t':
                        expected = "\\t";
                        break;
                    case '\f':
                        expected = "\\f";
                        break;
                    case '\r':
                        expected = "\\r";
                        break;
                    case '\n':
                        expected = "\\n";
                        break;
                    case '\a':
                        expected = "\\a";
                        break;
                    case '\b':
                        expected = "\\b";
                        break;
                    case '\v':
                        expected = "\\v";
                        break;
                    case '"':
                        expected = "\\\"";
                        break;
                    case '\\':
                        expected = "\\\\";
                        break;
                    default:
                        expected = FormatStringChar(ch);
                        break;
                }
                Assert.Equal(string.Format(format, expected), FormatValue(ch.ToString()));
                Assert.Equal(string.Format(format, expected), FormatValue(ch.ToString(), useHexadecimal: true));
            }

            var s = ((char)0xabcd).ToString();
            Assert.Equal(string.Format(format, s), FormatValue(s));
            Assert.Equal(string.Format(format, s), FormatValue(s, useHexadecimal: true));

            s = ((char)0xfeef).ToString();
            Assert.Equal(string.Format(format, s), FormatValue(s));
            Assert.Equal(string.Format(format, s), FormatValue(s, useHexadecimal: true));

            s = ((char)0xffef).ToString();
            Assert.Equal("\"\\uffef\"", FormatValue(s));
            Assert.Equal("\"\\uffef\"", FormatValue(s, useHexadecimal: true));

            s = char.MaxValue.ToString();
            Assert.Equal("\"\\uffff\"", FormatValue(s));
            Assert.Equal("\"\\uffff\"", FormatValue(s, useHexadecimal: true));

            string multiByte = "\ud83c\udfc8"; // unicode surrogates properly paired representing a printable Unicode codepoint
            Assert.Equal(string.Format(format, "🏈"), FormatValue(multiByte));
            Assert.Equal(string.Format(format, "🏈"), FormatValue(multiByte, useHexadecimal: true));
            Assert.Equal("🏈", multiByte);

            multiByte = "\udbff\udfff"; // unicode surrogates representing an unprintable Unicode codepoint
            Assert.Equal(string.Format(format, "\\U0010ffff"), FormatValue(multiByte));
            Assert.Equal(string.Format(format, "\\U0010ffff"), FormatValue(multiByte, useHexadecimal: true));

            multiByte = "\udfc8\ud83c"; // unicode surrogates not properly paired (in the wrong order in this case)
            Assert.Equal(string.Format(format, "\\udfc8\\ud83c"), FormatValue(multiByte));
            Assert.Equal(string.Format(format, "\\udfc8\\ud83c"), FormatValue(multiByte, useHexadecimal: true));
        }

        private static string FormatStringChar(char c)
        {
            return (CharUnicodeInfo.GetUnicodeCategory(c) == UnicodeCategory.Control) ?
                $"\\u{((int)c).ToString("x4")}" :
                c.ToString();
        }

        [Fact]
        public void Void()
        {
            // Something happens but, in practice, we expect the debugger to recognize
            // that the value is of type void and turn it into the error string 
            // "Expression has been evaluated and has no value".
            Assert.Equal("{void}", FormatValue(null, typeof(void)));
        }

        [Fact]
        public void InvalidValue_1()
        {
            const string errorMessage = "An error has occurred.";
            var clrValue = CreateDkmClrValue(errorMessage, typeof(string), evalFlags: DkmEvaluationResultFlags.None, valueFlags: DkmClrValueFlags.Error);
            Assert.Equal(errorMessage, ((DkmFailedEvaluationResult)FormatResult("invalidIdentifier", clrValue)).ErrorMessage);
        }

        [Fact]
        public void InvalidValue_2()
        {
            const string errorMessage = "An error has occurred.";
            var clrValue = CreateDkmClrValue(errorMessage, typeof(int), evalFlags: DkmEvaluationResultFlags.None, valueFlags: DkmClrValueFlags.Error);
            Assert.Equal(errorMessage, ((DkmFailedEvaluationResult)FormatResult("invalidIdentifier", clrValue)).ErrorMessage);
        }

        [Fact]
        public void NonFlagsEnum()
        {
            var source = @"
enum E
{
    A = 1,
    B = 2,
}
";
            var assembly = GetAssembly(source);

            var type = assembly.GetType("E");

            Assert.Equal("0", FormatValue(0, type));
            Assert.Equal("A", FormatValue(1, type));
            Assert.Equal("B", FormatValue(2, type));
            Assert.Equal("3", FormatValue(3, type));
        }

        [Fact]
        public void NonFlagsEnum_Negative()
        {
            var source = @"
enum E
{
    A = -1,
    B = -2,
}
";
            var assembly = GetAssembly(source);

            var type = assembly.GetType("E");

            Assert.Equal("0", FormatValue(0, type));
            Assert.Equal("A", FormatValue(-1, type));
            Assert.Equal("B", FormatValue(-2, type));
            Assert.Equal("-3", FormatValue(-3, type));
        }

        [Fact]
        public void NonFlagsEnum_Order()
        {
            var source = @"
enum E1 
{
    A = 1,
    B = 1,
}

enum E2
{
    B = 1,
    A = 1,
}
";
            var assembly = GetAssembly(source);

            var e1 = assembly.GetType("E1");
            var e2 = assembly.GetType("E2");

            Assert.Equal("A", FormatValue(1, e1));
            Assert.Equal("A", FormatValue(1, e2));
        }

        [Fact]
        public void FlagsEnum()
        {
            var source = @"
using System;

[Flags]
enum E
{
    A = 1,
    B = 2,
}
";
            var assembly = GetAssembly(source);

            var type = assembly.GetType("E");

            Assert.Equal("0", FormatValue(0, type));
            Assert.Equal("A", FormatValue(1, type));
            Assert.Equal("B", FormatValue(2, type));
            Assert.Equal("A | B", FormatValue(3, type));
            Assert.Equal("4", FormatValue(4, type));
        }

        [Fact]
        public void FlagsEnum_Zero()
        {
            var source = @"
using System;

[Flags]
enum E
{
    None = 0,
    A = 1,
    B = 2,
}
";
            var assembly = GetAssembly(source);

            var type = assembly.GetType("E");

            Assert.Equal("None", FormatValue(0, type));
            Assert.Equal("A", FormatValue(1, type));
            Assert.Equal("B", FormatValue(2, type));
            Assert.Equal("A | B", FormatValue(3, type));
            Assert.Equal("4", FormatValue(4, type));
        }

        [Fact]
        public void FlagsEnum_Combination()
        {
            var source = @"
using System;

[Flags]
enum E
{
    None = 0,
    A = 1,
    B = 2,
    C = A | B,
}
";
            var assembly = GetAssembly(source);

            var type = assembly.GetType("E");

            Assert.Equal("None", FormatValue(0, type));
            Assert.Equal("A", FormatValue(1, type));
            Assert.Equal("B", FormatValue(2, type));
            Assert.Equal("C", FormatValue(3, type));
            Assert.Equal("4", FormatValue(4, type));
        }

        [Fact]
        public void FlagsEnum_Negative()
        {
            var source = @"
using System;

[Flags]
enum E
{
    None = 0,
    A = -1,
    B = -2,
}
";
            var assembly = GetAssembly(source);

            var type = assembly.GetType("E");

            Assert.Equal("None", FormatValue(0, type));
            Assert.Equal("A", FormatValue(-1, type));
            Assert.Equal("B", FormatValue(-2, type));
            Assert.Equal("-3", FormatValue(-3, type));
            Assert.Equal("-4", FormatValue(-4, type));
        }

        [Fact]
        public void FlagsEnum_Order()
        {
            var source = @"
using System;

[Flags]
enum E1 
{
    A = 1,
    B = 1,
    C = 2,
    D = 2,
}

[Flags]
enum E2
{
    D = 2,
    C = 2,
    B = 1,
    A = 1,
}
";
            var assembly = GetAssembly(source);

            var e1 = assembly.GetType("E1");
            var e2 = assembly.GetType("E2");

            Assert.Equal("0", FormatValue(0, e1));
            Assert.Equal("A", FormatValue(1, e1));
            Assert.Equal("C", FormatValue(2, e1));
            Assert.Equal("A | C", FormatValue(3, e1));

            Assert.Equal("0", FormatValue(0, e2));
            Assert.Equal("A", FormatValue(1, e2));
            Assert.Equal("C", FormatValue(2, e2));
            Assert.Equal("A | C", FormatValue(3, e2));
        }

        [Fact]
        public void Arrays()
        {
            var source = @"
namespace N
{
    public class A<T>
    {
        public class B<U>
        {
        }
    }
}
";
            var assembly = GetAssembly(source);
            var typeA = assembly.GetType("N.A`1");
            var typeB = typeA.GetNestedType("B`1");
            var constructedType = typeB.MakeGenericType(typeof(bool), typeof(long));

            var vectorInstance = Array.CreateInstance(constructedType, 2);
            var matrixInstance = Array.CreateInstance(constructedType, 3, 4);
            var irregularInstance = Array.CreateInstance(constructedType, new[] { 1, 2 }, new[] { 3, 4 });

            Assert.Equal("{N.A<bool>.B<long>[2]}", FormatValue(vectorInstance));
            Assert.Equal("{N.A<bool>.B<long>[3, 4]}", FormatValue(matrixInstance));
            Assert.Equal("{N.A<bool>.B<long>[3..3, 4..5]}", FormatValue(irregularInstance));

            Assert.Equal("{N.A<bool>.B<long>[0x00000002]}", FormatValue(vectorInstance, useHexadecimal: true));
            Assert.Equal("{N.A<bool>.B<long>[0x00000003, 0x00000004]}", FormatValue(matrixInstance, useHexadecimal: true));
            Assert.Equal("{N.A<bool>.B<long>[0x00000003..0x00000003, 0x00000004..0x00000005]}", FormatValue(irregularInstance, useHexadecimal: true));
        }

        [Fact]
        public void Pointers()
        {
            var pointerType = typeof(int).MakePointerType();
            var doublePointerType = pointerType.MakePointerType();

            Assert.Equal("0x00000001", FormatValue(1, pointerType, useHexadecimal: false)); // In hex, regardless.
            Assert.Equal("0x00000001", FormatValue(1, pointerType, useHexadecimal: true));

            Assert.Equal("0xffffffff", FormatValue(-1, doublePointerType, useHexadecimal: false)); // In hex, regardless.
            Assert.Equal("0xffffffff", FormatValue(-1, doublePointerType, useHexadecimal: true));
        }

        [Fact]
        public void Nullable()
        {
            var source = @"
namespace N
{
    public struct A<T>
    {
        public struct B<U>
        {
            public override string ToString()
            {
                return ""ToString() called."";
            }
        }
    }
}
";
            var assembly = GetAssembly(source);
            var typeA = assembly.GetType("N.A`1");
            var typeB = typeA.GetNestedType("B`1");
            var constructedType = typeB.MakeGenericType(typeof(bool), typeof(long));
            var nullableType = typeof(Nullable<>);
            var nullableConstructedType = nullableType.MakeGenericType(constructedType);
            var nullableInt = nullableType.MakeGenericType(typeof(int));

            Assert.Equal("null", FormatValue(null, nullableConstructedType));
            Assert.Equal("{ToString() called.}", FormatValue(constructedType.Instantiate(), nullableConstructedType));

            Assert.Equal("null", FormatValue(null, nullableInt));
            Assert.Equal("1", FormatValue(1, nullableInt));
        }

        [Fact]
        public void ToStringOverrides()
        {
            var source = @"
public class A<T>
{
}

public class B : A<int>
{
    public override string ToString()
    {
        return ""B.ToString()"";
    }
}

public class C : B
{
}
";
            var assembly = GetAssembly(source);
            var typeC = assembly.GetType("C");
            var typeB = assembly.GetType("B");
            var typeA = typeB.BaseType;

            Assert.Equal("null", FormatValue(null, typeA));
            Assert.Equal("null", FormatValue(null, typeB));
            Assert.Equal("null", FormatValue(null, typeC));

            Assert.Equal("{A<int>}", FormatValue(typeA.Instantiate()));
            Assert.Equal("{B.ToString()}", FormatValue(typeB.Instantiate()));
            Assert.Equal("{B.ToString()}", FormatValue(typeC.Instantiate()));
        }

        [Fact]
        public void ValuesWithUnderlyingString()
        {
            Assert.True(HasUnderlyingString("Test"));
            Assert.False(HasUnderlyingString(null, typeof(string)));
            Assert.False(HasUnderlyingString(0));
            Assert.False(HasUnderlyingString(DkmEvaluationFlags.None)); // Enum
            Assert.False(HasUnderlyingString('a'));
            Assert.False(HasUnderlyingString(new int[] { 1, 2, 3 }));
            Assert.False(HasUnderlyingString(new object()));
            Assert.False(HasUnderlyingString(0, typeof(int*)));
        }

        [Fact]
        public void VisualizeString()
        {
            Assert.Equal("\r\n", GetUnderlyingString("\r\n"));
        }

        [Fact]
        public void VisualizeSqlString()
        {
            var source = @"
namespace System.Data.SqlTypes
{
    public struct SqlString
    {
        private string m_value;

        public void Set(string value)
        {
            m_value = value;
        }
    }
}
";
            var assembly = GetAssembly(source);
            var type = assembly.GetType("System.Data.SqlTypes.SqlString");

            object sqlString = type.Instantiate();
            var setMethod = type.GetMethod("Set");
            setMethod.Invoke(sqlString, new object[] { "Test" });

            Assert.Equal("Test", GetUnderlyingString(sqlString));
        }

        [Fact]
        public void VisualizeXNode()
        {
            var source = @"
namespace System.Xml.Linq
{
    public class XNode
    {
        public override string ToString()
        {
            return ""Test1"";
        }
    }

    public class XContainer : XNode
    {
    }

    public class XElement : XContainer
    {
        public override string ToString()
        {
            return ""Test2"";
        }
    }
}
";
            var assembly = GetAssembly(source);
            var xnType = assembly.GetType("System.Xml.Linq.XNode");
            var xeType = assembly.GetType("System.Xml.Linq.XElement");

            Assert.Equal("Test1", GetUnderlyingString(xnType.Instantiate()));
            Assert.Equal("Test2", GetUnderlyingString(xeType.Instantiate()));
        }
    }
}
