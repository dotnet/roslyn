// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.IO;
using Roslyn.Test.Utilities;
using Xunit;
using System.Collections.Generic;
using System.Reflection.PortableExecutable;
using ProprietaryTestResources = Microsoft.CodeAnalysis.Test.Resources.Proprietary;

namespace Microsoft.CodeAnalysis.UnitTests
{
    public class ModuleMetadataTests : TestBase
    {
        private char SystemDrive = Environment.GetFolderPath(Environment.SpecialFolder.Windows)[0];

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
            var assembly = TestResources.MetadataTests.Basic.Members;
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

            Assert.Throws<ArgumentException>(() => ModuleMetadata.CreateFromImage(default(ImmutableArray<byte>)));
            Assert.Throws<ArgumentException>(() => ModuleMetadata.CreateFromImage(default(IEnumerable<byte>)));
            Assert.Throws<ArgumentException>(() => ModuleMetadata.CreateFromImage(default(byte[])));

            // It's not particularly important that this not throw. The parsing of the metadata is now lazy, and the result is that an exception
            // will be thrown when something tugs on the metadata later.
            Assert.DoesNotThrow(() => ModuleMetadata.CreateFromImage(TestResources.MetadataTests.Invalid.EmptyModuleTable));
        }

        [Fact]
        public void CreateFromImageStream()
        {
            Assert.Throws<ArgumentNullException>(() => ModuleMetadata.CreateFromImageStream(peStream: null));
            Assert.Throws<ArgumentException>(() => ModuleMetadata.CreateFromImageStream(new TestStream(canRead: false, canSeek: true)));
            Assert.Throws<ArgumentException>(() => ModuleMetadata.CreateFromImageStream(new TestStream(canRead: true, canSeek: false)));
        }

        [Fact]
        public void CreateFromFile()
        {
            Assert.Throws<ArgumentNullException>(() => MetadataFileFactory.CreateModule((string)null));
            Assert.Throws<ArgumentException>(() => MetadataFileFactory.CreateModule(""));
            Assert.Throws<ArgumentException>(() => MetadataFileFactory.CreateModule("foo.dll"));
            Assert.Throws<ArgumentException>(() => MetadataFileFactory.CreateModule("c:foo.dll"));
            Assert.Throws<ArgumentException>(() => MetadataFileFactory.CreateModule(@".\foo.dll"));
            Assert.Throws<ArgumentException>(() => MetadataFileFactory.CreateModule(@"\foo.dll"));
            Assert.Throws<ArgumentException>(() => MetadataFileFactory.CreateModule(@"http://foo.bar"));
            Assert.Throws<ArgumentException>(() => MetadataFileFactory.CreateModule(@"c:\*"));
            Assert.Throws<ArgumentException>(() => MetadataFileFactory.CreateModule(@"\\.\COM1"));

            Assert.Throws<FileNotFoundException>(() => MetadataFileFactory.CreateModule(SystemDrive + @":\file_that_does_not_exists.dll"));
            Assert.Throws<FileNotFoundException>(() => MetadataFileFactory.CreateModule(SystemDrive + @":\directory_that_does_not_exists\file_that_does_not_exists.dll"));
            Assert.Throws<PathTooLongException>(() => MetadataFileFactory.CreateModule(SystemDrive + @":\" + new string('x', 1000)));
            Assert.Throws<IOException>(() => MetadataFileFactory.CreateModule(Environment.GetFolderPath(Environment.SpecialFolder.Windows)));
        }

        [Fact]
        public void Disposal()
        {
            var md = ModuleMetadata.CreateFromImage(ProprietaryTestResources.NetFX.v4_0_30319.mscorlib);
            md.Dispose();
            Assert.Throws<ObjectDisposedException>(() => md.Module);
            md.Dispose();
        }

        [Fact]
        public void ImageOwnership()
        {
            var m = ModuleMetadata.CreateFromImage(ProprietaryTestResources.NetFX.v4_0_30319.mscorlib);
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
    }
}
