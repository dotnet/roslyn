// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;
using System.Globalization;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.Emit
{
    public class ResourceTests : CSharpTestBase
    {
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr LoadLibraryEx(string lpFileName, IntPtr hFile, uint dwFlags);
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool FreeLibrary([In] IntPtr hFile);

        [Fact]
        public void DefaultVersionResource()
        {
            string source = @"
public class Maine
{
    public static void Main()
    {
    }
}
";
            var c1 = CreateCompilationWithMscorlib(source, assemblyName: "Win32VerNoAttrs", options: TestOptions.ReleaseExe);
            var exe = Temp.CreateFile();

            using (FileStream output = exe.Open())
            {
                c1.Emit(output, win32Resources: c1.CreateDefaultWin32Resources(true, false, null, null));
            }

            c1 = null;

            //Open as data
            IntPtr lib = IntPtr.Zero;
            string versionData;
            string mftData;
            try
            {
                lib = LoadLibraryEx(exe.Path, IntPtr.Zero, 0x00000002);
                if (lib == IntPtr.Zero)
                    throw new Win32Exception(Marshal.GetLastWin32Error());

                //the manifest and version primitives are tested elsewhere. This is to test that the default
                //values are passed to the primitives that assemble the resources.

                uint size;
                IntPtr versionRsrc = Win32Res.GetResource(lib, "#1", "#16", out size);
                versionData = Win32Res.VersionResourceToXml(versionRsrc);

                uint mftSize;
                IntPtr mftRsrc = Win32Res.GetResource(lib, "#1", "#24", out mftSize);
                mftData = Win32Res.ManifestResourceToXml(mftRsrc, mftSize);
            }
            finally
            {
                if (lib != IntPtr.Zero)
                {
                    FreeLibrary(lib);
                }
            }

            string expected =
@"<?xml version=""1.0"" encoding=""utf-16""?>
<VersionResource Size=""612"">
  <VS_FIXEDFILEINFO FileVersionMS=""00000000"" FileVersionLS=""00000000"" ProductVersionMS=""00000000"" ProductVersionLS=""00000000"" />
  <KeyValuePair Key=""FileDescription"" Value="" "" />
  <KeyValuePair Key=""FileVersion"" Value=""0.0.0.0"" />
  <KeyValuePair Key=""InternalName"" Value=""Win32VerNoAttrs.exe"" />
  <KeyValuePair Key=""LegalCopyright"" Value="" "" />
  <KeyValuePair Key=""OriginalFilename"" Value=""Win32VerNoAttrs.exe"" />
  <KeyValuePair Key=""ProductVersion"" Value=""0.0.0.0"" />
  <KeyValuePair Key=""Assembly Version"" Value=""0.0.0.0"" />
</VersionResource>";

            Assert.Equal(expected, versionData);

            expected = @"<?xml version=""1.0"" encoding=""utf-16""?>
<ManifestResource Size=""490"">
  <Contents><![CDATA[<?xml version=""1.0"" encoding=""UTF-8"" standalone=""yes""?>

<assembly xmlns=""urn:schemas-microsoft-com:asm.v1"" manifestVersion=""1.0"">
  <assemblyIdentity version=""1.0.0.0"" name=""MyApplication.app""/>
  <trustInfo xmlns=""urn:schemas-microsoft-com:asm.v2"">
    <security>
      <requestedPrivileges xmlns=""urn:schemas-microsoft-com:asm.v3"">
        <requestedExecutionLevel level=""asInvoker"" uiAccess=""false""/>
      </requestedPrivileges>
    </security>
  </trustInfo>
</assembly>]]></Contents>
</ManifestResource>";

            Assert.Equal(expected, mftData);

            //look at the same data through the FileVersion API.
            //If the codepage and resource language information is not
            //written correctly into the internal resource directory of
            //the PE, then GetVersionInfo will fail to find the FileVersionInfo. 
            //Once upon a time in Roslyn, the codepage and lang info was not written correctly.
            var fileVer = FileVersionInfo.GetVersionInfo(exe.Path);
            Assert.Equal(" ", fileVer.LegalCopyright);
        }

        [Fact]
        public void ResourcesInCoff()
        {
            //this is to test that resources coming from a COFF can be added to a binary.
            string source = @"
class C
{
}
";
            var c1 = CreateCompilationWithMscorlib(source, assemblyName: "Win32WithCoff", options: TestOptions.ReleaseDll);
            var exe = Temp.CreateFile();

            using (FileStream output = exe.Open())
            {
                var memStream = new MemoryStream(TestResources.General.nativeCOFFResources);
                c1.Emit(output, win32Resources: memStream);
            }

            c1 = null;

            //Open as data
            IntPtr lib = IntPtr.Zero;
            string versionData;
            try
            {
                lib = LoadLibraryEx(exe.Path, IntPtr.Zero, 0x00000002);
                if (lib == IntPtr.Zero)
                    throw new Win32Exception(Marshal.GetLastWin32Error());

                //the manifest and version primitives are tested elsewhere. This is to test that the resources
                //we expect are present. Also need to check that the actual contents of at least one of the resources
                //is good. That tests our processing of the relocations.

                uint size;
                IntPtr versionRsrc = Win32Res.GetResource(lib, "#1", "#16", out size);
                versionData = Win32Res.VersionResourceToXml(versionRsrc);

                uint stringTableSize;
                IntPtr stringTable = Win32Res.GetResource(lib, "#1", "#6", out stringTableSize);
                Assert.NotNull(stringTable);

                uint elevenSize;
                IntPtr elevenRsrc = Win32Res.GetResource(lib, "#1", "#11", out elevenSize);
                Assert.NotNull(elevenRsrc);

                uint wevtSize;
                IntPtr wevtRsrc = Win32Res.GetResource(lib, "#1", "WEVT_TEMPLATE", out wevtSize);
                Assert.NotNull(wevtRsrc);
            }
            finally
            {
                if (lib != IntPtr.Zero)
                {
                    FreeLibrary(lib);
                }
            }

            string expected =
@"<?xml version=""1.0"" encoding=""utf-16""?>
<VersionResource Size=""1104"">
  <VS_FIXEDFILEINFO FileVersionMS=""000b0000"" FileVersionLS=""eacc0000"" ProductVersionMS=""000b0000"" ProductVersionLS=""eacc0000"" />
  <KeyValuePair Key=""CompanyName"" Value=""Microsoft Corporation"" />
  <KeyValuePair Key=""FileDescription"" Value=""Team Foundation Server Object Model"" />
  <KeyValuePair Key=""FileVersion"" Value=""11.0.60108.0 built by: TOOLSET_ROSLYN(GNAMBOO-DEV-GNAMBOO)"" />
  <KeyValuePair Key=""InternalName"" Value=""Microsoft.TeamFoundation.Framework.Server.dll"" />
  <KeyValuePair Key=""LegalCopyright"" Value=""© Microsoft Corporation. All rights reserved."" />
  <KeyValuePair Key=""OriginalFilename"" Value=""Microsoft.TeamFoundation.Framework.Server.dll"" />
  <KeyValuePair Key=""ProductName"" Value=""Microsoft® Visual Studio® 2012"" />
  <KeyValuePair Key=""ProductVersion"" Value=""11.0.60108.0"" />
</VersionResource>";

            Assert.Equal(expected, versionData);

            //look at the same data through the FileVersion API.
            //If the codepage and resource language information is not
            //written correctly into the internal resource directory of
            //the PE, then GetVersionInfo will fail to find the FileVersionInfo. 
            //Once upon a time in Roslyn, the codepage and lang info was not written correctly.
            var fileVer = FileVersionInfo.GetVersionInfo(exe.Path);
            Assert.Equal("Microsoft Corporation", fileVer.CompanyName);
        }

        [Fact]
        public void FaultyResourceDataProvider()
        {
            var c1 = CreateCompilationWithMscorlib("");

            var result = c1.Emit(new MemoryStream(), manifestResources:
                new[]
                {
                    new ResourceDescription("r2", "file", () => { throw new Exception("bad stuff"); }, false)
                });

            result.Diagnostics.Verify(
                // error CS1566: Error reading resource 'file' -- 'bad stuff'
                Diagnostic(ErrorCode.ERR_CantReadResource).WithArguments("file", "bad stuff")
            );

            result = c1.Emit(new MemoryStream(), manifestResources:
                new[]
                {
                    new ResourceDescription("r2", "file", () => null, false)
                });

            result.Diagnostics.Verify(
                // error CS1566: Error reading resource 'file' -- 'Resource data provider should return non-null stream'
                Diagnostic(ErrorCode.ERR_CantReadResource).WithArguments("file", CodeAnalysisResources.ResourceDataProviderShouldReturnNonNullStream)
            );
        }

        [WorkItem(543501, "DevDiv")]
        [Fact]
        public void CS1508_DuplicateMainfestResourceIdentifier()
        {
            var c1 = CreateCompilationWithMscorlib("");
            Func<Stream> dataProvider = () => new MemoryStream(new byte[] { });

            var result = c1.Emit(new MemoryStream(), manifestResources:
                new[]
                {
                    new ResourceDescription("A", "x.foo", dataProvider, true),
                    new ResourceDescription("A", "y.foo", dataProvider, true)
                });

            result.Diagnostics.Verify(
                // error CS1508: Resource identifier 'A' has already been used in this assembly
                Diagnostic(ErrorCode.ERR_ResourceNotUnique).WithArguments("A")
            );
        }

        [WorkItem(543501, "DevDiv")]
        [Fact]
        public void CS1508_DuplicateMainfestResourceIdentifier_EmbeddedResource()
        {
            var c1 = CreateCompilationWithMscorlib("");
            Func<Stream> dataProvider = () => new MemoryStream(new byte[] { });

            var result = c1.Emit(new MemoryStream(), manifestResources:
                new[]
                {
                    new ResourceDescription("A", dataProvider, true),
                    new ResourceDescription("A", null, dataProvider, true, isEmbedded: true, checkArgs: true)
                });

            result.Diagnostics.Verify(
                // error CS1508: Resource identifier 'A' has already been used in this assembly
                Diagnostic(ErrorCode.ERR_ResourceNotUnique).WithArguments("A")
            );

            // file name ignored for embedded manifest resources
            result = c1.Emit(new MemoryStream(), manifestResources:
                new[]
                {
                    new ResourceDescription("A", "x.foo", dataProvider, true, isEmbedded: true, checkArgs: true),
                    new ResourceDescription("A", "x.foo", dataProvider, true, isEmbedded: false, checkArgs: true)
                });

            result.Diagnostics.Verify(
                // error CS1508: Resource identifier 'A' has already been used in this assembly
                Diagnostic(ErrorCode.ERR_ResourceNotUnique).WithArguments("A")
            );
        }

        [WorkItem(543501, "DevDiv")]
        [Fact]
        public void CS7041_DuplicateMainfestResourceFileName()
        {
            var c1 = CSharpCompilation.Create("foo", references: new[] { MscorlibRef }, options: TestOptions.ReleaseDll);
            Func<Stream> dataProvider = () => new MemoryStream(new byte[] { });

            var result = c1.Emit(new MemoryStream(), manifestResources:
                new[]
                {
                    new ResourceDescription("A", "x.foo", dataProvider, true),
                    new ResourceDescription("B", "x.foo", dataProvider, true)
                });

            result.Diagnostics.Verify(
                // error CS7041: Each linked resource and module must have a unique filename. Filename 'x.foo' is specified more than once in this assembly
                Diagnostic(ErrorCode.ERR_ResourceFileNameNotUnique).WithArguments("x.foo")
            );
        }

        [WorkItem(543501, "DevDiv")]
        [Fact]
        public void NoDuplicateMainfestResourceFileNameDiagnosticForEmbeddedResources()
        {
            var c1 = CreateCompilationWithMscorlib("");
            Func<Stream> dataProvider = () => new MemoryStream(new byte[] { });

            var result = c1.Emit(new MemoryStream(), manifestResources:
                new[]
                {
                    new ResourceDescription("A", dataProvider, true),
                    new ResourceDescription("B", null, dataProvider, true, isEmbedded: true, checkArgs: true)
                });

            result.Diagnostics.Verify();

            // file name ignored for embedded manifest resources
            result = c1.Emit(new MemoryStream(), manifestResources:
                new[]
                {
                    new ResourceDescription("A", "x.foo", dataProvider, true, isEmbedded: true, checkArgs: true),
                    new ResourceDescription("B", "x.foo", dataProvider, true, isEmbedded: false, checkArgs: true)
                });

            result.Diagnostics.Verify();
        }

        [WorkItem(543501, "DevDiv"), WorkItem(546297, "DevDiv")]
        [Fact]
        public void CS1508_CS7041_DuplicateMainfestResourceDiagnostics()
        {
            var c1 = CreateCompilationWithMscorlib("");
            Func<Stream> dataProvider = () => new MemoryStream(new byte[] { });

            var result = c1.Emit(new MemoryStream(), manifestResources:
                new[]
                {
                    new ResourceDescription("A", "x.foo", dataProvider, true),
                    new ResourceDescription("A", "x.foo", dataProvider, true)
                });

            result.Diagnostics.Verify(
                // error CS1508: Resource identifier 'A' has already been used in this assembly
                Diagnostic(ErrorCode.ERR_ResourceNotUnique).WithArguments("A"),
                // error CS7041: Each linked resource and module must have a unique filename. Filename 'x.foo' is specified more than once in this assembly
                Diagnostic(ErrorCode.ERR_ResourceFileNameNotUnique).WithArguments("x.foo")
            );

            result = c1.Emit(new MemoryStream(), manifestResources:
                new[]
                {
                    new ResourceDescription("A", "x.foo", dataProvider, true),
                    new ResourceDescription("B", "x.foo", dataProvider, true),
                    new ResourceDescription("B", "y.foo", dataProvider, true)
                });

            result.Diagnostics.Verify(
                // error CS7041: Each linked resource and module must have a unique filename. Filename 'x.foo' is specified more than once in this assembly
                Diagnostic(ErrorCode.ERR_ResourceFileNameNotUnique).WithArguments("x.foo"),
                // error CS1508: Resource identifier 'B' has already been used in this assembly
                Diagnostic(ErrorCode.ERR_ResourceNotUnique).WithArguments("B")
            );

            result = c1.Emit(new MemoryStream(), manifestResources:
                new[]
                {
                    new ResourceDescription("A", "foo.dll", dataProvider, true),
                });

            //make sure there's no problem when the name of the primary module conflicts with a file name of an added resource.
            result.Diagnostics.Verify();

            var netModule1 = TestReferences.SymbolsTests.netModule.netModule1;

            c1 = CreateCompilationWithMscorlib("", references: new[] { netModule1 });

            result = c1.Emit(new MemoryStream(), manifestResources:
                new[]
                {
                    new ResourceDescription("A", "netmodule1.netmodule", dataProvider, true),
                });

            // Native compiler gives CS0013 (FTL_MetadataEmitFailure) at Emit stage
            result.Diagnostics.Verify(
                // error CS7041: Each linked resource and module must have a unique filename. Filename 'netmodule1.netmodule' is specified more than once in this assembly
                Diagnostic(ErrorCode.ERR_ResourceFileNameNotUnique).WithArguments("netModule1.netmodule")
            );
        }

        [Fact]
        public void AddManagedResource()
        {
            string source = @"public class C { static public void Main() {} }";

            // Do not name the compilation, a unique guid is used as a name by default. It prevents conflicts with other assemblies loaded via Assembly.ReflectionOnlyLoad.
            var c1 = CreateCompilationWithMscorlib(source);

            var resourceFileName = "RoslynResourceFile.foo";
            var output = new MemoryStream();

            const string r1Name = "some.dotted.NAME";
            const string r2Name = "another.DoTtEd.NAME";

            var arrayOfEmbeddedData = new byte[] { 1, 2, 3, 4, 5 };
            var resourceFileData = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };

            var result = c1.Emit(output, manifestResources:
                new ResourceDescription[]
                {
                    new ResourceDescription(r1Name, () => new MemoryStream(arrayOfEmbeddedData), true),
                    new ResourceDescription(r2Name, resourceFileName, () => new MemoryStream(resourceFileData), false)
                });

            Assert.True(result.Success);

            var assembly = Assembly.ReflectionOnlyLoad(output.ToArray());

            string[] resourceNames = assembly.GetManifestResourceNames();
            Assert.Equal(2, resourceNames.Length);

            var rInfo = assembly.GetManifestResourceInfo(r1Name);
            Assert.Equal(ResourceLocation.Embedded | ResourceLocation.ContainedInManifestFile, rInfo.ResourceLocation);

            var rData = assembly.GetManifestResourceStream(r1Name);
            var rBytes = new byte[rData.Length];
            rData.Read(rBytes, 0, (int)rData.Length);
            Assert.Equal(arrayOfEmbeddedData, rBytes);

            rInfo = assembly.GetManifestResourceInfo(r2Name);
            Assert.Equal(resourceFileName, rInfo.FileName);

            c1 = null;
        }
        [Fact]

        public void AddResourceToModule()
        {
            for (int metadataOnlyIfNonzero = 0; metadataOnlyIfNonzero < 2; metadataOnlyIfNonzero++)
            {
                var metadataOnly = metadataOnlyIfNonzero != 0;
                Func<Compilation, Stream, ResourceDescription[], CodeAnalysis.Emit.EmitResult> emit;
                emit = (c, s, r) => c.Emit(s, manifestResources: r, options: new EmitOptions(metadataOnly: metadataOnly));

                var sourceTree = SyntaxFactory.ParseSyntaxTree("");

                // Do not name the compilation, a unique guid is used as a name by default. It prevents conflicts with other assemblies loaded via Assembly.ReflectionOnlyLoad.
                var c1 = CSharpCompilation.Create(
                    Guid.NewGuid().ToString(),
                    new[] { sourceTree },
                    new[] { MscorlibRef },
                    TestOptions.ReleaseModule);

                var resourceFileName = "RoslynResourceFile.foo";
                var output = new MemoryStream();

                const string r1Name = "some.dotted.NAME";
                const string r2Name = "another.DoTtEd.NAME";

                var arrayOfEmbeddedData = new byte[] { 1, 2, 3, 4, 5 };
                var resourceFileData = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };

                var result = emit(c1, output,
                    new ResourceDescription[]
                    {
                        new ResourceDescription(r1Name, () => new MemoryStream(arrayOfEmbeddedData), true),
                        new ResourceDescription(r2Name, resourceFileName, () => new MemoryStream(resourceFileData), false)
                    });

                Assert.False(result.Success);
                Assert.NotEmpty(result.Diagnostics.Where(x => x.Code == (int)ErrorCode.ERR_CantRefResource));

                result = emit(c1, output,
                    new ResourceDescription[]
                    {
                        new ResourceDescription(r2Name, resourceFileName, () => new MemoryStream(resourceFileData), false),
                        new ResourceDescription(r1Name, () => new MemoryStream(arrayOfEmbeddedData), true)
                    });

                Assert.False(result.Success);
                Assert.NotEmpty(result.Diagnostics.Where(x => x.Code == (int)ErrorCode.ERR_CantRefResource));

                result = emit(c1, output,
                    new ResourceDescription[]
                    {
                        new ResourceDescription(r2Name, resourceFileName, () => new MemoryStream(resourceFileData), false)
                    });

                Assert.False(result.Success);
                Assert.NotEmpty(result.Diagnostics.Where(x => x.Code == (int)ErrorCode.ERR_CantRefResource));

                var c_mod1 = CSharpCompilation.Create(
                    Guid.NewGuid().ToString(),
                    new[] { sourceTree },
                    new[] { MscorlibRef },
                    TestOptions.ReleaseModule);

                var output_mod1 = new MemoryStream();
                result = emit(c_mod1, output_mod1,
                    new ResourceDescription[]
                    {
                        new ResourceDescription(r1Name, () => new MemoryStream(arrayOfEmbeddedData), true)
                    });

                Assert.True(result.Success);
                var mod1 = ModuleMetadata.CreateFromImage(output_mod1.ToImmutable());
                var ref_mod1 = mod1.GetReference();
                Assert.Equal(ManifestResourceAttributes.Public, mod1.Module.GetEmbeddedResourcesOrThrow()[0].Attributes);

                {
                    var c2 = CreateCompilationWithMscorlib(sourceTree, new[] { ref_mod1 }, TestOptions.ReleaseDll);
                    var output2 = new MemoryStream();
                    var result2 = c2.Emit(output2);

                    Assert.True(result2.Success);
                    var assembly = System.Reflection.Assembly.ReflectionOnlyLoad(output2.ToArray());

                    assembly.ModuleResolve += (object sender, ResolveEventArgs e) =>
                    {
                        if (e.Name.Equals(c_mod1.SourceModule.Name))
                        {
                            return assembly.LoadModule(e.Name, output_mod1.ToArray());
                        }

                        return null;
                    };

                    string[] resourceNames = assembly.GetManifestResourceNames();
                    Assert.Equal(1, resourceNames.Length);

                    var rInfo = assembly.GetManifestResourceInfo(r1Name);
                    Assert.Equal(System.Reflection.ResourceLocation.Embedded, rInfo.ResourceLocation);
                    Assert.Equal(c_mod1.SourceModule.Name, rInfo.FileName);

                    var rData = assembly.GetManifestResourceStream(r1Name);
                    var rBytes = new byte[rData.Length];
                    rData.Read(rBytes, 0, (int)rData.Length);
                    Assert.Equal(arrayOfEmbeddedData, rBytes);
                }

                var c_mod2 = CSharpCompilation.Create(
                    Guid.NewGuid().ToString(),
                    new[] { sourceTree },
                    new[] { MscorlibRef },
                    TestOptions.ReleaseModule);

                var output_mod2 = new MemoryStream();
                result = emit(c_mod2, output_mod2,
                    new ResourceDescription[]
                    {
                        new ResourceDescription(r1Name, () => new MemoryStream(arrayOfEmbeddedData), true),
                        new ResourceDescription(r2Name, () => new MemoryStream(resourceFileData), true)
                    });

                Assert.True(result.Success);
                var ref_mod2 = ModuleMetadata.CreateFromImage(output_mod2.ToImmutable()).GetReference();

                {
                    var c3 = CreateCompilationWithMscorlib(sourceTree, new[] { ref_mod2 }, TestOptions.ReleaseDll);
                    var output3 = new MemoryStream();
                    var result3 = c3.Emit(output3);

                    Assert.True(result3.Success);
                    var assembly = Assembly.ReflectionOnlyLoad(output3.ToArray());

                    assembly.ModuleResolve += (object sender, ResolveEventArgs e) =>
                    {
                        if (e.Name.Equals(c_mod2.SourceModule.Name))
                        {
                            return assembly.LoadModule(e.Name, output_mod2.ToArray());
                        }

                        return null;
                    };

                    string[] resourceNames = assembly.GetManifestResourceNames();
                    Assert.Equal(2, resourceNames.Length);

                    var rInfo = assembly.GetManifestResourceInfo(r1Name);
                    Assert.Equal(ResourceLocation.Embedded, rInfo.ResourceLocation);
                    Assert.Equal(c_mod2.SourceModule.Name, rInfo.FileName);

                    var rData = assembly.GetManifestResourceStream(r1Name);
                    var rBytes = new byte[rData.Length];
                    rData.Read(rBytes, 0, (int)rData.Length);
                    Assert.Equal(arrayOfEmbeddedData, rBytes);

                    rInfo = assembly.GetManifestResourceInfo(r2Name);
                    Assert.Equal(ResourceLocation.Embedded, rInfo.ResourceLocation);
                    Assert.Equal(c_mod2.SourceModule.Name, rInfo.FileName);

                    rData = assembly.GetManifestResourceStream(r2Name);
                    rBytes = new byte[rData.Length];
                    rData.Read(rBytes, 0, (int)rData.Length);
                    Assert.Equal(resourceFileData, rBytes);
                }

                var c_mod3 = CSharpCompilation.Create(
                    Guid.NewGuid().ToString(),
                    new[] { sourceTree },
                    new[] { MscorlibRef },
                    TestOptions.ReleaseModule);

                var output_mod3 = new MemoryStream();
                result = emit(c_mod3, output_mod3,
                    new ResourceDescription[]
                    {
                        new ResourceDescription(r2Name, () => new MemoryStream(resourceFileData), false)
                    });

                Assert.True(result.Success);
                var mod3 = ModuleMetadata.CreateFromImage(output_mod3.ToImmutable());
                var ref_mod3 = mod3.GetReference();
                Assert.Equal(ManifestResourceAttributes.Private, mod3.Module.GetEmbeddedResourcesOrThrow()[0].Attributes);

                {
                    var c4 = CreateCompilationWithMscorlib(sourceTree, new[] { ref_mod3 }, TestOptions.ReleaseDll);
                    var output4 = new MemoryStream();
                    var result4 = c4.Emit(output4, manifestResources:
                        new ResourceDescription[]
                        {
                            new ResourceDescription(r1Name, () => new MemoryStream(arrayOfEmbeddedData), false)
                        });

                    Assert.True(result4.Success);
                    var assembly = System.Reflection.Assembly.ReflectionOnlyLoad(output4.ToArray());

                    assembly.ModuleResolve += (object sender, ResolveEventArgs e) =>
                    {
                        if (e.Name.Equals(c_mod3.SourceModule.Name))
                        {
                            return assembly.LoadModule(e.Name, output_mod3.ToArray());
                        }

                        return null;
                    };

                    string[] resourceNames = assembly.GetManifestResourceNames();
                    Assert.Equal(2, resourceNames.Length);

                    var rInfo = assembly.GetManifestResourceInfo(r1Name);
                    Assert.Equal(ResourceLocation.Embedded | ResourceLocation.ContainedInManifestFile, rInfo.ResourceLocation);

                    var rData = assembly.GetManifestResourceStream(r1Name);
                    var rBytes = new byte[rData.Length];
                    rData.Read(rBytes, 0, (int)rData.Length);
                    Assert.Equal(arrayOfEmbeddedData, rBytes);

                    rInfo = assembly.GetManifestResourceInfo(r2Name);
                    Assert.Equal(ResourceLocation.Embedded, rInfo.ResourceLocation);
                    Assert.Equal(c_mod3.SourceModule.Name, rInfo.FileName);

                    rData = assembly.GetManifestResourceStream(r2Name);
                    rBytes = new byte[rData.Length];
                    rData.Read(rBytes, 0, (int)rData.Length);
                    Assert.Equal(resourceFileData, rBytes);
                }

                {
                    var c5 = CreateCompilationWithMscorlib(sourceTree, new[] { ref_mod1, ref_mod3 }, TestOptions.ReleaseDll);
                    var output5 = new MemoryStream();
                    var result5 = emit(c5, output5, null);

                    Assert.True(result5.Success);
                    var assembly = Assembly.ReflectionOnlyLoad(output5.ToArray());

                    assembly.ModuleResolve += (object sender, ResolveEventArgs e) =>
                    {
                        if (e.Name.Equals(c_mod1.SourceModule.Name))
                        {
                            return assembly.LoadModule(e.Name, output_mod1.ToArray());
                        }
                        else if (e.Name.Equals(c_mod3.SourceModule.Name))
                        {
                            return assembly.LoadModule(e.Name, output_mod3.ToArray());
                        }

                        return null;
                    };

                    string[] resourceNames = assembly.GetManifestResourceNames();
                    Assert.Equal(2, resourceNames.Length);

                    var rInfo = assembly.GetManifestResourceInfo(r1Name);
                    Assert.Equal(ResourceLocation.Embedded, rInfo.ResourceLocation);
                    Assert.Equal(c_mod1.SourceModule.Name, rInfo.FileName);

                    var rData = assembly.GetManifestResourceStream(r1Name);
                    var rBytes = new byte[rData.Length];
                    rData.Read(rBytes, 0, (int)rData.Length);
                    Assert.Equal(arrayOfEmbeddedData, rBytes);

                    rInfo = assembly.GetManifestResourceInfo(r2Name);
                    Assert.Equal(ResourceLocation.Embedded, rInfo.ResourceLocation);
                    Assert.Equal(c_mod3.SourceModule.Name, rInfo.FileName);

                    rData = assembly.GetManifestResourceStream(r2Name);
                    rBytes = new byte[rData.Length];
                    rData.Read(rBytes, 0, (int)rData.Length);
                    Assert.Equal(resourceFileData, rBytes);
                }

                {
                    var c6 = CreateCompilationWithMscorlib(sourceTree, new[] { ref_mod1, ref_mod2 }, TestOptions.ReleaseDll);
                    var output6 = new MemoryStream();
                    var result6 = emit(c6, output6, null);

                    if (metadataOnly)
                    {
                        Assert.True(result6.Success);
                    }
                    else
                    {
                        Assert.False(result6.Success);
                        result6.Diagnostics.Verify(
                            // error CS1508: Resource identifier 'some.dotted.NAME' has already been used in this assembly
                            Diagnostic(ErrorCode.ERR_ResourceNotUnique).WithArguments("some.dotted.NAME")
                            );
                    }

                    result6 = emit(c6, output6,
                        new ResourceDescription[]
                        {
                            new ResourceDescription(r2Name, () => new MemoryStream(resourceFileData), false)
                        });

                    if (metadataOnly)
                    {
                        Assert.True(result6.Success);
                    }
                    else
                    {
                        Assert.False(result6.Success);
                        result6.Diagnostics.Verify(
                            // error CS1508: Resource identifier 'some.dotted.NAME' has already been used in this assembly
                            Diagnostic(ErrorCode.ERR_ResourceNotUnique).WithArguments("some.dotted.NAME"),
                            // error CS1508: Resource identifier 'another.DoTtEd.NAME' has already been used in this assembly
                            Diagnostic(ErrorCode.ERR_ResourceNotUnique).WithArguments("another.DoTtEd.NAME")
                            );
                    }

                    c6 = CreateCompilationWithMscorlib(sourceTree, new[] { ref_mod1, ref_mod2 }, TestOptions.ReleaseModule);

                    result6 = emit(c6, output6,
                        new ResourceDescription[]
                        {
                            new ResourceDescription(r2Name, () => new MemoryStream(resourceFileData), false)
                        });

                    Assert.True(result6.Success);
                }
            }
        }

        [Fact]
        public void AddManagedLinkedResourceFail()
        {
            string source = @"
public class Maine
{
    static public void Main()
    {
    }
}
";
            var c1 = CreateCompilationWithMscorlib(source);

            var output = new MemoryStream();

            const string r2Name = "another.DoTtEd.NAME";

            var result = c1.Emit(output, manifestResources:
                new ResourceDescription[]
                {
                    new ResourceDescription(r2Name, "nonExistent", () => { throw new NotSupportedException("error in data provider"); }, false)
                });

            Assert.False(result.Success);
            Assert.Equal((int)ErrorCode.ERR_CantReadResource, result.Diagnostics.ToArray()[0].Code);
        }

        [Fact]
        public void AddManagedEmbeddedResourceFail()
        {
            string source = @"
public class Maine
{
    static public void Main()
    {
    }
}
";
            var c1 = CreateCompilationWithMscorlib(source);

            var output = new MemoryStream();

            const string r2Name = "another.DoTtEd.NAME";

            var result = c1.Emit(output, manifestResources:
                new ResourceDescription[]
                {
                    new ResourceDescription(r2Name, () => null, true),
                });

            Assert.False(result.Success);
            Assert.Equal((int)ErrorCode.ERR_CantReadResource, result.Diagnostics.ToArray()[0].Code);
        }

        [Fact]
        public void ResourceWithAttrSettings()
        {
            string source = @"
[assembly: System.Reflection.AssemblyVersion(""1.2.3.4"")]
[assembly: System.Reflection.AssemblyFileVersion(""5.6.7.8"")]
[assembly: System.Reflection.AssemblyTitle(""One Hundred Years of Solitude"")] 
[assembly: System.Reflection.AssemblyDescription(""A classic of magical realist literature"")]
[assembly: System.Reflection.AssemblyCompany(""MossBrain"")]
[assembly: System.Reflection.AssemblyProduct(""Sound Cannon"")]
[assembly: System.Reflection.AssemblyCopyright(""circle C"")]
[assembly: System.Reflection.AssemblyTrademark(""circle R"")]
[assembly: System.Reflection.AssemblyInformationalVersion(""1.2.3garbage"")]

public class Maine
{
    public static void Main()
    {
    }
}
";
            var c1 = CreateCompilationWithMscorlib(source, assemblyName: "Win32VerAttrs", options: TestOptions.ReleaseExe);
            var exeFile = Temp.CreateFile();

            using (FileStream output = exeFile.Open())
            {
                c1.Emit(output, win32Resources: c1.CreateDefaultWin32Resources(true, false, null, null));
            }

            c1 = null;
            string versionData;

            //Open as data
            IntPtr lib = IntPtr.Zero;
            try
            {
                lib = LoadLibraryEx(exeFile.Path, IntPtr.Zero, 0x00000002);
                Assert.True(lib != IntPtr.Zero, String.Format("LoadLibrary failed with HResult: {0:X}", +Marshal.GetLastWin32Error()));

                //the manifest and version primitives are tested elsewhere. This is to test that the default
                //values are passed to the primitives that assemble the resources.

                uint size;
                IntPtr versionRsrc = Win32Res.GetResource(lib, "#1", "#16", out size);
                versionData = Win32Res.VersionResourceToXml(versionRsrc);
            }
            finally
            {
                if (lib != IntPtr.Zero)
                {
                    FreeLibrary(lib);
                }
            }

            string expected =
@"<?xml version=""1.0"" encoding=""utf-16""?>
<VersionResource Size=""964"">
  <VS_FIXEDFILEINFO FileVersionMS=""00050006"" FileVersionLS=""00070008"" ProductVersionMS=""00000000"" ProductVersionLS=""00000000"" />
  <KeyValuePair Key=""Comments"" Value=""A classic of magical realist literature"" />
  <KeyValuePair Key=""CompanyName"" Value=""MossBrain"" />
  <KeyValuePair Key=""FileDescription"" Value=""One Hundred Years of Solitude"" />
  <KeyValuePair Key=""FileVersion"" Value=""5.6.7.8"" />
  <KeyValuePair Key=""InternalName"" Value=""Win32VerAttrs.exe"" />
  <KeyValuePair Key=""LegalCopyright"" Value=""circle C"" />
  <KeyValuePair Key=""LegalTrademarks"" Value=""circle R"" />
  <KeyValuePair Key=""OriginalFilename"" Value=""Win32VerAttrs.exe"" />
  <KeyValuePair Key=""ProductName"" Value=""Sound Cannon"" />
  <KeyValuePair Key=""ProductVersion"" Value=""1.2.3garbage"" />
  <KeyValuePair Key=""Assembly Version"" Value=""1.2.3.4"" />
</VersionResource>";

            Assert.Equal(expected, versionData);
        }

        [Fact]
        public void ResourceProviderStreamGivesBadLength()
        {
            var backingStream = new MemoryStream(new byte[] { 1, 2, 3, 4 });
            var stream = new TestStream(
                readFunc: backingStream.Read,
                length: 6, // Lie about the length (> backingStream.Length)
                getPosition: () => backingStream.Position);

            var c1 = CreateCompilationWithMscorlib("");

            using (new EnsureEnglishUICulture())
            {
                var result = c1.Emit(new MemoryStream(), manifestResources:
                    new[]
                    {
                    new ResourceDescription("res", () => stream, false)
                    });

                result.Diagnostics.Verify(
    // error CS1566: Error reading resource 'res' -- 'Resource stream ended at 4 bytes, expected 6 bytes.'
    Diagnostic(ErrorCode.ERR_CantReadResource).WithArguments("res", "Resource stream ended at 4 bytes, expected 6 bytes.").WithLocation(1, 1));
            }
        }
    }
}
