// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Host;

/// <summary>
/// Temporarily stores text and streams in memory mapped files.
/// </summary>
#if NETCOREAPP
[SupportedOSPlatform("windows")]
#endif
internal sealed partial class TemporaryStorageService : ITemporaryStorageServiceInternal
{
    /// <summary>
    /// The maximum size in bytes of a single storage unit in a memory mapped file which is shared with other storage
    /// units.
    /// </summary>
    /// <remarks>
    /// <para>The value of 256k reduced the number of files dumped to separate memory mapped files by 60% compared to
    /// the next lower power-of-2 size for Roslyn.sln itself.</para>
    /// </remarks>
    /// <seealso cref="_weakFileReference"/>
    private const long SingleFileThreshold = 256 * 1024;

    /// <summary>
    /// The size in bytes of a memory mapped file created to store multiple temporary objects.
    /// </summary>
    /// <remarks>
    /// <para>This value (8mb) creates roughly 35 memory mapped files (around 300MB) to store the contents of all of
    /// Roslyn.sln a snapshot. This keeps the data safe, so that we can drop it from memory when not needed, but
    /// reconstitute the contents we originally had in the snapshot in case the original files change on disk.</para>
    /// </remarks>
    /// <seealso cref="_weakFileReference"/>
    private const long MultiFileBlockSize = SingleFileThreshold * 32;

    private readonly IWorkspaceThreadingService? _workspaceThreadingService;
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
    /// allocation until space is no longer available in it.  Access should be synchronized on <see cref="_gate"/>
    /// </summary>
    private ReferenceCountedDisposable<MemoryMappedFile>.WeakReference _weakFileReference;

    /// <summary>The name of the current memory mapped file for multiple storage units. Access should be synchronized on
    /// <see cref="_gate"/></summary>
    /// <seealso cref="_weakFileReference"/>
    private string? _name;

    /// <summary>The total size of the current memory mapped file for multiple storage units. Access should be
    /// synchronized on <see cref="_gate"/></summary>
    /// <seealso cref="_weakFileReference"/>
    private long _fileSize;

    /// <summary>
    /// The offset into the current memory mapped file where the next storage unit can be held. Access should be
    /// synchronized on <see cref="_gate"/>.
    /// </summary>
    /// <seealso cref="_weakFileReference"/>
    private long _offset;

    [Obsolete(MefConstruction.FactoryMethodMessage, error: true)]
    private TemporaryStorageService(IWorkspaceThreadingService? workspaceThreadingService, ITextFactoryService textFactory)
    {
        _workspaceThreadingService = workspaceThreadingService;
        _textFactory = textFactory;
    }

    public ITemporaryTextStorageInternal CreateTemporaryTextStorage()
        => new TemporaryTextStorage(this);

    public TemporaryTextStorage AttachTemporaryTextStorage(
        string storageName, long offset, long size, SourceHashAlgorithm checksumAlgorithm, Encoding? encoding, ImmutableArray<byte> contentHash)
        => new(this, storageName, offset, size, checksumAlgorithm, encoding, contentHash);

    public TemporaryStorageHandle WriteToTemporaryStorage(Stream stream, CancellationToken cancellationToken)
    {
        var storage = new TemporaryStreamStorage(this);
        storage.WriteStream(stream, cancellationToken);
        var identifier = new TemporaryStorageIdentifier(storage.Name, storage.Offset, storage.Size);
        return new(storage, identifier);
    }

    Stream ITemporaryStorageServiceInternal.ReadFromTemporaryStorageService(TemporaryStorageIdentifier storageIdentifier, CancellationToken cancellationToken)
        => ReadFromTemporaryStorageService(storageIdentifier, cancellationToken);

    public UnmanagedMemoryStream ReadFromTemporaryStorageService(TemporaryStorageIdentifier storageIdentifier, CancellationToken cancellationToken)
    {
        var storage = new TemporaryStreamStorage(this, storageIdentifier.Name, storageIdentifier.Offset, storageIdentifier.Size);
        return storage.ReadStream(cancellationToken);
    }

    internal TemporaryStorageHandle GetHandle(TemporaryStorageIdentifier storageIdentifier)
    {
        var storage = new TemporaryStreamStorage(this, storageIdentifier.Name, storageIdentifier.Offset, storageIdentifier.Size);
        return new(storage, storageIdentifier);
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

    public sealed class TemporaryTextStorage : ITemporaryTextStorageInternal, ITemporaryStorageWithName
    {
        private readonly TemporaryStorageService _service;
        private SourceHashAlgorithm _checksumAlgorithm;
        private Encoding? _encoding;
        private ImmutableArray<byte> _contentHash;
        private MemoryMappedInfo? _memoryMappedInfo;

        public TemporaryTextStorage(TemporaryStorageService service)
            => _service = service;

        public TemporaryTextStorage(
            TemporaryStorageService service,
            string storageName,
            long offset,
            long size,
            SourceHashAlgorithm checksumAlgorithm,
            Encoding? encoding,
            ImmutableArray<byte> contentHash)
        {
            _service = service;
            _checksumAlgorithm = checksumAlgorithm;
            _encoding = encoding;
            _contentHash = contentHash;
            _memoryMappedInfo = new MemoryMappedInfo(storageName, offset, size);
        }

        // TODO: cleanup https://github.com/dotnet/roslyn/issues/43037
        // Offset, Size not accessed if Name is null
        public string? Name => _memoryMappedInfo?.Name;
        public long Offset => _memoryMappedInfo!.Offset;
        public long Size => _memoryMappedInfo!.Size;

        /// <summary>
        /// Gets the value for the <see cref="SourceText.ChecksumAlgorithm"/> property for the <see cref="SourceText"/>
        /// represented by this temporary storage.
        /// </summary>
        public SourceHashAlgorithm ChecksumAlgorithm => _checksumAlgorithm;

        /// <summary>
        /// Gets the value for the <see cref="SourceText.Encoding"/> property for the <see cref="SourceText"/>
        /// represented by this temporary storage.
        /// </summary>
        public Encoding? Encoding => _encoding;

        /// <summary>
        /// Gets the checksum for the <see cref="SourceText"/> represented by this temporary storage. This is equivalent
        /// to calling <see cref="SourceText.GetContentHash"/>.
        /// </summary>
        public ImmutableArray<byte> ContentHash => _contentHash;

        public void Dispose()
        {
            // Destructors of SafeHandle and FileStream in MemoryMappedFile
            // will eventually release resources if this Dispose is not called
            // explicitly
            _memoryMappedInfo?.Dispose();

            _memoryMappedInfo = null;
            _encoding = null;
            _contentHash = default;
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
                using var reader = CreateTextReaderFromTemporaryStorage(stream);

                // we pass in encoding we got from original source text even if it is null.
                return _service._textFactory.CreateText(reader, _encoding, _checksumAlgorithm, cancellationToken);
            }
        }

        public async Task<SourceText> ReadTextAsync(CancellationToken cancellationToken)
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
            if (_service._workspaceThreadingService is { IsOnMainThread: true })
            {
                await Task.Yield().ConfigureAwait(false);
                cancellationToken.ThrowIfCancellationRequested();
            }

            return ReadText(cancellationToken);
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
                _contentHash = text.GetContentHash();

                // the method we use to get text out of SourceText uses Unicode (2bytes per char). 
                var size = Encoding.Unicode.GetMaxByteCount(text.Length);
                _memoryMappedInfo = _service.CreateTemporaryStorage(size);

                // Write the source text out as Unicode. We expect that to be cheap.
                using var stream = _memoryMappedInfo.CreateWritableStream();
                using var writer = new StreamWriter(stream, Encoding.Unicode);

                text.Write(writer, cancellationToken);
            }
        }

        public async Task WriteTextAsync(SourceText text, CancellationToken cancellationToken)
        {
            // See commentary in ReadTextAsync for why this is implemented this way.
            if (_service._workspaceThreadingService is { IsOnMainThread: true })
            {
                await Task.Yield().ConfigureAwait(false);
                cancellationToken.ThrowIfCancellationRequested();
            }

            WriteText(text, cancellationToken);
        }

        private static unsafe TextReader CreateTextReaderFromTemporaryStorage(UnmanagedMemoryStream stream)
        {
            var src = (char*)stream.PositionPointer;

            // BOM: Unicode, little endian
            // Skip the BOM when creating the reader
            Debug.Assert(*src == 0xFEFF);

            return new DirectMemoryAccessStreamReader(src + 1, (int)stream.Length / sizeof(char) - 1);
        }
    }

    internal sealed class TemporaryStreamStorage : IDisposable
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

        public string Name => _memoryMappedInfo!.Name;
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

        public UnmanagedMemoryStream ReadStream(CancellationToken cancellationToken)
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

        public void WriteStream(Stream stream, CancellationToken cancellationToken)
        {
            if (_memoryMappedInfo != null)
                throw new InvalidOperationException(WorkspacesResources.Temporary_storage_cannot_be_written_more_than_once);

            using (Logger.LogBlock(FunctionId.TemporaryStorageServiceFactory_WriteStream, cancellationToken))
            {
                var size = stream.Length;
                _memoryMappedInfo = _service.CreateTemporaryStorage(size);
                using var viewStream = _memoryMappedInfo.CreateWritableStream();

                using var pooledObject = SharedPools.ByteArray.GetPooledObject();
                var buffer = pooledObject.Object;
                while (true)
                {
                    var count = stream.Read(buffer, 0, buffer.Length);
                    if (count == 0)
                        break;

                    viewStream.Write(buffer, 0, count);
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
