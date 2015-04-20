using System;
using System.Text;

namespace Microsoft.CodeAnalysis.Text
{
    /// <summary>
    /// CodePage 1252 decoder. This is a single byte character set encoding that is very close
    /// to Latin1 except some of the code points in the control set 1 range (0x80 to 0x9F) are
    /// replaced with typographic characters.
    /// </summary>
    internal sealed class CodePage1252Encoding : Encoding
    {
        public static readonly CodePage1252Encoding Instance = new CodePage1252Encoding();

        public override int CodePage => 1252;

        public override int GetByteCount(char[] chars, int index, int count)
        {
            // This is a decoder only
            throw new NotSupportedException();
        }

        public override int GetBytes(char[] chars, int charIndex, int charCount, byte[] bytes, int byteIndex)
        {
            // This is a decoder only
            throw new NotSupportedException();
        }

        // This is SBCS, so the number of characters is the same as the number of bytes.
        public override int GetCharCount(byte[] bytes, int index, int count) => count;

        // This is SBCS, so the number of characters is the same as the number of bytes.
        public override unsafe int GetCharCount(byte* bytes, int count) => count;

        public override int GetChars(byte[] bytes, int byteIndex, int byteCount, char[] chars, int charIndex)
        {
            int byteEndIndex = byteIndex + byteCount;
            while (byteIndex < byteEndIndex)
            {
                chars[charIndex++] = Decode(bytes[byteIndex++]);
            }

            return byteCount;
        }

        public override unsafe int GetChars(byte* bytes, int byteCount, char* chars, int charCount)
        {
            if (charCount < byteCount)
            {
                throw new ArgumentException(nameof(charCount)); // The output char buffer is too small to contain the decoded characters
            }

            byte* end = bytes + byteCount;
            while (bytes < end)
            {
                *chars++ = Decode(*bytes++);
            }

            return byteCount;
        }

        private static readonly char[] s_c1ControlChars =
        {
            /* x80 */ '\u20ac' /* Euro Sign */,
            /* x81 */ '\u0081' /* <control> */,
            /* x82 */ '\u201a' /* Colon Sign */,
            /* x83 */ '\u0192' /* Latin Small Letter F With Hook */,
            /* x84 */ '\u201e' /* Double Low-9 Quotation Mark */,
            /* x85 */ '\u2026' /* Horizontal Ellipsis */,
            /* x86 */ '\u2020' /* Dagger */,
            /* x87 */ '\u2021' /* Double Dagger */,
            /* x88 */ '\u02c6' /* Modifier Letter Cicumflex Accent */,
            /* x89 */ '\u2030' /* Per Mille Sign */,
            /* x8a */ '\u0160' /* Latin Capital Letter S With Caron */,
            /* x8b */ '\u2039' /* Single Left-Pointing Angle Quotation Mark */,
            /* x8c */ '\u0152' /* Latin Capital Ligature Oe */,
            /* x8d */ '\u008d' /* Reverse Line Feed */,
            /* x8e */ '\u017d' /* Latin Capital Letter Z With Caron */,
            /* x8f */ '\u008f' /* Single Shift Three */,
            /* x90 */ '\u0090' /* Device Control String */,
            /* x91 */ '\u2018' /* Left Single Quotation Mark */,
            /* x92 */ '\u2019' /* Right Single Quotation Mark */,
            /* x93 */ '\u201c' /* Left Double Quotation Mark */,
            /* x94 */ '\u201d' /* Right Double Quotation Mark */,
            /* x95 */ '\u2022' /* Bullet */,
            /* x96 */ '\u2013' /* En Dash */,
            /* x97 */ '\u2014' /* Em Dash */,
            /* x98 */ '\u02dc' /* Small Tilde */,
            /* x99 */ '\u2122' /* Trade Mark Sign */,
            /* x9a */ '\u0161' /* Latin Small Letter S With Caron */,
            /* x9b */ '\u203a' /* Single Right-Pointing Angle Quotation Mark */,
            /* x9c */ '\u0153' /* Latin Small Ligature Oe */,
            /* x9d */ '\u009d' /* Operating System Command */,
            /* x9e */ '\u017e' /* Latin Small Letter Z With Caron */,
            /* x9f */ '\u0178' /* Latin Captial Letter Y With Diaeresis */,
        };

        private static char Decode(byte b)
        {
            // For characters in the C1 Control set (0x80 to 0x9F), use the table.
            // Otherwise, the mapping is 1 to 1.
            uint c1 = unchecked(b - 0x80u);
            return c1 < 0x20u ? s_c1ControlChars[c1] : (char)b;
        }

        public override int GetMaxByteCount(int charCount)
        {
            // This is a decoder only
            throw new NotSupportedException();
        }

        public override int GetMaxCharCount(int byteCount)
        {
            if (byteCount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(byteCount));
            }

            // This is SBCS, so everything is 1:1
            return byteCount;
        }
    }
}
