// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
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
    [ExportWorkspaceServiceFactory(typeof(ITemporaryStorageService), ServiceLayer.Host), Shared]
    internal partial class TemporaryStorageServiceFactory : IWorkspaceServiceFactory
    {
        public IWorkspaceService CreateService(HostWorkspaceServices workspaceServices)
        {
            var textFactory = workspaceServices.GetService<ITextFactoryService>();
            return new TemporaryStorageService(textFactory);
        }

        /// <summary>
        /// Temporarily stores text and streams in memory mapped files.
        /// </summary>
        internal class TemporaryStorageService : ITemporaryStorageService2
        {
            /// <summary>
            /// The maximum size of a single storage unit in a memory mapped file which is shared with other storage
            /// units.
            /// </summary>
            private const long SingleFileThreshold = 128 * 1024;

            /// <summary>
            /// The size of a memory mapped file created to store multiple temporary objects.
            /// </summary>
            private const long MultiFileBlockSize = SingleFileThreshold * 32;

            private readonly ITextFactoryService _textFactory;

            /// <summary>
            /// The most recent memory mapped file for creating multiple storage units. It will be used via bump-pointer
            /// allocation until space is no longer available in it.
            /// </summary>
            private MemoryMappedFileStorage _storage;

            public TemporaryStorageService(ITextFactoryService textFactory)
            {
                _textFactory = textFactory;
            }

            public ITemporaryTextStorage CreateTemporaryTextStorage(CancellationToken cancellationToken)
            {
                return new TemporaryTextStorage(this);
            }

            public ITemporaryTextStorage AttachTemporaryTextStorage(string storageName, long offset, long size, Encoding encoding, CancellationToken cancellationToken)
            {
                return new TemporaryTextStorage(this, storageName, offset, size, encoding);
            }

            public ITemporaryStreamStorage CreateTemporaryStreamStorage(CancellationToken cancellationToken)
            {
                return new TemporaryStreamStorage(this);
            }

            public ITemporaryStreamStorage AttachTemporaryStreamStorage(string storageName, long offset, long size, CancellationToken cancellationToken)
            {
                return new TemporaryStreamStorage(this, storageName, offset, size);
            }

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
                    return new MemoryMappedInfo(new ReferenceCountedDisposable<MemoryMappedFile>(storage), mapName, 0, size);
                }

                while (true)
                {
                    // Obtain the storage location, creating one if necessary. If a reference counted handle to a memory
                    // mapped file is obtained in this section, it must either be disposed within the loop or returned
                    // to the caller who will own it through the MemoryMappedInfo.
                    var storage = Volatile.Read(ref _storage);
                    var reference = storage?.WeakFileReference.TryAddReference();
                    if (reference == null)
                    {
                        var oldStorage = storage;
                        (storage, reference) = MemoryMappedFileStorage.Create(MultiFileBlockSize);
                        if (Interlocked.CompareExchange(ref _storage, storage, oldStorage) != oldStorage)
                        {
                            // Another thread created the next storage unit; try again
                            reference.Dispose();
                            continue;
                        }
                    }

                    // Try to reserve additional space in this storage location
                    var endOffset = Interlocked.Add(ref storage.Offset, size);
                    if (endOffset > storage.Size)
                    {
                        // No more space is available. Invalidate the current storage block if no other thread has done
                        // so, and try again to allocate from a different storage block.
                        Interlocked.CompareExchange(ref _storage, null, storage);
                        reference.Dispose();
                        continue;
                    }

                    return new MemoryMappedInfo(reference, storage.Name, endOffset - size, size);
                }
            }

            public static string CreateUniqueName(long size)
            {
                return "Roslyn Temp Storage " + size.ToString() + " " + Guid.NewGuid().ToString("N");
            }

            /// <summary>
            /// This class stores the information necessary to describe a single shared memory mapped file which can
            /// serve multiple allocation requests. Use <see cref="CreateTemporaryStorage"/> rather than interacting
            /// with this class directly.
            /// </summary>
            private sealed class MemoryMappedFileStorage
            {
                public readonly ReferenceCountedDisposable<MemoryMappedFile>.WeakReference WeakFileReference;
                public readonly string Name;
                public readonly long Size;
                public long Offset;

                private MemoryMappedFileStorage(ReferenceCountedDisposable<MemoryMappedFile>.WeakReference memoryMappedFile, string name, long size)
                {
                    WeakFileReference = memoryMappedFile;
                    Name = name;
                    Size = size;
                }

                public static (MemoryMappedFileStorage storage, ReferenceCountedDisposable<MemoryMappedFile> firstReference) Create(long size)
                {
                    var mapName = CreateUniqueName(size);
                    var file = MemoryMappedFile.CreateNew(mapName, size);

                    var firstReference = new ReferenceCountedDisposable<MemoryMappedFile>(file);
                    var weakReference = new ReferenceCountedDisposable<MemoryMappedFile>.WeakReference(firstReference);
                    var storage = new MemoryMappedFileStorage(weakReference, mapName, size);
                    return (storage, firstReference);
                }
            }

            private class TemporaryTextStorage : ITemporaryTextStorage, ITemporaryStorageWithName
            {
                private readonly TemporaryStorageService _service;
                private Encoding _encoding;
                private MemoryMappedInfo _memoryMappedInfo;

                public TemporaryTextStorage(TemporaryStorageService service)
                {
                    _service = service;
                }

                public TemporaryTextStorage(TemporaryStorageService service, string storageName, long offset, long size, Encoding encoding)
                {
                    _service = service;
                    _encoding = encoding;
                    _memoryMappedInfo = new MemoryMappedInfo(storageName, offset, size);
                }

                public string Name => _memoryMappedInfo?.Name;
                public long Offset => _memoryMappedInfo.Offset;
                public long Size => _memoryMappedInfo.Size;

                public void Dispose()
                {
                    if (_memoryMappedInfo != null)
                    {
                        // Destructors of SafeHandle and FileStream in MemoryMappedFile
                        // will eventually release resources if this Dispose is not called
                        // explicitly
                        _memoryMappedInfo.Dispose();
                        _memoryMappedInfo = null;
                    }

                    if (_encoding != null)
                    {
                        _encoding = null;
                    }
                }

                public SourceText ReadText(CancellationToken cancellationToken)
                {
                    if (_memoryMappedInfo == null)
                    {
                        throw new InvalidOperationException();
                    }

                    using (Logger.LogBlock(FunctionId.TemporaryStorageServiceFactory_ReadText, cancellationToken))
                    {
                        using (var stream = _memoryMappedInfo.CreateReadableStream())
                        using (var reader = CreateTextReaderFromTemporaryStorage((ISupportDirectMemoryAccess)stream, (int)stream.Length, cancellationToken))
                        {
                            // we pass in encoding we got from original source text even if it is null.
                            return _service._textFactory.CreateText(reader, _encoding, cancellationToken);
                        }
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
                        throw new InvalidOperationException();
                    }

                    using (Logger.LogBlock(FunctionId.TemporaryStorageServiceFactory_WriteText, cancellationToken))
                    {
                        _encoding = text.Encoding;

                        // the method we use to get text out of SourceText uses Unicode (2bytes per char). 
                        var size = Encoding.Unicode.GetMaxByteCount(text.Length);
                        _memoryMappedInfo = _service.CreateTemporaryStorage(size);

                        // Write the source text out as Unicode. We expect that to be cheap.
                        using (var stream = _memoryMappedInfo.CreateWritableStream())
                        {
                            using (var writer = new StreamWriter(stream, Encoding.Unicode))
                            {
                                text.Write(writer, cancellationToken);
                            }
                        }
                    }
                }

                public Task WriteTextAsync(SourceText text, CancellationToken cancellationToken = default(CancellationToken))
                {
                    // See commentary in ReadTextAsync for why this is implemented this way.
                    return Task.Factory.StartNew(() => WriteText(text, cancellationToken), cancellationToken, TaskCreationOptions.None, TaskScheduler.Default);
                }

                private unsafe TextReader CreateTextReaderFromTemporaryStorage(ISupportDirectMemoryAccess accessor, int streamLength, CancellationToken cancellationToken)
                {
                    char* src = (char*)accessor.GetPointer();

                    // BOM: Unicode, little endian
                    // Skip the BOM when creating the reader
                    Debug.Assert(*src == 0xFEFF);

                    return new DirectMemoryAccessStreamReader(src + 1, streamLength / sizeof(char) - 1);
                }
            }

            private class TemporaryStreamStorage : ITemporaryStreamStorage, ITemporaryStorageWithName
            {
                private readonly TemporaryStorageService _service;
                private MemoryMappedInfo _memoryMappedInfo;

                public TemporaryStreamStorage(TemporaryStorageService service)
                {
                    _service = service;
                }

                public TemporaryStreamStorage(TemporaryStorageService service, string storageName, long offset, long size)
                {
                    _service = service;
                    _memoryMappedInfo = new MemoryMappedInfo(storageName, offset, size);
                }

                public string Name => _memoryMappedInfo?.Name;
                public long Offset => _memoryMappedInfo.Offset;
                public long Size => _memoryMappedInfo.Size;

                public void Dispose()
                {
                    if (_memoryMappedInfo != null)
                    {
                        // Destructors of SafeHandle and FileStream in MemoryMappedFile
                        // will eventually release resources if this Dispose is not called
                        // explicitly
                        _memoryMappedInfo.Dispose();
                        _memoryMappedInfo = null;
                    }
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

                public Task<Stream> ReadStreamAsync(CancellationToken cancellationToken = default(CancellationToken))
                {
                    // See commentary in ReadTextAsync for why this is implemented this way.
                    return Task.Factory.StartNew(() => ReadStream(cancellationToken), cancellationToken, TaskCreationOptions.None, TaskScheduler.Default);
                }

                public void WriteStream(Stream stream, CancellationToken cancellationToken = default(CancellationToken))
                {
                    // The Wait() here will not actually block, since with useAsync: false, the
                    // entire operation will already be done when WaitStreamMaybeAsync completes.
                    WriteStreamMaybeAsync(stream, useAsync: false, cancellationToken: cancellationToken).GetAwaiter().GetResult();
                }

                public Task WriteStreamAsync(Stream stream, CancellationToken cancellationToken = default(CancellationToken))
                {
                    return WriteStreamMaybeAsync(stream, useAsync: true, cancellationToken: cancellationToken);
                }

                private async Task WriteStreamMaybeAsync(Stream stream, bool useAsync, CancellationToken cancellationToken)
                {
                    if (_memoryMappedInfo != null)
                    {
                        throw new InvalidOperationException(WorkspacesResources.Temporary_storage_cannot_be_written_more_than_once);
                    }

                    if (stream.Length == 0)
                    {
                        throw new ArgumentOutOfRangeException();
                    }

                    using (Logger.LogBlock(FunctionId.TemporaryStorageServiceFactory_WriteStream, cancellationToken))
                    {
                        var size = stream.Length;
                        _memoryMappedInfo = _service.CreateTemporaryStorage(size);
                        using (var viewStream = _memoryMappedInfo.CreateWritableStream())
                        {
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
        }

        internal unsafe class DirectMemoryAccessStreamReader : TextReaderWithLength
        {
            private char* _position;
            private readonly char* _end;

            public DirectMemoryAccessStreamReader(char* src, int length) :
                base(length)
            {
                Debug.Assert(src != null);
                Debug.Assert(length >= 0);

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

