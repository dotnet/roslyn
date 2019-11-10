// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Reflection.Metadata;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Debugging;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// Represents text to be embedded in a PDB.
    /// </summary>
    public sealed class EmbeddedText
    {
        /// <summary>
        /// The maximum number of bytes in to write out uncompressed.
        ///
        /// This prevents wasting resources on compressing tiny files with little to negative gain
        /// in PDB file size.
        ///
        /// Chosen as the point at which we start to see > 10% blob size reduction using all
        /// current source files in corefx and roslyn as sample data. 
        /// </summary>
        internal const int CompressionThreshold = 200;

        /// <summary>
        /// The path to the file to embed.
        /// </summary>
        /// <remarks>See remarks of <see cref="SyntaxTree.FilePath"/></remarks>
        /// <remarks>Empty file paths are disallowed, as the debugger finds source by looking up files by their name (and then verifying their signature)</remarks>
        public string FilePath { get; }

        /// <summary>
        /// Hash algorithm to use to calculate checksum of the text that's saved to PDB.
        /// </summary>
        public SourceHashAlgorithm ChecksumAlgorithm { get; }

        /// <summary>
        /// The <see cref="ChecksumAlgorithm"/> hash of the uncompressed bytes
        /// that's saved to the PDB.
        /// </summary>
        public ImmutableArray<byte> Checksum { get; }

        private EmbeddedText(string filePath, ImmutableArray<byte> checksum, SourceHashAlgorithm checksumAlgorithm, ImmutableArray<byte> blob)
        {
            Debug.Assert(filePath?.Length > 0);
            Debug.Assert(SourceHashAlgorithms.IsSupportedAlgorithm(checksumAlgorithm));
            Debug.Assert(!blob.IsDefault && blob.Length >= sizeof(int));

            FilePath = filePath;
            Checksum = checksum;
            ChecksumAlgorithm = checksumAlgorithm;
            Blob = blob;
        }

        /// <summary>
        /// The content that will be written to the PDB.
        /// </summary>
        /// <remarks>
        /// Internal since this is an implementation detail. The only public
        /// contract is that you can pass EmbeddedText instances to Emit.
        /// It just so happened that doing this up-front was most practical
        /// and efficient, but we don't want to be tied to it.
        /// 
        /// For efficiency, the format of this blob is exactly as it is written
        /// to the PDB,which prevents extra copies being made during emit.
        ///
        /// The first 4 bytes (little endian int32) indicate the format:
        ///
        ///            0: data that follows is uncompressed
        ///     Positive: data that follows is deflate compressed and value is original, uncompressed size
        ///     Negative: invalid at this time, but reserved to mark a different format in the future.
        /// </remarks>
        internal ImmutableArray<byte> Blob { get; }

        /// <summary>
        /// Constructs a <see cref="EmbeddedText"/> for embedding the given <see cref="SourceText"/>.
        /// </summary>
        /// <param name="filePath">The file path (pre-normalization) to use in the PDB.</param>
        /// <param name="text">The source text to embed.</param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="filePath"/> is null.
        /// <paramref name="text"/> is null.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// <paramref name="filePath"/> empty.
        /// <paramref name="text"/> cannot be embedded (see <see cref="SourceText.CanBeEmbedded"/>).
        /// </exception>
        public static EmbeddedText FromSource(string filePath, SourceText text)
        {
            ValidateFilePath(filePath);

            if (text == null)
            {
                throw new ArgumentNullException(nameof(text));
            }

            if (!text.CanBeEmbedded)
            {
                throw new ArgumentException(CodeAnalysisResources.SourceTextCannotBeEmbedded, nameof(text));
            }

            if (!text.PrecomputedEmbeddedTextBlob.IsDefault)
            {
                return new EmbeddedText(filePath, text.GetChecksum(), text.ChecksumAlgorithm, text.PrecomputedEmbeddedTextBlob);
            }

            return new EmbeddedText(filePath, text.GetChecksum(), text.ChecksumAlgorithm, CreateBlob(text));
        }

        /// <summary>
        /// Constructs an <see cref="EmbeddedText"/> from stream content.
        /// </summary>
        /// <param name="filePath">The file path (pre-normalization) to use in the PDB.</param>
        /// <param name="stream">The stream.</param>
        /// <param name="checksumAlgorithm">Hash algorithm to use to calculate checksum of the text that's saved to PDB.</param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="filePath" /> is null.
        /// <paramref name="stream"/> is null.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// <paramref name="filePath" /> is empty.
        /// <paramref name="stream"/> doesn't support reading or seeking.
        /// <paramref name="checksumAlgorithm"/> is not supported.
        /// </exception>
        /// <exception cref="IOException">An I/O error occurs.</exception>
        /// <remarks>Reads from the beginning of the stream. Leaves the stream open.</remarks>
        public static EmbeddedText FromStream(string filePath, Stream stream, SourceHashAlgorithm checksumAlgorithm = SourceHashAlgorithm.Sha1)
        {
            ValidateFilePath(filePath);

            if (stream == null)
            {
                throw new ArgumentNullException(nameof(stream));
            }

            if (!stream.CanRead || !stream.CanSeek)
            {
                throw new ArgumentException(CodeAnalysisResources.StreamMustSupportReadAndSeek, nameof(stream));
            }

            SourceText.ValidateChecksumAlgorithm(checksumAlgorithm);

            return new EmbeddedText(
                filePath,
                SourceText.CalculateChecksum(stream, checksumAlgorithm),
                checksumAlgorithm,
                CreateBlob(stream));
        }

        /// <summary>
        /// Constructs an <see cref="EmbeddedText"/> from bytes.
        /// </summary>
        /// <param name="filePath">The file path (pre-normalization) to use in the PDB.</param>
        /// <param name="bytes">The bytes.</param>
        /// <param name="checksumAlgorithm">Hash algorithm to use to calculate checksum of the text that's saved to PDB.</param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="bytes"/> is default-initialized.
        /// <paramref name="filePath" /> is null.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// <paramref name="filePath" /> is empty.
        /// <paramref name="checksumAlgorithm"/> is not supported.
        /// </exception>
        /// <exception cref="IOException">An I/O error occurs.</exception>
        /// <remarks>Reads from the beginning of the stream. Leaves the stream open.</remarks>
        public static EmbeddedText FromBytes(string filePath, ArraySegment<byte> bytes, SourceHashAlgorithm checksumAlgorithm = SourceHashAlgorithm.Sha1)
        {
            ValidateFilePath(filePath);

            if (bytes.Array == null)
            {
                throw new ArgumentNullException(nameof(bytes));
            }

            SourceText.ValidateChecksumAlgorithm(checksumAlgorithm);

            return new EmbeddedText(
                filePath,
                SourceText.CalculateChecksum(bytes.Array, bytes.Offset, bytes.Count, checksumAlgorithm),
                checksumAlgorithm,
                CreateBlob(bytes));
        }

        /// <exception cref="ArgumentNullException"><paramref name="filePath"/> is null.</exception>
        /// <exception cref="ArgumentException"><paramref name="filePath"/> is empty.</exception>
        private static void ValidateFilePath(string filePath)
        {
            if (filePath == null)
            {
                throw new ArgumentNullException(nameof(filePath));
            }

            if (filePath.Length == 0)
            {
                throw new ArgumentException(CodeAnalysisResources.ArgumentCannotBeEmpty, nameof(filePath));
            }
        }

        /// <summary>
        /// Creates the blob to be saved to the PDB.
        /// </summary>
        internal static ImmutableArray<byte> CreateBlob(Stream stream)
        {
            Debug.Assert(stream != null);
            Debug.Assert(stream.CanRead);
            Debug.Assert(stream.CanSeek);

            long longLength = stream.Length;
            Debug.Assert(longLength >= 0);

            if (longLength > int.MaxValue)
            {
                throw new IOException(CodeAnalysisResources.StreamIsTooLong);
            }

            stream.Seek(0, SeekOrigin.Begin);
            int length = (int)longLength;

            if (length < CompressionThreshold)
            {
                using (var builder = Cci.PooledBlobBuilder.GetInstance())
                {
                    builder.WriteInt32(0);
                    int bytesWritten = builder.TryWriteBytes(stream, length);

                    if (length != bytesWritten)
                    {
                        throw new EndOfStreamException();
                    }

                    return builder.ToImmutableArray();
                }
            }
            else
            {
                using (var builder = BlobBuildingStream.GetInstance())
                {
                    builder.WriteInt32(length);

                    using (var deflater = new CountingDeflateStream(builder, CompressionLevel.Optimal, leaveOpen: true))
                    {
                        stream.CopyTo(deflater);

                        if (length != deflater.BytesWritten)
                        {
                            throw new EndOfStreamException();
                        }
                    }

                    return builder.ToImmutableArray();
                }
            }
        }

        internal static ImmutableArray<byte> CreateBlob(ArraySegment<byte> bytes)
        {
            Debug.Assert(bytes.Array != null);

            if (bytes.Count < CompressionThreshold)
            {
                using (var builder = Cci.PooledBlobBuilder.GetInstance())
                {
                    builder.WriteInt32(0);
                    builder.WriteBytes(bytes.Array, bytes.Offset, bytes.Count);
                    return builder.ToImmutableArray();
                }
            }
            else
            {
                using (var builder = BlobBuildingStream.GetInstance())
                {
                    builder.WriteInt32(bytes.Count);

                    using (var deflater = new CountingDeflateStream(builder, CompressionLevel.Optimal, leaveOpen: true))
                    {
                        deflater.Write(bytes.Array, bytes.Offset, bytes.Count);
                    }

                    return builder.ToImmutableArray();
                }
            }
        }

        private static ImmutableArray<byte> CreateBlob(SourceText text)
        {
            Debug.Assert(text != null);
            Debug.Assert(text.CanBeEmbedded);
            Debug.Assert(text.Encoding != null);
            Debug.Assert(text.PrecomputedEmbeddedTextBlob.IsDefault);

            int maxByteCount;
            try
            {
                maxByteCount = text.Encoding.GetMaxByteCount(text.Length);
            }
            catch (ArgumentOutOfRangeException)
            {
                // Encoding does not provide a way to predict that max byte count would not
                // fit in Int32 and we must therefore catch ArgumentOutOfRange to handle that
                // case.
                maxByteCount = int.MaxValue;
            }

            using (var builder = BlobBuildingStream.GetInstance())
            {
                if (maxByteCount < CompressionThreshold)
                {
                    builder.WriteInt32(0);

                    using (var writer = new StreamWriter(builder, text.Encoding, bufferSize: Math.Max(1, text.Length), leaveOpen: true))
                    {
                        text.Write(writer);
                    }
                }
                else
                {
                    Blob reserved = builder.ReserveBytes(4);

                    using (var deflater = new CountingDeflateStream(builder, CompressionLevel.Optimal, leaveOpen: true))
                    {
                        using (var writer = new StreamWriter(deflater, text.Encoding, bufferSize: 1024, leaveOpen: true))
                        {
                            text.Write(writer);
                        }

                        new BlobWriter(reserved).WriteInt32(deflater.BytesWritten);
                    }
                }

                return builder.ToImmutableArray();
            }
        }

        internal Cci.DebugSourceInfo GetDebugSourceInfo()
        {
            return new Cci.DebugSourceInfo(Checksum, ChecksumAlgorithm, Blob);
        }

        private sealed class CountingDeflateStream : DeflateStream
        {
            public CountingDeflateStream(Stream stream, CompressionLevel compressionLevel, bool leaveOpen)
                : base(stream, compressionLevel, leaveOpen)
            {
            }

            public int BytesWritten { get; private set; }

            public override void Write(byte[] array, int offset, int count)
            {
                base.Write(array, offset, count);

                // checked arithmetic is release-enabled quasi-assert. We start with at most 
                // int.MaxValue chars so compression or encoding would have to be abysmal for
                // this to overflow. We'd probably be lucky to even get this far but if we do
                // we should fail fast.
                checked { BytesWritten += count; }
            }

            public override void WriteByte(byte value)
            {
                base.WriteByte(value);

                // same rationale for checked arithmetic as above.
                checked { BytesWritten++; };
            }

            public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
            {
                throw ExceptionUtilities.Unreachable;
            }
        }
    }
}
