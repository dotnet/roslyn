// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.IO.Hashing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Text
{
    /// <summary>
    /// An abstraction of source text.
    /// </summary>
    public abstract class SourceText
    {
        private const int CharBufferSize = 32 * 1024;
        private const int CharBufferCount = 5;
        internal const int LargeObjectHeapLimitInChars = 40 * 1024; // 40KB

        private static readonly ObjectPool<char[]> s_charArrayPool = new ObjectPool<char[]>(() => new char[CharBufferSize], CharBufferCount);
        private static readonly ObjectPool<XxHash128> s_contentHashPool = new ObjectPool<XxHash128>(() => new XxHash128());

        private readonly SourceHashAlgorithm _checksumAlgorithm;
        private SourceTextContainer? _lazyContainer;
        private TextLineCollection? _lazyLineInfo;

        /// <summary>
        /// Backing store of <see cref="GetChecksum"/>
        /// </summary>
        private ImmutableArray<byte> _lazyChecksum;
        private readonly ImmutableArray<byte> _precomputedEmbeddedTextBlob;

        private ImmutableArray<byte> _lazyContentHash;

        private static readonly Encoding s_utf8EncodingWithNoBOM = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: false);

        protected SourceText(ImmutableArray<byte> checksum = default, SourceHashAlgorithm checksumAlgorithm = SourceHashAlgorithm.Sha1, SourceTextContainer? container = null)
        {
            ValidateChecksumAlgorithm(checksumAlgorithm);

            if (!checksum.IsDefault && checksum.Length != CryptographicHashProvider.GetHashSize(checksumAlgorithm))
            {
                throw new ArgumentException(CodeAnalysisResources.InvalidHash, nameof(checksum));
            }

            _checksumAlgorithm = checksumAlgorithm;
            _lazyChecksum = checksum;
            _lazyContainer = container;
        }

        internal SourceText(ImmutableArray<byte> checksum, SourceHashAlgorithm checksumAlgorithm, ImmutableArray<byte> embeddedTextBlob)
            : this(checksum, checksumAlgorithm, container: null)
        {
            // We should never have precomputed the embedded text blob without precomputing the checksum.
            Debug.Assert(embeddedTextBlob.IsDefault || !checksum.IsDefault);

            if (!checksum.IsDefault && embeddedTextBlob.IsDefault)
            {
                // We can't compute the embedded text blob lazily if we're given a precomputed checksum.
                // This happens when source bytes/stream were given, but canBeEmbedded=true was not passed.
                _precomputedEmbeddedTextBlob = ImmutableArray<byte>.Empty;
            }
            else
            {
                _precomputedEmbeddedTextBlob = embeddedTextBlob;
            }
        }

        internal static void ValidateChecksumAlgorithm(SourceHashAlgorithm checksumAlgorithm)
        {
            if (!SourceHashAlgorithms.IsSupportedAlgorithm(checksumAlgorithm))
            {
                throw new ArgumentException(CodeAnalysisResources.UnsupportedHashAlgorithm, nameof(checksumAlgorithm));
            }
        }

        /// <summary>
        /// Constructs a <see cref="SourceText"/> from text in a string.
        /// </summary>
        /// <param name="text">Text.</param>
        /// <param name="encoding">
        /// Encoding of the file that the <paramref name="text"/> was read from or is going to be saved to.
        /// <c>null</c> if the encoding is unspecified.
        /// If the encoding is not specified the resulting <see cref="SourceText"/> isn't debuggable.
        /// If an encoding-less <see cref="SourceText"/> is written to a file a <see cref="Encoding.UTF8"/> shall be used as a default.
        /// </param>
        /// <param name="checksumAlgorithm">
        /// Hash algorithm to use to calculate checksum of the text that's saved to PDB.
        /// </param>
        /// <exception cref="ArgumentNullException"><paramref name="text"/> is null.</exception>
        /// <exception cref="ArgumentException"><paramref name="checksumAlgorithm"/> is not supported.</exception>
        public static SourceText From(string text, Encoding? encoding = null, SourceHashAlgorithm checksumAlgorithm = SourceHashAlgorithm.Sha1)
        {
            if (text == null)
            {
                throw new ArgumentNullException(nameof(text));
            }

            return new StringText(text, encoding, checksumAlgorithm: checksumAlgorithm);
        }

        /// <summary>
        /// Constructs a <see cref="SourceText"/> from text in a string.
        /// </summary>
        /// <param name="reader">TextReader</param>
        /// <param name="length">length of content from <paramref name="reader"/></param>
        /// <param name="encoding">
        /// Encoding of the file that the <paramref name="reader"/> was read from or is going to be saved to.
        /// <c>null</c> if the encoding is unspecified.
        /// If the encoding is not specified the resulting <see cref="SourceText"/> isn't debuggable.
        /// If an encoding-less <see cref="SourceText"/> is written to a file a <see cref="Encoding.UTF8"/> shall be used as a default.
        /// </param>
        /// <param name="checksumAlgorithm">
        /// Hash algorithm to use to calculate checksum of the text that's saved to PDB.
        /// </param>
        /// <exception cref="ArgumentNullException"><paramref name="reader"/> is null.</exception>
        /// <exception cref="ArgumentException"><paramref name="checksumAlgorithm"/> is not supported.</exception>
        public static SourceText From(
            TextReader reader,
            int length,
            Encoding? encoding = null,
            SourceHashAlgorithm checksumAlgorithm = SourceHashAlgorithm.Sha1)
        {
            if (reader == null)
            {
                throw new ArgumentNullException(nameof(reader));
            }

            // If the resulting string would end up on the large object heap, then use LargeEncodedText.
            if (length >= LargeObjectHeapLimitInChars)
            {
                return LargeText.Decode(reader, length, encoding, checksumAlgorithm);
            }

            string text = reader.ReadToEnd();
            return From(text, encoding, checksumAlgorithm);
        }

        // 1.0 BACKCOMPAT OVERLOAD - DO NOT TOUCH
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static SourceText From(Stream stream, Encoding? encoding, SourceHashAlgorithm checksumAlgorithm, bool throwIfBinaryDetected)
          => From(stream, encoding, checksumAlgorithm, throwIfBinaryDetected, canBeEmbedded: false);

        /// <summary>
        /// Constructs a <see cref="SourceText"/> from stream content.
        /// </summary>
        /// <param name="stream">Stream. The stream must be seekable.</param>
        /// <param name="encoding">
        /// Data encoding to use if the stream doesn't start with Byte Order Mark specifying the encoding.
        /// <see cref="Encoding.UTF8"/> if not specified.
        /// </param>
        /// <param name="checksumAlgorithm">
        /// Hash algorithm to use to calculate checksum of the text that's saved to PDB.
        /// </param>
        /// <param name="throwIfBinaryDetected">If the decoded text contains at least two consecutive NUL
        /// characters, then an <see cref="InvalidDataException"/> is thrown.</param>
        /// <param name="canBeEmbedded">True if the text can be passed to <see cref="EmbeddedText.FromSource"/> and be embedded in a PDB.</param>
        /// <exception cref="ArgumentNullException"><paramref name="stream"/> is null.</exception>
        /// <exception cref="ArgumentException">
        /// <paramref name="stream"/> doesn't support reading or seeking.
        /// <paramref name="checksumAlgorithm"/> is not supported.
        /// </exception>
        /// <exception cref="DecoderFallbackException">If the given encoding is set to use a throwing decoder as a fallback</exception>
        /// <exception cref="InvalidDataException">Two consecutive NUL characters were detected in the decoded text and <paramref name="throwIfBinaryDetected"/> was true.</exception>
        /// <exception cref="IOException">An I/O error occurs.</exception>
        /// <remarks>Reads from the beginning of the stream. Leaves the stream open.</remarks>
        public static SourceText From(
            Stream stream,
            Encoding? encoding = null,
            SourceHashAlgorithm checksumAlgorithm = SourceHashAlgorithm.Sha1,
            bool throwIfBinaryDetected = false,
            bool canBeEmbedded = false)
        {
            if (stream == null)
            {
                throw new ArgumentNullException(nameof(stream));
            }

            if (!stream.CanRead)
            {
                throw new ArgumentException(CodeAnalysisResources.StreamMustSupportReadAndSeek, nameof(stream));
            }

            ValidateChecksumAlgorithm(checksumAlgorithm);

            encoding = encoding ?? s_utf8EncodingWithNoBOM;

            if (stream.CanSeek)
            {
                // If the resulting string would end up on the large object heap, then use LargeEncodedText.
                if (GetMaxCharCountOrThrowIfHuge(encoding, stream) >= LargeObjectHeapLimitInChars)
                {
                    return LargeText.Decode(stream, encoding, checksumAlgorithm, throwIfBinaryDetected, canBeEmbedded);
                }
            }

            string text = Decode(stream, encoding, out encoding);
            if (throwIfBinaryDetected && IsBinary(text))
            {
                throw new InvalidDataException();
            }

            // We must compute the checksum and embedded text blob now while we still have the original bytes in hand.
            // We cannot re-encode to obtain checksum and blob as the encoding is not guaranteed to round-trip.
            var checksum = CalculateChecksum(stream, checksumAlgorithm);
            var embeddedTextBlob = canBeEmbedded ? EmbeddedText.CreateBlob(stream) : default(ImmutableArray<byte>);
            return new StringText(text, encoding, checksum, checksumAlgorithm, embeddedTextBlob);
        }

        // 1.0 BACKCOMPAT OVERLOAD - DO NOT TOUCH
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static SourceText From(byte[] buffer, int length, Encoding? encoding, SourceHashAlgorithm checksumAlgorithm, bool throwIfBinaryDetected)
            => From(buffer, length, encoding, checksumAlgorithm, throwIfBinaryDetected, canBeEmbedded: false);

        /// <summary>
        /// Constructs a <see cref="SourceText"/> from a byte array.
        /// </summary>
        /// <param name="buffer">The encoded source buffer.</param>
        /// <param name="length">The number of bytes to read from the buffer.</param>
        /// <param name="encoding">
        /// Data encoding to use if the encoded buffer doesn't start with Byte Order Mark.
        /// <see cref="Encoding.UTF8"/> if not specified.
        /// </param>
        /// <param name="checksumAlgorithm">
        /// Hash algorithm to use to calculate checksum of the text that's saved to PDB.
        /// </param>
        /// <param name="throwIfBinaryDetected">If the decoded text contains at least two consecutive NUL
        /// characters, then an <see cref="InvalidDataException"/> is thrown.</param>
        /// <returns>The decoded text.</returns>
        /// <param name="canBeEmbedded">True if the text can be passed to <see cref="EmbeddedText.FromSource"/> and be embedded in a PDB.</param>
        /// <exception cref="ArgumentNullException">The <paramref name="buffer"/> is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException">The <paramref name="length"/> is negative or longer than the <paramref name="buffer"/>.</exception>
        /// <exception cref="ArgumentException"><paramref name="checksumAlgorithm"/> is not supported.</exception>
        /// <exception cref="DecoderFallbackException">If the given encoding is set to use a throwing decoder as a fallback</exception>
        /// <exception cref="InvalidDataException">Two consecutive NUL characters were detected in the decoded text and <paramref name="throwIfBinaryDetected"/> was true.</exception>
        public static SourceText From(
            byte[] buffer,
            int length,
            Encoding? encoding = null,
            SourceHashAlgorithm checksumAlgorithm = SourceHashAlgorithm.Sha1,
            bool throwIfBinaryDetected = false,
            bool canBeEmbedded = false)
        {
            if (buffer == null)
            {
                throw new ArgumentNullException(nameof(buffer));
            }

            if (length < 0 || length > buffer.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(length));
            }

            ValidateChecksumAlgorithm(checksumAlgorithm);

            string text = Decode(buffer, length, encoding ?? s_utf8EncodingWithNoBOM, out encoding);
            if (throwIfBinaryDetected && IsBinary(text))
            {
                throw new InvalidDataException();
            }

            // We must compute the checksum and embedded text blob now while we still have the original bytes in hand.
            // We cannot re-encode to obtain checksum and blob as the encoding is not guaranteed to round-trip.
            var checksum = CalculateChecksum(buffer, 0, length, checksumAlgorithm);
            var embeddedTextBlob = canBeEmbedded ? EmbeddedText.CreateBlob(new ArraySegment<byte>(buffer, 0, length)) : default(ImmutableArray<byte>);
            return new StringText(text, encoding, checksum, checksumAlgorithm, embeddedTextBlob);
        }

        /// <summary>
        /// Decode text from a stream.
        /// </summary>
        /// <param name="stream">The stream containing encoded text.</param>
        /// <param name="encoding">The encoding to use if an encoding cannot be determined from the byte order mark.</param>
        /// <param name="actualEncoding">The actual encoding used.</param>
        /// <returns>The decoded text.</returns>
        /// <exception cref="DecoderFallbackException">If the given encoding is set to use a throwing decoder as a fallback</exception>
        private static string Decode(Stream stream, Encoding encoding, out Encoding actualEncoding)
        {
            RoslynDebug.Assert(stream != null);
            RoslynDebug.Assert(encoding != null);
            const int maxBufferSize = 4096;
            int bufferSize = maxBufferSize;

            if (stream.CanSeek)
            {
                stream.Seek(0, SeekOrigin.Begin);

                int length = (int)stream.Length;
                if (length == 0)
                {
                    actualEncoding = encoding;
                    return string.Empty;
                }

                bufferSize = Math.Min(maxBufferSize, length);
            }

            // Note: We are setting the buffer size to 4KB instead of the default 1KB. That's
            // because we can reach this code path for FileStreams and, to avoid FileStream
            // buffer allocations for small files, we may intentionally be using a FileStream
            // with a very small (1 byte) buffer. Using 4KB here matches the default buffer
            // size for FileStream and means we'll still be doing file I/O in 4KB chunks.
            using (var reader = new StreamReader(stream, encoding, detectEncodingFromByteOrderMarks: true, bufferSize: bufferSize, leaveOpen: true))
            {
                string text = reader.ReadToEnd();
                actualEncoding = reader.CurrentEncoding;
                return text;
            }
        }

        /// <summary>
        /// Decode text from a byte array.
        /// </summary>
        /// <param name="buffer">The byte array containing encoded text.</param>
        /// <param name="length">The count of valid bytes in <paramref name="buffer"/>.</param>
        /// <param name="encoding">The encoding to use if an encoding cannot be determined from the byte order mark.</param>
        /// <param name="actualEncoding">The actual encoding used.</param>
        /// <returns>The decoded text.</returns>
        /// <exception cref="DecoderFallbackException">If the given encoding is set to use a throwing decoder as a fallback</exception>
        private static string Decode(byte[] buffer, int length, Encoding encoding, out Encoding actualEncoding)
        {
            RoslynDebug.Assert(buffer != null);
            RoslynDebug.Assert(encoding != null);
            int preambleLength;
            actualEncoding = TryReadByteOrderMark(buffer, length, out preambleLength) ?? encoding;
            return actualEncoding.GetString(buffer, preambleLength, length - preambleLength);
        }

        /// <summary>
        /// Check for occurrence of two consecutive NUL (U+0000) characters.
        /// This is unlikely to appear in genuine text, so it's a good heuristic
        /// to detect binary files.
        /// </summary>
        /// <remarks>
        /// internal for unit testing
        /// </remarks>
        internal static bool IsBinary(ReadOnlySpan<char> text)
        {
#if NET
            // On .NET Core, Contains has an optimized vectorized implementation, much faster than a custom loop.
            return text.Contains("\0\0", StringComparison.Ordinal);
#else
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
#endif
        }

        /// <inheritdoc cref="IsBinary(ReadOnlySpan{char})" />
        internal static bool IsBinary(string text) => IsBinary(text.AsSpan());

        /// <summary>
        /// Hash algorithm to use to calculate checksum of the text that's saved to PDB.
        /// </summary>
        public SourceHashAlgorithm ChecksumAlgorithm => _checksumAlgorithm;

        /// <summary>
        /// Encoding of the file that the text was read from or is going to be saved to.
        /// <c>null</c> if the encoding is unspecified.
        /// </summary>
        /// <remarks>
        /// If the encoding is not specified the source isn't debuggable.
        /// If an encoding-less <see cref="SourceText"/> is written to a file a <see cref="Encoding.UTF8"/> shall be used as a default.
        /// </remarks>
        public abstract Encoding? Encoding { get; }

        /// <summary>
        /// The length of the text in characters.
        /// </summary>
        public abstract int Length { get; }

        /// <summary>
        /// The size of the storage representation of the text (in characters).
        /// This can differ from length when storage buffers are reused to represent fragments/subtext.
        /// </summary>
        internal virtual int StorageSize
        {
            get { return this.Length; }
        }

        internal virtual ImmutableArray<SourceText> Segments
        {
            get { return ImmutableArray<SourceText>.Empty; }
        }

        internal virtual SourceText StorageKey
        {
            get { return this; }
        }

        /// <summary>
        /// Indicates whether this source text can be embedded in the PDB.
        /// </summary>
        /// <remarks>
        /// If this text was constructed via <see cref="From(byte[], int, Encoding, SourceHashAlgorithm, bool, bool)"/> or
        /// <see cref="From(Stream, Encoding, SourceHashAlgorithm, bool, bool)"/>, then the canBeEmbedded arg must have
        /// been true.
        ///
        /// Otherwise, <see cref="Encoding" /> must be non-null.
        /// </remarks>
        public bool CanBeEmbedded
        {
            get
            {
                if (_precomputedEmbeddedTextBlob.IsDefault)
                {
                    // If we didn't precompute the embedded text blob from bytes/stream, 
                    // we can only support embedding if we have an encoding with which 
                    // to encode the text in the PDB.
                    return Encoding != null;
                }

                // We use a sentinel empty blob to indicate that embedding has been disallowed.
                return !_precomputedEmbeddedTextBlob.IsEmpty;
            }
        }

        /// <summary>
        /// If the text was created from a stream or byte[] and canBeEmbedded argument was true, 
        /// this provides the embedded text blob that was precomputed using the original stream
        /// or byte[]. The precomputation was required in that case so that the bytes written to
        /// the PDB match the original bytes exactly (and match the checksum of the original 
        /// bytes). 
        /// </summary>
        internal ImmutableArray<byte> PrecomputedEmbeddedTextBlob => _precomputedEmbeddedTextBlob;

        /// <summary>
        /// Returns a character at given position.
        /// </summary>
        /// <param name="position">The position to get the character from.</param>
        /// <returns>The character.</returns>
        /// <exception cref="ArgumentOutOfRangeException">When position is negative or 
        /// greater than <see cref="Length"/>.</exception>
        public abstract char this[int position] { get; }

        /// <summary>
        /// Copy a range of characters from this SourceText to a destination array.
        /// </summary>
        public abstract void CopyTo(int sourceIndex, char[] destination, int destinationIndex, int count);

        /// <summary>
        /// The container of this <see cref="SourceText"/>.
        /// </summary>
        public virtual SourceTextContainer Container
        {
            get
            {
                if (_lazyContainer == null)
                {
                    Interlocked.CompareExchange(ref _lazyContainer, new StaticContainer(this), null);
                }

                return _lazyContainer;
            }
        }

        internal void CheckSubSpan(TextSpan span)
        {
            Debug.Assert(0 <= span.Start && span.Start <= span.End);

            if (span.End > this.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(span));
            }
        }

        /// <summary>
        /// Gets a <see cref="SourceText"/> that contains the characters in the specified span of this text.
        /// </summary>
        public virtual SourceText GetSubText(TextSpan span)
        {
            CheckSubSpan(span);

            int spanLength = span.Length;
            if (spanLength == 0)
            {
                return SourceText.From(string.Empty, this.Encoding, this.ChecksumAlgorithm);
            }
            else if (spanLength == this.Length && span.Start == 0)
            {
                return this;
            }
            else
            {
                return new SubText(this, span);
            }
        }

        /// <summary>
        /// Returns a <see cref="SourceText"/> that has the contents of this text including and after the start position.
        /// </summary>
        public SourceText GetSubText(int start)
        {
            if (start < 0 || start > this.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(start));
            }

            if (start == 0)
            {
                return this;
            }
            else
            {
                return this.GetSubText(new TextSpan(start, this.Length - start));
            }
        }

        /// <summary>
        /// Write this <see cref="SourceText"/> to a text writer.
        /// </summary>
        public void Write(TextWriter textWriter, CancellationToken cancellationToken = default(CancellationToken))
        {
            this.Write(textWriter, new TextSpan(0, this.Length), cancellationToken);
        }

        /// <summary>
        /// Write a span of text to a text writer.
        /// </summary>
        public virtual void Write(TextWriter writer, TextSpan span, CancellationToken cancellationToken = default(CancellationToken))
        {
            CheckSubSpan(span);

            var buffer = s_charArrayPool.Allocate();
            try
            {
                int offset = span.Start;
                int end = span.End;
                while (offset < end)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    int count = Math.Min(buffer.Length, end - offset);
                    this.CopyTo(offset, buffer, 0, count);
                    writer.Write(buffer, 0, count);
                    offset += count;
                }
            }
            finally
            {
                s_charArrayPool.Free(buffer);
            }
        }

        /// <summary>
        /// Cryptographic checksum determined by <see cref="ChecksumAlgorithm"/>.  Computed using the original bytes
        /// that were used to produce this <see cref="SourceText"/> (if any of the <c>From</c> methods were used that
        /// take a <c>byte[]</c> or <see cref="Stream"/>).  Otherwise, computed by writing this <see cref="SourceText"/>
        /// back to a <see cref="Stream"/> (using the provided <see cref="Encoding"/>), and computing the hash off of
        /// that.
        /// </summary>
        /// <remarks>
        /// Two different <see cref="SourceText"/> instances with the same content (see <see cref="ContentEquals"/>) may
        /// have different results for this method.  This is because different originating bytes may end up with the
        /// same final content.  For example, a utf8 stream with a byte-order-mark will produce the same contents as a
        /// utf8 stream without one.  However, these preamble bytes will be part of the checksum, leading to different
        /// results.
        /// <para/>
        /// Similarly, two different <see cref="SourceText"/> instances with <em>different</em> contents can have the
        /// same checksum in <em>normal</em> scenarios.  This is because the use of the <see cref="Encoding"/> can lead
        /// to different characters being mapped to the same sequence of <em>encoded</em> bytes.
        /// <para/>
        /// As such, this function should only be used by clients who need to know the exact SHA hash from the original
        /// content bytes, and for no other purposes. 
        /// </remarks>
        public ImmutableArray<byte> GetChecksum()
        {
            if (_lazyChecksum.IsDefault)
            {
                using (var stream = new SourceTextStream(this, useDefaultEncodingIfNull: true))
                {
                    ImmutableInterlocked.InterlockedInitialize(ref _lazyChecksum, CalculateChecksum(stream, _checksumAlgorithm));
                }
            }

            return _lazyChecksum;
        }

        /// <summary>
        /// Produces a hash of this <see cref="SourceText"/> based solely on the contents it contains.  Two different
        /// <see cref="SourceText"/> instances that are <see cref="ContentEquals"/> will have the same content hash. Two
        /// instances of <see cref="SourceText"/> with different content are virtually certain to not have the same
        /// hash.  This hash can be used for fingerprinting of text instances, but does not provide cryptographic
        /// guarantees.
        /// </summary>
        /// <remarks>
        /// This hash is safe to use across platforms and across processes, as long as the same version of Roslyn is
        /// used in all those locations.  As such, it is safe to use as a fast proxy for comparing text instances in
        /// different memory spaces.  Different versions of Roslyn may produce different content hashes.
        /// </remarks>
        public ImmutableArray<byte> GetContentHash()
        {
            if (_lazyContentHash.IsDefault)
            {
                ImmutableInterlocked.InterlockedInitialize(ref _lazyContentHash, computeContentHash());
            }

            return _lazyContentHash;

            ImmutableArray<byte> computeContentHash()
            {
                var hash = s_contentHashPool.Allocate();
                var charBuffer = s_charArrayPool.Allocate();
                Debug.Assert(charBuffer.Length == CharBufferSize);
                try
                {
                    // Grab chunks of this SourceText, copying into 'charBuffer'.  Then reinterpret that buffer as a
                    // Span<byte> and append into the running hash.
                    for (int index = 0, length = this.Length; index < length; index += CharBufferSize)
                    {
                        var charsToCopy = Math.Min(CharBufferSize, length - index);
                        this.CopyTo(
                            sourceIndex: index, destination: charBuffer,
                            destinationIndex: 0, count: charsToCopy);

                        var charSpan = charBuffer.AsSpan(0, charsToCopy);

                        // Ensure everything is always little endian, so we get the same results across all platforms.
                        // This will be entirely elided by the jit on a little endian machine.
                        if (!BitConverter.IsLittleEndian)
                        {
                            var shortSpan = MemoryMarshal.Cast<char, short>(charSpan);

                            ReverseEndianness(shortSpan);
                        }

                        hash.Append(MemoryMarshal.AsBytes(charSpan));
                    }

                    return ImmutableCollectionsMarshal.AsImmutableArray(hash.GetHashAndReset());
                }
                finally
                {
                    s_charArrayPool.Free(charBuffer);

                    // Technically not needed.  But adding this reset out of an abundance of paranoia.
                    hash.Reset();
                    s_contentHashPool.Free(hash);
                }
            }
        }

        internal static void ReverseEndianness(Span<short> shortSpan)
        {
#if NET8_0_OR_GREATER
            // Defer to the platform to do the reversal.  It ships with a vectorized
            // implementation for this on .NET 8 and above.
            BinaryPrimitives.ReverseEndianness(source: shortSpan, destination: shortSpan);
#else
            // Otherwise, fallback to the simple approach of reversing each pair of bytes.
            for (var i = 0; i < shortSpan.Length; i++)
                shortSpan[i] = BinaryPrimitives.ReverseEndianness(shortSpan[i]);
#endif
        }

        internal static ImmutableArray<byte> CalculateChecksum(byte[] buffer, int offset, int count, SourceHashAlgorithm algorithmId)
        {
            using (var algorithm = CryptographicHashProvider.TryGetAlgorithm(algorithmId))
            {
                RoslynDebug.Assert(algorithm != null);
                return ImmutableArray.Create(algorithm.ComputeHash(buffer, offset, count));
            }
        }

        internal static ImmutableArray<byte> CalculateChecksum(Stream stream, SourceHashAlgorithm algorithmId)
        {
            using (var algorithm = CryptographicHashProvider.TryGetAlgorithm(algorithmId))
            {
                RoslynDebug.Assert(algorithm != null);
                if (stream.CanSeek)
                {
                    stream.Seek(0, SeekOrigin.Begin);
                }
                return ImmutableArray.Create(algorithm.ComputeHash(stream));
            }
        }

        /// <summary>
        /// Provides a string representation of the SourceText.
        /// </summary>
        public override string ToString()
        {
            return ToString(new TextSpan(0, this.Length));
        }

        /// <summary>
        /// Gets a string containing the characters in specified span.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">When given span is outside of the text range.</exception>
        public virtual string ToString(TextSpan span)
        {
            CheckSubSpan(span);

            // default implementation constructs text using CopyTo
            var tempBuffer = s_charArrayPool.Allocate();
            string result;

            int position = Math.Max(Math.Min(span.Start, this.Length), 0);
            int length = Math.Min(span.End, this.Length) - position;

#if NET
            result = string.Create(length, (this, position, length, tempBuffer), static (buffer, arg) =>
            {
                var (sourceText, position, length, tempBuffer) = arg;

                while (buffer.Length > 0)
                {
                    int copyLength = Math.Min(tempBuffer.Length, length);
                    sourceText.CopyTo(position, tempBuffer, 0, copyLength);
                    tempBuffer.AsSpan(0, copyLength).CopyTo(buffer);

                    buffer = buffer[copyLength..];
                    length -= copyLength;
                    position += copyLength;
                }
            });
#else
            var builder = PooledStringBuilder.GetInstance();
            builder.Builder.EnsureCapacity(length);

            while (position < this.Length && length > 0)
            {
                int copyLength = Math.Min(tempBuffer.Length, length);
                this.CopyTo(position, tempBuffer, 0, copyLength);
                builder.Builder.Append(tempBuffer, 0, copyLength);
                length -= copyLength;
                position += copyLength;
            }

            result = builder.ToStringAndFree();
#endif

            s_charArrayPool.Free(tempBuffer);
            return result;
        }

        #region Changes

        /// <summary>
        /// Constructs a new SourceText from this text with the specified changes.
        /// </summary>
        public virtual SourceText WithChanges(IEnumerable<TextChange> changes)
        {
            if (changes == null)
            {
                throw new ArgumentNullException(nameof(changes));
            }

            if (!changes.Any())
            {
                return this;
            }

            var segments = ArrayBuilder<SourceText>.GetInstance();
            var changeRanges = ArrayBuilder<TextChangeRange>.GetInstance();
            try
            {
                int position = 0;

                foreach (var change in changes)
                {
                    if (change.Span.End > this.Length)
                        throw new ArgumentException(CodeAnalysisResources.ChangesMustBeWithinBoundsOfSourceText, nameof(changes));

                    // there can be no overlapping changes
                    if (change.Span.Start < position)
                    {
                        // Handle the case of unordered changes by sorting the input and retrying. This is inefficient, but
                        // downstream consumers have been known to hit this case in the past and we want to avoid crashes.
                        // https://github.com/dotnet/roslyn/pull/26339
                        if (change.Span.End <= changeRanges.Last().Span.Start)
                        {
                            changes = (from c in changes
                                       where !c.Span.IsEmpty || c.NewText?.Length > 0
                                       orderby c.Span
                                       select c).ToList();
                            return WithChanges(changes);
                        }

                        throw new ArgumentException(CodeAnalysisResources.ChangesMustNotOverlap, nameof(changes));
                    }

                    var newTextLength = change.NewText?.Length ?? 0;

                    // ignore changes that don't change anything
                    if (change.Span.Length == 0 && newTextLength == 0)
                        continue;

                    // if we've skipped a range, add
                    if (change.Span.Start > position)
                    {
                        var subText = this.GetSubText(new TextSpan(position, change.Span.Start - position));
                        CompositeText.AddSegments(segments, subText);
                    }

                    if (newTextLength > 0)
                    {
                        var segment = SourceText.From(change.NewText!, this.Encoding, this.ChecksumAlgorithm);
                        CompositeText.AddSegments(segments, segment);
                    }

                    position = change.Span.End;

                    changeRanges.Add(new TextChangeRange(change.Span, newTextLength));
                }

                // no changes actually happened?
                if (position == 0 && segments.Count == 0)
                {
                    return this;
                }

                if (position < this.Length)
                {
                    var subText = this.GetSubText(new TextSpan(position, this.Length - position));
                    CompositeText.AddSegments(segments, subText);
                }

                var newText = CompositeText.ToSourceText(segments, this, adjustSegments: true);
                if (newText != this)
                {
                    return new ChangedText(this, newText, changeRanges.ToImmutable());
                }
                else
                {
                    return this;
                }
            }
            finally
            {
                segments.Free();
                changeRanges.Free();
            }
        }

        /// <summary>
        /// Constructs a new SourceText from this text with the specified changes.
        /// </summary>        
        /// <remarks>
        /// Changes do not have to be in sorted order.  However, <see cref="WithChanges(IEnumerable{TextChange})"/> will
        /// perform better if they are.
        /// </remarks>
        /// <exception cref="ArgumentException">If any changes are not in bounds of this <see cref="SourceText"/>.</exception>
        /// <exception cref="ArgumentException">If any changes overlap other changes.</exception>        
        public SourceText WithChanges(params TextChange[] changes)
        {
            return this.WithChanges((IEnumerable<TextChange>)changes);
        }

        /// <summary>
        /// Returns a new SourceText with the specified span of characters replaced by the new text.
        /// </summary>
        public SourceText Replace(TextSpan span, string newText)
        {
            return this.WithChanges(new TextChange(span, newText));
        }

        /// <summary>
        /// Returns a new SourceText with the specified range of characters replaced by the new text.
        /// </summary>
        public SourceText Replace(int start, int length, string newText)
        {
            return this.Replace(new TextSpan(start, length), newText);
        }

        /// <summary>
        /// Gets the set of <see cref="TextChangeRange"/> that describe how the text changed
        /// between this text an older version. This may be multiple detailed changes
        /// or a single change encompassing the entire text.
        /// </summary>
        public virtual IReadOnlyList<TextChangeRange> GetChangeRanges(SourceText oldText)
        {
            if (oldText == null)
            {
                throw new ArgumentNullException(nameof(oldText));
            }

            if (oldText == this)
            {
                return TextChangeRange.NoChanges;
            }
            else
            {
                return ImmutableArray.Create(new TextChangeRange(new TextSpan(0, oldText.Length), this.Length));
            }
        }

        /// <summary>
        /// Gets the set of <see cref="TextChange"/> that describe how the text changed
        /// between this text and an older version. This may be multiple detailed changes 
        /// or a single change encompassing the entire text.
        /// </summary>
        public virtual IReadOnlyList<TextChange> GetTextChanges(SourceText oldText)
        {
            int newPosDelta = 0;

            var ranges = this.GetChangeRanges(oldText).ToList();
            var textChanges = new List<TextChange>(ranges.Count);

            foreach (var range in ranges)
            {
                var newPos = range.Span.Start + newPosDelta;

                // determine where in the newText this text exists
                string newt;
                if (range.NewLength > 0)
                {
                    var span = new TextSpan(newPos, range.NewLength);
                    newt = this.ToString(span);
                }
                else
                {
                    newt = string.Empty;
                }

                textChanges.Add(new TextChange(range.Span, newt));

                newPosDelta += range.NewLength - range.Span.Length;
            }

            return textChanges.ToImmutableArrayOrEmpty();
        }

        #endregion

        #region Lines

        /// <summary>
        /// The collection of individual text lines.
        /// </summary>
        public TextLineCollection Lines
        {
            get
            {
                var info = _lazyLineInfo;
                return info ?? Interlocked.CompareExchange(ref _lazyLineInfo, info = GetLinesCore(), null) ?? info;
            }
        }

        internal bool TryGetLines([NotNullWhen(returnValue: true)] out TextLineCollection? lines)
        {
            lines = _lazyLineInfo;
            return lines != null;
        }

        /// <summary>
        /// Called from <see cref="Lines"/> to initialize the <see cref="TextLineCollection"/>. Thereafter,
        /// the collection is cached.
        /// </summary>
        /// <returns>A new <see cref="TextLineCollection"/> representing the individual text lines.</returns>
        protected virtual TextLineCollection GetLinesCore()
        {
            return new LineInfo(this, ParseLineStarts());
        }

        internal sealed class LineInfo : TextLineCollection
        {
            private readonly SourceText _text;
            private readonly SegmentedList<int> _lineStarts;
            private int _lastLineNumber;

            public LineInfo(SourceText text, SegmentedList<int> lineStarts)
            {
                _text = text;
                _lineStarts = lineStarts;
            }

            public override int Count => _lineStarts.Count;

            public override TextLine this[int index]
            {
                get
                {
                    if (index < 0 || index >= _lineStarts.Count)
                    {
                        throw new ArgumentOutOfRangeException(nameof(index));
                    }

                    int start = _lineStarts[index];
                    if (index == _lineStarts.Count - 1)
                    {
                        return TextLine.FromSpanUnsafe(_text, TextSpan.FromBounds(start, _text.Length));
                    }
                    else
                    {
                        int end = _lineStarts[index + 1];
                        return TextLine.FromSpanUnsafe(_text, TextSpan.FromBounds(start, end));
                    }
                }
            }

            public override int IndexOf(int position)
            {
                if (position < 0 || position > _text.Length)
                {
                    throw new ArgumentOutOfRangeException(nameof(position));
                }

                int lineNumber;

                // it is common to ask about position on the same line 
                // as before or on the next couple lines
                var lastLineNumber = _lastLineNumber;
                if (position >= _lineStarts[lastLineNumber])
                {
                    var limit = Math.Min(_lineStarts.Count, lastLineNumber + 4);
                    for (int i = lastLineNumber; i < limit; i++)
                    {
                        if (position < _lineStarts[i])
                        {
                            lineNumber = i - 1;
                            _lastLineNumber = lineNumber;
                            return lineNumber;
                        }
                    }
                }

                // Binary search to find the right line
                // if no lines start exactly at position, round to the left
                // EoF position will map to the last line.
                lineNumber = _lineStarts.BinarySearch(position);
                if (lineNumber < 0)
                {
                    lineNumber = (~lineNumber) - 1;
                }

                _lastLineNumber = lineNumber;
                return lineNumber;
            }

            public override TextLine GetLineFromPosition(int position)
            {
                return this[IndexOf(position)];
            }
        }

        private void EnumerateChars(Action<int, char[], int> action)
        {
            var position = 0;
            var buffer = s_charArrayPool.Allocate();

            var length = this.Length;
            while (position < length)
            {
                var contentLength = Math.Min(length - position, buffer.Length);
                this.CopyTo(position, buffer, 0, contentLength);
                action(position, buffer, contentLength);
                position += contentLength;
            }

            // once more with zero length to signal the end
            action(position, buffer, 0);

            s_charArrayPool.Free(buffer);
        }

        private SegmentedList<int> ParseLineStarts()
        {
            // Corner case check
            if (0 == this.Length)
            {
                return [0];
            }

            // Initial line capacity estimated at 64 chars / line. This value was obtained by
            // looking at ratios in large files in the roslyn repo.
            var lineStarts = new SegmentedList<int>(Length / 64)
            {
                0 // there is always the first line
            };

            var lastWasCR = false;

            // The following loop goes through every character in the text. It is highly
            // performance critical, and thus inlines knowledge about common line breaks
            // and non-line breaks.
            EnumerateChars((int position, char[] buffer, int length) =>
            {
                var index = 0;
                if (lastWasCR)
                {
                    if (length > 0 && buffer[0] == '\n')
                    {
                        index++;
                    }

                    lineStarts.Add(position + index);
                    lastWasCR = false;
                }

                while (index < length)
                {
                    char c = buffer[index];
                    index++;

                    // Common case - ASCII & not a line break
                    // if (c > '\r' && c <= 127)
                    // if (c >= ('\r'+1) && c <= 127)
                    const uint bias = '\r' + 1;
                    if (unchecked(c - bias) <= (127 - bias))
                    {
                        continue;
                    }

                    // Assumes that the only 2-char line break sequence is CR+LF
                    if (c == '\r')
                    {
                        if (index < length && buffer[index] == '\n')
                        {
                            index++;
                        }
                        else if (index >= length)
                        {
                            lastWasCR = true;
                            continue;
                        }
                    }
                    else if (!TextUtilities.IsAnyLineBreakCharacter(c))
                    {
                        continue;
                    }

                    // next line starts at index
                    lineStarts.Add(position + index);
                }
            });

            return lineStarts;
        }
        #endregion

        /// <summary>
        /// Compares the content with content of another <see cref="SourceText"/>.
        /// </summary>
        public bool ContentEquals(SourceText other)
        {
            if (ReferenceEquals(this, other))
            {
                return true;
            }

            // Content hashing provides strong enough guarantees (see
            // https://github.com/Cyan4973/xxHash/wiki/Collision-ratio-comparison#testing-128-bit-hashes-) to be certain
            // about content equality based solely on hash equality.
            //
            // DO NOT examine '_lazyChecksum' in this method.  Checksums (as opposed to hashes) do not provide the same
            // guarantee.  Indeed, due to the user of lossy encodings, it's easily possible to have source texts with
            // different contents that produce the same checksums.  As an example, the Encoding.ASCII encoding maps many
            // System.Char values down to the ascii `?` character, leading to easy collisions at the checksum level.

            var leftContentHash = _lazyContentHash;
            var rightContentHash = other._lazyContentHash;
            if (!leftContentHash.IsDefault && !rightContentHash.IsDefault)
                return leftContentHash.SequenceEqual(rightContentHash);

            return ContentEqualsImpl(other);
        }

        /// <summary>
        /// Implements equality comparison of the content of two different instances of <see cref="SourceText"/>.
        /// </summary>
        protected virtual bool ContentEqualsImpl(SourceText other)
        {
            if (other == null)
            {
                return false;
            }

            if (ReferenceEquals(this, other))
            {
                return true;
            }

            if (this.Length != other.Length)
            {
                return false;
            }

            var buffer1 = s_charArrayPool.Allocate();
            var buffer2 = s_charArrayPool.Allocate();
            Debug.Assert(buffer1.Length == buffer2.Length);
            Debug.Assert(buffer1.Length == CharBufferSize);

            try
            {
                for (int position = 0, length = this.Length; position < length; position += CharBufferSize)
                {
                    var count = Math.Min(this.Length - position, CharBufferSize);
                    this.CopyTo(sourceIndex: position, buffer1, destinationIndex: 0, count);
                    other.CopyTo(sourceIndex: position, buffer2, destinationIndex: 0, count);

                    if (!buffer1.AsSpan(0, count).SequenceEqual(buffer2.AsSpan(0, count)))
                        return false;
                }

                return true;
            }
            finally
            {
                s_charArrayPool.Free(buffer2);
                s_charArrayPool.Free(buffer1);
            }
        }

        /// <summary>
        /// Detect an encoding by looking for byte order marks.
        /// </summary>
        /// <param name="source">A buffer containing the encoded text.</param>
        /// <param name="length">The length of valid data in the buffer.</param>
        /// <param name="preambleLength">The length of any detected byte order marks.</param>
        /// <returns>The detected encoding or null if no recognized byte order mark was present.</returns>
        internal static Encoding? TryReadByteOrderMark(byte[] source, int length, out int preambleLength)
        {
            RoslynDebug.Assert(source != null);
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

        private class StaticContainer : SourceTextContainer
        {
            private readonly SourceText _text;

            public StaticContainer(SourceText text)
            {
                _text = text;
            }

            public override SourceText CurrentText => _text;

            public override event EventHandler<TextChangeEventArgs> TextChanged
            {
                add
                {
                    // do nothing
                }

                remove
                {
                    // do nothing
                }
            }
        }

        /// <summary>
        /// Get maximum char count needed to decode the entire stream.
        /// </summary>
        /// <exception cref="IOException">Stream is so big that max char count can't fit in <see cref="int"/>.</exception> 
        internal static int GetMaxCharCountOrThrowIfHuge(Encoding encoding, Stream stream)
        {
            Debug.Assert(stream.CanSeek);

            if (encoding.TryGetMaxCharCount(stream.Length, out int maxCharCount))
            {
                return maxCharCount;
            }

            throw new IOException(CodeAnalysisResources.StreamIsTooLong);
        }

        internal SourceText WithChecksumAlgorithm(SourceHashAlgorithm checksumAlgorithm)
        {
            if (checksumAlgorithm == SourceHashAlgorithm.None || checksumAlgorithm == ChecksumAlgorithm)
                return this;

            return new SourceTextWithAlgorithm(this, checksumAlgorithm);
        }

        private sealed class SourceTextWithAlgorithm : SourceText
        {
            private readonly SourceText _underlying;

            public SourceTextWithAlgorithm(SourceText underlying, SourceHashAlgorithm checksumAlgorithm) : base(checksumAlgorithm: checksumAlgorithm)
            {
                Debug.Assert(checksumAlgorithm != SourceHashAlgorithm.None);
                Debug.Assert(checksumAlgorithm != underlying.ChecksumAlgorithm);
                _underlying = underlying;
            }

            public override char this[int position] => _underlying[position];

            public override Encoding? Encoding => _underlying.Encoding;

            public override int Length => _underlying.Length;

            public override void CopyTo(int sourceIndex, char[] destination, int destinationIndex, int count)
            {
                _underlying.CopyTo(sourceIndex, destination, destinationIndex, count);
            }
        }
    }
}
