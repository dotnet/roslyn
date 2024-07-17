// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Runtime;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Host;

internal partial class TemporaryStorageService
{
    /// <summary>
    /// Our own abstraction on top of memory map file so that we can have shared views over mmf files. 
    /// Otherwise, each view has minimum size of 64K due to requirement forced by windows.
    /// 
    /// most of our view will have short lifetime, but there are cases where view might live a bit longer such as
    /// metadata dll shadow copy. shared view will help those cases.
    /// </summary>
    /// <remarks>
    /// <para>This class and its nested types have familiar APIs and predictable behavior when used in other code, but
    /// are non-trivial to work on.</para>
    /// </remarks>
    internal sealed class MemoryMappedInfo(MemoryMappedFile memoryMappedFile, string? name, long offset, long size)
    {
        /// <summary>
        /// The memory mapped file.
        /// </summary>
        public readonly MemoryMappedFile MemoryMappedFile = memoryMappedFile;

        /// <summary>
        /// A weak reference to a read-only view for the memory mapped file.
        /// </summary>
        /// <remarks>
        /// <para>This holds a weak counted reference to current <see cref="MemoryMappedViewAccessor"/>, which allows
        /// additional accessors for the same address space to be obtained up until the point when no external code is
        /// using it. When the memory is no longer being used by any <see cref="MemoryMappedViewUnmanagedMemoryStream"/>
        /// objects, the view of the memory mapped file is unmapped, making the process address space it previously
        /// claimed available for other purposes. If/when it is needed again, a new view is created.</para>
        ///
        /// <para>This view is read-only, so it is only used by <see cref="CreateReadableStream"/>.</para>
        /// </remarks>
        private ReferenceCountedDisposable<MemoryMappedViewAccessor>.WeakReference _weakReadAccessor;

        public static MemoryMappedInfo CreateNew(string? name, long size)
            => new(MemoryMappedFile.CreateNew(name, size), name, offset: 0, size);

        /// <summary>
        /// The name of the memory mapped file. Non null on systems that support named memory mapped files, null
        /// otherwise..
        /// </summary>
        public string? Name { get; } = name;

        /// <summary>
        /// The offset into the memory mapped file of the region described by the current
        /// <see cref="MemoryMappedInfo"/>.
        /// </summary>
        public long Offset { get; } = offset;

        /// <summary>
        /// The size of the region of the memory mapped file described by the current
        /// <see cref="MemoryMappedInfo"/>.
        /// </summary>
        public long Size { get; } = size;

        /// <summary>
        /// Caller is responsible for disposing the returned stream.
        /// multiple call of this will not increase VM.
        /// </summary>
        public UnmanagedMemoryStream CreateReadableStream()
        {
            // Note: TryAddReference behaves according to its documentation even if the target object has been disposed.
            // If it returns non-null, then the object will not be disposed before the returned reference is disposed
            // (see comments on _memoryMappedFile and TryAddReference).
            var streamAccessor = _weakReadAccessor.TryAddReference();
            if (streamAccessor == null)
            {
                var rawAccessor = RunWithCompactingGCFallback(
                    function: static info => info.MemoryMappedFile.CreateViewAccessor(info.Offset, info.Size, MemoryMappedFileAccess.Read),
                    argument: this);
                streamAccessor = new ReferenceCountedDisposable<MemoryMappedViewAccessor>(rawAccessor);
                _weakReadAccessor = new ReferenceCountedDisposable<MemoryMappedViewAccessor>.WeakReference(streamAccessor);
            }

            Debug.Assert(streamAccessor.Target.CanRead);
            return new MemoryMappedViewUnmanagedMemoryStream(streamAccessor, Size);
        }

        /// <summary>
        /// Caller is responsible for disposing the returned stream.
        /// multiple call of this will increase VM.
        /// </summary>
        public Stream CreateWritableStream()
        {
            return RunWithCompactingGCFallback(
                function: static info => info.MemoryMappedFile.CreateViewStream(info.Offset, info.Size, MemoryMappedFileAccess.Write),
                argument: this);
        }

        /// <summary>
        /// Run a function which may fail with an <see cref="IOException"/> if not enough memory is available to satisfy
        /// the request. In this case, a full compacting GC pass is forced and the function is attempted again.
        /// </summary>
        /// <remarks>
        /// <para><see cref="MemoryMappedFile.CreateViewAccessor(long, long, MemoryMappedFileAccess)"/> and <see
        /// cref="MemoryMappedFile.CreateViewStream(long, long, MemoryMappedFileAccess)"/> will use a native memory map,
        /// which can't trigger a GC. In this case, we'd otherwise crash with OOM, so we don't care about creating a UI
        /// delay with a full forced compacting GC. If it crashes the second try, it means we're legitimately out of
        /// resources.</para>
        /// </remarks>
        /// <typeparam name="TArg">The type of argument to pass to the callback.</typeparam>
        /// <typeparam name="T">The type returned by the function.</typeparam>
        /// <param name="function">The function to execute.</param>
        /// <param name="argument">The argument to pass to the function.</param>
        /// <returns>The value returned by <paramref name="function"/>.</returns>
        private static T RunWithCompactingGCFallback<TArg, T>(TArg argument, Func<TArg, T> function)
        {
            try
            {
                return function(argument);
            }
            catch (IOException)
            {
                ForceCompactingGC();
                return function(argument);
            }
        }

        private static void ForceCompactingGC()
        {
            // repeated GC.Collect / WaitForPendingFinalizers till memory freed delta is super small, ignore the return value
            GC.GetTotalMemory(forceFullCollection: true);

            // compact the LOH
            GCSettings.LargeObjectHeapCompactionMode = GCLargeObjectHeapCompactionMode.CompactOnce;
            GC.Collect();
        }

        private sealed unsafe class MemoryMappedViewUnmanagedMemoryStream(
            ReferenceCountedDisposable<MemoryMappedViewAccessor> accessor,
            long length) : UnmanagedMemoryStream(
                (byte*)accessor.Target.SafeMemoryMappedViewHandle.DangerousGetHandle() + accessor.Target.PointerOffset,
                length)
        {
            protected override void Dispose(bool disposing)
            {
                base.Dispose(disposing);
                if (disposing)
                    accessor.Dispose();
            }
        }
    }
}
