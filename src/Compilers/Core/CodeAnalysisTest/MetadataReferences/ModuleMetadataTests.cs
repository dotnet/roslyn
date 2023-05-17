// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Immutable;
using System.IO;
using Roslyn.Test.Utilities;
using Xunit;
using System.Collections.Generic;
using System.Reflection.PortableExecutable;

namespace Microsoft.CodeAnalysis.UnitTests
{
    public class ModuleMetadataTests : TestBase
    {
        [Fact]
        public unsafe void CreateFromMetadata_Errors()
        {
            Assert.Throws<ArgumentNullException>(() => ModuleMetadata.CreateFromMetadata(IntPtr.Zero, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => { fixed (byte* ptr = new byte[] { 1, 2, 3 }) ModuleMetadata.CreateFromMetadata((IntPtr)ptr, 0); });
            Assert.Throws<ArgumentOutOfRangeException>(() => { fixed (byte* ptr = new byte[] { 1, 2, 3 }) ModuleMetadata.CreateFromMetadata((IntPtr)ptr, -1); });

            fixed (byte* ptr = new byte[] { 1, 2, 3 })
            {
                var metadata = ModuleMetadata.CreateFromMetadata((IntPtr)ptr, 3);
                Assert.Throws<BadImageFormatException>(() => metadata.GetModuleNames());
            }
        }

        [Fact]
        public unsafe void CreateFromMetadata_Assembly()
        {
            var assembly = TestResources.Basic.Members;
            PEHeaders h = new PEHeaders(new MemoryStream(assembly));

            fixed (byte* ptr = &assembly[h.MetadataStartOffset])
            {
                var metadata = ModuleMetadata.CreateFromMetadata((IntPtr)ptr, h.MetadataSize);
                Assert.Equal(new AssemblyIdentity("Members"), metadata.Module.ReadAssemblyIdentityOrThrow());
            }
        }

        [Fact]
        public unsafe void CreateFromMetadata_Module()
        {
            var netModule = TestResources.MetadataTests.NetModule01.ModuleCS00;
            PEHeaders h = new PEHeaders(new MemoryStream(netModule));

            fixed (byte* ptr = &netModule[h.MetadataStartOffset])
            {
                ModuleMetadata.CreateFromMetadata((IntPtr)ptr, h.MetadataSize);
            }
        }

        [Fact]
        public unsafe void CreateFromMetadata_Assembly_Stream()
        {
            var assembly = TestResources.Basic.Members;
            PEHeaders h = new PEHeaders(new MemoryStream(assembly));

            fixed (byte* ptr = &assembly[h.MetadataStartOffset])
            {
                var stream = new UnmanagedMemoryStream(ptr, h.MetadataSize);
                var metadata = ModuleMetadata.CreateFromMetadata((IntPtr)stream.PositionPointer, (int)stream.Length, stream.Dispose);
                Assert.Equal(new AssemblyIdentity("Members"), metadata.Module.ReadAssemblyIdentityOrThrow());
            }
        }

        [Fact]
        public unsafe void CreateFromMetadata_Module_Stream()
        {
            var netModule = TestResources.MetadataTests.NetModule01.ModuleCS00;
            PEHeaders h = new PEHeaders(new MemoryStream(netModule));

            fixed (byte* ptr = &netModule[h.MetadataStartOffset])
            {
                var stream = new UnmanagedMemoryStream(ptr, h.MetadataSize);
                ModuleMetadata.CreateFromMetadata((IntPtr)stream.PositionPointer, (int)stream.Length, stream.Dispose);
            }
        }

        [Fact]
        public unsafe void CreateFromImage()
        {
            Assert.Throws<ArgumentNullException>(() => ModuleMetadata.CreateFromImage(IntPtr.Zero, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => { fixed (byte* ptr = new byte[] { 1, 2, 3 }) ModuleMetadata.CreateFromImage((IntPtr)ptr, 0); });
            Assert.Throws<ArgumentOutOfRangeException>(() => { fixed (byte* ptr = new byte[] { 1, 2, 3 }) ModuleMetadata.CreateFromImage((IntPtr)ptr, -1); });

            Assert.Throws<ArgumentNullException>(() => ModuleMetadata.CreateFromImage(default(ImmutableArray<byte>)));

            IEnumerable<byte> enumerableImage = null;
            Assert.Throws<ArgumentNullException>(() => ModuleMetadata.CreateFromImage(enumerableImage));

            byte[] arrayImage = null;
            Assert.Throws<ArgumentNullException>(() => ModuleMetadata.CreateFromImage(arrayImage));

            // It's not particularly important that this not throw. The parsing of the metadata is now lazy, and the result is that an exception
            // will be thrown when something tugs on the metadata later.
            ModuleMetadata.CreateFromImage(TestResources.MetadataTests.Invalid.EmptyModuleTable);
        }

        [Fact]
        public void CreateFromImageStream()
        {
            Assert.Throws<ArgumentNullException>(() => ModuleMetadata.CreateFromStream(peStream: null));
            Assert.Throws<ArgumentException>(() => ModuleMetadata.CreateFromStream(new TestStream(canRead: false, canSeek: true)));
            Assert.Throws<ArgumentException>(() => ModuleMetadata.CreateFromStream(new TestStream(canRead: true, canSeek: false)));
        }

        [ConditionalFact(typeof(WindowsDesktopOnly), Reason = "https://github.com/dotnet/roslyn/issues/30289")]
        public void CreateFromFile()
        {
            Assert.Throws<ArgumentNullException>(() => ModuleMetadata.CreateFromFile((string)null));
            Assert.Throws<ArgumentException>(() => ModuleMetadata.CreateFromFile(""));
            Assert.Throws<ArgumentException>(() => ModuleMetadata.CreateFromFile(@"c:\*"));

            char systemDrive = Environment.GetFolderPath(Environment.SpecialFolder.Windows)[0];
            Assert.Throws<IOException>(() => ModuleMetadata.CreateFromFile(@"http://goo.bar"));
            Assert.Throws<FileNotFoundException>(() => ModuleMetadata.CreateFromFile(systemDrive + @":\file_that_does_not_exists.dll"));
            Assert.Throws<FileNotFoundException>(() => ModuleMetadata.CreateFromFile(systemDrive + @":\directory_that_does_not_exists\file_that_does_not_exists.dll"));
            Assert.Throws<PathTooLongException>(() => ModuleMetadata.CreateFromFile(systemDrive + @":\" + new string('x', 1000)));
            Assert.Throws<IOException>(() => ModuleMetadata.CreateFromFile(Environment.GetFolderPath(Environment.SpecialFolder.Windows)));
        }

        [Fact]
        public void Disposal()
        {
            var md = ModuleMetadata.CreateFromImage(TestMetadata.ResourcesNet451.mscorlib);
            md.Dispose();
            Assert.Throws<ObjectDisposedException>(() => md.Module);
            md.Dispose();
        }

        [Fact]
        public void ImageOwnership()
        {
            var m = ModuleMetadata.CreateFromImage(TestMetadata.ResourcesNet451.mscorlib);
            var copy1 = m.Copy();
            var copy2 = copy1.Copy();

            Assert.True(m.IsImageOwner, "Metadata should own the image");
            Assert.False(copy1.IsImageOwner, "Copy should not own the image");
            Assert.False(copy2.IsImageOwner, "Copy should not own the image");

            // the image is shared
            Assert.NotNull(m.Module);
            Assert.Equal(copy1.Module, m.Module);
            Assert.Equal(copy2.Module, m.Module);

            // dispose copy - no effect on the underlying image or other copies:
            copy1.Dispose();
            Assert.Throws<ObjectDisposedException>(() => copy1.Module);
            Assert.NotNull(m.Module);
            Assert.NotNull(copy2.Module);

            // dispose the owner - all copies are invalidated:
            m.Dispose();
            Assert.Throws<ObjectDisposedException>(() => m.Module);
            Assert.Throws<ObjectDisposedException>(() => copy1.Module);
            Assert.Throws<ObjectDisposedException>(() => copy2.Module);
        }

        [Fact, WorkItem(2988, "https://github.com/dotnet/roslyn/issues/2988")]
        public void EmptyStream()
        {
            ModuleMetadata.CreateFromStream(new MemoryStream(), PEStreamOptions.Default);
            Assert.Throws<BadImageFormatException>(() => ModuleMetadata.CreateFromStream(new MemoryStream(), PEStreamOptions.PrefetchMetadata));
            Assert.Throws<BadImageFormatException>(() => ModuleMetadata.CreateFromStream(new MemoryStream(), PEStreamOptions.PrefetchMetadata | PEStreamOptions.PrefetchEntireImage));
        }

        [Fact]
        public unsafe void CreateFromUnmanagedMemoryStream_LeaveOpenFalse()
        {
            var assembly = TestResources.Basic.Members;
            fixed (byte* assemblyPtr = assembly)
            {
                var disposed = false;
                var seeked = false;
                var stream = new MockUnmanagedMemoryStream(assemblyPtr, assembly.LongLength)
                {
                    OnDispose = _ => disposed = true,
                    OnSeek = (_, _) => seeked = true,
                };

                var metadata = ModuleMetadata.CreateFromStream(stream, leaveOpen: false);

                Assert.Equal(new AssemblyIdentity("Members"), metadata.Module.ReadAssemblyIdentityOrThrow());

                // Disposing the metadata should dispose the stream.
                metadata.Dispose();
                Assert.True(disposed);

                // We should have never seeked.  The pointer should have been used directly.
                Assert.False(seeked);
            }
        }

        [Fact]
        public unsafe void CreateFromMetadata_Assembly_Stream_DisposeOwnerTrue()
        {
            var assembly = TestResources.Basic.Members;
            PEHeaders h = new PEHeaders(new MemoryStream(assembly));

            fixed (byte* ptr = &assembly[h.MetadataStartOffset])
            {
                var disposed = false;
                var seeked = false;
                var stream = new MockUnmanagedMemoryStream(ptr, h.MetadataSize)
                {
                    OnDispose = _ => disposed = true,
                    OnSeek = (_, _) => seeked = true,
                };

                var metadata = ModuleMetadata.CreateFromMetadata((IntPtr)stream.PositionPointer, (int)stream.Length, stream.Dispose);
                Assert.Equal(new AssemblyIdentity("Members"), metadata.Module.ReadAssemblyIdentityOrThrow());

                // Disposing the metadata should dispose the stream.
                metadata.Dispose();
                Assert.True(disposed);

                // We should have never seeked.  The pointer should have been used directly.
                Assert.False(seeked);
            }
        }

        [Fact]
        public unsafe void CreateFromUnmanagedMemoryStream_LeaveOpenTrue()
        {
            var assembly = TestResources.Basic.Members;
            fixed (byte* assemblyPtr = assembly)
            {
                var disposed = false;
                var seeked = false;
                var stream = new MockUnmanagedMemoryStream(assemblyPtr, assembly.LongLength)
                {
                    OnDispose = _ => disposed = true,
                    OnSeek = (_, _) => seeked = true,
                };

                var metadata = ModuleMetadata.CreateFromStream(stream, leaveOpen: true);

                Assert.Equal(new AssemblyIdentity("Members"), metadata.Module.ReadAssemblyIdentityOrThrow());

                // Disposing the metadata should not dispose the stream.
                metadata.Dispose();
                Assert.False(disposed);

                stream.Dispose();
                Assert.True(disposed);

                // We should have never seeked.  The pointer should have been used directly.
                Assert.False(seeked);
            }
        }

        [Fact]
        public unsafe void CreateFromMetadata_Assembly_Stream_DisposeOwnerFalse()
        {
            var assembly = TestResources.Basic.Members;
            PEHeaders h = new PEHeaders(new MemoryStream(assembly));

            fixed (byte* ptr = &assembly[h.MetadataStartOffset])
            {
                var disposed = false;
                var seeked = false;
                var stream = new MockUnmanagedMemoryStream(ptr, h.MetadataSize)
                {
                    OnDispose = _ => disposed = true,
                    OnSeek = (_, _) => seeked = true,
                };

                var metadata = ModuleMetadata.CreateFromMetadata((IntPtr)stream.PositionPointer, (int)stream.Length);
                Assert.Equal(new AssemblyIdentity("Members"), metadata.Module.ReadAssemblyIdentityOrThrow());

                // Disposing the metadata should not dispose the stream.
                metadata.Dispose();
                Assert.False(disposed);

                stream.Dispose();
                Assert.True(disposed);

                // We should have never seeked.  The pointer should have been used directly.
                Assert.False(seeked);
            }
        }

        [Theory]
        [InlineData(PEStreamOptions.PrefetchEntireImage)]
        [InlineData(PEStreamOptions.PrefetchMetadata)]
        [InlineData(PEStreamOptions.PrefetchEntireImage | PEStreamOptions.PrefetchMetadata)]
        public unsafe void CreateFromUnmanagedMemoryStream_Prefetch_LeaveOpenFalse(PEStreamOptions options)
        {
            var assembly = TestResources.Basic.Members;
            fixed (byte* assemblyPtr = assembly)
            {
                var disposed = false;
                var seeked = false;
                var stream = new MockUnmanagedMemoryStream(assemblyPtr, assembly.LongLength)
                {
                    OnDispose = _ => disposed = true,
                    OnSeek = (_, _) => seeked = true,
                };

                var metadata = ModuleMetadata.CreateFromStream(stream, options);

                Assert.Equal(new AssemblyIdentity("Members"), metadata.Module.ReadAssemblyIdentityOrThrow());

                // Disposing the metadata should dispose the stream.
                metadata.Dispose();
                Assert.True(disposed);

                // We should have seeked.  This stream will be viewed as a normal stream since we're prefetching
                // everything.
                Assert.True(seeked);
            }
        }

        [Theory]
        [InlineData(PEStreamOptions.PrefetchEntireImage)]
        [InlineData(PEStreamOptions.PrefetchMetadata)]
        [InlineData(PEStreamOptions.PrefetchEntireImage | PEStreamOptions.PrefetchMetadata)]
        public unsafe void CreateFromUnmanagedMemoryStream_Prefetcha_LeaveOpenTrue(PEStreamOptions options)
        {
            var assembly = TestResources.Basic.Members;
            fixed (byte* assemblyPtr = assembly)
            {
                var disposed = false;
                var seeked = false;
                var stream = new MockUnmanagedMemoryStream(assemblyPtr, assembly.LongLength)
                {
                    OnDispose = _ => disposed = true,
                    OnSeek = (_, _) => seeked = true,
                };

                var metadata = ModuleMetadata.CreateFromStream(stream, options | PEStreamOptions.LeaveOpen);

                Assert.Equal(new AssemblyIdentity("Members"), metadata.Module.ReadAssemblyIdentityOrThrow());

                // Disposing the metadata should not dispose the stream.
                metadata.Dispose();
                Assert.False(disposed);

                stream.Dispose();
                Assert.True(disposed);

                // We should have seeked.  This stream will be viewed as a normal stream since we're prefetching
                // everything.
                Assert.True(seeked);
            }
        }

        /// <summary>
        /// Only test in 64-bit process. <see cref="UnmanagedMemoryStream"/> throws if the given length is greater than the size of the available address space.
        /// </summary>
        [ConditionalFact(typeof(Bitness64))]
        public unsafe void CreateFromUnmanagedMemoryStream_LargeIntSize()
        {
            var assembly = TestResources.Basic.Members;
            fixed (byte* assemblyPtr = assembly)
            {
                // ensure that having an extremely large stream is not a problem (e.g. that we don't wrap the int around
                // to be a negative size).
                var disposed = false;
                var seeked = false;
                var stream = new MockUnmanagedMemoryStream(assemblyPtr, (long)int.MaxValue + 1)
                {
                    OnDispose = _ => disposed = true,
                    OnSeek = (_, _) => seeked = true,
                };

                var metadata = ModuleMetadata.CreateFromStream(stream, leaveOpen: false);

                Assert.Equal(new AssemblyIdentity("Members"), metadata.Module.ReadAssemblyIdentityOrThrow());

                // Disposing the metadata should dispose the stream.
                metadata.Dispose();
                Assert.True(disposed);

                // We should have not seeked.  This stream will still be read as a direct memory block.
                Assert.False(seeked);
            }
        }

        private class MockUnmanagedMemoryStream : UnmanagedMemoryStream
        {
            public unsafe MockUnmanagedMemoryStream(byte* pointer, long length) : base(pointer, length)
            {
            }

            public Action<bool> OnDispose;
            public Action<long, SeekOrigin> OnSeek;

            protected override void Dispose(bool disposing)
            {
                OnDispose?.Invoke(disposing);
                base.Dispose(disposing);
            }

            public override long Seek(long offset, SeekOrigin loc)
            {
                OnSeek?.Invoke(offset, loc);
                return base.Seek(offset, loc);
            }
        }
    }
}
