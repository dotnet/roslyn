// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
        public unsafe void CreateFromImage()
        {
            Assert.Throws<ArgumentNullException>(() => ModuleMetadata.CreateFromImage(IntPtr.Zero, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => { fixed (byte* ptr = new byte[] { 1, 2, 3 }) ModuleMetadata.CreateFromImage((IntPtr)ptr, 0); });
            Assert.Throws<ArgumentOutOfRangeException>(() => { fixed (byte* ptr = new byte[] { 1, 2, 3 }) ModuleMetadata.CreateFromImage((IntPtr)ptr, -1); });

            Assert.Throws<ArgumentNullException>(() => ModuleMetadata.CreateFromImage(default(ImmutableArray<byte>)));
            Assert.Throws<ArgumentNullException>(() => ModuleMetadata.CreateFromImage(default(IEnumerable<byte>)));
            Assert.Throws<ArgumentNullException>(() => ModuleMetadata.CreateFromImage(default(byte[])));

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
            var md = ModuleMetadata.CreateFromImage(TestResources.NetFX.v4_0_30319.mscorlib);
            md.Dispose();
            Assert.Throws<ObjectDisposedException>(() => md.Module);
            md.Dispose();
        }

        [Fact]
        public void ImageOwnership()
        {
            var m = ModuleMetadata.CreateFromImage(TestResources.NetFX.v4_0_30319.mscorlib);
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
    }
}
