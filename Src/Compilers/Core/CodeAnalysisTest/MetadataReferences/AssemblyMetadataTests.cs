// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using Roslyn.Test.Utilities;
using Xunit;
using System.Collections.Generic;

namespace Microsoft.CodeAnalysis.UnitTests
{
    public class AssemblyMetadataTests : TestBase
    {
        [Fact]
        public void Ctor_Errors()
        {
            Assert.Throws<ArgumentException>(() => AssemblyMetadata.CreateFromImage(default(ImmutableArray<byte>)));
            Assert.Throws<ArgumentException>(() => AssemblyMetadata.CreateFromImage(default(IEnumerable<byte>)));
            Assert.Throws<ArgumentException>(() => AssemblyMetadata.CreateFromImage(default(byte[])));
            Assert.Throws<ArgumentNullException>(() => AssemblyMetadata.Create((ModuleMetadata)null));
            Assert.Throws<ArgumentException>(() => AssemblyMetadata.Create(default(ImmutableArray<ModuleMetadata>)));
            Assert.Throws<ArgumentException>(() => AssemblyMetadata.Create(ImmutableArray.Create<ModuleMetadata>()));

            var m1 = ModuleMetadata.CreateFromImage(TestResources.SymbolsTests.MultiModule.MultiModule);
            var m2 = ModuleMetadata.CreateFromImage(TestResources.SymbolsTests.MultiModule.mod2);
            var m3 = ModuleMetadata.CreateFromImage(TestResources.SymbolsTests.MultiModule.mod3);

            Assert.Throws<ArgumentException>(() => AssemblyMetadata.Create(m1, m2.Copy(), m3));
            Assert.Throws<ArgumentException>(() => AssemblyMetadata.Create(new List<ModuleMetadata>(new ModuleMetadata[] { m1.Copy(), m2.Copy(), m3.Copy() })));
            Assert.Throws<ArgumentNullException>(() => AssemblyMetadata.Create(ImmutableArray.Create(m1, m2, null)));
            Assert.Throws<ArgumentNullException>(() => AssemblyMetadata.Create(ImmutableArray.Create((ModuleMetadata)null)));

            Assert.Throws<ArgumentNullException>(() => MetadataFileFactory.CreateAssembly((string)null));
            Assert.Throws<ArgumentException>(() => MetadataFileFactory.CreateAssembly("foo.dll"));
            Assert.Throws<ArgumentException>(() => MetadataFileFactory.CreateAssembly("c:foo.dll"));
            Assert.Throws<ArgumentException>(() => MetadataFileFactory.CreateAssembly(@".\foo.dll"));
            Assert.Throws<ArgumentException>(() => MetadataFileFactory.CreateAssembly(@"\foo.dll"));
        }

        [Fact]
        public void CreateFromBytes()
        {
            using (var a = AssemblyMetadata.CreateFromImage(TestResources.SymbolsTests.MultiModule.MultiModule))
            {
                // even though the image refers to other modules only the manifest module is loaded:
                Assert.Equal(1, a.GetModules().Length);
                Assert.Equal("MultiModule.dll", a.GetModules()[0].Name);
            }
        }

        [Fact]
        public void CreateFromFile()
        {
            var dir = Temp.CreateDirectory();
            var mm = dir.CreateFile("MultiModule.dll").WriteAllBytes(TestResources.SymbolsTests.MultiModule.MultiModule).Path;
            dir.CreateFile("mod2.netmodule").WriteAllBytes(TestResources.SymbolsTests.MultiModule.mod2);
            dir.CreateFile("mod3.netmodule").WriteAllBytes(TestResources.SymbolsTests.MultiModule.mod3);

            using (var a = MetadataFileFactory.CreateAssembly(mm))
            {
                Assert.Equal(3, a.GetModules().Length);
                Assert.Equal("MultiModule.dll", a.GetModules()[0].Name);
                Assert.Equal("mod2.netmodule", a.GetModules()[1].Name);
                Assert.Equal("mod3.netmodule", a.GetModules()[2].Name);
            }
        }
        
        [Fact]
        public void Disposal()
        {
            ModuleMetadata m1, m2, m3;
            var md = AssemblyMetadata.Create(
                m1 = ModuleMetadata.CreateFromImage(TestResources.SymbolsTests.MultiModule.MultiModule),
                m2 = ModuleMetadata.CreateFromImage(TestResources.SymbolsTests.MultiModule.mod2),
                m3 = ModuleMetadata.CreateFromImage(TestResources.SymbolsTests.MultiModule.mod3));

            md.Dispose();
            Assert.Throws<ObjectDisposedException>(() => m1.Module);
            Assert.Throws<ObjectDisposedException>(() => m2.Module);
            Assert.Throws<ObjectDisposedException>(() => m3.Module);
            md.Dispose();
        }

        [Fact]
        public void ImageOwnership()
        {
            ModuleMetadata m1, m2, m3;
            var a = AssemblyMetadata.Create(
                m1 = ModuleMetadata.CreateFromImage(TestResources.SymbolsTests.MultiModule.MultiModule),
                m2 = ModuleMetadata.CreateFromImage(TestResources.SymbolsTests.MultiModule.mod2),
                m3 = ModuleMetadata.CreateFromImage(TestResources.SymbolsTests.MultiModule.mod3));

            Assert.True(a.IsImageOwner, "Assembly should own the image");
            Assert.True(m1.IsImageOwner, "Module should own the image");
            Assert.True(m2.IsImageOwner, "Module should own the image");
            Assert.True(m3.IsImageOwner, "Module should own the image");

            var copy1 = a.Copy();
            Assert.False(copy1.IsImageOwner, "Assembly should not own the image");
            Assert.False(copy1.GetModules()[0].IsImageOwner, "Module should not own the image");
            Assert.False(copy1.GetModules()[1].IsImageOwner, "Module should not own the image");
            Assert.False(copy1.GetModules()[2].IsImageOwner, "Module should not own the image");

            Assert.Equal(m1.Module, copy1.GetModules()[0].Module);
            Assert.Equal(m2.Module, copy1.GetModules()[1].Module);
            Assert.Equal(m3.Module, copy1.GetModules()[2].Module);

            var copy2 = copy1.Copy();
            Assert.False(copy2.IsImageOwner, "Assembly should not own the image");
            Assert.False(copy2.GetModules()[0].IsImageOwner, "Module should not own the image");
            Assert.False(copy2.GetModules()[1].IsImageOwner, "Module should not own the image");
            Assert.False(copy2.GetModules()[2].IsImageOwner, "Module should not own the image");

            Assert.Equal(m1.Module, copy2.GetModules()[0].Module);
            Assert.Equal(m2.Module, copy2.GetModules()[1].Module);
            Assert.Equal(m3.Module, copy2.GetModules()[2].Module);

            copy1.Dispose();
            Assert.Throws<ObjectDisposedException>(() => copy1.GetModules()[0].Module);
            Assert.Throws<ObjectDisposedException>(() => copy1.GetModules()[1].Module);
            Assert.Throws<ObjectDisposedException>(() => copy1.GetModules()[2].Module);

            Assert.NotNull(a.GetModules()[0].Module);
            Assert.NotNull(a.GetModules()[1].Module);
            Assert.NotNull(a.GetModules()[2].Module);

            a.Dispose();

            Assert.Throws<ObjectDisposedException>(() => a.GetModules()[0].Module);
            Assert.Throws<ObjectDisposedException>(() => a.GetModules()[1].Module);
            Assert.Throws<ObjectDisposedException>(() => a.GetModules()[2].Module);
        }

        [Fact]
        public void BadImageFormat()
        {
            var invalidModuleName = Temp.CreateFile().WriteAllBytes(TestResources.MetadataTests.Invalid.InvalidModuleName);
            var metadata = MetadataFileFactory.CreateAssembly(invalidModuleName.Path);
            Assert.Throws<BadImageFormatException>(() => metadata.GetModules());
        }

        [Fact, WorkItem(547015, "DevDiv")]
        public void IncorrectCustomAssemblyTableSize_TooManyMethodSpecs()
        {
            var metadata = AssemblyMetadata.CreateFromImage(TestResources.MetadataTests.Invalid.IncorrectCustomAssemblyTableSize_TooManyMethodSpecs);
            Assert.Throws<BadImageFormatException>(() => metadata.GetModules());
        }
    }
}
