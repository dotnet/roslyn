// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Buffers;
using System.Buffers.Binary;
using System.IO;
using System.IO.Pipelines;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Serialization;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Remote
{
    /// <summary>
    /// Provides solution assets present locally (in the current process) to a remote process where the solution is being replicated to.
    /// </summary>
    internal sealed class SolutionAssetProvider(SolutionServices services) : ISolutionAssetProvider
    {
        private static readonly ObjectPool<Stream> s_streamPool = new(SerializableBytes.CreateWritableStream);

        public const string ServiceName = "SolutionAssetProvider";

        internal static ServiceDescriptor ServiceDescriptor { get; } = ServiceDescriptor.CreateInProcServiceDescriptor(ServiceDescriptors.ComponentName, ServiceName, suffix: "", ServiceDescriptors.GetFeatureDisplayName);

        private readonly SolutionServices _services = services;

        public ValueTask WriteAssetsAsync(
            PipeWriter pipeWriter,
            Checksum solutionChecksum,
            AssetPath assetPath,
            ReadOnlyMemory<Checksum> checksums,
            CancellationToken cancellationToken)
        {
            // Suppress ExecutionContext flow for asynchronous operations operate on the pipe. In addition to avoiding
            // ExecutionContext allocations, this clears the LogicalCallContext and avoids the need to clone data set by
            // CallContext.LogicalSetData at each yielding await in the task tree.
            //
            // ⚠ DO NOT AWAIT INSIDE THE USING. The Dispose method that restores ExecutionContext flow must run on the
            // same thread where SuppressFlow was originally run.
            using var _ = FlowControlHelper.TrySuppressFlow();
            return WriteAssetsSuppressedFlowAsync(pipeWriter, solutionChecksum, assetPath, checksums, cancellationToken);

            async ValueTask WriteAssetsSuppressedFlowAsync(PipeWriter pipeWriter, Checksum solutionChecksum, AssetPath assetPath, ReadOnlyMemory<Checksum> checksums, CancellationToken cancellationToken)
            {
                // The responsibility is on us (as per the requirements of RemoteCallback.InvokeAsync) to Complete the
                // pipewriter.  This will signal to streamjsonrpc that the writer passed into it is complete, which will
                // allow the calling side know to stop reading results.
                Exception? exception = null;
                try
                {
                    await WriteAssetsWorkerAsync(pipeWriter, solutionChecksum, assetPath, checksums, cancellationToken).ConfigureAwait(false);
                }
                catch (Exception ex) when ((exception = ex) == null)
                {
                    throw ExceptionUtilities.Unreachable();
                }
                finally
                {
                    await pipeWriter.CompleteAsync(exception).ConfigureAwait(false);
                }
            }
        }

        private async ValueTask WriteAssetsWorkerAsync(
            PipeWriter pipeWriter,
            Checksum solutionChecksum,
            AssetPath assetPath,
            ReadOnlyMemory<Checksum> checksums,
            CancellationToken cancellationToken)
        {
            var assetStorage = _services.GetRequiredService<ISolutionAssetStorageProvider>().AssetStorage;
            var serializer = _services.GetRequiredService<ISerializerService>();
            var scope = assetStorage.GetScope(solutionChecksum);

            var pipeWriterStream = pipeWriter.AsStream();

            var foundChecksumCount = 0;

            await scope.AddAssetsAsync(
                assetPath,
                checksums,
                WriteAssetToPipeAsync,
                cancellationToken).ConfigureAwait(false);

            Contract.ThrowIfTrue(foundChecksumCount != checksums.Length);

            return;

            async ValueTask WriteAssetToPipeAsync(Checksum checksum, object asset, CancellationToken cancellationToken)
            {
                Contract.ThrowIfNull(asset);
                foundChecksumCount++;

                using var pooledObject = s_streamPool.GetPooledObject();
                var tempStream = pooledObject.Object;
                tempStream.Position = 0;
                tempStream.SetLength(0);

                WriteAssetToTempStream(tempStream, checksum, asset);

                // Write the length of the asset to the pipe writer so the reader knows how much data to read.
                WriteLengthToPipeWriter(tempStream.Length);

                // Ensure we flush out the length so the reading side knows how much data to read.
                await pipeWriterStream.FlushAsync(cancellationToken).ConfigureAwait(false);

                // Now, asynchronously copy the temp buffer over to the writer stream.
                tempStream.Position = 0;
                await tempStream.CopyToAsync(pipeWriter, cancellationToken).ConfigureAwait(false);

                // We flush after each item as that forms a reasonably sized chunk of data to want to then send over
                // the pipe for the reader on the other side to read.  This allows the item-writing to remain
                // entirely synchronous without any blocking on async flushing, while also ensuring that we're not
                // buffering the entire stream of data into the pipe before it gets sent to the other side.
                await pipeWriterStream.FlushAsync(cancellationToken).ConfigureAwait(false);
            }

            void WriteAssetToTempStream(Stream tempStream, Checksum checksum, object asset)
            {
                // Write the asset to a temporary buffer so we can calculate its length.
                using var objectWriter = new ObjectWriter(tempStream, leaveOpen: true, cancellationToken);
                {
                    // Write the checksum for the asset we're writing out, so the other side knows what asset this is.
                    checksum.WriteTo(objectWriter);

                    // Write out the kind so the receiving end knows how to deserialize this asset.
                    var kind = asset.GetWellKnownSynchronizationKind();
                    objectWriter.WriteInt32((int)kind);

                    // Now serialize out the asset itself.
                    serializer.Serialize(asset, objectWriter, scope.ReplicationContext, cancellationToken);
                }
            }

            void WriteLengthToPipeWriter(long length)
            {
                Contract.ThrowIfTrue(length > int.MaxValue);

                var span = pipeWriter.GetSpan(sizeof(int));
                BinaryPrimitives.WriteInt32LittleEndian(span, (int)length);
                pipeWriter.Advance(span.Length);
            }
        }

        /// <summary>
        /// Simple port of
        /// https://github.com/AArnott/Nerdbank.Streams/blob/dafeb5846702bc29e261c9ddf60f42feae01654c/src/Nerdbank.Streams/BufferWriterStream.cs#L16.
        /// Wraps a <see cref="PipeWriter"/> in a <see cref="Stream"/> interface.  Preferred over <see
        /// cref="PipeWriter.AsStream(bool)"/> as that API produces a stream that will synchronously flush after
        /// <em>every</em> write.  That's undesirable as that will then block a thread pool thread on the actual
        /// asynchronous flush call to the underlying PipeWriter
        /// </summary>
        /// <remarks>
        /// Note: this stream does not have to <see cref="PipeWriter.Complete"/> the underlying <see cref="_writer"/> it
        /// is holding onto (including within <see cref="Flush"/>, <see cref="FlushAsync"/>, or <see cref="Dispose"/>).
        /// Responsibility for that is solely in the hands of <see cref="WriteAssetsAsync"/>.
        /// </remarks>
#if false
        private class PipeWriterStream : Stream, IDisposableObservable
        {
            private readonly PipeWriter _writer;

            public bool IsDisposed { get; private set; }

            public override bool CanRead => false;
            public override bool CanSeek => false;
            public override bool CanWrite => !this.IsDisposed;

            internal PipeWriterStream(PipeWriter writer)
            {
                _writer = writer;
            }

            protected override void Dispose(bool disposing)
            {
                this.IsDisposed = true;
                base.Dispose(disposing);

                // DO NOT CALL .Complete on the PipeWriter here (see remarks on type).
            }

            private Exception ThrowDisposedOr(Exception ex)
            {
                Verify.NotDisposed(this);
                throw ex;
            }

            /// <summary>
            /// Intentionally a no op. We know that we and <see cref="RemoteHostAssetSerialization.WriteDataAsync"/>
            /// will call <see cref="FlushAsync"/> at appropriate times to ensure data is being sent through the writer
            /// at a reasonable cadence (once per asset).
            /// </summary>
            public override void Flush()
            {
                Verify.NotDisposed(this);

                // DO NOT CALL .Complete on the PipeWriter here (see remarks on type).
            }

            public override async Task FlushAsync(CancellationToken cancellationToken)
            {
                await _writer.FlushAsync(cancellationToken).ConfigureAwait(false);

                // DO NOT CALL .Complete on the PipeWriter here (see remarks on type).
            }

            public override void Write(byte[] buffer, int offset, int count)
            {
                Requires.NotNull(buffer, nameof(buffer));
                Verify.NotDisposed(this);

#if NET
                _writer.Write(buffer.AsSpan(offset, count));
#else
                var span = _writer.GetSpan(count);
                buffer.AsSpan(offset, count).CopyTo(span);
                _writer.Advance(count);
#endif
            }

            public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
            {
                cancellationToken.ThrowIfCancellationRequested();
                this.Write(buffer, offset, count);
                return Task.CompletedTask;
            }

            public override void WriteByte(byte value)
            {
                Verify.NotDisposed(this);
                var span = _writer.GetSpan(1);
                span[0] = value;
                _writer.Advance(1);
            }

#if NET

            public override void Write(ReadOnlySpan<byte> buffer)
            {
                Verify.NotDisposed(this);
                _writer.Write(buffer);
            }

            public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken)
            {
                cancellationToken.ThrowIfCancellationRequested();
                this.Write(buffer.Span);
                return default;
            }

#endif

            #region read/seek api (not supported)

            public override long Length => throw this.ThrowDisposedOr(new NotSupportedException());
            public override long Position
            {
                get => throw this.ThrowDisposedOr(new NotSupportedException());
                set => this.ThrowDisposedOr(new NotSupportedException());
            }

            public override int Read(byte[] buffer, int offset, int count)
                => throw this.ThrowDisposedOr(new NotSupportedException());

            public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
                => throw this.ThrowDisposedOr(new NotSupportedException());

#if NET

            public override int Read(Span<byte> buffer)
                => throw this.ThrowDisposedOr(new NotSupportedException());

            public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken)
                => throw this.ThrowDisposedOr(new NotSupportedException());

#endif

            public override int ReadByte()
                => throw this.ThrowDisposedOr(new NotSupportedException());

            public override long Seek(long offset, SeekOrigin origin)
                => throw this.ThrowDisposedOr(new NotSupportedException());

            public override void SetLength(long value)
                => this.ThrowDisposedOr(new NotSupportedException());

            #endregion
        }
#endif
    }
}
