// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Buffers;
using System.Collections.Immutable;
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
        public const string ServiceName = "SolutionAssetProvider";

        internal static ServiceDescriptor ServiceDescriptor { get; } = ServiceDescriptor.CreateInProcServiceDescriptor(ServiceDescriptors.ComponentName, ServiceName, suffix: "", ServiceDescriptors.GetFeatureDisplayName);

        private readonly SolutionServices _services = services;

        public ValueTask WriteAssetsAsync(
            PipeWriter pipeWriter,
            Checksum solutionChecksum,
            AssetHint assetHint,
            ImmutableArray<Checksum> checksums,
            CancellationToken cancellationToken)
        {
            // Suppress ExecutionContext flow for asynchronous operations operate on the pipe. In addition to avoiding
            // ExecutionContext allocations, this clears the LogicalCallContext and avoids the need to clone data set by
            // CallContext.LogicalSetData at each yielding await in the task tree.
            //
            // ⚠ DO NOT AWAIT INSIDE THE USING. The Dispose method that restores ExecutionContext flow must run on the
            // same thread where SuppressFlow was originally run.
            using var _ = FlowControlHelper.TrySuppressFlow();
            return WriteAssetsSuppressedFlowAsync(pipeWriter, solutionChecksum, assetHint, checksums, cancellationToken);

            async ValueTask WriteAssetsSuppressedFlowAsync(PipeWriter pipeWriter, Checksum solutionChecksum, AssetHint assetHint, ImmutableArray<Checksum> checksums, CancellationToken cancellationToken)
            {
                // The responsibility is on us (as per the requirements of RemoteCallback.InvokeAsync) to Complete the
                // pipewriter.  This will signal to streamjsonrpc that the writer passed into it is complete, which will
                // allow the calling side know to stop reading results.
                Exception? exception = null;
                try
                {
                    await WriteAssetsWorkerAsync(pipeWriter, solutionChecksum, assetHint, checksums, cancellationToken).ConfigureAwait(false);
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
            AssetHint assetHint,
            ImmutableArray<Checksum> checksums,
            CancellationToken cancellationToken)
        {
            var assetStorage = _services.GetRequiredService<ISolutionAssetStorageProvider>().AssetStorage;
            var serializer = _services.GetRequiredService<ISerializerService>();
            var scope = assetStorage.GetScope(solutionChecksum);

            using var _ = Creator.CreateResultMap(out var resultMap);

            await scope.AddAssetsAsync(assetHint, checksums, resultMap, cancellationToken).ConfigureAwait(false);

            cancellationToken.ThrowIfCancellationRequested();

            using var stream = new PipeWriterStream(pipeWriter);
            await RemoteHostAssetSerialization.WriteDataAsync(
                stream, resultMap, serializer, scope.ReplicationContext,
                solutionChecksum, checksums, cancellationToken).ConfigureAwait(false);
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
    }
}
