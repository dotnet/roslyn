// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Threading;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public class ObjectDisplayTests
    {
        [Fact]
        public void IntegralPrimitives()
        {
            unchecked
            {
                Assert.Equal("1", FormatPrimitive((byte)1));
                Assert.Equal(@"123", FormatPrimitive((byte)123));
                Assert.Equal("255", FormatPrimitive((byte)-1));
                Assert.Equal("1", FormatPrimitive((sbyte)1));
                Assert.Equal(@"123", FormatPrimitive((sbyte)123));
                Assert.Equal("-1", FormatPrimitive((sbyte)-1));
                Assert.Equal("1", FormatPrimitive((ushort)1));
                Assert.Equal(@"123", FormatPrimitive((ushort)123));
                Assert.Equal("65535", FormatPrimitive((ushort)-1));
                Assert.Equal("1", FormatPrimitive((short)1));
                Assert.Equal(@"123", FormatPrimitive((short)123));
                Assert.Equal("-1", FormatPrimitive((short)-1));
                Assert.Equal("1", FormatPrimitive((uint)1));
                Assert.Equal(@"123", FormatPrimitive((uint)123));
                Assert.Equal("4294967295", FormatPrimitive((uint)-1));
                Assert.Equal("1", FormatPrimitive((int)1));
                Assert.Equal(@"123", FormatPrimitive((int)123));
                Assert.Equal("-1", FormatPrimitive((int)-1));
                Assert.Equal("1", FormatPrimitive((ulong)1));
                Assert.Equal(@"123", FormatPrimitive((ulong)123));
                Assert.Equal("18446744073709551615", FormatPrimitive((ulong)-1));
                Assert.Equal("1", FormatPrimitive((long)1));
                Assert.Equal(@"123", FormatPrimitive((long)123));
                Assert.Equal("-1", FormatPrimitive((long)-1));

                Assert.Equal("0x01", FormatPrimitiveUsingHexadecimalNumbers((byte)1));
                Assert.Equal(@"0x7f", FormatPrimitiveUsingHexadecimalNumbers((byte)0x7f));
                Assert.Equal("0xff", FormatPrimitiveUsingHexadecimalNumbers((byte)-1));
                Assert.Equal("0x01", FormatPrimitiveUsingHexadecimalNumbers((sbyte)1));
                Assert.Equal(@"0x7f", FormatPrimitiveUsingHexadecimalNumbers((sbyte)0x7f));
                Assert.Equal("0xffffffff", FormatPrimitiveUsingHexadecimalNumbers((sbyte)-1)); // As in dev11.
                Assert.Equal(@"0xfffffffe", FormatPrimitiveUsingHexadecimalNumbers((sbyte)(-2)));
                Assert.Equal("0x0001", FormatPrimitiveUsingHexadecimalNumbers((ushort)1));
                Assert.Equal(@"0x007f", FormatPrimitiveUsingHexadecimalNumbers((ushort)0x7f));
                Assert.Equal("0xffff", FormatPrimitiveUsingHexadecimalNumbers((ushort)-1));
                Assert.Equal("0x0001", FormatPrimitiveUsingHexadecimalNumbers((short)1));
                Assert.Equal(@"0x007f", FormatPrimitiveUsingHexadecimalNumbers((short)0x7f));
                Assert.Equal("0xffffffff", FormatPrimitiveUsingHexadecimalNumbers((short)-1)); // As in dev11.
                Assert.Equal(@"0xfffffffe", FormatPrimitiveUsingHexadecimalNumbers((short)(-2)));
                Assert.Equal("0x00000001", FormatPrimitiveUsingHexadecimalNumbers((uint)1));
                Assert.Equal(@"0x0000007f", FormatPrimitiveUsingHexadecimalNumbers((uint)0x7f));
                Assert.Equal("0xffffffff", FormatPrimitiveUsingHexadecimalNumbers((uint)-1));
                Assert.Equal("0x00000001", FormatPrimitiveUsingHexadecimalNumbers((int)1));
                Assert.Equal(@"0x0000007f", FormatPrimitiveUsingHexadecimalNumbers((int)0x7f));
                Assert.Equal("0xffffffff", FormatPrimitiveUsingHexadecimalNumbers((int)-1));
                Assert.Equal(@"0xfffffffe", FormatPrimitiveUsingHexadecimalNumbers((int)(-2)));
                Assert.Equal("0x0000000000000001", FormatPrimitiveUsingHexadecimalNumbers((ulong)1));
                Assert.Equal(@"0x000000000000007f", FormatPrimitiveUsingHexadecimalNumbers((ulong)0x7f));
                Assert.Equal("0xffffffffffffffff", FormatPrimitiveUsingHexadecimalNumbers((ulong)-1));
                Assert.Equal("0x0000000000000001", FormatPrimitiveUsingHexadecimalNumbers((long)1));
                Assert.Equal(@"0x000000000000007f", FormatPrimitiveUsingHexadecimalNumbers((long)0x7f));
                Assert.Equal("0xffffffffffffffff", FormatPrimitiveUsingHexadecimalNumbers((long)-1));
                Assert.Equal(@"0xfffffffffffffffe", FormatPrimitiveUsingHexadecimalNumbers((long)(-2)));
            }
        }

        [Fact]
        public void Booleans()
        {
            Assert.Equal(@"true", FormatPrimitive(true));
            Assert.Equal(@"false", FormatPrimitive(false));
        }

        [Fact]
        public void NullLiterals()
        {
            Assert.Equal(@"null", FormatPrimitive(null));
        }

        [Fact]
        public void Decimals()
        {
            Assert.Equal(@"2", FormatPrimitive((decimal)2));
        }

        [Fact]
        public void Floats()
        {
            Assert.Equal(@"2", FormatPrimitive((float)2));
        }

        [Fact]
        public void Doubles()
        {
            Assert.Equal(@"2", FormatPrimitive((double)2));
        }

        [Fact]
        public void Characters()
        {
            Assert.Equal("120 'x'", ObjectDisplay.FormatLiteral('x', ObjectDisplayOptions.UseQuotes | ObjectDisplayOptions.EscapeNonPrintableCharacters | ObjectDisplayOptions.IncludeCodePoints));
            Assert.Equal("120 x", ObjectDisplay.FormatLiteral('x', ObjectDisplayOptions.IncludeCodePoints));
            Assert.Equal("0x0078 'x'", ObjectDisplay.FormatLiteral('x', ObjectDisplayOptions.UseQuotes | ObjectDisplayOptions.EscapeNonPrintableCharacters | ObjectDisplayOptions.IncludeCodePoints | ObjectDisplayOptions.UseHexadecimalNumbers));
            Assert.Equal("0x0078 x", ObjectDisplay.FormatLiteral('x', ObjectDisplayOptions.IncludeCodePoints | ObjectDisplayOptions.UseHexadecimalNumbers));

            Assert.Equal("39 '\\''", ObjectDisplay.FormatLiteral('\'', ObjectDisplayOptions.UseQuotes | ObjectDisplayOptions.EscapeNonPrintableCharacters | ObjectDisplayOptions.IncludeCodePoints));
            Assert.Equal("39 '", ObjectDisplay.FormatLiteral('\'', ObjectDisplayOptions.IncludeCodePoints));
            Assert.Equal("0x001e '\\u001e'", ObjectDisplay.FormatLiteral('\u001e', ObjectDisplayOptions.UseQuotes | ObjectDisplayOptions.EscapeNonPrintableCharacters | ObjectDisplayOptions.IncludeCodePoints | ObjectDisplayOptions.UseHexadecimalNumbers));
            Assert.Equal("0x001e \u001e", ObjectDisplay.FormatLiteral('\u001e', ObjectDisplayOptions.IncludeCodePoints | ObjectDisplayOptions.UseHexadecimalNumbers));

            Assert.Equal("0x0008 '\\b'", ObjectDisplay.FormatLiteral('\b', ObjectDisplayOptions.UseQuotes | ObjectDisplayOptions.EscapeNonPrintableCharacters | ObjectDisplayOptions.IncludeCodePoints | ObjectDisplayOptions.UseHexadecimalNumbers));
            Assert.Equal("0x0009 '\\t'", ObjectDisplay.FormatLiteral('\t', ObjectDisplayOptions.UseQuotes | ObjectDisplayOptions.EscapeNonPrintableCharacters | ObjectDisplayOptions.IncludeCodePoints | ObjectDisplayOptions.UseHexadecimalNumbers));
            Assert.Equal("0x000a '\\n'", ObjectDisplay.FormatLiteral('\n', ObjectDisplayOptions.UseQuotes | ObjectDisplayOptions.EscapeNonPrintableCharacters | ObjectDisplayOptions.IncludeCodePoints | ObjectDisplayOptions.UseHexadecimalNumbers));
            Assert.Equal("0x000b '\\v'", ObjectDisplay.FormatLiteral('\v', ObjectDisplayOptions.UseQuotes | ObjectDisplayOptions.EscapeNonPrintableCharacters | ObjectDisplayOptions.IncludeCodePoints | ObjectDisplayOptions.UseHexadecimalNumbers));
            Assert.Equal("0x000d '\\r'", ObjectDisplay.FormatLiteral('\r', ObjectDisplayOptions.UseQuotes | ObjectDisplayOptions.EscapeNonPrintableCharacters | ObjectDisplayOptions.IncludeCodePoints | ObjectDisplayOptions.UseHexadecimalNumbers));
            Assert.Equal("0x000d \r", ObjectDisplay.FormatLiteral('\r', ObjectDisplayOptions.IncludeCodePoints | ObjectDisplayOptions.UseHexadecimalNumbers));
        }

        [Fact]
        public void Characters_QuotesAndEscaping()
        {
            Assert.Equal(QuoteAndEscapingCombinations('a'), new[] 
            {
                "a", "a", "'a'", "'a'",
                "97 a", "97 a", "97 'a'", "97 'a'",
            });

            Assert.Equal(QuoteAndEscapingCombinations('\t'), new[] 
            {
                "\t", "\\t", "'\t'", "'\\t'",
                "9 \t", "9 \\t", "9 '\t'", "9 '\\t'",
            });

            // Miscellaneous symbol
            Assert.Equal(QuoteAndEscapingCombinations('\u26f4'), new[]
            {
                "\u26f4", "\u26f4", "'\u26f4'", "'\u26f4'",
                "9972 \u26f4", "9972 \u26f4", "9972 '\u26f4'", "9972 '\u26f4'",
            });

            // Control character
            Assert.Equal(QuoteAndEscapingCombinations('\u007f'), new[]
            {
                "\u007f", "\\u007f", "'\u007f'", "'\\u007f'",
                "127 \u007f", "127 \\u007f", "127 '\u007f'", "127 '\\u007f'",
            });

            // Quote
            Assert.Equal(QuoteAndEscapingCombinations('\''), new[]
            {
                "'", "'", "'\\''", "'\\''",
                "39 '", "39 '", "39 '\\''", "39 '\\''",
            });
        }

        private static IEnumerable<string> QuoteAndEscapingCombinations(char ch)
        {
            return new[]
            {
                ObjectDisplay.FormatLiteral(ch, ObjectDisplayOptions.None),
                ObjectDisplay.FormatLiteral(ch, ObjectDisplayOptions.EscapeNonPrintableCharacters),
                ObjectDisplay.FormatLiteral(ch, ObjectDisplayOptions.UseQuotes),
                ObjectDisplay.FormatLiteral(ch, ObjectDisplayOptions.EscapeNonPrintableCharacters | ObjectDisplayOptions.UseQuotes),
                ObjectDisplay.FormatLiteral(ch, ObjectDisplayOptions.IncludeCodePoints | ObjectDisplayOptions.None),
                ObjectDisplay.FormatLiteral(ch, ObjectDisplayOptions.IncludeCodePoints | ObjectDisplayOptions.EscapeNonPrintableCharacters),
                ObjectDisplay.FormatLiteral(ch, ObjectDisplayOptions.IncludeCodePoints | ObjectDisplayOptions.UseQuotes),
                ObjectDisplay.FormatLiteral(ch, ObjectDisplayOptions.IncludeCodePoints | ObjectDisplayOptions.EscapeNonPrintableCharacters | ObjectDisplayOptions.UseQuotes),
            };
        }

        [Fact]
        public void Strings()
        {
            Assert.Equal("", ObjectDisplay.FormatLiteral("", ObjectDisplayOptions.None));
            Assert.Equal(@"a", ObjectDisplay.FormatLiteral(@"a", ObjectDisplayOptions.None));
            Assert.Equal(@"ab", ObjectDisplay.FormatLiteral(@"ab", ObjectDisplayOptions.None));
            Assert.Equal(@"\", ObjectDisplay.FormatLiteral(@"\", ObjectDisplayOptions.None));
            Assert.Equal(@"\a", ObjectDisplay.FormatLiteral(@"\a", ObjectDisplayOptions.None));
            Assert.Equal(@"a\b", ObjectDisplay.FormatLiteral(@"a\b", ObjectDisplayOptions.None));
            Assert.Equal(@"ab\c", ObjectDisplay.FormatLiteral(@"ab\c", ObjectDisplayOptions.None));
            Assert.Equal(@"ab\cd", ObjectDisplay.FormatLiteral(@"ab\cd", ObjectDisplayOptions.None));
            Assert.Equal(@"ab\cd\", ObjectDisplay.FormatLiteral(@"ab\cd\", ObjectDisplayOptions.None));
            Assert.Equal(@"ab\cd\e", ObjectDisplay.FormatLiteral(@"ab\cd\e", ObjectDisplayOptions.None));
            Assert.Equal(@"\\\\", ObjectDisplay.FormatLiteral(@"\\\\", ObjectDisplayOptions.None));

            Assert.Equal(@"""""", ObjectDisplay.FormatLiteral("", ObjectDisplayOptions.UseQuotes | ObjectDisplayOptions.EscapeNonPrintableCharacters));
            Assert.Equal(@"""\""\""""", ObjectDisplay.FormatLiteral(@"""""", ObjectDisplayOptions.UseQuotes | ObjectDisplayOptions.EscapeNonPrintableCharacters));
            Assert.Equal(@"""'""", ObjectDisplay.FormatLiteral(@"'", ObjectDisplayOptions.UseQuotes | ObjectDisplayOptions.EscapeNonPrintableCharacters));
            Assert.Equal(@"""ab""", ObjectDisplay.FormatLiteral(@"ab", ObjectDisplayOptions.UseQuotes | ObjectDisplayOptions.EscapeNonPrintableCharacters));
            Assert.Equal(@"""\\""", ObjectDisplay.FormatLiteral(@"\", ObjectDisplayOptions.UseQuotes | ObjectDisplayOptions.EscapeNonPrintableCharacters));

            Assert.Equal("\"x\"", ObjectDisplay.FormatLiteral("x", ObjectDisplayOptions.UseQuotes | ObjectDisplayOptions.EscapeNonPrintableCharacters));
            Assert.Equal("x", ObjectDisplay.FormatLiteral("x", ObjectDisplayOptions.None));

            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < 255; i++)
            {
                sb.Append((char)i);
            }
            var s = sb.ToString();

            var expected =
                "\"\\0\\u0001\\u0002\\u0003\\u0004\\u0005\\u0006\\a\\b\\t\\n\\v\\f\\r\\u000e\\u000f\\u0010" +
                "\\u0011\\u0012\\u0013\\u0014\\u0015\\u0016\\u0017\\u0018\\u0019\\u001a\\u001b\\u001c\\u001d" +
                "\\u001e\\u001f !\\\"#$%&'()*+,-./0123456789:;<=>?@ABCDEFGHIJKLMNOPQRSTUVWXYZ[" +
                "\\\\]^_`abcdefghijklmnopqrstuvwxyz{|}~\\u007f\\u0080\\u0081\\u0082\\u0083\\u0084\\u0085" +
                "\\u0086\\u0087\\u0088\\u0089\\u008a\\u008b\\u008c\\u008d\\u008e\\u008f\\u0090\\u0091\\u0092" +
                "\\u0093\\u0094\\u0095\\u0096\\u0097\\u0098\\u0099\\u009a\\u009b\\u009c\\u009d\\u009e\\u009f" +
                " ¡¢£¤¥¦§¨©ª«¬­®¯°±²³´µ¶·¸¹º»¼½¾¿ÀÁÂÃÄÅÆÇÈÉÊËÌÍÎÏÐÑÒÓÔÕÖ×ØÙÚÛÜÝÞßàáâãäåæçèé" +
                "êëìíîïðñòóôõö÷øùúûüýþ\"";
            Assert.Equal(
                expected,
                ObjectDisplay.FormatLiteral(s, ObjectDisplayOptions.UseQuotes | ObjectDisplayOptions.EscapeNonPrintableCharacters));

            expected =
                "\0\u0001\u0002\u0003\u0004\u0005\u0006\a\u0008\u0009\u000a\u000b\f\u000d\u000e\u000f\u0010" +
                "\u0011\u0012\u0013\u0014\u0015\u0016\u0017\u0018\u0019\u001a\u001b\u001c\u001d" +
                "\u001e\u001f !\"#$%&'()*+,-./0123456789:;<=>?@ABCDEFGHIJKLMNOPQRSTUVWXYZ[" +
                "\\]^_`abcdefghijklmnopqrstuvwxyz{|}~\u007f\u0080\u0081\u0082\u0083\u0084\u0085" +
                "\u0086\u0087\u0088\u0089\u008a\u008b\u008c\u008d\u008e\u008f\u0090\u0091\u0092" +
                "\u0093\u0094\u0095\u0096\u0097\u0098\u0099\u009a\u009b\u009c\u009d\u009e\u009f" +
                " ¡¢£¤¥¦§¨©ª«¬­®¯°±²³´µ¶·¸¹º»¼½¾¿ÀÁÂÃÄÅÆÇÈÉÊËÌÍÎÏÐÑÒÓÔÕÖ×ØÙÚÛÜÝÞßàáâãäåæçèé" +
                "êëìíîïðñòóôõö÷øùúûüýþ";
            Assert.Equal(
                expected,
                ObjectDisplay.FormatLiteral(s, ObjectDisplayOptions.None));

            var arabic = "انتخابات مبكرة، بعد يوم حافل بالاحداث السياسية، بعد";
            s = ObjectDisplay.FormatLiteral(arabic, ObjectDisplayOptions.None);
            Assert.Equal(arabic, s);

            var hebrew = "והמנהלים רפואיים של ארבעת קופות החולים. בסיום הפגישה הבהיר";
            s = ObjectDisplay.FormatLiteral(hebrew, ObjectDisplayOptions.None);
            Assert.Equal(hebrew, s);
        }

        [Fact]
        public void Strings_QuotesAndEscaping()
        {
            Assert.Equal(QuoteAndEscapingCombinations(""), new[] { "", "", "\"\"", "\"\"" });
            Assert.Equal(QuoteAndEscapingCombinations("a"), new[] { "a", "a", "\"a\"", "\"a\"" });
            Assert.Equal(QuoteAndEscapingCombinations("\t"), new[] { "\t", "\\t", "\"\t\"", "\"\\t\"" });
            Assert.Equal(QuoteAndEscapingCombinations("\u26F4"), new[] { "\u26F4", "\u26F4", "\"\u26F4\"", "\"\u26F4\"" });  // Miscellaneous symbol
            Assert.Equal(QuoteAndEscapingCombinations("\u007f"), new[] { "\u007f", "\\u007f", "\"\u007f\"", "\"\\u007f\"" }); // Control character
            Assert.Equal(QuoteAndEscapingCombinations("\""), new[] { "\"", "\"", "\"\\\"\"", "\"\\\"\"" }); // Quote
        }

        private static IEnumerable<string> QuoteAndEscapingCombinations(string s)
        {
            return new[]
            {
                ObjectDisplay.FormatLiteral(s, ObjectDisplayOptions.None),
                ObjectDisplay.FormatLiteral(s, ObjectDisplayOptions.EscapeNonPrintableCharacters),
                ObjectDisplay.FormatLiteral(s, ObjectDisplayOptions.UseQuotes),
                ObjectDisplay.FormatLiteral(s, ObjectDisplayOptions.EscapeNonPrintableCharacters | ObjectDisplayOptions.UseQuotes),
            };
        }

        [Fact]
        public void Strings_Verbatim()
        {
            Assert.Equal("@\"\n\"", ObjectDisplay.FormatLiteral("\n", ObjectDisplayOptions.UseQuotes));
            Assert.Equal("@\"\"\"\n\"", ObjectDisplay.FormatLiteral("\"\n", ObjectDisplayOptions.UseQuotes));
        }

        [Fact, WorkItem(529850)]
        public void CultureInvariance()
        {
            var originalCulture = Thread.CurrentThread.CurrentCulture;
            try
            {
                Thread.CurrentThread.CurrentCulture = new CultureInfo(1031); // de-DE

                var decimalValue = new Decimal(12.5);
                Assert.Equal("12,5", decimalValue.ToString());
                Assert.Equal("12.5", ObjectDisplay.FormatLiteral(decimalValue, ObjectDisplayOptions.None));
                Assert.Equal("12,5", ObjectDisplay.FormatLiteral(decimalValue, ObjectDisplayOptions.UseCurrentCulture));
                Assert.Equal("12.5M", ObjectDisplay.FormatLiteral(decimalValue, ObjectDisplayOptions.IncludeTypeSuffix));

                double doubleValue = 12.5;
                Assert.Equal("12,5", doubleValue.ToString());
                Assert.Equal("12.5", ObjectDisplay.FormatLiteral(doubleValue, ObjectDisplayOptions.None));
                Assert.Equal("12,5", ObjectDisplay.FormatLiteral(doubleValue, ObjectDisplayOptions.UseCurrentCulture));
                Assert.Equal("12.5D", ObjectDisplay.FormatLiteral(doubleValue, ObjectDisplayOptions.IncludeTypeSuffix));

                float singleValue = 12.5F;
                Assert.Equal("12,5", singleValue.ToString());
                Assert.Equal("12.5", ObjectDisplay.FormatLiteral(singleValue, ObjectDisplayOptions.None));
                Assert.Equal("12,5", ObjectDisplay.FormatLiteral(singleValue, ObjectDisplayOptions.UseCurrentCulture));
                Assert.Equal("12.5F", ObjectDisplay.FormatLiteral(singleValue, ObjectDisplayOptions.IncludeTypeSuffix));
            }
            finally
            {
                Thread.CurrentThread.CurrentCulture = originalCulture;
            }
        }

        [Fact]
        public void TypeSuffixes()
        {
            bool boolValue = true;
            Assert.Equal("true", FormatPrimitiveIncludingTypeSuffix(boolValue));

            byte sbyteValue = 0x2A;
            Assert.Equal("42", FormatPrimitiveIncludingTypeSuffix(sbyteValue));

            byte byteValue = 0x2A;
            Assert.Equal("42", FormatPrimitiveIncludingTypeSuffix(byteValue));

            short shortValue = 0x2A;
            Assert.Equal("42", FormatPrimitiveIncludingTypeSuffix(shortValue));

            ushort ushortValue = 0x2A;
            Assert.Equal("42", FormatPrimitiveIncludingTypeSuffix(ushortValue));

            int intValue = 0x2A;
            Assert.Equal("42", FormatPrimitiveIncludingTypeSuffix(intValue));

            uint uintValue = 0x2A;
            Assert.Equal("42U", FormatPrimitiveIncludingTypeSuffix(uintValue));

            long longValue = 0x2A;
            Assert.Equal("42L", FormatPrimitiveIncludingTypeSuffix(longValue));

            ulong ulongValue = 0x2A;
            Assert.Equal("42UL", FormatPrimitiveIncludingTypeSuffix(ulongValue));

            float floatValue = 3.14159F;
            Assert.Equal("3.14159F", FormatPrimitiveIncludingTypeSuffix(floatValue));

            double doubleValue = 26.2;
            Assert.Equal("26.2D", FormatPrimitiveIncludingTypeSuffix(doubleValue));

            decimal decimalValue = 12.5M;
            Assert.Equal("12.5M", FormatPrimitiveIncludingTypeSuffix(decimalValue, useHexadecimalNumbers: true));
        }

        [Fact]
        public void StringEscaping()
        {
            const string value = "a\tb";

            Assert.Equal("a\tb", ObjectDisplay.FormatPrimitive(value, ObjectDisplayOptions.None));
            Assert.Equal("\"a\tb\"", ObjectDisplay.FormatPrimitive(value, ObjectDisplayOptions.UseQuotes));
            Assert.Equal("a\\tb", ObjectDisplay.FormatPrimitive(value, ObjectDisplayOptions.EscapeNonPrintableCharacters));
            Assert.Equal("\"a\\tb\"", ObjectDisplay.FormatPrimitive(value, ObjectDisplayOptions.UseQuotes | ObjectDisplayOptions.EscapeNonPrintableCharacters));
        }

        private string FormatPrimitive(object obj, bool quoteStrings = false)
        {
            var options = quoteStrings ? ObjectDisplayOptions.UseQuotes : ObjectDisplayOptions.None;
            return ObjectDisplay.FormatPrimitive(obj, options | ObjectDisplayOptions.EscapeNonPrintableCharacters);
        }

        private string FormatPrimitiveUsingHexadecimalNumbers(object obj, bool quoteStrings = false)
        {
            var options = quoteStrings ? ObjectDisplayOptions.UseQuotes : ObjectDisplayOptions.None;
            return ObjectDisplay.FormatPrimitive(obj, options | ObjectDisplayOptions.UseHexadecimalNumbers | ObjectDisplayOptions.EscapeNonPrintableCharacters);
        }

        private string FormatPrimitiveIncludingTypeSuffix(object obj, bool useHexadecimalNumbers = false)
        {
            var options = useHexadecimalNumbers ? ObjectDisplayOptions.UseHexadecimalNumbers : ObjectDisplayOptions.None;
            return ObjectDisplay.FormatPrimitive(obj, options | ObjectDisplayOptions.IncludeTypeSuffix | ObjectDisplayOptions.EscapeNonPrintableCharacters);
        }
    }
}
