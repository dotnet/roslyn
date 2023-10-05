// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.IO;
using System.IO.Pipelines;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.ServiceHub.Framework;
using Microsoft.VisualStudio.Threading;
using Roslyn.Utilities;
using StreamJsonRpc;

namespace Microsoft.CodeAnalysis.Remote
{
    /// <summary>
    /// Wraps calls from a remote brokered service back to the client or to an in-proc brokered service.
    /// The purpose of this type is to handle exceptions thrown by the underlying remoting infrastructure
    /// in manner that's compatible with our exception handling policies.
    /// </summary>
    internal readonly struct RemoteCallback<T>
        where T : class
    {
        // We pick a value that is the largest multiple of 4096 that is still smaller than the large object heap threshold (85K). 
        // The CopyTo/CopyToAsync buffer is short-lived and is likely to be collected at Gen0, and it offers a significant 
        // improvement in Copy performance. 
        private const int DefaultCopyBufferSize = 81920;

        private static readonly ObjectPool<Pipe> s_pipePool = new(() => new Pipe(new PipeOptions(
            pool: new PinnedBlockMemoryPool(),
            readerScheduler: PipeScheduler.Inline,
            writerScheduler: PipeScheduler.Inline,
            minimumSegmentSize: DefaultCopyBufferSize)));

        private readonly T _callback;

        public RemoteCallback(T callback)
        {
            _callback = callback;
        }

        /// <summary>
        /// Use to perform a callback from ServiceHub process to an arbitrary brokered service hosted in the original process (usually devenv).
        /// </summary>
        public static async ValueTask<TResult> InvokeServiceAsync<TResult>(
            ServiceBrokerClient client,
            ServiceRpcDescriptor serviceDescriptor,
            Func<RemoteCallback<T>, CancellationToken, ValueTask<TResult>> invocation,
            CancellationToken cancellationToken)
        {
            ServiceBrokerClient.Rental<T> rental;
            try
            {
                rental = await client.GetProxyAsync<T>(serviceDescriptor, cancellationToken).ConfigureAwait(false);
            }
            catch (ObjectDisposedException e)
            {
                // When a connection is dropped ServiceHub's ServiceManager disposes the brokered service, which in turn disposes the ServiceBrokerClient.
                cancellationToken.ThrowIfCancellationRequested();
                throw new OperationCanceledIgnoringCallerTokenException(e);
            }

            Contract.ThrowIfNull(rental.Proxy);
            var callback = new RemoteCallback<T>(rental.Proxy);

            return await invocation(callback, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Invokes API on the callback object hosted in the original process (usually devenv) associated with the currently executing brokered service hosted in ServiceHub process.
        /// </summary>
        public async ValueTask InvokeAsync(Func<T, CancellationToken, ValueTask> invocation, CancellationToken cancellationToken)
        {
            try
            {
                await invocation(_callback, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception exception) when (ReportUnexpectedException(exception, cancellationToken))
            {
                throw new OperationCanceledIgnoringCallerTokenException(exception);
            }
        }

        /// <summary>
        /// Invokes API on the callback object hosted in the original process (usually devenv) associated with the currently executing brokered service hosted in ServiceHub process.
        /// </summary>
        public async ValueTask<TResult> InvokeAsync<TResult>(Func<T, CancellationToken, ValueTask<TResult>> invocation, CancellationToken cancellationToken)
        {
            try
            {
                return await invocation(_callback, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception exception) when (ReportUnexpectedException(exception, cancellationToken))
            {
                throw new OperationCanceledIgnoringCallerTokenException(exception);
            }
        }

        /// <summary>
        /// Invokes API on the callback object hosted in the original process (usually devenv) associated with the
        /// currently executing brokered service hosted in ServiceHub process. The API streams results back to the
        /// caller.
        /// </summary>
        /// <param name="invocation">A callback to asynchronously write data. The callback should always <see
        /// cref="PipeWriter.Complete"/> the <see cref="PipeWriter"/>.  If it does not then reading will hang</param>
        /// <param name="reader">A callback to asynchronously read data. The callback should not complete the <see
        /// cref="PipeReader"/>, but no harm will happen if it does.</param>
        /// <param name="cancellationToken">A cancellation token the operation will observe.</param>
        public async ValueTask<TResult> InvokeAsync<TResult>(
            Func<T, PipeWriter, CancellationToken, ValueTask> invocation,
            Func<PipeReader, CancellationToken, ValueTask<TResult>> reader,
            CancellationToken cancellationToken)
        {

            var pipe = s_pipePool.Allocate();
            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                // Kick off the work to do the writing to the pipe asynchronously.  It will start hot and will be able
                // to do work as the reading side attempts to pull in the data it is writing.

                var writeTask = WriteAsync(_callback, pipe.Writer);
                var readTask = ReadAsync(pipe.Reader);

                // Note: waiting on the write-task is not strictly necessary.  The read-task cannot complete unless it
                // the write-task completes (or it faults for some reason).  However, it's nice and clean to just not
                // use fire-and-forget here and avoids us having to consider things like async-tracking-tokens for
                // testing purposes.
                await Task.WhenAll(writeTask, readTask).ConfigureAwait(false);
                return await readTask.ConfigureAwait(false);
            }
            catch (Exception exception) when (ReportUnexpectedException(exception, cancellationToken))
            {
                throw new OperationCanceledIgnoringCallerTokenException(exception);
            }
            finally
            {
                pipe.Reset();
                s_pipePool.Free(pipe);
            }

            async Task WriteAsync(T service, PipeWriter pipeWriter)
            {
                Exception? exception = null;
                try
                {
                    // Intentionally yield this thread so that the caller can proceed concurrently and start reading.
                    // This is not strictly necessary (as we know the writer will always call FlushAsync()), but it is nice
                    // as it allows both to proceed concurrently on the initial writing/reading.
                    await Task.Yield();

                    await invocation(service, pipeWriter, cancellationToken).ConfigureAwait(false);
                }
                catch (Exception ex) when ((exception = ex) == null)
                {
                    throw ExceptionUtilities.Unreachable();
                }
                finally
                {
                    // Absolutely do not Complete/CompleteAsync the writer here *unless* an exception occurred.  The
                    // writer is passed to StreamJsonRPC which takes ownership of it.  The *inside* of that rpc is
                    // responsible for Completing the writer *it* is passed, which will signal the completion of the
                    // writer we have here.
                    //
                    // We *do* need to complete this writer in the event if an exception as that may have happened
                    // *prior* to even issuing the rpc.  If we don't complete the writer we will hang.  If the exception
                    // happened within the RPC the writer may already be completed, but it's fine for us to complete it
                    // a second time.
                    //
                    // The reason is *not* fine for us to complete the writer in a non-exception event is that it legal
                    // (and is the case in practice) that the code in StreamJsonRPC may still be using it (see
                    // https://github.com/AArnott/Nerdbank.Streams/blob/dafeb5846702bc29e261c9ddf60f42feae01654c/src/Nerdbank.Streams/PipeExtensions.cs#L428)
                    // where the writer may be advanced in an independent Task even once the rpc message has returned to
                    // the caller (us). 
                    //
                    // NOTE: it is intentinonal that the try/catch pattern here does NOT match the one in ReadAsync.  There
                    // are very different semantics around each.  The writer code passes ownership to StreamJsonRPC, while
                    // the reader code does not.  As such, the reader code is responsible for completing the reader in all
                    // cases, whereas the writer code only completes when faulting.

                    // DO NOT REMOVE THIS NULL CHECK WITHOUT DEEP AND CAREFUL REVIEW.
                    if (exception != null)
                        await pipeWriter.CompleteAsync(exception).ConfigureAwait(false);
                }
            }

            async Task<TResult> ReadAsync(PipeReader pipeReader)
            {
                // NOTE: it is intentional that the try/catch pattern here does NOT match the one in WriteAsync.  There
                // are very different semantics around each.  The writer code passes ownership to StreamJsonRPC, while
                // the reader code does not.  As such, the reader code is responsible for completing the reader in all
                // cases, whereas the writer code only completes when faulting.

                Exception? exception = null;
                try
                {
                    return await reader(pipeReader, cancellationToken).ConfigureAwait(false);
                }
                catch (Exception ex) when ((exception = ex) == null)
                {
                    throw ExceptionUtilities.Unreachable();
                }
                finally
                {
                    // ensure we always complete the reader so the pipe can clean up all its resources. in the case of
                    // an exception, attempt to complete the reader with that as well as that will tear down the writer
                    // allowing it to stop writing and allowing the pipe to be cleaned up.
                    await pipeReader.CompleteAsync(exception).ConfigureAwait(false);
                }
            }
        }

        // Remote calls can only throw 4 types of exceptions that correspond to
        //
        //   1) Connection issue (connection dropped for any reason)
        //   2) Serialization issue - bug in serialization of arguments (types are not serializable, etc.)
        //   3) Remote exception - an exception was thrown by the callee
        //   4) Cancelation
        //
        private static bool ReportUnexpectedException(Exception exception, CancellationToken cancellationToken)
        {
            if (exception is IOException)
            {
                // propagate intermittent exceptions without reporting telemetry:
                return false;
            }

            if (exception is OperationCanceledException)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    // Cancellation was requested and expected
                    return false;
                }

                // Log unexpected state where a cancellation exception occurs without being requested. This will return
                // 'true' and caller will convert this to an acceptable cancellation token that won't cause a second
                // NFW.
                return FatalError.ReportAndCatch(exception);
            }

            // When a connection is dropped we can see ConnectionLostException even though
            // CancelLocallyInvokedMethodsWhenConnectionIsClosed is set. That's because there might be a delay between
            // the JsonRpc detecting the disconnect and the call attempting to send a message. Catch the
            // ConnectionLostException exception here and convert it to OperationCanceledException.
            if (exception is ConnectionLostException)
                return true;

            // Indicates bug on client side or in serialization, report NFW and propagate the exception.
            return FatalError.ReportAndPropagate(exception);
        }

        // Copied from: https://github.com/dotnet/aspnetcore/blob/main/src/Shared/Buffers.MemoryPool/PinnedBlockMemoryPool.cs
        internal sealed class PinnedBlockMemoryPool : MemoryPool<byte>
        {
            /// <summary>
            /// The size of a block. 4096 is chosen because most operating systems use 4k pages.
            /// </summary>
            private const int _blockSize = 4096;

            /// <summary>
            /// Max allocation block size for pooled blocks,
            /// larger values can be leased but they will be disposed after use rather than returned to the pool.
            /// </summary>
            public override int MaxBufferSize { get; } = _blockSize;

            /// <summary>
            /// The size of a block. 4096 is chosen because most operating systems use 4k pages.
            /// </summary>
            public static int BlockSize => _blockSize;

            /// <summary>
            /// Thread-safe collection of blocks which are currently in the pool. A slab will pre-allocate all of the block tracking objects
            /// and add them to this collection. When memory is requested it is taken from here first, and when it is returned it is re-added.
            /// </summary>
            private readonly ConcurrentQueue<MemoryPoolBlock> _blocks = new ConcurrentQueue<MemoryPoolBlock>();

            /// <summary>
            /// This is part of implementing the IDisposable pattern.
            /// </summary>
            private bool _isDisposed; // To detect redundant calls

            private readonly object _disposeSync = new object();

            /// <summary>
            /// This default value passed in to Rent to use the default value for the pool.
            /// </summary>
            private const int AnySize = -1;

            public override IMemoryOwner<byte> Rent(int size = AnySize)
            {
                if (size > _blockSize)
                    throw new ArgumentOutOfRangeException(nameof(size));

                if (_isDisposed)
                    throw new ObjectDisposedException(this.GetType().Name);

                if (_blocks.TryDequeue(out var block))
                {
                    // block successfully taken from the stack - return it
                    return block;
                }

                return new MemoryPoolBlock(this, BlockSize);
            }

            /// <summary>
            /// Called to return a block to the pool. Once Return has been called the memory no longer belongs to the caller, and
            /// Very Bad Things will happen if the memory is read of modified subsequently. If a caller fails to call Return and the
            /// block tracking object is garbage collected, the block tracking object's finalizer will automatically re-create and return
            /// a new tracking object into the pool. This will only happen if there is a bug in the server, however it is necessary to avoid
            /// leaving "dead zones" in the slab due to lost block tracking objects.
            /// </summary>
            /// <param name="block">The block to return. It must have been acquired by calling Lease on the same memory pool instance.</param>
            internal void Return(MemoryPoolBlock block)
            {
#if BLOCK_LEASE_TRACKING
            Debug.Assert(block.Pool == this, "Returned block was not leased from this pool");
            Debug.Assert(block.IsLeased, $"Block being returned to pool twice: {block.Leaser}{Environment.NewLine}");
            block.IsLeased = false;
#endif

                if (!_isDisposed)
                {
                    _blocks.Enqueue(block);
                }
            }

            protected override void Dispose(bool disposing)
            {
                if (_isDisposed)
                {
                    return;
                }

                lock (_disposeSync)
                {
                    _isDisposed = true;

                    if (disposing)
                    {
                        // Discard blocks in pool
                        while (_blocks.TryDequeue(out _))
                        {

                        }
                    }
                }
            }
        }

        /// <summary>
        /// Wraps an array allocated in the pinned object heap in a reusable block of managed memory
        /// </summary>
        internal sealed class MemoryPoolBlock : IMemoryOwner<byte>
        {
            internal MemoryPoolBlock(PinnedBlockMemoryPool pool, int length)
            {
                Pool = pool;

#if NET
                var pinnedArray = GC.AllocateUninitializedArray<byte>(length, pinned: true);
#else
                var pinnedArray = new byte[length];
                GCHandle.Alloc(pinnedArray, GCHandleType.Pinned);
#endif

                Memory = MemoryMarshal.CreateFromPinnedArray(pinnedArray, 0, pinnedArray.Length);
            }

            /// <summary>
            /// Back-reference to the memory pool which this block was allocated from. It may only be returned to this pool.
            /// </summary>
            public PinnedBlockMemoryPool Pool { get; }

            public Memory<byte> Memory { get; }

            public void Dispose()
            {
                Pool.Return(this);
            }
        }
    }
}
