// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.Win32.SafeHandles;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// Represents data read from a stream.
    /// </summary>
    /// <remarks>
    /// Uses memory map to load data from streams backed by files that are bigger than <see cref="MemoryMapThreshold"/>.
    /// </remarks>
    internal sealed class StreamMemoryBlockProvider : MemoryBlockProvider
    {
        // We're trying to balance total VM usage (which is a minimum of 64KB for a memory mapped file) 
        // with private working set (since heap memory will be backed by the paging file and non-sharable).
        // Internal for testing.
        internal const int MemoryMapThreshold = 16 * 1024;

        private readonly Stream stream;
        private readonly bool leaveOpen;
        private MemoryMappedFile lazyMemoryMap;

        public StreamMemoryBlockProvider(Stream stream, bool leaveOpen)
        {
            Debug.Assert(stream.CanSeek && stream.CanRead);
            this.stream = stream;
            this.leaveOpen = leaveOpen;
        }

        protected override void Dispose(bool disposing)
        {
            if (!leaveOpen)
            {
                stream.Dispose();
            }

            var memoryMap = lazyMemoryMap;
            if (memoryMap != null)
            {
                memoryMap.Dispose();
            }
        }

        public override int Size
        {
            get
            {
                return (int)Math.Min(int.MaxValue, stream.Length);
            }
        }

        internal static NativeHeapMemoryBlock ReadMemoryBlock(Stream stream, int start, int size)
        {
            var fileStream = stream as FileStream;
            if (fileStream != null)
            {
                var block = new NativeHeapMemoryBlock(size);
                bool fault = true;
                try
                {
                    stream.Seek(start, SeekOrigin.Begin);
                    int bytesRead;
                    if (!ReadFile(fileStream.SafeFileHandle, block.Pointer, size, out bytesRead, IntPtr.Zero) || bytesRead != size)
                    {
                        throw new IOException(CodeAnalysisResources.UnableToReadMetadataFile, Marshal.GetLastWin32Error());
                    }

                    fault = false;
                }
                finally
                {
                    if (fault)
                    {
                        block.Dispose();
                    }
                }

                return block;
            }
            else
            {
                var block = new NativeHeapMemoryBlock(size);

                bool fault = true;
                try
                {
                    stream.Seek(start, SeekOrigin.Begin);
                    stream.CopyTo(block.Pointer, size);
                    fault = false;
                }
                finally
                {
                    if (fault)
                    {
                        block.Dispose();
                    }
                }

                return block;
            }
        }

        /// <exception cref="IOException">Error while reading from the stream.</exception>
        protected override AbstractMemoryBlock GetMemoryBlockImpl(int start, int size)
        {
            var fileStream = stream as FileStream;
            if (fileStream != null && size > MemoryMapThreshold)
            {
                return new MemoryMappedFileBlock(CreateMemoryMapAccessor(fileStream, start, size));
            }

            return ReadMemoryBlock(stream, start, size);
        }

        private MemoryMappedViewAccessor CreateMemoryMapAccessor(FileStream fileStream, int start, int size)
        {
            try
            {
                if (lazyMemoryMap == null)
                {
                    // leave the underlying stream open. It will be closed by the Dispose method.
                    var newMemoryMap = MemoryMappedFile.CreateFromFile(
                        fileStream, null, 0, MemoryMappedFileAccess.Read, default(MemoryMappedFileSecurity), HandleInheritability.None, leaveOpen: true);

                    if (Interlocked.CompareExchange(ref lazyMemoryMap, newMemoryMap, null) != null)
                    {
                        newMemoryMap.Dispose();
                    }
                }

                return lazyMemoryMap.CreateViewAccessor(start, size, MemoryMappedFileAccess.Read);
            }
            catch (Exception e)
            {
                throw new IOException(CodeAnalysisResources.UnableToReadMetadataFile, e);
            }
        }

        [DllImport(@"kernel32", CharSet = CharSet.Auto, SetLastError = true)]
        static extern unsafe bool ReadFile(
             SafeFileHandle hFile,      // handle to file
             IntPtr pBuffer,        // data buffer, should be fixed
             int NumberOfBytesToRead,  // number of bytes to read
             out int pNumberOfBytesRead,  // number of bytes read
             IntPtr lpOverlapped // should be fixed, if not null
        );
    }
}
