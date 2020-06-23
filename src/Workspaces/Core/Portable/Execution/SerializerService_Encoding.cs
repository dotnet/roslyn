// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System.Diagnostics;
using System.Text;
using System.Threading;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Serialization
{
    internal partial class SerializerService
    {
        private enum EncodingId : byte
        {
            None = 0,
            Named = 1,

            // well-known encodings (parameterized by BOM)
            UTF8 = 2,
            UTF8_BOM = 3,
            UTF32_BE = 4,
            UTF32_BE_BOM = 5,
            UTF32_LE = 6,
            UTF32_LE_BOM = 7,
            Unicode_BE = 8,
            Unicode_BE_BOM = 9,
            Unicode_LE = 10,
            Unicode_LE_BOM = 11,

            Count
        }

        private static readonly Encoding?[] _cachedEncodings = new Encoding[(int)EncodingId.Count];

        public static void WriteTo(Encoding? encoding, ObjectWriter writer, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var kind = GetEncodingKind(encoding);
            writer.WriteByte((byte)kind);

            if (kind == EncodingId.Named)
            {
                writer.WriteString(encoding!.WebName);
            }
        }

        private static EncodingId GetEncodingKind(Encoding? encoding)
        {
            if (encoding == null)
            {
                return EncodingId.None;
            }

            switch (encoding.CodePage)
            {
                case 1200:
                    Debug.Assert(HasPreamble(Encoding.Unicode));
                    return (encoding.Equals(Encoding.Unicode) || HasPreamble(encoding)) ? EncodingId.Unicode_LE_BOM : EncodingId.Unicode_LE;

                case 1201:
                    Debug.Assert(HasPreamble(Encoding.BigEndianUnicode));
                    return (encoding.Equals(Encoding.BigEndianUnicode) || HasPreamble(encoding)) ? EncodingId.Unicode_BE_BOM : EncodingId.Unicode_BE;

                case 12000:
                    Debug.Assert(HasPreamble(Encoding.UTF32));
                    return (encoding.Equals(Encoding.UTF32) || HasPreamble(encoding)) ? EncodingId.UTF32_LE_BOM : EncodingId.UTF32_LE;

                case 12001:
                    Debug.Assert(HasPreamble(Encoding.UTF32));
                    return (encoding.Equals(Encoding.UTF32) || HasPreamble(encoding)) ? EncodingId.UTF32_BE_BOM : EncodingId.UTF32_BE;

                case 65001:
                    Debug.Assert(HasPreamble(Encoding.UTF8));
                    return (encoding.Equals(Encoding.UTF8) || HasPreamble(encoding)) ? EncodingId.UTF8_BOM : EncodingId.UTF8;

                default:
                    return EncodingId.Named;
            }
        }

        private static bool HasPreamble(Encoding encoding)
#if NETCOREAPP
            => !encoding.Preamble.IsEmpty;
#else
            => !encoding.GetPreamble().IsEmpty();
#endif

        public static Encoding? ReadEncodingFrom(ObjectReader reader, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var kind = reader.ReadByte();
            return ((EncodingId)kind) switch
            {
                EncodingId.None => null,
                EncodingId.Named => Encoding.GetEncoding(reader.ReadString()),
                EncodingId.UTF8 => _cachedEncodings[kind] ??= new UTF8Encoding(encoderShouldEmitUTF8Identifier: false),
                EncodingId.UTF8_BOM => Encoding.UTF8,
                EncodingId.UTF32_BE => _cachedEncodings[kind] ??= new UTF32Encoding(bigEndian: true, byteOrderMark: false),
                EncodingId.UTF32_BE_BOM => _cachedEncodings[kind] ??= new UTF32Encoding(bigEndian: true, byteOrderMark: true),
                EncodingId.UTF32_LE => _cachedEncodings[kind] ??= new UTF32Encoding(bigEndian: false, byteOrderMark: false),
                EncodingId.UTF32_LE_BOM => Encoding.UTF32,
                EncodingId.Unicode_BE => _cachedEncodings[kind] ??= new UnicodeEncoding(bigEndian: true, byteOrderMark: false),
                EncodingId.Unicode_BE_BOM => Encoding.BigEndianUnicode,
                EncodingId.Unicode_LE => _cachedEncodings[kind] ??= new UnicodeEncoding(bigEndian: false, byteOrderMark: false),
                EncodingId.Unicode_LE_BOM => Encoding.Unicode,
                _ => throw ExceptionUtilities.UnexpectedValue(kind),
            };
        }
    }
}
