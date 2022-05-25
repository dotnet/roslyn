// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

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
using System.Globalization;

using static Roslyn.Utilities.PlatformInformation;

namespace Microsoft.CodeAnalysis.Scripting.Hosting.UnitTests
{
    // TODO: clean up and move to portable tests

    public class MetadataShadowCopyProviderTests : TestBase
    {
        private readonly MetadataShadowCopyProvider _provider;

        private static readonly ImmutableArray<string> s_systemNoShadowCopyDirectories = IsRunningOnMono
            ? ImmutableArray<string>.Empty
            : ImmutableArray.Create(
                FileUtilities.NormalizeDirectoryPath(Environment.GetFolderPath(Environment.SpecialFolder.Windows)),
                FileUtilities.NormalizeDirectoryPath(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles)),
                FileUtilities.NormalizeDirectoryPath(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86)),
                FileUtilities.NormalizeDirectoryPath(RuntimeEnvironment.GetRuntimeDirectory()));

        public MetadataShadowCopyProviderTests()
        {
            _provider = CreateProvider(CultureInfo.InvariantCulture);
        }

        private static MetadataShadowCopyProvider CreateProvider(CultureInfo culture)
        {
            return new MetadataShadowCopyProvider(TempRoot.Root, s_systemNoShadowCopyDirectories, culture);
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
            Assert.Throws<ArgumentException>(() => _provider.NeedsShadowCopy("c:goo.dll"));
            Assert.Throws<ArgumentException>(() => _provider.NeedsShadowCopy("bar.dll"));
            Assert.Throws<ArgumentException>(() => _provider.NeedsShadowCopy(@"\bar.dll"));
            Assert.Throws<ArgumentException>(() => _provider.NeedsShadowCopy(@"../bar.dll"));

            Assert.Throws<ArgumentNullException>(() => _provider.SuppressShadowCopy(null));
            Assert.Throws<ArgumentException>(() => _provider.SuppressShadowCopy("c:goo.dll"));
            Assert.Throws<ArgumentException>(() => _provider.SuppressShadowCopy("bar.dll"));
            Assert.Throws<ArgumentException>(() => _provider.SuppressShadowCopy(@"\bar.dll"));
            Assert.Throws<ArgumentException>(() => _provider.SuppressShadowCopy(@"../bar.dll"));

            Assert.Throws<ArgumentOutOfRangeException>(() => _provider.GetMetadataShadowCopy(IsRunningOnMono ? "/goo.dll" : @"c:\goo.dll", (MetadataImageKind)Byte.MaxValue));
            Assert.Throws<ArgumentNullException>(() => _provider.GetMetadataShadowCopy(null, MetadataImageKind.Assembly));
            Assert.Throws<ArgumentException>(() => _provider.GetMetadataShadowCopy("c:goo.dll", MetadataImageKind.Assembly));
            Assert.Throws<ArgumentException>(() => _provider.GetMetadataShadowCopy("bar.dll", MetadataImageKind.Assembly));
            Assert.Throws<ArgumentException>(() => _provider.GetMetadataShadowCopy(@"\bar.dll", MetadataImageKind.Assembly));
            Assert.Throws<ArgumentException>(() => _provider.GetMetadataShadowCopy(@"../bar.dll", MetadataImageKind.Assembly));

            Assert.Throws<ArgumentOutOfRangeException>(() => _provider.GetMetadata(IsRunningOnMono ? "/goo.dll" : @"c:\goo.dll", (MetadataImageKind)Byte.MaxValue));
            Assert.Throws<ArgumentNullException>(() => _provider.GetMetadata(null, MetadataImageKind.Assembly));
            Assert.Throws<ArgumentException>(() => _provider.GetMetadata("c:goo.dll", MetadataImageKind.Assembly));
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

        [ClrOnlyFact(ClrOnlyReason.Fusion)]
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

            var metadata1 = _provider.GetMetadata(path0, MetadataImageKind.Assembly) as AssemblyMetadata;
            Assert.NotNull(metadata1);
            Assert.Equal(3, metadata1.GetModules().Length);

            var scDir = Directory.GetFileSystemEntries(_provider.ShadowCopyDirectory).Single();
            Assert.True(Directory.Exists(scDir));

            var scFiles = Directory.GetFileSystemEntries(scDir);
            AssertEx.SetEqual(new[] { "MultiModule.dll", "mod2.netmodule", "mod3.netmodule" }, scFiles.Select(p => Path.GetFileName(p)));

            foreach (var sc in scFiles)
            {
                Assert.True(_provider.IsShadowCopy(sc));

                if (!IsRunningOnMono)
                {
                    // files should be locked:
                    Assert.Throws<IOException>(() => File.Delete(sc));
                }
            }

            // should get the same metadata:
            var metadata2 = _provider.GetMetadata(path0, MetadataImageKind.Assembly) as AssemblyMetadata;
            Assert.Same(metadata1, metadata2);

            // modify the file:
            File.SetLastWriteTimeUtc(path0, DateTime.Now + TimeSpan.FromHours(1));

            // we get an updated image if we ask again:
            var modifiedMetadata3 = _provider.GetMetadata(path0, MetadataImageKind.Assembly) as AssemblyMetadata;
            Assert.NotSame(modifiedMetadata3, metadata2);

            // the file has been modified - we get new metadata:
            for (int i = 0; i < metadata2.GetModules().Length; i++)
            {
                Assert.NotSame(metadata2.GetModules()[i], modifiedMetadata3.GetModules()[i]);
            }
        }

        [Fact]
        public unsafe void DisposalOnFailure()
        {
            var f0 = Temp.CreateFile().WriteAllText("bogus").Path;
            Assert.Throws<BadImageFormatException>(() => _provider.GetMetadata(f0, MetadataImageKind.Assembly));

            string f1 = Temp.CreateFile().WriteAllBytes(TestResources.SymbolsTests.MultiModule.MultiModuleDll).Path;
            Assert.Throws<FileNotFoundException>(() => _provider.GetMetadata(f1, MetadataImageKind.Assembly));
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
            var dir2 = Temp.CreateDirectory();
            var dll2 = dir2.CreateFile("a2.dll").WriteAllBytes(TestResources.MetadataTests.InterfaceAndClass.CSClasses01);

            Assert.Equal(1, _provider.CacheSize);
            var sc3a = _provider.GetMetadataShadowCopy(dll2.Path, MetadataImageKind.Module);
            Assert.Equal(2, _provider.CacheSize);
        }

        [Fact]
        public void XmlDocComments_SpecificCulture()
        {
            var elGR = CultureInfo.GetCultureInfo("el-GR");
            var arMA = CultureInfo.GetCultureInfo("ar-MA");

            var dir = Temp.CreateDirectory();
            var dll = dir.CreateFile("a.dll").WriteAllBytes(TestResources.MetadataTests.InterfaceAndClass.CSClasses01);
            var docInvariant = dir.CreateFile("a.xml").WriteAllText("Invariant");
            var docGreek = dir.CreateDirectory(elGR.Name).CreateFile("a.xml").WriteAllText("Greek");

            // invariant culture
            var provider = CreateProvider(CultureInfo.InvariantCulture);
            var sc = provider.GetMetadataShadowCopy(dll.Path, MetadataImageKind.Assembly);
            Assert.Equal(Path.Combine(Path.GetDirectoryName(sc.PrimaryModule.FullPath), @"a.xml"), sc.DocumentationFile.FullPath);
            Assert.Equal("Invariant", File.ReadAllText(sc.DocumentationFile.FullPath));

            // Greek culture
            provider = CreateProvider(elGR);
            sc = provider.GetMetadataShadowCopy(dll.Path, MetadataImageKind.Assembly);
            Assert.Equal(Path.Combine(Path.GetDirectoryName(sc.PrimaryModule.FullPath), @"el-GR", "a.xml"), sc.DocumentationFile.FullPath);
            Assert.Equal("Greek", File.ReadAllText(sc.DocumentationFile.FullPath));

            // Arabic culture (culture specific docs not found, use invariant)
            provider = CreateProvider(arMA);
            sc = provider.GetMetadataShadowCopy(dll.Path, MetadataImageKind.Assembly);
            Assert.Equal(Path.Combine(Path.GetDirectoryName(sc.PrimaryModule.FullPath), @"a.xml"), sc.DocumentationFile.FullPath);
            Assert.Equal("Invariant", File.ReadAllText(sc.DocumentationFile.FullPath));

            // no culture:
            provider = CreateProvider(null);
            sc = provider.GetMetadataShadowCopy(dll.Path, MetadataImageKind.Assembly);
            Assert.Null(sc.DocumentationFile);
        }
    }
}
