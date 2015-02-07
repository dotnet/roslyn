// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Text
{
    internal sealed class EncodedStringText : SourceText
    {
        /// <summary>
        /// Underlying string on which this SourceText instance is based
        /// </summary>
        private readonly string _source;

        private readonly Encoding _encoding;

        private const int LargeObjectHeapLimit = 80 * 1024; // 80KB

        private EncodedStringText(string source, Encoding encoding, ImmutableArray<byte> checksum, SourceHashAlgorithm checksumAlgorithm, bool throwIfBinary)
            : base(checksum: checksum, checksumAlgorithm: checksumAlgorithm)
        {
            if (throwIfBinary && IsBinary(source))
            {
                throw new InvalidDataException();
            }

            Debug.Assert(source != null);
            Debug.Assert(encoding != null);
            _source = source;
            _encoding = encoding;
        }

        /// <summary>
        /// Encoding to use when there is no byte order mark (BOM) on the stream. This encoder may throw a <see cref="DecoderFallbackException"/>
        /// if the stream contains invalid UTF-8 bytes.
        /// </summary>
        private static readonly Encoding FallbackEncoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);

        /// <summary>
        /// Initializes an instance of <see cref="EncodedStringText"/> with provided bytes.
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="defaultEncoding">
        /// Specifies an encoding to be used if the actual encoding can't be determined from the stream content (the stream doesn't start with Byte Order Mark).
        /// If not specified auto-detect heuristics are used to determine the encoding. If these heuristics fail the decoding is assumed to be <see cref="Encoding.Default"/>.
        /// Note that if the stream starts with Byte Order Mark the value of <paramref name="defaultEncoding"/> is ignored.
        /// </param>
        /// <param name="checksumAlgorithm">Hash algorithm used to calculate document checksum.</param>
        /// <exception cref="InvalidDataException">
        /// The stream content can't be decoded using the specified <paramref name="defaultEncoding"/>, or
        /// <paramref name="defaultEncoding"/> is null and the stream appears to be a binary file.
        /// </exception>
        /// <exception cref="IOException">An IO error occurred while reading from the stream.</exception>
        internal static SourceText Create(Stream stream, Encoding defaultEncoding = null, SourceHashAlgorithm checksumAlgorithm = SourceHashAlgorithm.Sha1)
        {
            Debug.Assert(stream != null);
            Debug.Assert(stream.CanRead && stream.CanSeek);

            bool detectEncoding = defaultEncoding == null;
            if (detectEncoding)
            {
                try
                {
                    return Decode(stream, FallbackEncoding, checksumAlgorithm, throwIfBinaryDetected: false);
                }
                catch (DecoderFallbackException)
                {
                    // Fall back to Encoding.Default
                }
            }

            try
            {
                return Decode(stream, defaultEncoding ?? Encoding.Default, checksumAlgorithm, throwIfBinaryDetected: detectEncoding);
            }
            catch (DecoderFallbackException e)
            {
                throw new InvalidDataException(e.Message);
            }
        }

        public override Encoding Encoding
        {
            get { return _encoding; }
        }

        /// <summary>
        /// Underlying string which is the source of this SourceText instance
        /// </summary>
        public string Source
        {
            get { return _source; }
        }

        /// <summary>
        /// The length of the text represented by <see cref="EncodedStringText"/>.
        /// </summary>
        public override int Length
        {
            get { return this.Source.Length; }
        }

        /// <summary>
        /// Returns a character at given position.
        /// </summary>
        /// <param name="position">The position to get the character from.</param>
        /// <returns>The character.</returns>
        /// <exception cref="ArgumentOutOfRangeException">When position is negative or 
        /// greater than <see cref="Length"/>.</exception>
        public override char this[int position]
        {
            get
            {
                // NOTE: we are not validating position here as that would not 
                //       add any value to the range check that string accessor performs anyways.

                return _source[position];
            }
        }

        /// <summary>
        /// Provides a string representation of the StringText located within given span.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">When given span is outside of the text range.</exception>
        public override string ToString(TextSpan span)
        {
            if (span.End > this.Source.Length)
            {
                throw new ArgumentOutOfRangeException("span");
            }

            if (span.Start == 0 && span.Length == this.Length)
            {
                return this.Source;
            }
            else
            {
                return this.Source.Substring(span.Start, span.Length);
            }
        }

        public override void CopyTo(int sourceIndex, char[] destination, int destinationIndex, int count)
        {
            this.Source.CopyTo(sourceIndex, destination, destinationIndex, count);
        }

        public override void Write(TextWriter textWriter, TextSpan span, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (span.Start == 0 && span.End == this.Length)
            {
                textWriter.Write(this.Source);
            }
            else
            {
                base.Write(textWriter, span, cancellationToken);
            }
        }

        #region Encoding Detection

        /// <summary>
        /// Check for occurrence of two consecutive NUL (U+0000) characters.
        /// This is unlikely to appear in genuine text, so it's a good heuristic
        /// to detect binary files.
        /// </summary>
        /// <remarks>
        /// internal for unit testing
        /// </remarks>
        internal static bool IsBinary(string text)
        {
            // PERF: We can advance two chars at a time unless we find a NUL.
            for (int i = 1; i < text.Length;)
            {
                if (text[i] == '\0')
                {
                    if (text[i - 1] == '\0')
                    {
                        return true;
                    }

                    i += 1;
                }
                else
                {
                    i += 2;
                }
            }

            return false;
        }

        /// <summary>
        /// Try to create a <see cref="EncodedStringText"/> from the given stream using the given encoding.
        /// </summary>
        /// <param name="data">The input stream containing the encoded text. The stream will not be closed.</param>
        /// <param name="encoding">The expected encoding of the stream. The actual encoding used may be different if byte order marks are detected.</param>
        /// <param name="checksumAlgorithm">The checksum algorithm to use.</param>
        /// <param name="throwIfBinaryDetected">Throw <see cref="InvalidDataException"/> if binary (non-text) data is detected.</param>
        /// <returns>The <see cref="EncodedStringText"/> decoded from the stream.</returns>
        /// <exception cref="DecoderFallbackException">The decoder was unable to decode the stream with the given encoding.</exception>
        /// <remarks>
        /// internal for unit testing
        /// </remarks>
        internal static SourceText Decode(Stream data, Encoding encoding, SourceHashAlgorithm checksumAlgorithm, bool throwIfBinaryDetected = false)
        {
            data.Seek(0, SeekOrigin.Begin);

            if (data.Length > LargeObjectHeapLimit)
            {
                return LargeEncodedText.Decode(data, encoding, checksumAlgorithm, throwIfBinaryDetected);
            }

            Encoding actualEncoding;
            ImmutableArray<byte> checksum = default(ImmutableArray<byte>);
            string text;

            byte[] buffer = TryGetByteArrayFromStream(data);
            if (buffer != null)
            {
                text = Decode(buffer, (int)data.Length, encoding, out actualEncoding);

                // Since we have the buffer, compute the checksum here. This saves allocations if we later
                // need to write out debugging information.
                checksum = CalculateChecksum(buffer, offset: 0, count: (int)data.Length, algorithmId: checksumAlgorithm);
            }
            else
            {
                text = Decode(data, encoding, out actualEncoding);
            }

            return new EncodedStringText(text, actualEncoding, checksum, checksumAlgorithm, throwIfBinary: throwIfBinaryDetected);
        }

        /// <summary>
        /// Some streams are easily represented as byte arrays.
        /// </summary>
        /// <param name="data">The stream</param>
        /// <returns>
        /// The contents of <paramref name="data"/> as a byte array or null if the stream can't easily
        /// be read into a byte array.
        /// </returns>
        private static byte[] TryGetByteArrayFromStream(Stream data)
        {
            byte[] buffer;

            // PERF: If the input is a MemoryStream, we may be able to get at the buffer directly
            var memoryStream = data as MemoryStream;
            if (memoryStream != null && TryGetByteArrayFromMemoryStream(memoryStream, out buffer))
            {
                return buffer;
            }

            // PERF: If the input is a FileStream, we may be able to minimize allocations
            var fileStream = data as FileStream;
            if (fileStream != null && TryGetByteArrayFromFileStream(fileStream, out buffer))
            {
                return buffer;
            }

            return null;
        }

        /// <summary>
        /// Decode the given stream using the given encoding. Does not
        /// close the stream afterwards.
        /// </summary>
        /// <param name="data">Data stream</param>
        /// <param name="encoding">Default encoding to use for decoding.</param>
        /// <param name="actualEncoding">Actual encoding used to read the text.</param>
        /// <exception cref="DecoderFallbackException">If the given encoding is set to use <see cref="DecoderExceptionFallback"/> as its fallback decoder.</exception>
        /// <returns>Decoded stream as a text string</returns>
        private static string Decode(Stream data, Encoding encoding, out Encoding actualEncoding)
        {
            data.Seek(0, SeekOrigin.Begin);

            int length = (int)data.Length;
            if (length == 0)
            {
                actualEncoding = encoding;
                return string.Empty;
            }

            // Note: We are setting the buffer size to 4KB instead of the default 1KB. That's
            // because we can reach this code path for FileStreams that are larger than 80KB
            // and, to avoid FileStream buffer allocations for small files, we may intentionally
            // be using a FileStream with a very small (1 byte) buffer. Using 4KB here matches
            // the default buffer size for FileStream and means we'll still be doing file I/O
            // in 4KB chunks.
            using (var reader = new StreamReader(data, encoding, detectEncodingFromByteOrderMarks: true, bufferSize: Math.Min(4096, length), leaveOpen: true))
            {
                string text = reader.ReadToEnd();
                actualEncoding = reader.CurrentEncoding;
                return text;
            }
        }

        /// <summary>
        /// If the MemoryStream was created with publiclyVisible=true, then we can access its buffer
        /// directly and save allocations in StreamReader. The input MemoryStream is not closed on exit.
        /// </summary>
        /// <returns>True if a byte array could be created.</returns>
        private static bool TryGetByteArrayFromMemoryStream(MemoryStream data, out byte[] buffer)
        {
            Debug.Assert(data.Position == 0);

            try
            {
                buffer = data.GetBuffer();
                return true;
            }
            catch (UnauthorizedAccessException)
            {
                buffer = null;
                return false;
            }
        }

        /// <summary>
        /// Read the contents of a <see cref="FileStream"/> into a byte array.
        /// </summary>
        /// <param name="stream">The FileStream with encoded text.</param>
        /// <param name="buffer">A byte array filled with the contents of the file.</param>
        /// <returns>True if a byte array could be created.</returns>
        private static bool TryGetByteArrayFromFileStream(FileStream stream, out byte[] buffer)
        {
            Debug.Assert(stream != null);
            Debug.Assert(stream.Position == 0);

            int length = (int)stream.Length;
            if (length == 0)
            {
                buffer = SpecializedCollections.EmptyBytes;
                return true;
            }

            // PERF: While this is an obvious byte array allocation, it is still cheaper than
            // using StreamReader.ReadToEnd. The alternative allocates:
            // 1. A 1KB byte array in the StreamReader for buffered reads
            // 2. A 4KB byte array in the FileStream for buffered reads
            // 3. A StringBuilder and its associated char arrays (enough to represent the final decoded string)

            // TODO: Can this allocation be pooled?
            buffer = new byte[length];

            // Note: FileStream.Read may still allocate its internal buffer if length is less
            // than the buffer size. The default buffer size is 4KB, so this will incur a 4KB
            // allocation for any files less than 4KB. That's why, for example, the command
            // line compiler actually specifies a very small buffer size.
            return stream.Read(buffer, 0, length) == length;
        }

        /// <summary>
        /// Decode text from a byte array.
        /// </summary>
        /// <param name="buffer">The byte array containing encoded text.</param>
        /// <param name="length">The count of valid bytes in <paramref name="buffer"/>.</param>
        /// <param name="encoding">The encoding to use if an encoding cannot be determined from the byte order mark.</param>
        /// <param name="actualEncoding">The actual encoding used.</param>
        /// <returns>The decoded text.</returns>
        /// <exception cref="DecoderFallbackException">If the given encoding is set to use <see cref="DecoderExceptionFallback"/> 
        /// as its fallback decoder.</exception>
        private static string Decode(byte[] buffer, int length, Encoding encoding, out Encoding actualEncoding)
        {
            int preambleLength;
            actualEncoding = TryReadByteOrderMark(buffer, length, out preambleLength) ?? encoding;
            return actualEncoding.GetString(buffer, preambleLength, length - preambleLength);
        }

        /// <summary>
        /// Detect an encoding by looking for byte order marks.
        /// </summary>
        /// <param name="source">A buffer containing the encoded text.</param>
        /// <param name="length">The length of valid data in the buffer.</param>
        /// <param name="preambleLength">The length of any detected byte order marks.</param>
        /// <returns>The detected encoding or null if no recognized byte order mark was present.</returns>
        private static Encoding TryReadByteOrderMark(byte[] source, int length, out int preambleLength)
        {
            Debug.Assert(source != null);
            Debug.Assert(length <= source.Length);

            if (length >= 2)
            {
                switch (source[0])
                {
                    case 0xFE:
                        if (source[1] == 0xFF)
                        {
                            preambleLength = 2;
                            return Encoding.BigEndianUnicode;
                        }

                        break;

                    case 0xFF:
                        if (source[1] == 0xFE)
                        {
                            preambleLength = 2;
                            return Encoding.Unicode;
                        }

                        break;

                    case 0xEF:
                        if (source[1] == 0xBB && length >= 3 && source[2] == 0xBF)
                        {
                            preambleLength = 3;
                            return Encoding.UTF8;
                        }

                        break;
                }
            }

            preambleLength = 0;
            return null;
        }

        [ThreadStatic]
        private static byte[] t_bomBytes;

        /// <summary>
        /// Detect an encoding by looking for byte order marks at the beginning of the stream.
        /// </summary>
        /// <param name="data">The stream containing encoded text.</param>
        /// <returns>The detected encoding or null if no recognized byte order mark was present.</returns>
        /// <remarks>
        /// On exit, the stream's position is set to the first position after any decoded byte order
        /// mark or rewound to the start if no byte order mark was detected.
        /// </remarks>
        internal static Encoding TryReadByteOrderMark(Stream data)
        {
            Debug.Assert(data != null);
            data.Seek(0, SeekOrigin.Begin);

            if (data.Length < 2)
            {
                // Not long enough for any valid BOM prefix
                return null;
            }

            // PERF: Avoid repeated calls to Stream.ReadByte since that method allocates a 1-byte array on each call.
            // Instead, using a thread local byte array.
            if (t_bomBytes == null)
            {
                t_bomBytes = new byte[3];
            }

            int validLength = Math.Min((int)data.Length, t_bomBytes.Length);
            data.Read(t_bomBytes, 0, validLength);

            int preambleLength;
            Encoding detectedEncoding = TryReadByteOrderMark(t_bomBytes, validLength, out preambleLength);

            if (preambleLength != validLength)
            {
                data.Seek(preambleLength, SeekOrigin.Begin);
            }

            return detectedEncoding;
        }

        #endregion
    }
}
