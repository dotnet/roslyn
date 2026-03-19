// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis;

/// <summary>
/// Well known encodings. Used to distinguish serialized encodings with BOM and without BOM.
/// </summary>
internal enum TextEncodingKind : byte
{
    None = 0,
    EncodingUtf8 = 1,
    EncodingUtf8_BOM = 2,
    EncodingUtf32_BE = 3,
    EncodingUtf32_BE_BOM = 4,
    EncodingUtf32_LE = 5,
    EncodingUtf32_LE_BOM = 6,
    EncodingUnicode_BE = 7,
    EncodingUnicode_BE_BOM = 8,
    EncodingUnicode_LE = 9,
    EncodingUnicode_LE_BOM = 10,
}

internal static partial class EncodingExtensions
{
    internal const TextEncodingKind FirstTextEncodingKind = TextEncodingKind.EncodingUtf8;
    internal const TextEncodingKind LastTextEncodingKind = TextEncodingKind.EncodingUnicode_LE_BOM;

    private static readonly Encoding s_encodingUtf8 = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
    private static readonly Encoding s_encodingUtf32_BE = new UTF32Encoding(bigEndian: true, byteOrderMark: false);
    private static readonly Encoding s_encodingUtf32_BE_BOM = new UTF32Encoding(bigEndian: true, byteOrderMark: true);
    private static readonly Encoding s_encodingUtf32_LE = new UTF32Encoding(bigEndian: false, byteOrderMark: false);
    private static readonly Encoding s_encodingUnicode_BE = new UnicodeEncoding(bigEndian: true, byteOrderMark: false);
    private static readonly Encoding s_encodingUnicode_LE = new UnicodeEncoding(bigEndian: false, byteOrderMark: false);

    public static Encoding GetEncoding(this TextEncodingKind kind)
        => kind switch
        {
            TextEncodingKind.EncodingUtf8 => s_encodingUtf8,
            TextEncodingKind.EncodingUtf8_BOM => Encoding.UTF8,
            TextEncodingKind.EncodingUtf32_BE => s_encodingUtf32_BE,
            TextEncodingKind.EncodingUtf32_BE_BOM => s_encodingUtf32_BE_BOM,
            TextEncodingKind.EncodingUtf32_LE => s_encodingUtf32_LE,
            TextEncodingKind.EncodingUtf32_LE_BOM => Encoding.UTF32,
            TextEncodingKind.EncodingUnicode_BE => s_encodingUnicode_BE,
            TextEncodingKind.EncodingUnicode_BE_BOM => Encoding.BigEndianUnicode,
            TextEncodingKind.EncodingUnicode_LE => s_encodingUnicode_LE,
            TextEncodingKind.EncodingUnicode_LE_BOM => Encoding.Unicode,
            _ => throw ExceptionUtilities.UnexpectedValue(kind)
        };

    public static bool TryGetEncodingKind(this Encoding encoding, out TextEncodingKind kind)
    {
        switch (encoding.CodePage)
        {
            case 1200:
                Debug.Assert(HasPreamble(Encoding.Unicode));
                kind = (encoding.Equals(Encoding.Unicode) || HasPreamble(encoding)) ? TextEncodingKind.EncodingUnicode_LE_BOM : TextEncodingKind.EncodingUnicode_LE;
                return true;

            case 1201:
                Debug.Assert(HasPreamble(Encoding.BigEndianUnicode));
                kind = (encoding.Equals(Encoding.BigEndianUnicode) || HasPreamble(encoding)) ? TextEncodingKind.EncodingUnicode_BE_BOM : TextEncodingKind.EncodingUnicode_BE;
                return true;

            case 12000:
                Debug.Assert(HasPreamble(Encoding.UTF32));
                kind = (encoding.Equals(Encoding.UTF32) || HasPreamble(encoding)) ? TextEncodingKind.EncodingUtf32_LE_BOM : TextEncodingKind.EncodingUtf32_LE;
                return true;

            case 12001:
                Debug.Assert(HasPreamble(Encoding.UTF32));
                kind = (encoding.Equals(Encoding.UTF32) || HasPreamble(encoding)) ? TextEncodingKind.EncodingUtf32_BE_BOM : TextEncodingKind.EncodingUtf32_BE;
                return true;

            case 65001:
                Debug.Assert(HasPreamble(Encoding.UTF8));
                kind = (encoding.Equals(Encoding.UTF8) || HasPreamble(encoding)) ? TextEncodingKind.EncodingUtf8_BOM : TextEncodingKind.EncodingUtf8;
                return true;

            default:
                kind = default;
                return false;
        }
    }

    public static bool HasPreamble(this Encoding encoding)
#if NET
        => !encoding.Preamble.IsEmpty;
#else
        => !encoding.GetPreamble().IsEmpty();
#endif
}
