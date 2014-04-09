// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using Roslyn.Utilities;

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

        /// <summary>
        /// Sha1 checksum of the underlying stream.
        /// </summary>
        private ImmutableArray<byte> sha1Checksum;

        /// <summary>
        /// Underlying string which is the source of this SourceText instance
        /// </summary>
        public string Source
        {
            get
            {
                return source;
            }
        }

        /// <summary>
        /// Initializes an instance of <see cref="T:StringText"/> with provided bytes.
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="encodingOpt">
        /// Automatically detected, if not specified: BigEndianUnicode, Unicode, UTF8
        /// (with or without byte order mark). Windows-1252 will be used as a fallback.
        /// This method will throw an InvalidDataException if the stream appears to be a binary file and 
        /// a DecoderFallbackException if it can't be decoded.
        /// </param>
        public EncodedStringText(Stream stream, Encoding encodingOpt)
        {
            Debug.Assert(stream != null);
            Debug.Assert(stream.CanRead && stream.CanSeek);

            if (encodingOpt == null)
            {
                this.source = DetectEncodingAndDecode(stream);
            }
            else
            {
                this.source = Decode(stream, encodingOpt);
            }

            this.sha1Checksum = Hash.ComputeSha1(stream);
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

        public override void Write(TextWriter textWriter, TextSpan span)
        {
            if (span.Start == 0 && span.End == this.Length)
            {
                textWriter.Write(this.Source);
            }
            else
            {
                base.Write(textWriter, span);
            }
        }

        protected override ImmutableArray<byte> GetSha1ChecksumImpl()
        {
            return this.sha1Checksum;
        }

        #region Encoding Detection

        /// <summary>
        /// The following encodings will be automatically detected: 
        /// BigEndianUnicode, Unicode, UTF8 (with or without byte order mark).
        /// The default windows codepage will be used as a fallback. If the 
        /// default windows codepage is 1252 (Western European), we will try to
        /// detect if the stream is binary encoded. Does not close the stream 
        /// after decoding.
        /// </summary>
        /// <exception cref="InvalidDataException">If a binary file is 
        /// detected.</exception>
        /// <exception cref="DecoderFallbackException">If the detected 
        /// encoding can't decode the stream.</exception>
        internal static string DetectEncodingAndDecode(Stream data)
        {
            Encoding encoding = null;

            data.Seek(0, SeekOrigin.Begin);

            // First, look for byte order marks...
            if (data.Length >= 2)
            {
                byte[] byteOrderMark = new byte[2];
                data.Read(byteOrderMark, 0, 2);

                if (0xFE == byteOrderMark[0] && 0xFF == byteOrderMark[1])
                {
                    encoding = Encoding.BigEndianUnicode;
                }
                else if (0xFF == byteOrderMark[0] && 0xFE == byteOrderMark[1])
                {
                    encoding = Encoding.Unicode;
                }
                else if (data.Length >= 3)
                {
                    if (0xEF == byteOrderMark[0] && 0xBB == byteOrderMark[1] && 0xBF == data.ReadByte())
                    {
                        encoding = Encoding.UTF8;
                    }
                }
            }

            // If we didn't find a recognized byte order mark, check to see if the file contents are valid UTF-8
            // with no byte order mark.  Detecting UTF-8 with no byte order mark implicitly decodes the entire array
            // to check each byte, so we won't decode again unless we've already detected some other encoding or
            // this is not valid UTF-8
            string text = null;
            if (encoding != null || !TryDecodeUTF8NoBOM(data, out text))
            {
                if (encoding == null)
                {
                    encoding = Encoding.Default;
                }

                text = DecodeIfNotBinary(data, encoding);
            }

            return text;
        }

        /// <summary>
        /// Decodes the file using the supplied <paramref name="encoding"/> if and only 
        /// if the file fails the heuristic for detecting a binary file. The heuristic checks
        /// for occurrence of two consecutive NUL (U+0000) characters in the stream, which are 
        /// highly unlikely to appear in a text file. Since the heuristic is applied after 
        /// the text has been decoded, it can be used with any encoding.
        /// Does not close the stream when finished.
        /// </summary>
        /// <param name="data">Data stream</param>
        /// <param name="encoding">Encoding to use for decode</param>
        /// <exception cref="InvalidDataException">If the stream is binary encoded</exception>
        /// <returns>Decoded stream as a text string</returns>
        internal static string DecodeIfNotBinary(Stream data, Encoding encoding)
        {
            var text = Decode(data, encoding);

            bool wasLastCharNul = text.Length > 0 ? text[0] == '\0' : false;
            for (int i = 1; i < text.Length; i++)
            {
                if (wasLastCharNul & (wasLastCharNul = text[i] == '\0'))
                {
                    throw new InvalidDataException();
                }
            }

            return text;
        }

        /// <summary>
        /// Decode the given stream using the given encoding. Does not
        /// close the stream afterwards.
        /// </summary>
        /// <param name="data">Data stream</param>
        /// <param name="encoding">Encoding to use for decode</param>
        /// <exception cref="DecoderFallbackException">If the given 
        /// encoding is set to use <see cref="DecoderExceptionFallback"/>
        /// as its fallback decoder.</exception>
        /// <returns>Decoded stream as a text string</returns>
        internal static string Decode(Stream data, Encoding encoding)
        {
            data.Seek(0, SeekOrigin.Begin);

            // PERF: Detect streams coming from TemporaryStorage.
            var memoryMappedViewStream = data as MemoryMappedViewStream;
            if (memoryMappedViewStream != null && encoding == Encoding.Unicode)
            {
                return ReadUnicodeStringFromMemoryMappedViewStream(memoryMappedViewStream);
            }

            string text;

            // PERF: If the input is a MemoryStream, we may be able to save an allocation
            var memoryStream = data as MemoryStream;
            if (memoryStream != null &&
                TryDecodeMemoryStream(memoryStream, encoding,
                                      includePreamble: true,
                                      decodedText: out text))
            {
                return text;
            }

            // No using block so we don't close the stream
            return new StreamReader(data, encoding).ReadToEnd();
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
        private static bool TryDecodeMemoryStream(MemoryStream data, Encoding encoding, bool includePreamble, out string decodedText)
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
                return false;
            }

            int index = includePreamble ? encoding.GetPreamble().Length : 0;

            // buffer.Length is the MemoryStream's capacity, we want to use
            // the original stream's length.
            int count = (int)data.Length - index;
            decodedText = encoding.GetString(buffer, index, count);
            return true;
        }

        /// <summary>
        /// Assume that the input is UTF8 encoded with no byte order mark (BOM)
        /// </summary>
        private static bool TryDecodeUTF8NoBOM(Stream data, out string text)
        {
            data.Seek(0, SeekOrigin.Begin);

            var encoding = new UTF8Encoding(false, true);

            // PERF: If the input is a MemoryStream, we may be able to save an allocation
            var memoryStream = data as MemoryStream;
            try
            {
                if (memoryStream == null || !TryDecodeMemoryStream(memoryStream, encoding, includePreamble: false, decodedText: out text))
                {
                    // We aren't using a 'using' block here because we don't want to automatically close the stream
                    text = new StreamReader(data, encoding).ReadToEnd();
                }

                return true;
            }
            catch (DecoderFallbackException)
            {
                text = null;
                return false;
            }
        }

        #endregion
    }
}
