// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Composition;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Internal.Log;
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
        internal class TemporaryStorageService : ITemporaryStorageService
        {
            private readonly ITextFactoryService _textFactory;
            private readonly MemoryMappedFileManager _memoryMappedFileManager = new MemoryMappedFileManager();

            public TemporaryStorageService(ITextFactoryService textFactory)
            {
                _textFactory = textFactory;
            }

            public ITemporaryTextStorage CreateTemporaryTextStorage(CancellationToken cancellationToken)
            {
                return new TemporaryTextStorage(this);
            }

            public ITemporaryStreamStorage CreateTemporaryStreamStorage(CancellationToken cancellationToken)
            {
                return new TemporaryStreamStorage(this);
            }

            private class TemporaryTextStorage : ITemporaryTextStorage
            {
                private readonly TemporaryStorageService _service;
                private Encoding _encoding;
                private MemoryMappedInfo _memoryMappedInfo;

                public TemporaryTextStorage(TemporaryStorageService service)
                {
                    _service = service;
                }

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
                        _memoryMappedInfo = _service._memoryMappedFileManager.CreateViewInfo(size);

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

                private unsafe class DirectMemoryAccessStreamReader : TextReader
                {
                    private char* _position;
                    private readonly char* _end;

                    public DirectMemoryAccessStreamReader(char* src, int length)
                    {
                        Debug.Assert(src != null);
                        Debug.Assert(length >= 0);

                        _position = src;
                        _end = _position + length;
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

            private class TemporaryStreamStorage : ITemporaryStreamStorage
            {
                private readonly TemporaryStorageService _service;
                private MemoryMappedInfo _memoryMappedInfo;

                public TemporaryStreamStorage(TemporaryStorageService service)
                {
                    _service = service;
                }

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
                        _memoryMappedInfo = _service._memoryMappedFileManager.CreateViewInfo(size);
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
    }
}

