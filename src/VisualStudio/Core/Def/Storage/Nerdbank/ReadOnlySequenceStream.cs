// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Copied from https://raw.githubusercontent.com/AArnott/Nerdbank.Streams/2b142fa6a38b15e4b06ecc53bf073aa49fd1de34/src/Nerdbank.Streams/ReadOnlySequenceStream.cs
// Remove once we move to Nerdbank.Streams 2.7.62-alpha

namespace Nerdbank.Streams
{
    using System;
    using System.Buffers;
    using System.IO;
    using System.Runtime.InteropServices;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft;

    internal class ReadOnlySequenceStream : Stream, IDisposableObservable
    {
        private static readonly Task<int> TaskOfZero = Task.FromResult(0);

        private readonly Action<object?>? disposeAction;
        private readonly object? disposeActionArg;

        /// <summary>
        /// A reusable task if two consecutive reads return the same number of bytes.
        /// </summary>
        private Task<int>? lastReadTask;

        private readonly ReadOnlySequence<byte> readOnlySequence;

        private SequencePosition position;

        internal ReadOnlySequenceStream(ReadOnlySequence<byte> readOnlySequence, Action<object?>? disposeAction, object? disposeActionArg)
        {
            this.readOnlySequence = readOnlySequence;
            this.disposeAction = disposeAction;
            this.disposeActionArg = disposeActionArg;
            this.position = readOnlySequence.Start;
        }

        /// <inheritdoc/>
        public override bool CanRead => !this.IsDisposed;

        /// <inheritdoc/>
        public override bool CanSeek => !this.IsDisposed;

        /// <inheritdoc/>
        public override bool CanWrite => false;

        /// <inheritdoc/>
        public override long Length => this.ReturnOrThrowDisposed(this.readOnlySequence.Length);

        /// <inheritdoc/>
        public override long Position
        {
            get => this.readOnlySequence.Slice(0, this.position).Length;
            set
            {
                Requires.Range(value >= 0, nameof(value));
                this.position = this.readOnlySequence.GetPosition(value, this.readOnlySequence.Start);
            }
        }

        /// <inheritdoc/>
        public bool IsDisposed { get; private set; }

        /// <inheritdoc/>
        public override void Flush() => this.ThrowDisposedOr(new NotSupportedException());

        /// <inheritdoc/>
        public override Task FlushAsync(CancellationToken cancellationToken) => throw this.ThrowDisposedOr(new NotSupportedException());

        /// <inheritdoc/>
        public override int Read(byte[] buffer, int offset, int count)
        {
            var remaining = this.readOnlySequence.Slice(this.position);
            var toCopy = remaining.Slice(0, Math.Min(count, remaining.Length));
            this.position = toCopy.End;
            toCopy.CopyTo(buffer.AsSpan(offset, count));
            return (int)toCopy.Length;
        }

        /// <inheritdoc/>
        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var bytesRead = this.Read(buffer, offset, count);
            if (bytesRead == 0)
            {
                return TaskOfZero;
            }

            if (this.lastReadTask?.Result == bytesRead)
            {
                return this.lastReadTask;
            }
            else
            {
                return this.lastReadTask = Task.FromResult(bytesRead);
            }
        }

        /// <inheritdoc/>
        public override int ReadByte()
        {
            var remaining = this.readOnlySequence.Slice(this.position);
            if (remaining.Length > 0)
            {
                var result = remaining.First.Span[0];
                this.position = this.readOnlySequence.GetPosition(1, this.position);
                return result;
            }
            else
            {
                return -1;
            }
        }

        /// <inheritdoc/>
        public override long Seek(long offset, SeekOrigin origin)
        {
            Verify.NotDisposed(this);

            SequencePosition relativeTo;
            switch (origin)
            {
                case SeekOrigin.Begin:
                    relativeTo = this.readOnlySequence.Start;
                    break;
                case SeekOrigin.Current:
                    if (offset >= 0)
                    {
                        relativeTo = this.position;
                    }
                    else
                    {
                        relativeTo = this.readOnlySequence.Start;
                        offset += this.Position;
                    }

                    break;
                case SeekOrigin.End:
                    if (offset >= 0)
                    {
                        relativeTo = this.readOnlySequence.End;
                    }
                    else
                    {
                        relativeTo = this.readOnlySequence.Start;
                        offset += this.Position;
                    }

                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(origin));
            }

            this.position = this.readOnlySequence.GetPosition(offset, relativeTo);
            return this.Position;
        }

        /// <inheritdoc/>
        public override void SetLength(long value) => this.ThrowDisposedOr(new NotSupportedException());

        /// <inheritdoc/>
        public override void Write(byte[] buffer, int offset, int count) => this.ThrowDisposedOr(new NotSupportedException());

        /// <inheritdoc/>
        public override void WriteByte(byte value) => this.ThrowDisposedOr(new NotSupportedException());

        /// <inheritdoc/>
        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) => throw this.ThrowDisposedOr(new NotSupportedException());

        /// <inheritdoc/>
        public override async Task CopyToAsync(Stream destination, int bufferSize, CancellationToken cancellationToken)
        {
            foreach (var segment in this.readOnlySequence)
            {
                await WriteAsync(destination, segment, cancellationToken).ConfigureAwait(false);
            }
        }

        private static ValueTask WriteAsync(Stream stream, ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
        {
            Requires.NotNull(stream, nameof(stream));

            if (MemoryMarshal.TryGetArray(buffer, out var array))
            {
                return new ValueTask(stream.WriteAsync(array.Array!, array.Offset, array.Count, cancellationToken));
            }
            else
            {
                var sharedBuffer = ArrayPool<byte>.Shared.Rent(buffer.Length);
                buffer.Span.CopyTo(sharedBuffer);
                return new ValueTask(FinishWriteAsync(stream.WriteAsync(sharedBuffer, 0, buffer.Length, cancellationToken), sharedBuffer));
            }

            async Task FinishWriteAsync(Task writeTask, byte[] localBuffer)
            {
                try
                {
                    await writeTask.ConfigureAwait(false);
                }
                finally
                {
                    ArrayPool<byte>.Shared.Return(localBuffer);
                }
            }
        }

#if SPAN_BUILTIN

        /// <inheritdoc/>
        public override int Read(Span<byte> buffer)
        {
            ReadOnlySequence<byte> remaining = this.readOnlySequence.Slice(this.position);
            ReadOnlySequence<byte> toCopy = remaining.Slice(0, Math.Min(buffer.Length, remaining.Length));
            this.position = toCopy.End;
            toCopy.CopyTo(buffer);
            return (int)toCopy.Length;
        }

        /// <inheritdoc/>
        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return new ValueTask<int>(this.Read(buffer.Span));
        }

        /// <inheritdoc/>
        public override void Write(ReadOnlySpan<byte> buffer) => throw this.ThrowDisposedOr(new NotSupportedException());

        /// <inheritdoc/>
        public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default) => throw this.ThrowDisposedOr(new NotSupportedException());

#endif

        /// <inheritdoc/>
        protected override void Dispose(bool disposing)
        {
            if (!this.IsDisposed)
            {
                this.IsDisposed = true;
                this.disposeAction?.Invoke(this.disposeActionArg);
                base.Dispose(disposing);
            }
        }

        private T ReturnOrThrowDisposed<T>(T value)
        {
            Verify.NotDisposed(this);
            return value;
        }

        private Exception ThrowDisposedOr(Exception ex)
        {
            Verify.NotDisposed(this);
            throw ex;
        }
    }
}
