// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Host
{
    [ExportWorkspaceServiceFactory(typeof(ITemporaryStorageService), ServiceLayer.Default), Shared]
    internal partial class TemporaryStorageServiceFactory : IWorkspaceServiceFactory
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public TemporaryStorageServiceFactory()
        {
        }

        public IWorkspaceService CreateService(HostWorkspaceServices workspaceServices)
        {
            var textFactory = workspaceServices.GetRequiredService<ITextFactoryService>();

            // MemoryMapped files which are used by the TemporaryStorageService are present in .NET Framework (including Mono)
            // and .NET Core Windows. For non-Windows .NET Core scenarios, we can return the TrivialTemporaryStorageService
            // until https://github.com/dotnet/runtime/issues/30878 is fixed.
            return PlatformInformation.IsWindows || PlatformInformation.IsRunningOnMono
                ? new TemporaryStorageService(textFactory)
                : TrivialTemporaryStorageService.Instance;
        }

        /// <summary>
        /// Temporarily stores text and streams in memory mapped files.
        /// </summary>
        internal class TemporaryStorageService : ITemporaryStorageService2
        {
            /// <summary>
            /// The maximum size in bytes of a single storage unit in a memory mapped file which is shared with other
            /// storage units.
            /// </summary>
            /// <remarks>
            /// <para>This value was arbitrarily chosen and appears to work well. Can be changed if data suggests
            /// something better.</para>
            /// </remarks>
            /// <seealso cref="_weakFileReference"/>
            private const long SingleFileThreshold = 128 * 1024;

            /// <summary>
            /// The size in bytes of a memory mapped file created to store multiple temporary objects.
            /// </summary>
            /// <remarks>
            /// <para>This value was arbitrarily chosen and appears to work well. Can be changed if data suggests
            /// something better.</para>
            /// </remarks>
            /// <seealso cref="_weakFileReference"/>
            private const long MultiFileBlockSize = SingleFileThreshold * 32;

            private readonly ITextFactoryService _textFactory;

            /// <summary>
            /// The synchronization object for accessing the memory mapped file related fields (indicated in the remarks
            /// of each field).
            /// </summary>
            /// <remarks>
            /// <para>PERF DEV NOTE: A concurrent (but complex) implementation of this type with identical semantics is
            /// available in source control history. The use of exclusive locks was not causing any measurable
            /// performance overhead even on 28-thread machines at the time this was written.</para>
            /// </remarks>
            private readonly object _gate = new();

            /// <summary>
            /// The most recent memory mapped file for creating multiple storage units. It will be used via bump-pointer
            /// allocation until space is no longer available in it.
            /// </summary>
            /// <remarks>
            /// <para>Access should be synchronized on <see cref="_gate"/>.</para>
            /// </remarks>
            private ReferenceCountedDisposable<MemoryMappedFile>.WeakReference _weakFileReference;

            /// <summary>The name of the current memory mapped file for multiple storage units.</summary>
            /// <remarks>
            /// <para>Access should be synchronized on <see cref="_gate"/>.</para>
            /// </remarks>
            /// <seealso cref="_weakFileReference"/>
            private string? _name;

            /// <summary>The total size of the current memory mapped file for multiple storage units.</summary>
            /// <remarks>
            /// <para>Access should be synchronized on <see cref="_gate"/>.</para>
            /// </remarks>
            /// <seealso cref="_weakFileReference"/>
            private long _fileSize;

            /// <summary>
            /// The offset into the current memory mapped file where the next storage unit can be held.
            /// </summary>
            /// <remarks>
            /// <para>Access should be synchronized on <see cref="_gate"/>.</para>
            /// </remarks>
            /// <seealso cref="_weakFileReference"/>
            private long _offset;

            public TemporaryStorageService(ITextFactoryService textFactory)
                => _textFactory = textFactory;

            public ITemporaryTextStorage CreateTemporaryTextStorage(CancellationToken cancellationToken)
                => new TemporaryTextStorage(this);

            public ITemporaryTextStorage AttachTemporaryTextStorage(string storageName, long offset, long size, SourceHashAlgorithm checksumAlgorithm, Encoding? encoding, CancellationToken cancellationToken)
                => new TemporaryTextStorage(this, storageName, offset, size, checksumAlgorithm, encoding);

            public ITemporaryStreamStorage CreateTemporaryStreamStorage(CancellationToken cancellationToken)
                => new TemporaryStreamStorage(this);

            public ITemporaryStreamStorage AttachTemporaryStreamStorage(string storageName, long offset, long size, CancellationToken cancellationToken)
                => new TemporaryStreamStorage(this, storageName, offset, size);

            /// <summary>
            /// Allocate shared storage of a specified size.
            /// </summary>
            /// <remarks>
            /// <para>"Small" requests are fulfilled from oversized memory mapped files which support several individual
            /// storage units. Larger requests are allocated in their own memory mapped files.</para>
            /// </remarks>
            /// <param name="size">The size of the shared storage block to allocate.</param>
            /// <returns>A <see cref="MemoryMappedInfo"/> describing the allocated block.</returns>
            private MemoryMappedInfo CreateTemporaryStorage(long size)
            {
                if (size >= SingleFileThreshold)
                {
                    // Larger blocks are allocated separately
                    var mapName = CreateUniqueName(size);
                    var storage = MemoryMappedFile.CreateNew(mapName, size);
                    return new MemoryMappedInfo(new ReferenceCountedDisposable<MemoryMappedFile>(storage), mapName, offset: 0, size: size);
                }

                lock (_gate)
                {
                    // Obtain a reference to the memory mapped file, creating one if necessary. If a reference counted
                    // handle to a memory mapped file is obtained in this section, it must either be disposed before
                    // returning or returned to the caller who will own it through the MemoryMappedInfo.
                    var reference = _weakFileReference.TryAddReference();
                    if (reference == null || _offset + size > _fileSize)
                    {
                        var mapName = CreateUniqueName(MultiFileBlockSize);
                        var file = MemoryMappedFile.CreateNew(mapName, MultiFileBlockSize);

                        reference = new ReferenceCountedDisposable<MemoryMappedFile>(file);
                        _weakFileReference = new ReferenceCountedDisposable<MemoryMappedFile>.WeakReference(reference);
                        _name = mapName;
                        _fileSize = MultiFileBlockSize;
                        _offset = size;
                        return new MemoryMappedInfo(reference, _name, offset: 0, size: size);
                    }
                    else
                    {
                        // Reserve additional space in the existing storage location
                        Contract.ThrowIfNull(_name);
                        _offset += size;
                        return new MemoryMappedInfo(reference, _name, _offset - size, size);
                    }
                }
            }

            public static string CreateUniqueName(long size)
                => "Roslyn Temp Storage " + size.ToString() + " " + Guid.NewGuid().ToString("N");

            private sealed class TemporaryTextStorage : ITemporaryTextStorage, ITemporaryTextStorageWithName
            {
                private readonly TemporaryStorageService _service;
                private SourceHashAlgorithm _checksumAlgorithm;
                private Encoding? _encoding;
                private ImmutableArray<byte> _checksum;
                private MemoryMappedInfo? _memoryMappedInfo;

                public TemporaryTextStorage(TemporaryStorageService service)
                    => _service = service;

                public TemporaryTextStorage(TemporaryStorageService service, string storageName, long offset, long size, SourceHashAlgorithm checksumAlgorithm, Encoding? encoding)
                {
                    _service = service;
                    _checksumAlgorithm = checksumAlgorithm;
                    _encoding = encoding;
                    _memoryMappedInfo = new MemoryMappedInfo(storageName, offset, size);
                }

                // TODO: cleanup https://github.com/dotnet/roslyn/issues/43037
                // Offet, Size not accessed if Name is null
                public string? Name => _memoryMappedInfo?.Name;
                public long Offset => _memoryMappedInfo!.Offset;
                public long Size => _memoryMappedInfo!.Size;
                public SourceHashAlgorithm ChecksumAlgorithm => _checksumAlgorithm;
                public Encoding? Encoding => _encoding;

                public ImmutableArray<byte> GetChecksum()
                {
                    if (_checksum.IsDefault)
                    {
                        ImmutableInterlocked.InterlockedInitialize(ref _checksum, ReadText(CancellationToken.None).GetChecksum());
                    }

                    return _checksum;
                }

                public void Dispose()
                {
                    // Destructors of SafeHandle and FileStream in MemoryMappedFile
                    // will eventually release resources if this Dispose is not called
                    // explicitly
                    _memoryMappedInfo?.Dispose();

                    _memoryMappedInfo = null;
                    _encoding = null;
                }

                public SourceText ReadText(CancellationToken cancellationToken)
                {
                    if (_memoryMappedInfo == null)
                    {
                        throw new InvalidOperationException();
                    }

                    using (Logger.LogBlock(FunctionId.TemporaryStorageServiceFactory_ReadText, cancellationToken))
                    {
                        using var stream = _memoryMappedInfo.CreateReadableStream();
                        using var reader = CreateTextReaderFromTemporaryStorage((ISupportDirectMemoryAccess)stream, (int)stream.Length);

                        // we pass in encoding we got from original source text even if it is null.
                        return _service._textFactory.CreateText(reader, _encoding, cancellationToken);
                    }
                }

                public Task<SourceText> ReadTextAsync(CancellationToken cancellationToken)
                {
                    // There is a reason for implementing it like this: proper async implementation
                    // that reads the underlying memory mapped file stream in an asynchronous fashion
                    // doesn't actually work. Windows doesn't offer
                    // any non-blocking way to read from a memory mapped file; the underlying memcpy
                    // may block as the memory pages back in and that's something you have to live
                    // with. Therefore, any implementation that attempts to use async will still
                    // always be blocking at least one threadpool thread in the memcpy in the case
                    // of a page fault. Therefore, if we're going to be blocking a thread, we should
                    // just block one thread and do the whole thing at once vs. a fake "async"
                    // implementation which will continue to requeue work back to the thread pool.
                    return Task.Factory.StartNew(() => ReadText(cancellationToken), cancellationToken, TaskCreationOptions.None, TaskScheduler.Default);
                }

                public void WriteText(SourceText text, CancellationToken cancellationToken)
                {
                    if (_memoryMappedInfo != null)
                    {
                        throw new InvalidOperationException(WorkspacesResources.Temporary_storage_cannot_be_written_more_than_once);
                    }

                    using (Logger.LogBlock(FunctionId.TemporaryStorageServiceFactory_WriteText, cancellationToken))
                    {
                        _checksumAlgorithm = text.ChecksumAlgorithm;
                        _encoding = text.Encoding;

                        // the method we use to get text out of SourceText uses Unicode (2bytes per char). 
                        var size = Encoding.Unicode.GetMaxByteCount(text.Length);
                        _memoryMappedInfo = _service.CreateTemporaryStorage(size);

                        // Write the source text out as Unicode. We expect that to be cheap.
                        using var stream = _memoryMappedInfo.CreateWritableStream();
                        using var writer = new StreamWriter(stream, Encoding.Unicode);

                        text.Write(writer, cancellationToken);
                    }
                }

                public Task WriteTextAsync(SourceText text, CancellationToken cancellationToken = default)
                {
                    // See commentary in ReadTextAsync for why this is implemented this way.
                    return Task.Factory.StartNew(() => WriteText(text, cancellationToken), cancellationToken, TaskCreationOptions.None, TaskScheduler.Default);
                }

                private static unsafe TextReader CreateTextReaderFromTemporaryStorage(ISupportDirectMemoryAccess accessor, int streamLength)
                {
                    var src = (char*)accessor.GetPointer();

                    // BOM: Unicode, little endian
                    // Skip the BOM when creating the reader
                    Debug.Assert(*src == 0xFEFF);

                    return new DirectMemoryAccessStreamReader(src + 1, streamLength / sizeof(char) - 1);
                }
            }

            private class TemporaryStreamStorage : ITemporaryStreamStorage, ITemporaryStorageWithName
            {
                private readonly TemporaryStorageService _service;
                private MemoryMappedInfo? _memoryMappedInfo;

                public TemporaryStreamStorage(TemporaryStorageService service)
                    => _service = service;

                public TemporaryStreamStorage(TemporaryStorageService service, string storageName, long offset, long size)
                {
                    _service = service;
                    _memoryMappedInfo = new MemoryMappedInfo(storageName, offset, size);
                }

                // TODO: clean up https://github.com/dotnet/roslyn/issues/43037
                // Offset, Size is only used when Name is not null.
                public string? Name => _memoryMappedInfo?.Name;
                public long Offset => _memoryMappedInfo!.Offset;
                public long Size => _memoryMappedInfo!.Size;

                public void Dispose()
                {
                    // Destructors of SafeHandle and FileStream in MemoryMappedFile
                    // will eventually release resources if this Dispose is not called
                    // explicitly
                    _memoryMappedInfo?.Dispose();
                    _memoryMappedInfo = null;
                }

                public Stream ReadStream(CancellationToken cancellationToken)
                {
                    if (_memoryMappedInfo == null)
                    {
                        throw new InvalidOperationException();
                    }

                    using (Logger.LogBlock(FunctionId.TemporaryStorageServiceFactory_ReadStream, cancellationToken))
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        return _memoryMappedInfo.CreateReadableStream();
                    }
                }

                public Task<Stream> ReadStreamAsync(CancellationToken cancellationToken = default)
                {
                    // See commentary in ReadTextAsync for why this is implemented this way.
                    return Task.Factory.StartNew(() => ReadStream(cancellationToken), cancellationToken, TaskCreationOptions.None, TaskScheduler.Default);
                }

                public void WriteStream(Stream stream, CancellationToken cancellationToken = default)
                {
                    // The Wait() here will not actually block, since with useAsync: false, the
                    // entire operation will already be done when WaitStreamMaybeAsync completes.
                    WriteStreamMaybeAsync(stream, useAsync: false, cancellationToken: cancellationToken).GetAwaiter().GetResult();
                }

                public Task WriteStreamAsync(Stream stream, CancellationToken cancellationToken = default)
                    => WriteStreamMaybeAsync(stream, useAsync: true, cancellationToken: cancellationToken);

                private async Task WriteStreamMaybeAsync(Stream stream, bool useAsync, CancellationToken cancellationToken)
                {
                    if (_memoryMappedInfo != null)
                    {
                        throw new InvalidOperationException(WorkspacesResources.Temporary_storage_cannot_be_written_more_than_once);
                    }

                    using (Logger.LogBlock(FunctionId.TemporaryStorageServiceFactory_WriteStream, cancellationToken))
                    {
                        var size = stream.Length;
                        _memoryMappedInfo = _service.CreateTemporaryStorage(size);
                        using var viewStream = _memoryMappedInfo.CreateWritableStream();

                        var buffer = SharedPools.ByteArray.Allocate();
                        try
                        {
                            while (true)
                            {
                                int count;
                                if (useAsync)
                                {
                                    count = await stream.ReadAsync(buffer, 0, buffer.Length, cancellationToken).ConfigureAwait(false);
                                }
                                else
                                {
                                    count = stream.Read(buffer, 0, buffer.Length);
                                }

                                if (count == 0)
                                {
                                    break;
                                }

                                viewStream.Write(buffer, 0, count);
                            }
                        }
                        finally
                        {
                            SharedPools.ByteArray.Free(buffer);
                        }
                    }
                }
            }
        }

        internal unsafe class DirectMemoryAccessStreamReader : TextReaderWithLength
        {
            private char* _position;
            private readonly char* _end;

            public DirectMemoryAccessStreamReader(char* src, int length)
                : base(length)
            {
                RoslynDebug.Assert(src != null);
                RoslynDebug.Assert(length >= 0);

                _position = src;
                _end = _position + length;
            }

            public override int Peek()
            {
                if (_position >= _end)
                {
                    return -1;
                }

                return *_position;
            }

            public override int Read()
            {
                if (_position >= _end)
                {
                    return -1;
                }

                return *_position++;
            }

            public override int Read(char[] buffer, int index, int count)
            {
                if (buffer == null)
                {
                    throw new ArgumentNullException(nameof(buffer));
                }

                if (index < 0 || index >= buffer.Length)
                {
                    throw new ArgumentOutOfRangeException(nameof(index));
                }

                if (count < 0 || (index + count) > buffer.Length)
                {
                    throw new ArgumentOutOfRangeException(nameof(count));
                }

                count = Math.Min(count, (int)(_end - _position));
                if (count > 0)
                {
                    Marshal.Copy((IntPtr)_position, buffer, index, count);
                    _position += count;
                }

                return count;
            }
        }
    }
}

