// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;

namespace Microsoft.CodeAnalysis.Text
{
    /// <summary>
    /// Implementation of SourceText based on a <see cref="T:System.String"/> input
    /// </summary>
    internal sealed partial class EncodedStringText : SourceText
    {
        /// <summary>
        /// Underlying string on which this SourceText instance is based
        /// </summary>
        private readonly string source;

        private readonly Encoding encoding;

        private EncodedStringText(string source, Encoding encoding)
        {
            Debug.Assert(source != null);
            Debug.Assert(encoding != null);
            this.source = source;
            this.encoding = encoding;
        }

        /// <summary>
        /// Initializes an instance of <see cref="T:StringText"/> with provided bytes.
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="defaultEncoding">
        /// Specifies an encoding to be used if the actual encoding can't be determined from the stream content (the stream doesn't start with Byte Order Mark).
        /// If not specified auto-detect heristics are used to determine the encoding. If these heristics fail the decoding is assumed to be <see cref="Encoding.Default"/>.
        /// Note that if the stream starts with Byte Order Mark the value of <paramref name="defaultEncoding"/> is ignored.
        /// </param>
        /// <exception cref="InvalidDataException">
        /// The stream content can't be decoded using the specified <paramref name="defaultEncoding"/>, or
        /// <paramref name="defaultEncoding"/> is null and the stream appears to be a binary file.
        /// </exception>
        /// <exception cref="IOException">An IO error occured while reading from the stream.</exception>
        internal static EncodedStringText Create(Stream stream, Encoding defaultEncoding = null)
        {
            Debug.Assert(stream != null);
            Debug.Assert(stream.CanRead && stream.CanSeek);

            bool detectEncoding = defaultEncoding == null;
            string text;
            Encoding preambleEncoding;
            Encoding actualEncoding;
            if (detectEncoding)
            {
                preambleEncoding = TryReadByteOrderMark(stream);

                if (preambleEncoding == null)
                {
                    // If we didn't find a recognized byte order mark, check to see if the file contents are valid UTF-8
                    // with no byte order mark.  Detecting UTF-8 with no byte order mark implicitly decodes the entire stream
                    // to check each byte, so we won't decode again unless we've already detected some other encoding or
                    // this is not valid UTF-8.

                    var utf8NoBom = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);
                    try
                    {
                        // If we successfully decode the content of the stream as UTF8 it is likely not binary,
                        // so we don't need to check that.
                        text = Decode(stream, utf8NoBom, out actualEncoding);
                        return new EncodedStringText(text, actualEncoding);
                    }
                    catch (DecoderFallbackException)
                    {
                        // fall back to default encoding
                    }
                }
            }
            else
            {
                preambleEncoding = null;
            }

            try
            {
                text = Decode(stream, preambleEncoding ?? defaultEncoding ?? Encoding.Default, out actualEncoding);
            }
            catch (DecoderFallbackException e)
            {
                throw new InvalidDataException(e.Message);
            }

            if (detectEncoding && IsBinary(text))
            {
                throw new InvalidDataException();
            }

            return new EncodedStringText(text, actualEncoding);
        }

        public override Encoding Encoding
        {
            get { return this.encoding; }
        }

        /// <summary>
        /// Underlying string which is the source of this SourceText instance
        /// </summary>
        public string Source
        {
            get { return source; }
        }

        /// <summary>
        /// The length of the text represented by <see cref="T:StringText"/>.
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
        /// <exception cref="T:ArgumentOutOfRangeException">When position is negative or 
        /// greater than <see cref="T:"/> length.</exception>
        public override char this[int position]
        {
            get
            {
                // NOTE: we are not validating position here as that would not 
                //       add any value to the range check that string accessor performs anyways.

                return this.source[position];
            }
        }

        /// <summary>
        /// Provides a string representation of the StringText located within given span.
        /// </summary>
        /// <exception cref="T:ArgumentOutOfRangeException">When given span is outside of the text range.</exception>
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
        /// The heuristic checks
        /// for occurrence of two consecutive NUL (U+0000) characters in the stream, which are 
        /// highly unlikely to appear in a text file. Since the heuristic is applied after 
        /// the text has been decoded, it can be used with any encoding.
        /// </summary>
        internal static bool IsBinary(string text)
        {
            bool wasLastCharNul = text.Length > 0 ? text[0] == '\0' : false;
            for (int i = 1; i < text.Length; i++)
            {
                if (wasLastCharNul & (wasLastCharNul = text[i] == '\0'))
                {
                    return true;
                }
            }

            return false;
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
        internal static string Decode(Stream data, Encoding encoding, out Encoding actualEncoding)
        {
            data.Seek(0, SeekOrigin.Begin);

            // PERF: Detect streams coming from TemporaryStorage.
            var memoryMappedViewStream = data as MemoryMappedViewStream;
            if (memoryMappedViewStream != null && encoding == Encoding.Unicode)
            {
                actualEncoding = encoding;
                return ReadUnicodeStringFromMemoryMappedViewStream(memoryMappedViewStream);
            }

            string text;

            // PERF: If the input is a MemoryStream, we may be able to save an allocation
            var memoryStream = data as MemoryStream;
            if (memoryStream != null && TryDecodeMemoryStream(memoryStream, encoding, out actualEncoding, out text))
            {
                return text;
            }

            // No using block so we don't close the stream
            var reader = new StreamReader(data, encoding);
            text = reader.ReadToEnd();
            actualEncoding = reader.CurrentEncoding;
            return text;
        }

        /// <summary>
        /// Read a Unicode string from a memory mapped view. The stream is not closed on exit.
        /// </summary>
        /// <param name="memoryMappedViewStream">A view over a memory mapped stream which contains a Unicode string (preceded by a Unicode BOM)</param>
        /// <returns>The string</returns>
        private static unsafe string ReadUnicodeStringFromMemoryMappedViewStream(MemoryMappedViewStream memoryMappedViewStream)
        {
            var buffer = memoryMappedViewStream.SafeMemoryMappedViewHandle;
            var privateOffset = GetPrivateOffset(memoryMappedViewStream); // Workaround known bug Devdiv 6441
            byte* ptr = null;
            RuntimeHelpers.PrepareConstrainedRegions();
            try
            {
                buffer.AcquirePointer(ref ptr);
                ptr += privateOffset;
                char* src = (char*)ptr;
                Debug.Assert(*src == 0xFEFF); // BOM: Unicode, little endian
                int length = (int)(memoryMappedViewStream.Length / sizeof(char)) - 1; // -1 since we don't need the BOM
                return new string(src, startIndex: 1, length: length);
            }
            finally
            {
                if (ptr != null)
                {
                    buffer.ReleasePointer();
                }
            }
        }

        // This is a Reflection workaround for known bug Devdiv 6441.  
        //
        // MemoryMappedViewStream.SafeMemoryMappedViewHandle.AcquirePointer returns a pointer
        // that has been aligned to SYSTEM_INFO.dwAllocationGranularity.  Unfortunately the 
        // offset from this pointer to our requested offset into the MemoryMappedFile is only
        // available through the UnmanagedMemoryStream._offset field which is not exposed publicly.
        //
        // Cache the FieldInfo here to minimize any reflection overhead.
        private static FieldInfo unmanagedMemoryStreamOffset = null;
        private static long GetPrivateOffset(UnmanagedMemoryStream stream)
        {
            // Reflection code to workaround known bug 6441
            if (unmanagedMemoryStreamOffset == null)
            {
                unmanagedMemoryStreamOffset = typeof(UnmanagedMemoryStream).GetField("_offset", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.GetField);
            }

            return (long)unmanagedMemoryStreamOffset.GetValue(stream);
        }

        /// <summary>
        /// If the MemoryStream was created with publiclyVisible=true, then we can access its buffer
        /// directly and save allocations in StreamReader. The input MemoryStream is not closed on exit.
        /// </summary>
        /// <exception cref="DecoderFallbackException">If the given encoding is set to use <see cref="DecoderExceptionFallback"/> 
        /// as its fallback decoder.</exception>
        private static bool TryDecodeMemoryStream(MemoryStream data, Encoding encoding, out Encoding actualEncoding, out string decodedText)
        {
            Debug.Assert(data.Position == 0);

            byte[] buffer;
            try
            {
                buffer = data.GetBuffer();
            }
            catch (UnauthorizedAccessException)
            {
                decodedText = null;
                actualEncoding = null;
                return false;
            }

            actualEncoding = TryReadByteOrderMark(data) ?? encoding;
            int preambleSize = (int)data.Position;

            decodedText = actualEncoding.GetString(buffer, preambleSize, (int)data.Length - preambleSize);
            return true;
        }

        private static bool StartsWith(byte[] bytes, byte[] prefix)
        {
            if (bytes.Length < prefix.Length)
            {
                return false;
            }

            for (int i = 0; i < prefix.Length; i++)
            {
                if (bytes[i] != prefix[i])
                {
                    return false;
                }
            }

            return true;
        }

        internal static Encoding TryReadByteOrderMark(Stream data)
        {
            data.Seek(0, SeekOrigin.Begin);

            switch (data.ReadByte())
            {
                case 0xFE:
                    if (data.ReadByte() == 0xFF)
                    {
                        return Encoding.BigEndianUnicode;
                    }

                    break;

                case 0xFF:
                    if (data.ReadByte() == 0xFE)
                    {
                        return Encoding.Unicode;
                    }

                    break;

                case 0xEF:
                    if (data.ReadByte() == 0xBB && data.ReadByte() == 0xBF)
                    {
                        return Encoding.UTF8;
                    }

                    break;
            }

            data.Seek(0, SeekOrigin.Begin);
            return null;
        }

        #endregion
    }
}
