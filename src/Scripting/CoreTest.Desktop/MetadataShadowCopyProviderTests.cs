// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.
using System;
using System.Linq;
using System.IO;
using Roslyn.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;
using Microsoft.CodeAnalysis;
using System.Collections.Immutable;
using Roslyn.Utilities;
using System.Runtime.InteropServices;

namespace Microsoft.CodeAnalysis.Scripting.Hosting.UnitTests
{
    // TODO: clean up and move to portable tests

    public class MetadataShadowCopyProviderTests : TestBase, IDisposable
    {
        private readonly MetadataShadowCopyProvider _provider;

        private static readonly ImmutableArray<string> s_systemNoShadowCopyDirectories = ImmutableArray.Create(
                FileUtilities.NormalizeDirectoryPath(Environment.GetFolderPath(Environment.SpecialFolder.Windows)),
                FileUtilities.NormalizeDirectoryPath(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles)),
                FileUtilities.NormalizeDirectoryPath(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86)),
                FileUtilities.NormalizeDirectoryPath(RuntimeEnvironment.GetRuntimeDirectory()));

        public MetadataShadowCopyProviderTests()
        {
            _provider = new MetadataShadowCopyProvider(TempRoot.Root, s_systemNoShadowCopyDirectories);
        }

        public override void Dispose()
        {
            _provider.Dispose();
            Assert.False(Directory.Exists(_provider.ShadowCopyDirectory), "Shadow copy directory should have been deleted");
            base.Dispose();
        }

        [Fact]
        public void Errors()
        {
            Assert.Throws<ArgumentNullException>(() => _provider.NeedsShadowCopy(null));
            Assert.Throws<ArgumentException>(() => _provider.NeedsShadowCopy("c:foo.dll"));
            Assert.Throws<ArgumentException>(() => _provider.NeedsShadowCopy("bar.dll"));
            Assert.Throws<ArgumentException>(() => _provider.NeedsShadowCopy(@"\bar.dll"));
            Assert.Throws<ArgumentException>(() => _provider.NeedsShadowCopy(@"../bar.dll"));

            Assert.Throws<ArgumentNullException>(() => _provider.SuppressShadowCopy(null));
            Assert.Throws<ArgumentException>(() => _provider.SuppressShadowCopy("c:foo.dll"));
            Assert.Throws<ArgumentException>(() => _provider.SuppressShadowCopy("bar.dll"));
            Assert.Throws<ArgumentException>(() => _provider.SuppressShadowCopy(@"\bar.dll"));
            Assert.Throws<ArgumentException>(() => _provider.SuppressShadowCopy(@"../bar.dll"));

            Assert.Throws<ArgumentNullException>(() => _provider.GetReference(null));
            Assert.Throws<ArgumentException>(() => _provider.GetReference("c:foo.dll"));
            Assert.Throws<ArgumentException>(() => _provider.GetReference("bar.dll"));
            Assert.Throws<ArgumentException>(() => _provider.GetReference(@"\bar.dll"));
            Assert.Throws<ArgumentException>(() => _provider.GetReference(@"../bar.dll"));

            Assert.Throws<ArgumentOutOfRangeException>(() => _provider.GetMetadataShadowCopy(@"c:\foo.dll", (MetadataImageKind)Byte.MaxValue));
            Assert.Throws<ArgumentNullException>(() => _provider.GetMetadataShadowCopy(null, MetadataImageKind.Assembly));
            Assert.Throws<ArgumentException>(() => _provider.GetMetadataShadowCopy("c:foo.dll", MetadataImageKind.Assembly));
            Assert.Throws<ArgumentException>(() => _provider.GetMetadataShadowCopy("bar.dll", MetadataImageKind.Assembly));
            Assert.Throws<ArgumentException>(() => _provider.GetMetadataShadowCopy(@"\bar.dll", MetadataImageKind.Assembly));
            Assert.Throws<ArgumentException>(() => _provider.GetMetadataShadowCopy(@"../bar.dll", MetadataImageKind.Assembly));

            Assert.Throws<ArgumentOutOfRangeException>(() => _provider.GetMetadata(@"c:\foo.dll", (MetadataImageKind)Byte.MaxValue));
            Assert.Throws<ArgumentNullException>(() => _provider.GetMetadata(null, MetadataImageKind.Assembly));
            Assert.Throws<ArgumentException>(() => _provider.GetMetadata("c:foo.dll", MetadataImageKind.Assembly));
        }

        [Fact]
        public void Copy()
        {
            var dir = Temp.CreateDirectory();
            var dll = dir.CreateFile("a.dll").WriteAllBytes(TestResources.MetadataTests.InterfaceAndClass.CSClasses01);
            var doc = dir.CreateFile("a.xml").WriteAllText("<hello>");

            var sc1 = _provider.GetMetadataShadowCopy(dll.Path, MetadataImageKind.Assembly);
            var sc2 = _provider.GetMetadataShadowCopy(dll.Path, MetadataImageKind.Assembly);
            Assert.Equal(sc2, sc1);
            Assert.Equal(dll.Path, sc1.PrimaryModule.OriginalPath);
            Assert.NotEqual(dll.Path, sc1.PrimaryModule.FullPath);

            Assert.False(sc1.Metadata.IsImageOwner, "Copy expected");

            Assert.Equal(File.ReadAllBytes(dll.Path), File.ReadAllBytes(sc1.PrimaryModule.FullPath));
            Assert.Equal(File.ReadAllBytes(doc.Path), File.ReadAllBytes(sc1.DocumentationFile.FullPath));
        }

        [Fact]
        public void SuppressCopy1()
        {
            var dll = Temp.CreateFile().WriteAllText("blah");

            _provider.SuppressShadowCopy(dll.Path);

            var sc1 = _provider.GetMetadataShadowCopy(dll.Path, MetadataImageKind.Assembly);
            Assert.Null(sc1);
        }

        [Fact]
        public void SuppressCopy_Framework()
        {
            // framework assemblies not copied:
            string mscorlib = typeof(object).Assembly.Location;
            var sc2 = _provider.GetMetadataShadowCopy(mscorlib, MetadataImageKind.Assembly);
            Assert.Null(sc2);
        }

        [Fact]
        public void SuppressCopy_ShadowCopyDirectory()
        {
            // shadow copies not copied:
            var dll = Temp.CreateFile("a.dll").WriteAllBytes(TestResources.MetadataTests.InterfaceAndClass.CSClasses01);

            // copy:
            var sc1 = _provider.GetMetadataShadowCopy(dll.Path, MetadataImageKind.Assembly);
            Assert.NotEqual(dll.Path, sc1.PrimaryModule.FullPath);

            // file not copied:
            var sc2 = _provider.GetMetadataShadowCopy(sc1.PrimaryModule.FullPath, MetadataImageKind.Assembly);
            Assert.Null(sc2);
        }

        [Fact]
        public void Modules()
        {
            // modules: { MultiModule.dll, mod2.netmodule, mod3.netmodule }
            var dir = Temp.CreateDirectory();
            string path0 = dir.CreateFile("MultiModule.dll").WriteAllBytes(TestResources.SymbolsTests.MultiModule.MultiModuleDll).Path;
            string path1 = dir.CreateFile("mod2.netmodule").WriteAllBytes(TestResources.SymbolsTests.MultiModule.mod2).Path;
            string path2 = dir.CreateFile("mod3.netmodule").WriteAllBytes(TestResources.SymbolsTests.MultiModule.mod3).Path;

            var reference1 = _provider.GetReference(path0);
            Assert.NotNull(reference1);
            Assert.Equal(0, _provider.CacheSize);
            Assert.Equal(path0, reference1.FilePath);

            var metadata1 = reference1.GetMetadata() as AssemblyMetadata;
            Assert.NotNull(metadata1);
            Assert.Equal(3, metadata1.GetModules().Length);

            var scDir = Directory.GetFileSystemEntries(_provider.ShadowCopyDirectory).Single();
            Assert.True(Directory.Exists(scDir));

            var scFiles = Directory.GetFileSystemEntries(scDir);
            AssertEx.SetEqual(new[] { "MultiModule.dll", "mod2.netmodule", "mod3.netmodule" }, scFiles.Select(p => Path.GetFileName(p)));

            foreach (var sc in scFiles)
            {
                Assert.True(_provider.IsShadowCopy(sc));

                // files should be locked:
                Assert.Throws<IOException>(() => File.Delete(sc));
            }

            // should get the same metadata:
            var metadata2 = reference1.GetMetadata() as AssemblyMetadata;
            Assert.Same(metadata1, metadata2);

            // a new reference is created:
            var reference2 = _provider.GetReference(path0);
            Assert.NotNull(reference2);
            Assert.Equal(path0, reference2.FilePath);
            Assert.NotSame(reference1, reference2);

            // the original file wasn't modified so we still get the same metadata:
            var metadata3 = reference2.GetMetadata() as AssemblyMetadata;
            Assert.Same(metadata3, metadata2);

            // modify the file:
            File.SetLastWriteTimeUtc(path0, DateTime.Now + TimeSpan.FromHours(1));

            // the reference doesn't own the metadata, so we get an updated image if we ask again:
            var modifiedMetadata3 = reference2.GetMetadata() as AssemblyMetadata;
            Assert.NotSame(modifiedMetadata3, metadata2);

            // a new reference is created, again we get the modified image (which is copied to the shadow copy directory):
            var reference4 = _provider.GetReference(path0);
            Assert.NotNull(reference4);
            Assert.Equal(path0, reference4.FilePath);
            Assert.NotSame(reference2, reference4);

            // the file has been modified - we get new metadata:
            var metadata4 = reference4.GetMetadata() as AssemblyMetadata;
            Assert.NotSame(metadata4, metadata3);
            for (int i = 0; i < metadata4.GetModules().Length; i++)
            {
                Assert.NotSame(metadata4.GetModules()[i], metadata3.GetModules()[i]);
            }
        }

        [Fact]
        public unsafe void DisposalOnFailure()
        {
            var f0 = Temp.CreateFile().WriteAllText("bogus").Path;
            var r0 = _provider.GetReference(f0);
            Assert.Throws<BadImageFormatException>(() => r0.GetMetadata());

            string f1 = Temp.CreateFile().WriteAllBytes(TestResources.SymbolsTests.MultiModule.MultiModuleDll).Path;
            var r1 = _provider.GetReference(f1);
            Assert.Throws<FileNotFoundException>(() => r1.GetMetadata());
        }

        [Fact]
        public void GetMetadata()
        {
            var dir = Temp.CreateDirectory();
            var dll = dir.CreateFile("a.dll").WriteAllBytes(TestResources.MetadataTests.InterfaceAndClass.CSClasses01);
            var doc = dir.CreateFile("a.xml").WriteAllText("<hello>");

            var sc1 = _provider.GetMetadataShadowCopy(dll.Path, MetadataImageKind.Assembly);
            var sc2 = _provider.GetMetadataShadowCopy(dll.Path, MetadataImageKind.Assembly);

            var md1 = _provider.GetMetadata(dll.Path, MetadataImageKind.Assembly);
            Assert.NotNull(md1);
            Assert.Equal(MetadataImageKind.Assembly, md1.Kind);

            // This needs to be in different folder from referencesdir to cause the other code path 
            // to be triggered for NeedsShadowCopy method
            var dir2 = System.IO.Path.GetTempPath();
            string dll2 = System.IO.Path.Combine(dir2, "a2.dll");
            System.IO.File.WriteAllBytes(dll2, TestResources.MetadataTests.InterfaceAndClass.CSClasses01);

            Assert.Equal(1, _provider.CacheSize);
            var sc3a = _provider.GetMetadataShadowCopy(dll2, MetadataImageKind.Module);
            Assert.Equal(2, _provider.CacheSize);
        }
    }
}
