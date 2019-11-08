// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Text
{
    internal static class EncodedStringText
    {
        private const int LargeObjectHeapLimitInChars = 40 * 1024; // 40KB

        /// <summary>
        /// Encoding to use when there is no byte order mark (BOM) on the stream. This encoder may throw a <see cref="DecoderFallbackException"/>
        /// if the stream contains invalid UTF-8 bytes.
        /// </summary>
        private static readonly Encoding s_utf8Encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);

        /// <summary>
        /// Encoding to use when UTF-8 fails. We try to find the following, in order, if available:
        ///     1. The default ANSI codepage
        ///     2. CodePage 1252.
        ///     3. Latin1.
        /// </summary>
        private static readonly Lazy<Encoding> s_fallbackEncoding = new Lazy<Encoding>(GetFallbackEncoding);

        private static Encoding GetFallbackEncoding()
        {
            try
            {
                if (CodePagesEncodingProvider.Instance != null)
                {
                    // If we're running on CoreCLR we have to register the CodePagesEncodingProvider
                    // first
                    Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
                }

                // Try to get the default ANSI code page in the operating system's
                // regional and language settings, and fall back to 1252 otherwise
                return Encoding.GetEncoding(0)
                    ?? Encoding.GetEncoding(1252);
            }
            catch (NotSupportedException)
            {
                return Encoding.GetEncoding(name: "Latin1");
            }
        }

        /// <summary>
        /// Initializes an instance of <see cref="SourceText"/> from the provided stream. This version differs
        /// from <see cref="SourceText.From(Stream, Encoding, SourceHashAlgorithm, bool)"/> in two ways:
        /// 1. It attempts to minimize allocations by trying to read the stream into a byte array.
        /// 2. If <paramref name="defaultEncoding"/> is null, it will first try UTF8 and, if that fails, it will
        ///    try CodePage 1252. If CodePage 1252 is not available on the system, then it will try Latin1.
        /// </summary>
        /// <param name="stream">The stream containing encoded text.</param>
        /// <param name="defaultEncoding">
        /// Specifies an encoding to be used if the actual encoding can't be determined from the stream content (the stream doesn't start with Byte Order Mark).
        /// If not specified auto-detect heuristics are used to determine the encoding. If these heuristics fail the decoding is assumed to be Encoding.Default.
        /// Note that if the stream starts with Byte Order Mark the value of <paramref name="defaultEncoding"/> is ignored.
        /// </param>
        /// <param name="canBeEmbedded">Indicates if the file can be embedded in the PDB.</param>
        /// <param name="checksumAlgorithm">Hash algorithm used to calculate document checksum.</param>
        /// <exception cref="InvalidDataException">
        /// The stream content can't be decoded using the specified <paramref name="defaultEncoding"/>, or
        /// <paramref name="defaultEncoding"/> is null and the stream appears to be a binary file.
        /// </exception>
        /// <exception cref="IOException">An IO error occurred while reading from the stream.</exception>
        internal static SourceText Create(Stream stream,
            Encoding defaultEncoding = null,
            SourceHashAlgorithm checksumAlgorithm = SourceHashAlgorithm.Sha1,
            bool canBeEmbedded = false)
        {
            return Create(stream,
                s_fallbackEncoding,
                defaultEncoding: defaultEncoding,
                checksumAlgorithm: checksumAlgorithm,
                canBeEmbedded: canBeEmbedded);
        }

        private static SourceText Create(Stream stream, Lazy<Encoding> getEncoding,
            Encoding defaultEncoding = null,
            SourceHashAlgorithm checksumAlgorithm = SourceHashAlgorithm.Sha1,
            bool canBeEmbedded = false)
        {
            Debug.Assert(stream != null);
            Debug.Assert(stream is { CanRead: true, CanSeek: true });

            bool detectEncoding = defaultEncoding == null;
            if (detectEncoding)
            {
                try
                {
                    return Decode(stream, s_utf8Encoding, checksumAlgorithm, throwIfBinaryDetected: false, canBeEmbedded: canBeEmbedded);
                }
                catch (DecoderFallbackException)
                {
                    // Fall back to Encoding.ASCII
                }
            }

            try
            {
                return Decode(stream, defaultEncoding ?? getEncoding.Value, checksumAlgorithm, throwIfBinaryDetected: detectEncoding, canBeEmbedded: canBeEmbedded);
            }
            catch (DecoderFallbackException e)
            {
                throw new InvalidDataException(e.Message);
            }
        }

        /// <summary>
        /// Try to create a <see cref="SourceText"/> from the given stream using the given encoding.
        /// </summary>
        /// <param name="data">The input stream containing the encoded text. The stream will not be closed.</param>
        /// <param name="encoding">The expected encoding of the stream. The actual encoding used may be different if byte order marks are detected.</param>
        /// <param name="checksumAlgorithm">The checksum algorithm to use.</param>
        /// <param name="throwIfBinaryDetected">Throw <see cref="InvalidDataException"/> if binary (non-text) data is detected.</param>
        /// <param name="canBeEmbedded">Indicates if the text can be embedded in the PDB.</param>
        /// <returns>The <see cref="SourceText"/> decoded from the stream.</returns>
        /// <exception cref="DecoderFallbackException">The decoder was unable to decode the stream with the given encoding.</exception>
        /// <exception cref="IOException">Error reading from stream.</exception> 
        private static SourceText Decode(
            Stream data,
            Encoding encoding,
            SourceHashAlgorithm checksumAlgorithm,
            bool throwIfBinaryDetected = false,
            bool canBeEmbedded = false)
        {
            Debug.Assert(data != null);
            Debug.Assert(encoding != null);

            data.Seek(0, SeekOrigin.Begin);

            // For small streams, see if we can read the byte buffer directly.
            if (encoding.GetMaxCharCountOrThrowIfHuge(data) < LargeObjectHeapLimitInChars)
            {
                if (TryGetBytesFromStream(data, out ArraySegment<byte> bytes) && bytes.Offset == 0)
                {
                    return SourceText.From(bytes.Array,
                                           (int)data.Length,
                                           encoding,
                                           checksumAlgorithm,
                                           throwIfBinaryDetected,
                                           canBeEmbedded);
                }
            }

            return SourceText.From(data, encoding, checksumAlgorithm, throwIfBinaryDetected, canBeEmbedded);
        }

        /// <summary>
        /// Some streams are easily represented as bytes.
        /// </summary>
        /// <param name="data">The stream</param>
        /// <param name="bytes">The bytes, if available.</param>
        /// <returns>
        /// True if the stream's bytes could easily be read, false otherwise.
        /// </returns>
        internal static bool TryGetBytesFromStream(Stream data, out ArraySegment<byte> bytes)
        {
            // PERF: If the input is a MemoryStream, we may be able to get at the buffer directly
            var memoryStream = data as MemoryStream;
            if (memoryStream != null)
            {
                return memoryStream.TryGetBuffer(out bytes);
            }

            // PERF: If the input is a FileStream, we may be able to minimize allocations
            var fileStream = data as FileStream;
            if (fileStream != null)
            {
                return TryGetBytesFromFileStream(fileStream, out bytes);
            }

            bytes = new ArraySegment<byte>(Array.Empty<byte>());
            return false;
        }

        /// <summary>
        /// Read the contents of a FileStream into a byte array.
        /// </summary>
        /// <param name="stream">The FileStream with encoded text.</param>
        /// <param name="bytes">A byte array filled with the contents of the file.</param>
        /// <returns>True if a byte array could be created.</returns>
        private static bool TryGetBytesFromFileStream(FileStream stream,
                                                      out ArraySegment<byte> bytes)
        {
            Debug.Assert(stream != null);
            Debug.Assert(stream.Position == 0);

            int length = (int)stream.Length;
            if (length == 0)
            {
                bytes = new ArraySegment<byte>(Array.Empty<byte>());
                return true;
            }

            // PERF: While this is an obvious byte array allocation, it is still cheaper than
            // using StreamReader.ReadToEnd. The alternative allocates:
            // 1. A 1KB byte array in the StreamReader for buffered reads
            // 2. A 4KB byte array in the FileStream for buffered reads
            // 3. A StringBuilder and its associated char arrays (enough to represent the final decoded string)

            // TODO: Can this allocation be pooled?
            var buffer = new byte[length];

            // Note: FileStream.Read may still allocate its internal buffer if length is less
            // than the buffer size. The default buffer size is 4KB, so this will incur a 4KB
            // allocation for any files less than 4KB. That's why, for example, the command
            // line compiler actually specifies a very small buffer size.
            var success = stream.TryReadAll(buffer, 0, length) == length;

            bytes = success
                ? new ArraySegment<byte>(buffer)
                : new ArraySegment<byte>(Array.Empty<byte>());

            return success;
        }

        internal static class TestAccessor
        {
            internal static SourceText Create(Stream stream, Lazy<Encoding> getEncoding, Encoding defaultEncoding, SourceHashAlgorithm checksumAlgorithm, bool canBeEmbedded)
                => EncodedStringText.Create(stream, getEncoding, defaultEncoding, checksumAlgorithm, canBeEmbedded);

            internal static SourceText Decode(Stream data, Encoding encoding, SourceHashAlgorithm checksumAlgorithm, bool throwIfBinaryDetected, bool canBeEmbedded)
                => EncodedStringText.Decode(data, encoding, checksumAlgorithm, throwIfBinaryDetected, canBeEmbedded);
        }
    }
}
