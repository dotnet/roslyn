using System;
using System.Globalization;
using System.Text;
using Roslyn.Compilers.Collections;
using Roslyn.Utilities;

namespace Roslyn.Compilers.CSharp
{
    internal static class ObjectDisplay
    {
        public static string FormatPrimitive(object obj, bool quoteStrings, bool useHexadecimalNumbers)
        {
            if (obj == null)
            {
                return NullLiteral;
            }

            if (obj is bool)
            {
                return FormatLiteral((bool)obj);
            }

            string str = obj as string;
            if (str != null)
            {
                return FormatLiteral(str, quoteStrings);
            }

            if (obj is char)
            {
                return FormatLiteral((char)obj, quoteStrings);
            }

            if (obj is sbyte)
            {
                return FormatLiteral((sbyte)obj, useHexadecimalNumbers);
            }

            if (obj is byte)
            {
                return FormatLiteral((byte)obj, useHexadecimalNumbers);
            }

            if (obj is short)
            {
                return FormatLiteral((short)obj, useHexadecimalNumbers);
            }

            if (obj is ushort)
            {
                return FormatLiteral((ushort)obj, useHexadecimalNumbers);
            }

            if (obj is int)
            {
                return FormatLiteral((int)obj, useHexadecimalNumbers);
            }

            if (obj is uint)
            {
                return FormatLiteral((uint)obj, useHexadecimalNumbers);
            }

            if (obj is long)
            {
                return FormatLiteral((long)obj, useHexadecimalNumbers);
            }

            if (obj is ulong)
            {
                return FormatLiteral((ulong)obj, useHexadecimalNumbers);
            }

            if (obj is double)
            {
                return FormatLiteral((double)obj);
            }

            if (obj is float)
            {
                return FormatLiteral((float)obj);
            }

            if (obj is decimal)
            {
                return FormatLiteral((decimal)obj);
            }

            return null;
        }

        public static string NullLiteral
        {
            get
            {
                return "null";
            }
        }

        public static string FormatLiteral(bool value)
        {
            return value ? "true" : "false";
        }

        internal static string FormatString(string str, char quote, bool escapeNonPrintable)
        {
            PooledStringBuilder pooledBuilder = null;
            StringBuilder sb = null;
            int lastEscape = -1;
            for (int i = 0; i < str.Length; i++)
            {
                char c = str[i];
                char replaceWith = '\0';
                bool unicodeEscape = false;
                switch (c)
                {
                    case '\\':
                        replaceWith = c;
                        break;

                    case '\0':
                        replaceWith = '0';
                        break;

                    case '\r':
                        replaceWith = 'r';
                        break;

                    case '\n':
                        replaceWith = 'n';
                        break;

                    case '\t':
                        replaceWith = 't';
                        break;

                    case '\b':
                        replaceWith = 'b';
                        break;

                    case '\v':
                        replaceWith = 'v';
                        break;

                    default:
                        if (quote == c)
                        {
                            replaceWith = c;
                            break;
                        }

                        if (escapeNonPrintable)
                        {
                            switch (Char.GetUnicodeCategory(c))
                            {
                                case UnicodeCategory.OtherNotAssigned:
                                case UnicodeCategory.ParagraphSeparator:
                                case UnicodeCategory.Control:
                                    unicodeEscape = true;
                                    break;
                            }
                        }

                        break;
                }

                if (unicodeEscape || replaceWith != '\0')
                {
                    if (pooledBuilder == null)
                    {
                        pooledBuilder = PooledStringBuilder.GetInstance();
                        sb = pooledBuilder.Builder;
                        if (quote != 0)
                        {
                            sb.Append(quote);
                        }
                    }

                    sb.Append(str, lastEscape + 1, i - (lastEscape + 1));
                    sb.Append('\\');
                    if (unicodeEscape)
                    {
                        sb.Append('u');
                        sb.Append(((int)c).ToString("x4"));
                    }
                    else
                    {
                        sb.Append(replaceWith);
                    }

                    lastEscape = i;
                }
            }

            if (sb != null)
            {
                sb.Append(str, lastEscape + 1, str.Length - (lastEscape + 1));
                if (quote != 0)
                {
                    sb.Append(quote);
                }

                return pooledBuilder.ToStringAndFree();
            }

            switch (quote)
            {
                case '"': return String.Concat("\"", str, "\"");
                case '\'': return String.Concat("'", str, "'");
                case '\0': return str;
            }

            throw Contract.Unreachable;
        }

        public static string FormatLiteral(string value, bool quote)
        {
            if (value == null)
            {
                throw new ArgumentNullException("value");
            }

            return FormatString(value, quote ? '"' : '\0', escapeNonPrintable: true);
        }

        public static string FormatLiteral(char c, bool quote, bool useHexadecimalNumbers, bool includeCodePoints)
        {
            var result = FormatString(c.ToString(), quote ? '\'' : '\0', escapeNonPrintable: !includeCodePoints);
            if (includeCodePoints)
            {
                var codepoint = useHexadecimalNumbers ? "0x" + ((int)c).ToString("x4") : ((int)c).ToString();
                return codepoint + " " + result;
            }

            return result;
        }

        public static string FormatLiteral(sbyte value, bool useHexadecimalNumbers)
        {
            if (useHexadecimalNumbers)
            {
                // Special Case: for sbyte and short, specifically, negatives are shown
                // with extra precision.
                return "0x" + (value >= 0 ? value.ToString("x2") : ((int)value).ToString("x8"));
            }
            else
            {
                return value.ToString(CultureInfo.InvariantCulture);
            }
        }

        public static string FormatLiteral(byte value, bool useHexadecimalNumbers)
        {
            if (useHexadecimalNumbers)
            {
                return "0x" + value.ToString("x2");
            }
            else
            {
                return value.ToString(CultureInfo.InvariantCulture);
            }
        }

        public static string FormatLiteral(short value, bool useHexadecimalNumbers)
        {
            if (useHexadecimalNumbers)
            {
                // Special Case: for sbyte and short, specifically, negatives are shown
                // with extra precision.
                return "0x" + (value >= 0 ? value.ToString("x4") : ((int)value).ToString("x8"));
            }
            else
            {
                return value.ToString(CultureInfo.InvariantCulture);
            }
        }

        public static string FormatLiteral(ushort value, bool useHexadecimalNumbers)
        {
            if (useHexadecimalNumbers)
            {
                return "0x" + value.ToString("x4");
            }
            else
            {
                return value.ToString(CultureInfo.InvariantCulture);
            }
        }

        public static string FormatLiteral(int value, bool useHexadecimalNumbers)
        {
            if (useHexadecimalNumbers)
            {
                return "0x" + value.ToString("x8");
            }
            else
            {
                return value.ToString(CultureInfo.InvariantCulture);
            }
        }

        public static string FormatLiteral(uint value, bool useHexadecimalNumbers)
        {
            if (useHexadecimalNumbers)
            {
                return "0x" + value.ToString("x8");
            }
            else
            {
                return value.ToString(CultureInfo.InvariantCulture);
            }
        }

        public static string FormatLiteral(long value, bool useHexadecimalNumbers)
        {
            if (useHexadecimalNumbers)
            {
                return "0x" + value.ToString("x16");
            }
            else
            {
                return value.ToString(CultureInfo.InvariantCulture);
            }
        }

        public static string FormatLiteral(ulong value, bool useHexadecimalNumbers)
        {
            if (useHexadecimalNumbers)
            {
                return "0x" + value.ToString("x16");
            }
            else
            {
                return value.ToString(CultureInfo.InvariantCulture);
            }
        }

        public static string FormatLiteral(double value)
        {
            return value.ToString("R", CultureInfo.InvariantCulture);
        }

        public static string FormatLiteral(float value)
        {
            return value.ToString("R", CultureInfo.InvariantCulture);
        }

        public static string FormatLiteral(decimal value)
        {
            return value.ToString(CultureInfo.InvariantCulture);
        }
    }
}