// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Composition;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Host
{
    [ExportWorkspaceServiceFactory(typeof(ITemporaryStorageService), ServiceLayer.Default), Shared]
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
            private readonly ITextFactoryService textFactory;
            private readonly MemoryMappedFileManager memoryMappedFileManager = new MemoryMappedFileManager();

            public TemporaryStorageService(ITextFactoryService textFactory)
            {
                this.textFactory = textFactory;
            }

            public ITemporaryStorage CreateTemporaryStorage(CancellationToken cancellationToken)
            {
                return new TemporaryStorage(this);
            }

            private class TemporaryStorage : ITemporaryStorage
            {
                private readonly TemporaryStorageService service;
                private MemoryMappedInfo memoryMappedInfo;

                public TemporaryStorage(TemporaryStorageService service)
                {
                    this.service = service;
                }

                public void Dispose()
                {
                    if (memoryMappedInfo != null)
                    {
                        // Destructors of SafeHandle and FileStream in MemoryMappedFile
                        // will eventually release resources if this Dispose is not called
                        // explicitly
                        memoryMappedInfo.Dispose();
                        memoryMappedInfo = null;
                    }
                }

                public SourceText ReadText(CancellationToken cancellationToken)
                {
                    if (memoryMappedInfo == null)
                    {
                        throw new InvalidOperationException();
                    }

                    using (Logger.LogBlock(FunctionId.TemporaryStorageServiceFactory_ReadText, cancellationToken))
                    {
                        using (var stream = memoryMappedInfo.CreateReadableStream())
                        {
                            return this.service.textFactory.CreateText(stream, Encoding.Unicode, cancellationToken);
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
                    if (memoryMappedInfo != null)
                    {
                        throw new InvalidOperationException();
                    }

                    using (Logger.LogBlock(FunctionId.TemporaryStorageServiceFactory_WriteText, cancellationToken))
                    {
                        var size = Encoding.Unicode.GetMaxByteCount(text.Length);
                        memoryMappedInfo = service.memoryMappedFileManager.CreateViewInfo(size);

                        using (var stream = memoryMappedInfo.CreateWritableStream())
                        {
                            // PERF: Don't call text.Write(writer) directly since it can cause multiple large string
                            // allocations from String.Substring.  Instead use one of our pooled char[] buffers.
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

                public Stream ReadStream(CancellationToken cancellationToken)
                {
                    if (memoryMappedInfo == null)
                    {
                        throw new InvalidOperationException();
                    }

                    using (Logger.LogBlock(FunctionId.TemporaryStorageServiceFactory_ReadStream, cancellationToken))
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        return memoryMappedInfo.CreateReadableStream();
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
                    if (memoryMappedInfo != null)
                    {
                        throw new InvalidOperationException(WorkspacesResources.TemporaryStorageCannotBeWrittenMultipleTimes);
                    }

                    if (stream.Length == 0)
                    {
                        throw new ArgumentOutOfRangeException();
                    }

                    using (Logger.LogBlock(FunctionId.TemporaryStorageServiceFactory_WriteStream, cancellationToken))
                    {
                        var size = stream.Length;
                        memoryMappedInfo = service.memoryMappedFileManager.CreateViewInfo(size);
                        using (var viewStream = memoryMappedInfo.CreateWritableStream())
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
