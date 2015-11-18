// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Symbols.Metadata.PE;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public class AssemblyAttributeTests : CSharpTestBase
    {
        private readonly string _netModuleName = GetUniqueName() + ".netmodule";

        [Fact]
        public void VersionAttribute()
        {
            string s = @"[assembly: System.Reflection.AssemblyVersion(""1.2.3.4"")] public class C {}";

            var other = CreateCompilationWithMscorlib(s, options: TestOptions.ReleaseDll);
            Assert.Empty(other.GetDiagnostics());
            Assert.Equal(new Version(1, 2, 3, 4), other.Assembly.Identity.Version);
        }

        [Fact]
        public void VersionAttribute02()
        {
            string s = @"[assembly: System.Reflection.AssemblyVersion(""1.22.333.4444"")] public class C {}";

            var comp = CreateCompilationWithMscorlib(s, options: TestOptions.ReleaseDll);
            VerifyAssemblyTable(comp, r =>
            {
                Assert.Equal(new Version(1, 22, 333, 4444), r.Version);
            });

            s = @"[assembly: System.Reflection.AssemblyVersion(""10101.0.*"")] public class C {}";
            comp = CreateCompilationWithMscorlib(s, options: TestOptions.ReleaseDll);
            VerifyAssemblyTable(comp, r =>
            {
                Assert.Equal(10101, r.Version.Major);
                Assert.Equal(0, r.Version.Minor);
            });
        }

        [Fact, WorkItem(545947, "DevDiv")]
        public void VersionAttributeErr()
        {
            string s = @"[assembly: System.Reflection.AssemblyVersion(""1.*"")] public class C {}";

            var other = CreateCompilationWithMscorlib(s, options: TestOptions.ReleaseDll);
            other.VerifyDiagnostics(
                // (1,46): error CS7034: The specified version string does not conform to the required format - major[.minor[.build[.revision]]]
                // [assembly: System.Reflection.AssemblyVersion("1.*")] public class C {}
                Diagnostic(ErrorCode.ERR_InvalidVersionFormat, @"""1.*""").WithLocation(1, 46));

            s = @"[assembly: System.Reflection.AssemblyVersion(""-1"")] public class C {}";
            other = CreateCompilationWithMscorlib(s, options: TestOptions.ReleaseDll);
            other.VerifyDiagnostics(
                // (1,46): error CS7034: The specified version string does not conform to the required format - major[.minor[.build[.revision]]]
                // [assembly: System.Reflection.AssemblyVersion("-1")] public class C {}
                Diagnostic(ErrorCode.ERR_InvalidVersionFormat, @"""-1""").WithLocation(1, 46));
        }

        [Fact]
        public void FileVersionAttribute()
        {
            string s = @"[assembly: System.Reflection.AssemblyFileVersion(""1.2.3.4"")] public class C {}";

            var other = CreateCompilationWithMscorlib(s, options: TestOptions.ReleaseDll);
            Assert.Empty(other.GetDiagnostics());
            Assert.Equal("1.2.3.4", ((SourceAssemblySymbol)other.Assembly).FileVersion);
        }

        [Fact]
        public void FileVersionAttributeWrn()
        {
            string s = @"[assembly: System.Reflection.AssemblyFileVersion(""1.2.*"")] public class C {}";

            var other = CreateCompilationWithMscorlib(s, options: TestOptions.ReleaseDll);
            other.VerifyDiagnostics(Diagnostic(ErrorCode.WRN_InvalidVersionFormat, @"""1.2.*"""));

            // Confirm that suppressing the old alink warning 1607 shuts off WRN_ConflictingMachineAssembly
            var warnings = new System.Collections.Generic.Dictionary<string, ReportDiagnostic>();
            warnings.Add(MessageProvider.Instance.GetIdForErrorCode((int)ErrorCode.WRN_ALinkWarn), ReportDiagnostic.Suppress);
            other = other.WithOptions(other.Options.WithSpecificDiagnosticOptions(warnings));
            other.VerifyEmitDiagnostics();
        }

        [Fact, WorkItem(545947, "DevDiv"), WorkItem(546971, "DevDiv")]
        public void SatelliteContractVersionAttributeErr()
        {
            string s = @"[assembly: System.Resources.SatelliteContractVersionAttribute(""1.2.3.A"")] public class C {}";

            var other = CreateCompilationWithMscorlib(s, options: TestOptions.ReleaseDll);
            other.VerifyDiagnostics(
                // (1,63): error CS7031: The specified version string does not conform to the required format - major.minor.build.revision
                // [assembly: System.Resources.SatelliteContractVersionAttribute("1.2.3.A")] public class C {}
                Diagnostic(ErrorCode.ERR_InvalidVersionFormat2, @"""1.2.3.A""").WithLocation(1, 63));

            s = @"[assembly: System.Resources.SatelliteContractVersionAttribute(""1.2.*"")] public class C {}";

            other = CreateCompilationWithMscorlib(s, options: TestOptions.ReleaseDll);
            other.VerifyDiagnostics(
                // (1,63): error CS7031: The specified version string does not conform to the required format - major.minor.build.revision
                // [assembly: System.Resources.SatelliteContractVersionAttribute("1.2.*")] public class C {}
                Diagnostic(ErrorCode.ERR_InvalidVersionFormat2, @"""1.2.*""").WithLocation(1, 63));

            s = @"[assembly: System.Resources.SatelliteContractVersionAttribute(""1"")] public class C {}";

            other = CreateCompilationWithMscorlib(s, options: TestOptions.ReleaseDll);
            other.VerifyDiagnostics();
        }

        [Fact]
        public void TitleAttribute()
        {
            string s = @"[assembly: System.Reflection.AssemblyTitle(""One Hundred Years of Solitude"")] public class C {}";

            var other = CreateCompilationWithMscorlib(s, options: TestOptions.ReleaseDll);
            Assert.Empty(other.GetDiagnostics());
            Assert.Equal("One Hundred Years of Solitude", ((SourceAssemblySymbol)other.Assembly).Title);
        }

        [Fact]
        public void TitleAttributeNull()
        {
            string s = @"[assembly: System.Reflection.AssemblyTitle(null)] public class C {}";

            var other = CreateCompilationWithMscorlib(s, options: TestOptions.ReleaseDll);
            Assert.Empty(other.GetDiagnostics());
            Assert.Null(((SourceAssemblySymbol)other.Assembly).Title);
        }

        [Fact]
        public void DescriptionAttribute()
        {
            string s = @"[assembly: System.Reflection.AssemblyDescription(""A classic of magical realist literature"")] public class C {}";

            var other = CreateCompilationWithMscorlib(s, options: TestOptions.ReleaseDll);
            Assert.Empty(other.GetDiagnostics());
            Assert.Equal("A classic of magical realist literature", ((SourceAssemblySymbol)other.Assembly).Description);
        }

        [Fact]
        public void CultureAttribute()
        {
            string s = @"[assembly: System.Reflection.AssemblyCulture(""pt-BR"")] public class C {}";

            var other = CreateCompilationWithMscorlib(s, options: TestOptions.ReleaseDll);
            Assert.Empty(other.GetDiagnostics());
            Assert.Equal("pt-BR", (other.Assembly.Identity.CultureName));
        }

        [Fact]
        public void CultureAttribute02()
        {
            string s = @"[assembly: System.Reflection.AssemblyCultureAttribute("""")]";
            var comp = CreateCompilationWithMscorlib(s, options: TestOptions.ReleaseDll);
            VerifyAssemblyTable(comp, r => { Assert.True(r.Culture.IsNil); });

            s = @"[assembly: System.Reflection.AssemblyCulture(null)] public class C {  static void Main() { }  }";
            comp = CreateCompilationWithMscorlib(s, options: TestOptions.ReleaseDll);
            VerifyAssemblyTable(comp, r => { Assert.True(r.Culture.IsNil); });

            s = @"[assembly: System.Reflection.AssemblyCultureAttribute(""zh-CN"")]";
            comp = CreateCompilationWithMscorlib(s, options: TestOptions.ReleaseDll);
            VerifyAssemblyTable(comp, null, strData: "zh-CN");
        }

        [Fact, WorkItem(545949, "DevDiv")]
        public void CultureAttribute03()
        {
            // Executables cannot be satellite assemblies; culture should always be empty
            string s = @"[assembly: System.Reflection.AssemblyCulture(null)] public class C {  static void Main() { }  }";
            var comp = CreateCompilationWithMscorlib(s, options: TestOptions.ReleaseExe);
            VerifyAssemblyTable(comp, r => { Assert.True(r.Culture.IsNil); });

            s = @"[assembly: System.Reflection.AssemblyCulture("""")] public class C {  static void Main() { }  }";
            comp = CreateCompilationWithMscorlib(s, options: TestOptions.ReleaseExe);
            VerifyAssemblyTable(comp, r => { Assert.True(r.Culture.IsNil); });
        }

        [Fact, WorkItem(545949, "DevDiv")]
        public void CultureAttributeErr()
        {
            string s = @"[assembly: System.Reflection.AssemblyCulture(""pt-BR"")] public class C {  static void Main() { }  }";
            var comp = CreateCompilationWithMscorlib(s, options: TestOptions.ReleaseExe);
            comp.VerifyDiagnostics(
                // (1,46): error CS7059: Executables cannot be satellite assemblies; culture should always be empty
                // [assembly: System.Reflection.AssemblyCulture("pt-BR")] public class C {  static void Main() { }  }
                Diagnostic(ErrorCode.ERR_InvalidAssemblyCultureForExe, @"""pt-BR""").WithLocation(1, 46));
        }

        [Fact, WorkItem(1032718)]
        public void MismatchedSurrogateInAssemblyCultureAttribute()
        {
            string s = @"[assembly: System.Reflection.AssemblyCultureAttribute(""\uD800"")]";
            var comp = CreateCompilationWithMscorlib(s, options: TestOptions.ReleaseDll);

            CompileAndVerify(comp, verify: false, symbolValidator: m =>
            {
                var utf8 = new System.Text.UTF8Encoding(false, false);
                Assert.Equal(utf8.GetString(utf8.GetBytes("\uD800")), m.ContainingAssembly.Identity.CultureName);
            });
        }

        [Fact, WorkItem(1034455)]
        public void NulCharInAssemblyCultureAttribute()
        {
            string s = @"[assembly: System.Reflection.AssemblyCultureAttribute(""\0"")]";
            var comp = CreateCompilationWithMscorlib(s, options: TestOptions.ReleaseDll);
            comp.VerifyDiagnostics(
                // (1,55): error CS7100: Assembly culture strings may not contain embedded NUL characters.
                // [assembly: System.Reflection.AssemblyCultureAttribute("\0")]
                Diagnostic(ErrorCode.ERR_InvalidAssemblyCulture, @"""\0""").WithLocation(1, 55));
        }

        [Fact(Skip = "Issue #321")]
        public void CultureAttributeMismatch()
        {
            var neutral = CreateCompilationWithMscorlib(
@"
public class neutral
{}
", options: TestOptions.ReleaseDll, assemblyName: "neutral");

            var neutralRef = new CSharpCompilationReference(neutral);

            var de = CreateCompilationWithMscorlib(
@"
[assembly: System.Reflection.AssemblyCultureAttribute(""de"")]

public class de
{}
", options: TestOptions.ReleaseDll, assemblyName: "de");

            var deRef = new CSharpCompilationReference(de);

            var en_us = CreateCompilationWithMscorlib(
@"
[assembly: System.Reflection.AssemblyCultureAttribute(""en-us"")]

public class en_us
{}
", options: TestOptions.ReleaseDll, assemblyName: "en_us");

            var en_usRef = new CSharpCompilationReference(en_us);

            CSharpCompilation compilation;
            string assemblyNameBase = Guid.NewGuid().ToString();

            compilation = CreateCompilationWithMscorlib(
@"
[assembly: System.Reflection.AssemblyCultureAttribute(""en-US"")]

public class en_US
{
    void M(de x)
    {}
}
", new MetadataReference[] { deRef, neutralRef }, TestOptions.ReleaseDll, assemblyName: assemblyNameBase + "10");

            CompileAndVerify(compilation).VerifyDiagnostics(
    // warning CS8009: Referenced assembly 'de, Version=0.0.0.0, Culture=de, PublicKeyToken=null' has different culture setting of 'de'.
    Diagnostic(ErrorCode.WRN_RefCultureMismatch).WithArguments("de, Version=0.0.0.0, Culture=de, PublicKeyToken=null", "de")
                );

            compilation = compilation.WithOptions(TestOptions.ReleaseModule);
            compilation.VerifyEmitDiagnostics();

            compilation = CreateCompilationWithMscorlib("", new MetadataReference[] { compilation.EmitToImageReference() }, TestOptions.ReleaseDll, assemblyName: assemblyNameBase + "20");

            CompileAndVerify(compilation).VerifyDiagnostics(
    // warning CS8009: Referenced assembly 'de, Version=0.0.0.0, Culture=de, PublicKeyToken=null' has different culture setting of 'de'.
    Diagnostic(ErrorCode.WRN_RefCultureMismatch).WithArguments("de, Version=0.0.0.0, Culture=de, PublicKeyToken=null", "de")
                );

            // Confirm that suppressing the old alink warning 1607 shuts off WRN_RefCultureMismatch
            var warnings = new Dictionary<string, ReportDiagnostic>();
            warnings.Add(MessageProvider.Instance.GetIdForErrorCode((int)ErrorCode.WRN_ALinkWarn), ReportDiagnostic.Suppress);
            compilation = compilation.WithOptions(compilation.Options.WithSpecificDiagnosticOptions(warnings));
            compilation.VerifyEmitDiagnostics();

            compilation = CreateCompilationWithMscorlib(
@"
[assembly: System.Reflection.AssemblyCultureAttribute(""en-US"")]

public class Test
{
    void M(en_us x)
    {}
}
", new MetadataReference[] { en_usRef }, TestOptions.ReleaseDll, assemblyName: assemblyNameBase + "23");

            compilation.VerifyEmitDiagnostics();

            compilation = compilation.WithOptions(TestOptions.ReleaseModule);
            compilation.VerifyEmitDiagnostics();

            compilation = CreateCompilationWithMscorlib("", new MetadataReference[] { compilation.EmitToImageReference() }, TestOptions.ReleaseDll, assemblyName: assemblyNameBase + "25");
            compilation.VerifyEmitDiagnostics();

            compilation = CreateCompilationWithMscorlib(
@"
[assembly: System.Reflection.AssemblyCultureAttribute(""en-US"")]

public class en_US
{
    void M(neutral x)
    {}
}
", new MetadataReference[] { deRef, neutralRef }, TestOptions.ReleaseDll, assemblyName: assemblyNameBase + "30");

            CompileAndVerify(compilation).VerifyDiagnostics();

            compilation = compilation.WithOptions(TestOptions.ReleaseModule);
            compilation.VerifyEmitDiagnostics();

            compilation = CreateCompilationWithMscorlib("", new MetadataReference[] { compilation.EmitToImageReference() }, TestOptions.ReleaseDll, assemblyName: assemblyNameBase + "40");

            CompileAndVerify(compilation,
                // TODO: KevinH - I'm not sure why PeVerify started requiring this assembly after I refactored some test helpers.
                //       I verified that the actual assemblies being compiled only differ by MVID before/after, so I don't think
                //       it's a product issue.  I *think* that one of the CompileAndVerify calls above may have been writing the
                //       neutral assembly to disk somewhere that Fusion could find and load it (perhaps RefEmit wrote it to disk?).
                dependencies: new[]
                {
                    new ModuleData(
                        neutral.Assembly.Identity,
                        OutputKind.DynamicallyLinkedLibrary,
                        neutral.EmitToArray(options: new EmitOptions(metadataOnly: true)),
                        pdb: default(ImmutableArray<byte>),
                        inMemoryModule: true)
                },
                sourceSymbolValidator: m =>
                {
                    Assert.Equal(1, m.GetReferencedAssemblySymbols().Length);

                    var naturalRef = m.ContainingAssembly.Modules[1].GetReferencedAssemblySymbols()[1];
                    Assert.True(naturalRef.IsMissing);
                    Assert.Equal("neutral, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null", naturalRef.ToTestDisplayString());
                },
                symbolValidator: m =>
                {
                    Assert.Equal(2, ((PEModuleSymbol)m).GetReferencedAssemblySymbols().Length);
                    Assert.Equal("neutral, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null", m.GetReferencedAssemblySymbols()[1].ToTestDisplayString());
                }).VerifyDiagnostics();

            compilation = CreateCompilationWithMscorlib(
@"
public class neutral
{
    void M(de x)
    {}
}
", new MetadataReference[] { deRef }, TestOptions.ReleaseDll, assemblyName: assemblyNameBase + "50");

            CompileAndVerify(compilation).VerifyDiagnostics(
    // warning CS8009: Referenced assembly 'de, Version=0.0.0.0, Culture=de, PublicKeyToken=null' has different culture setting of 'de'.
    Diagnostic(ErrorCode.WRN_RefCultureMismatch).WithArguments("de, Version=0.0.0.0, Culture=de, PublicKeyToken=null", "de")
                );

            compilation = compilation.WithOptions(TestOptions.ReleaseModule);
            compilation.VerifyEmitDiagnostics();

            compilation = CreateCompilationWithMscorlib("", new MetadataReference[] { compilation.EmitToImageReference() }, TestOptions.ReleaseDll, assemblyName: assemblyNameBase + "60");

            CompileAndVerify(compilation).VerifyDiagnostics(
    // warning CS8009: Referenced assembly 'de, Version=0.0.0.0, Culture=de, PublicKeyToken=null' has different culture setting of 'de'.
    Diagnostic(ErrorCode.WRN_RefCultureMismatch).WithArguments("de, Version=0.0.0.0, Culture=de, PublicKeyToken=null", "de")
                );
        }

        [Fact]
        public void CompanyAttribute()
        {
            string s = @"[assembly: System.Reflection.AssemblyCompany(""MossBrain"")] public class C {}";

            var other = CreateCompilationWithMscorlib(s, options: TestOptions.ReleaseDll);
            Assert.Empty(other.GetDiagnostics());
            Assert.Equal("MossBrain", ((SourceAssemblySymbol)other.Assembly).Company);

            s = @"[assembly: System.Reflection.AssemblyCompany(""微软"")] public class C {}";

            other = CreateCompilationWithMscorlib(s, options: TestOptions.ReleaseDll);
            Assert.Empty(other.GetDiagnostics());
            Assert.Equal("微软", ((SourceAssemblySymbol)other.Assembly).Company);
        }

        [Fact]
        public void ProductAttribute()
        {
            string s = @"[assembly: System.Reflection.AssemblyProduct(""Sound Cannon"")] public class C {}";

            var other = CreateCompilationWithMscorlib(s, options: TestOptions.ReleaseDll);
            Assert.Empty(other.GetDiagnostics());
            Assert.Equal("Sound Cannon", ((SourceAssemblySymbol)other.Assembly).Product);
        }

        [Fact]
        public void CopyrightAttribute()
        {
            string s = @"[assembly: System.Reflection.AssemblyCopyright(""مايكروسوفت"")] public class C {}";

            var other = CreateCompilationWithMscorlib(s, options: TestOptions.ReleaseDll);
            Assert.Empty(other.GetDiagnostics());
            Assert.Equal("مايكروسوفت", ((SourceAssemblySymbol)other.Assembly).Copyright);
        }

        [Fact]
        public void TrademarkAttribute()
        {
            string s = @"[assembly: System.Reflection.AssemblyTrademark(""circle R"")] public class C {}";

            var other = CreateCompilationWithMscorlib(s, options: TestOptions.ReleaseDll);
            Assert.Empty(other.GetDiagnostics());
            Assert.Equal("circle R", ((SourceAssemblySymbol)other.Assembly).Trademark);

            s = @"[assembly: System.Reflection.AssemblyTrademark("""")] namespace N {}";

            other = CreateCompilationWithMscorlib(s, options: TestOptions.ReleaseDll);
            Assert.Empty(other.GetDiagnostics());
            Assert.Equal("", ((SourceAssemblySymbol)other.Assembly).Trademark);
        }

        [Fact]
        public void InformationalVersionAttribute()
        {
            string s = @"[assembly: System.Reflection.AssemblyInformationalVersion(""1.2.3garbage"")] public class C {}";

            var other = CreateCompilationWithMscorlib(s, options: TestOptions.ReleaseDll);
            Assert.Empty(other.GetDiagnostics());
            Assert.Equal("1.2.3garbage", ((SourceAssemblySymbol)other.Assembly).InformationalVersion);
        }

        [Fact, WorkItem(529921, "DevDiv")]
        public void AlgorithmIdAttribute()
        {
            var hash_module = TestReferences.SymbolsTests.netModule.hash_module;

            var hash_resources = new[] {new ResourceDescription("hash_resource", "snKey.snk",
                () => new MemoryStream(TestResources.General.snKey, writable: false),
                true)};

            CSharpCompilation compilation;

            compilation = CreateCompilationWithMscorlib(
@"
class Program
{
    void M(Test x) {}
}
", options: TestOptions.ReleaseDll, references: new[] { hash_module });

            CompileAndVerify(compilation,
                manifestResources: hash_resources,
                validator: (peAssembly) =>
                {
                    var reader = peAssembly.ManifestModule.GetMetadataReader();
                    AssemblyDefinition assembly = reader.GetAssemblyDefinition();
                    Assert.Equal(AssemblyHashAlgorithm.Sha1, assembly.HashAlgorithm);

                    var file1 = reader.GetAssemblyFile(MetadataTokens.AssemblyFileHandle(1));
                    Assert.Equal(new byte[] { 0x6C, 0x9C, 0x3E, 0xDA, 0x60, 0x0F, 0x81, 0x93, 0x4A, 0xC1, 0x0D, 0x41, 0xB3, 0xE9, 0xB2, 0xB7, 0x2D, 0xEE, 0x59, 0xA8 },
                        reader.GetBlobBytes(file1.HashValue));

                    var file2 = reader.GetAssemblyFile(MetadataTokens.AssemblyFileHandle(2));
                    Assert.Equal(new byte[] { 0x7F, 0x28, 0xEA, 0xD1, 0xF4, 0xA1, 0x7C, 0xB8, 0x0C, 0x14, 0xC0, 0x2E, 0x8C, 0xFF, 0x10, 0xEC, 0xB3, 0xC2, 0xA5, 0x1D },
                        reader.GetBlobBytes(file2.HashValue));

                    Assert.Null(peAssembly.ManifestModule.FindTargetAttributes(peAssembly.Handle, AttributeDescription.AssemblyAlgorithmIdAttribute));
                });

            compilation = CreateCompilationWithMscorlib(
@"
[assembly: System.Reflection.AssemblyAlgorithmIdAttribute(System.Configuration.Assemblies.AssemblyHashAlgorithm.None)]

class Program
{
    void M(Test x) {}
}
", options: TestOptions.ReleaseDll, references: new[] { hash_module });

            CompileAndVerify(compilation,
                manifestResources: hash_resources,
                validator: (peAssembly) =>
                {
                    var reader = peAssembly.ManifestModule.GetMetadataReader();
                    AssemblyDefinition assembly = reader.GetAssemblyDefinition();
                    Assert.Equal(AssemblyHashAlgorithm.None, assembly.HashAlgorithm);

                    var file1 = reader.GetAssemblyFile(MetadataTokens.AssemblyFileHandle(1));
                    Assert.Equal(new byte[] { 0x6C, 0x9C, 0x3E, 0xDA, 0x60, 0x0F, 0x81, 0x93, 0x4A, 0xC1, 0x0D, 0x41, 0xB3, 0xE9, 0xB2, 0xB7, 0x2D, 0xEE, 0x59, 0xA8 },
                        reader.GetBlobBytes(file1.HashValue));

                    var file2 = reader.GetAssemblyFile(MetadataTokens.AssemblyFileHandle(2));
                    Assert.Equal(new byte[] { 0x7F, 0x28, 0xEA, 0xD1, 0xF4, 0xA1, 0x7C, 0xB8, 0x0C, 0x14, 0xC0, 0x2E, 0x8C, 0xFF, 0x10, 0xEC, 0xB3, 0xC2, 0xA5, 0x1D },
                        reader.GetBlobBytes(file2.HashValue));

                    Assert.Null(peAssembly.ManifestModule.FindTargetAttributes(peAssembly.Handle, AttributeDescription.AssemblyAlgorithmIdAttribute));
                });

            compilation = CreateCompilationWithMscorlib(
@"
[assembly: System.Reflection.AssemblyAlgorithmIdAttribute((uint)System.Configuration.Assemblies.AssemblyHashAlgorithm.MD5)]

class Program
{
    void M(Test x) {}
}
", options: TestOptions.ReleaseDll, references: new[] { hash_module });

            CompileAndVerify(compilation,
                manifestResources: hash_resources,
                validator: (peAssembly) =>
                {
                    var reader = peAssembly.ManifestModule.GetMetadataReader();
                    AssemblyDefinition assembly = reader.GetAssemblyDefinition();
                    Assert.Equal(AssemblyHashAlgorithm.MD5, assembly.HashAlgorithm);

                    var file1 = reader.GetAssemblyFile(MetadataTokens.AssemblyFileHandle(1));
                    Assert.Equal(new byte[] { 0x24, 0x22, 0x03, 0xC3, 0x94, 0xD5, 0xC2, 0xD9, 0x99, 0xB3, 0x6D, 0x59, 0xB2, 0xCA, 0x23, 0xBC },
                        reader.GetBlobBytes(file1.HashValue));

                    var file2 = reader.GetAssemblyFile(MetadataTokens.AssemblyFileHandle(2));
                    Assert.Equal(new byte[] { 0x8D, 0xFE, 0xBF, 0x49, 0x8D, 0x62, 0x2A, 0x88, 0x89, 0xD1, 0x0E, 0x00, 0x9E, 0x29, 0x72, 0xF1 },
                        reader.GetBlobBytes(file2.HashValue));

                    Assert.Null(peAssembly.ManifestModule.FindTargetAttributes(peAssembly.Handle, AttributeDescription.AssemblyAlgorithmIdAttribute));
                });

            compilation = CreateCompilationWithMscorlib(
@"
[assembly: System.Reflection.AssemblyAlgorithmIdAttribute(System.Configuration.Assemblies.AssemblyHashAlgorithm.SHA1)]

class Program
{
    void M(Test x) {}
}
", options: TestOptions.ReleaseDll, references: new[] { hash_module });

            CompileAndVerify(compilation,
                manifestResources: hash_resources,
                validator: (peAssembly) =>
                {
                    var reader = peAssembly.ManifestModule.GetMetadataReader();
                    AssemblyDefinition assembly = reader.GetAssemblyDefinition();
                    Assert.Equal(AssemblyHashAlgorithm.Sha1, assembly.HashAlgorithm);

                    var file1 = reader.GetAssemblyFile(MetadataTokens.AssemblyFileHandle(1));
                    Assert.Equal(new byte[] { 0x6C, 0x9C, 0x3E, 0xDA, 0x60, 0x0F, 0x81, 0x93, 0x4A, 0xC1, 0x0D, 0x41, 0xB3, 0xE9, 0xB2, 0xB7, 0x2D, 0xEE, 0x59, 0xA8 },
                        reader.GetBlobBytes(file1.HashValue));

                    var file2 = reader.GetAssemblyFile(MetadataTokens.AssemblyFileHandle(2));
                    Assert.Equal(new byte[] { 0x7F, 0x28, 0xEA, 0xD1, 0xF4, 0xA1, 0x7C, 0xB8, 0x0C, 0x14, 0xC0, 0x2E, 0x8C, 0xFF, 0x10, 0xEC, 0xB3, 0xC2, 0xA5, 0x1D },
                        reader.GetBlobBytes(file2.HashValue));
                    Assert.Null(peAssembly.ManifestModule.FindTargetAttributes(peAssembly.Handle, AttributeDescription.AssemblyAlgorithmIdAttribute));
                });

            compilation = CreateCompilation(
@"
[assembly: System.Reflection.AssemblyAlgorithmIdAttribute(System.Configuration.Assemblies.AssemblyHashAlgorithm.SHA256)]

class Program
{
    void M(Test x) {}
}
", options: TestOptions.ReleaseDll, references: new[] { MscorlibRef_v4_0_30316_17626, hash_module });

            CompileAndVerify(compilation, verify: false,
                manifestResources: hash_resources,
                validator: (peAssembly) =>
                {
                    var reader = peAssembly.ManifestModule.GetMetadataReader();
                    AssemblyDefinition assembly = reader.GetAssemblyDefinition();
                    Assert.Equal(System.Configuration.Assemblies.AssemblyHashAlgorithm.SHA256, (System.Configuration.Assemblies.AssemblyHashAlgorithm)assembly.HashAlgorithm);

                    var file1 = reader.GetAssemblyFile(MetadataTokens.AssemblyFileHandle(1));
                    Assert.Equal(new byte[] { 0xA2, 0x32, 0x3F, 0x0D, 0xF4, 0xB8, 0xED, 0x5A, 0x1B, 0x7B, 0xBE, 0x14, 0x4F, 0xEC, 0xBF, 0x88, 0x23, 0x61, 0xEB, 0x40, 0xF7, 0xF9, 0x46, 0xEF, 0x68, 0x3B, 0x70, 0x29, 0xCF, 0x12, 0x05, 0x35 },
                        reader.GetBlobBytes(file1.HashValue));

                    var file2 = reader.GetAssemblyFile(MetadataTokens.AssemblyFileHandle(2));
                    Assert.Equal(new byte[] { 0xCC, 0xAE, 0xA0, 0xB4, 0x9E, 0xAE, 0x28, 0xE0, 0xA3, 0x46, 0xE9, 0xCF, 0xF3, 0xEF, 0xEA, 0xF7,
                                              0x1D, 0xDE, 0x62, 0x8F, 0xD6, 0xF4, 0x87, 0x76, 0x1A, 0xC3, 0x6F, 0xAD, 0x10, 0x1C, 0x10, 0xAC},
                        reader.GetBlobBytes(file2.HashValue));
                    Assert.Null(peAssembly.ManifestModule.FindTargetAttributes(peAssembly.Handle, AttributeDescription.AssemblyAlgorithmIdAttribute));
                });

            compilation = CreateCompilation(
@"
[assembly: System.Reflection.AssemblyAlgorithmIdAttribute(System.Configuration.Assemblies.AssemblyHashAlgorithm.SHA384)]

class Program
{
    void M(Test x) {}
}
", options: TestOptions.ReleaseDll, references: new[] { MscorlibRef_v4_0_30316_17626, hash_module });

            CompileAndVerify(compilation, verify: false,
                manifestResources: hash_resources,
                validator: (peAssembly) =>
                {
                    var reader = peAssembly.ManifestModule.GetMetadataReader();
                    AssemblyDefinition assembly = reader.GetAssemblyDefinition();
                    Assert.Equal(System.Configuration.Assemblies.AssemblyHashAlgorithm.SHA384, (System.Configuration.Assemblies.AssemblyHashAlgorithm)assembly.HashAlgorithm);

                    var file1 = reader.GetAssemblyFile(MetadataTokens.AssemblyFileHandle(1));
                    Assert.Equal(new byte[] { 0xB6, 0x35, 0x9B, 0xBE, 0x82, 0x89, 0xFF, 0x01, 0x22, 0x8B, 0x56, 0x5E, 0x9B, 0x15, 0x5D, 0x10,
                                              0x68, 0x83, 0xF7, 0x75, 0x4E, 0xA6, 0x30, 0xF7, 0x8D, 0x39, 0x9A, 0xB7, 0xE8, 0xB6, 0x47, 0x1F,
                                              0xF6, 0xFD, 0x1E, 0x64, 0x63, 0x6B, 0xE7, 0xF4, 0xBE, 0xA7, 0x21, 0xED, 0xFC, 0x82, 0x38, 0x95},
                        reader.GetBlobBytes(file1.HashValue));

                    var file2 = reader.GetAssemblyFile(MetadataTokens.AssemblyFileHandle(2));
                    Assert.Equal(new byte[] { 0x45, 0x05, 0x2E, 0x90, 0x9B, 0x61, 0xA3, 0xF8, 0x60, 0xD2, 0x86, 0xCB, 0x10, 0x33, 0xC9, 0x86,
                                              0x68, 0xA5, 0xEE, 0x4A, 0xCF, 0x21, 0x10, 0xA9, 0x8F, 0x14, 0x62, 0x8D, 0x3E, 0x7D, 0xFD, 0x7E,
                                              0xE6, 0x23, 0x6F, 0x2D, 0xBA, 0x04, 0xE7, 0x13, 0xE4, 0x5E, 0x8C, 0xEB, 0x80, 0x68, 0xA3, 0x17},
                        reader.GetBlobBytes(file2.HashValue));

                    Assert.Null(peAssembly.ManifestModule.FindTargetAttributes(peAssembly.Handle, AttributeDescription.AssemblyAlgorithmIdAttribute));
                });

            compilation = CreateCompilation(
@"
[assembly: System.Reflection.AssemblyAlgorithmIdAttribute(System.Configuration.Assemblies.AssemblyHashAlgorithm.SHA512)]

class Program
{
    void M(Test x) {}
}
", options: TestOptions.ReleaseDll, references: new[] { MscorlibRef_v4_0_30316_17626, hash_module });

            CompileAndVerify(compilation, verify: false,
                manifestResources: hash_resources,
                validator: (peAssembly) =>
                {
                    var reader = peAssembly.ManifestModule.GetMetadataReader();
                    AssemblyDefinition assembly = reader.GetAssemblyDefinition();
                    Assert.Equal(System.Configuration.Assemblies.AssemblyHashAlgorithm.SHA512, (System.Configuration.Assemblies.AssemblyHashAlgorithm)assembly.HashAlgorithm);

                    var file1 = reader.GetAssemblyFile(MetadataTokens.AssemblyFileHandle(1));
                    Assert.Equal(new byte[] { 0x5F, 0x4D, 0x7E, 0x63, 0xC9, 0x87, 0xD9, 0xEB, 0x4F, 0x5C, 0xFD, 0x96, 0x3F, 0x25, 0x58, 0x74,
                                              0x86, 0xDF, 0x97, 0x75, 0x93, 0xEE, 0xC2, 0x5F, 0xFD, 0x8A, 0x40, 0x5C, 0x92, 0x5E, 0xB5, 0x07,
                                              0xD6, 0x12, 0xE9, 0x21, 0x55, 0xCE, 0xD7, 0xE5, 0x15, 0xF5, 0xBA, 0xBC, 0x1B, 0x31, 0xAD, 0x3C,
                                              0x5E, 0xE0, 0x91, 0x98, 0xC2, 0xE0, 0x96, 0xBB, 0xAD, 0x0D, 0x4E, 0xF4, 0x91, 0x53, 0x3D, 0x84},
                        reader.GetBlobBytes(file1.HashValue));

                    var file2 = reader.GetAssemblyFile(MetadataTokens.AssemblyFileHandle(2));
                    Assert.Equal(new byte[] { 0x79, 0xFE, 0x97, 0xAB, 0x08, 0x8E, 0xDF, 0x74, 0xC2, 0xEF, 0x84, 0xBB, 0xFC, 0x74, 0xAC, 0x60,
                                              0x18, 0x6E, 0x1A, 0xD2, 0xC5, 0x94, 0xE0, 0xDA, 0xE0, 0x45, 0x33, 0x43, 0x99, 0xF0, 0xF3, 0xF1,
                                              0x72, 0x05, 0x4B, 0x0F, 0x37, 0x50, 0xC5, 0xD9, 0xCE, 0x29, 0x82, 0x4C, 0xF7, 0xE6, 0x94, 0x5F,
                                              0xE5, 0x07, 0x2B, 0x4A, 0x18, 0x09, 0x56, 0xC9, 0x52, 0x69, 0x7D, 0xC4, 0x48, 0x63, 0x70, 0xF2},
                        reader.GetBlobBytes(file2.HashValue));

                    Assert.Null(peAssembly.ManifestModule.FindTargetAttributes(peAssembly.Handle, AttributeDescription.AssemblyAlgorithmIdAttribute));
                });

            var hash_module_Comp = CreateCompilationWithMscorlib(
@"
[assembly: System.Reflection.AssemblyAlgorithmIdAttribute(System.Configuration.Assemblies.AssemblyHashAlgorithm.MD5)]

public class Test
{}", options: TestOptions.ReleaseModule);

            compilation = CreateCompilationWithMscorlib(
@"
class Program
{
    void M(Test x) {}
}
", options: TestOptions.ReleaseDll, references: new[] { hash_module_Comp.EmitToImageReference() });

            CompileAndVerify(compilation,
                validator: (peAssembly) =>
                {
                    var peReader = peAssembly.ManifestModule.GetMetadataReader();
                    AssemblyDefinition assembly = peReader.GetAssemblyDefinition();
                    Assert.Equal(AssemblyHashAlgorithm.MD5, assembly.HashAlgorithm);
                    Assert.Null(peAssembly.ManifestModule.FindTargetAttributes(peAssembly.Handle, AttributeDescription.AssemblyAlgorithmIdAttribute));
                });

            compilation = CreateCompilationWithMscorlib(
@"
[assembly: System.Reflection.AssemblyAlgorithmIdAttribute(12345)]

class Program
{
    void M() {}
}
", options: TestOptions.ReleaseDll);

            // no error reported if we don't need to hash
            compilation.VerifyEmitDiagnostics();

            compilation = CreateCompilationWithMscorlib(
@"
[assembly: System.Reflection.AssemblyAlgorithmIdAttribute(12345)]

class Program
{
    void M(Test x) {}
}
", options: TestOptions.ReleaseDll, references: new[] { hash_module });

            compilation.VerifyEmitDiagnostics(
                // error CS8013: Cryptographic failure while creating hashes.
                Diagnostic(ErrorCode.ERR_CryptoHashFailed));

            compilation = CreateCompilationWithMscorlib(
@"
[assembly: System.Reflection.AssemblyAlgorithmIdAttribute(12345)]

class Program
{
    void M() {}
}
", options: TestOptions.ReleaseDll);

            compilation.VerifyEmitDiagnostics(hash_resources,
                // error CS8013: Cryptographic failure while creating hashes.
                Diagnostic(ErrorCode.ERR_CryptoHashFailed));

            string s = @"[assembly: System.Reflection.AssemblyAlgorithmIdAttribute(System.Configuration.Assemblies.AssemblyHashAlgorithm.MD5)] public class C {}";
            var comp = CreateCompilationWithMscorlib(s, options: TestOptions.ReleaseDll);
            Assert.Empty(comp.GetDiagnostics());
            var attrs = comp.Assembly.GetAttributes();
            Assert.Equal(1, attrs.Length);
            VerifyAssemblyTable(comp, r => { Assert.Equal(AssemblyHashAlgorithm.MD5, r.HashAlgorithm); });

            s = @"[assembly: System.Reflection.AssemblyAlgorithmIdAttribute(System.Configuration.Assemblies.AssemblyHashAlgorithm.None)] public class C {}";
            comp = CreateCompilationWithMscorlib(s, options: TestOptions.ReleaseDll);
            VerifyAssemblyTable(comp, r => { Assert.Equal(AssemblyHashAlgorithm.None, r.HashAlgorithm); });

            s = @"[assembly: System.Reflection.AssemblyAlgorithmIdAttribute(12345)] public class C {}";
            comp = CreateCompilationWithMscorlib(s, options: TestOptions.ReleaseDll);
            VerifyAssemblyTable(comp, r => { Assert.Equal(12345, (int)r.HashAlgorithm); });
        }

        [Fact]
        public void AssemblyFlagsAttribute()
        {
            string s = @"using System.Reflection;
[assembly: AssemblyFlags(AssemblyNameFlags.EnableJITcompileOptimizer | AssemblyNameFlags.Retargetable)]
public class C {}
";

            var comp = CreateCompilationWithMscorlib(s, options: TestOptions.ReleaseDll);
            Assert.Empty(comp.GetDiagnostics());
            var attrs = comp.Assembly.GetAttributes();
            Assert.Equal(1, attrs.Length);
            var flags = System.Reflection.AssemblyNameFlags.EnableJITcompileOptimizer | System.Reflection.AssemblyNameFlags.Retargetable;
            VerifyAssemblyTable(comp, r => { Assert.Equal((int)flags, (int)r.Flags); });
        }

        [Fact, WorkItem(546635, "DevDiv")]
        public void AssemblyFlagsAttribute02()
        {
            string s = @"[assembly: System.Reflection.AssemblyFlags(12345)] public class C {} ";

            var comp = CreateCompilationWithMscorlib(s, options: TestOptions.ReleaseDll);
            // Both native & Roslyn PEVerifier fail: [MD]: Error: Invalid Assembly flags (0x3038). [token:0x20000001]
            VerifyAssemblyTable(comp, r => { Assert.Equal((uint)(12345 - 1), (uint)r.Flags); });

            comp.VerifyDiagnostics(
            // (1,12): warning CS0618: 'System.Reflection.AssemblyFlagsAttribute.AssemblyFlagsAttribute(int)' is obsolete: 'This constructor has been deprecated. Please use AssemblyFlagsAttribute(AssemblyNameFlags) instead. http://go.microsoft.com/fwlink/?linkid=14202'
            // [assembly: System.Reflection.AssemblyFlags(12345)] public class C {} 
            Diagnostic(ErrorCode.WRN_DeprecatedSymbolStr, "System.Reflection.AssemblyFlags(12345)").WithArguments("System.Reflection.AssemblyFlagsAttribute.AssemblyFlagsAttribute(int)", "This constructor has been deprecated. Please use AssemblyFlagsAttribute(AssemblyNameFlags) instead. http://go.microsoft.com/fwlink/?linkid=14202"));
        }

        [Fact]
        public void AssemblyFlagsAttribute03()
        {
            string s = @"[assembly: System.Reflection.AssemblyFlags(12345U)] public class C {} ";

            var comp = CreateCompilationWithMscorlib(s, options: TestOptions.ReleaseDll);
            // Both native & Roslyn PEVerifier fail: [MD]: Error: Invalid Assembly flags (0x3038). [token:0x20000001]
            VerifyAssemblyTable(comp, r => { Assert.Equal((uint)(12345 - 1), (uint)r.Flags); });

            comp.VerifyDiagnostics(
            // (1,12): warning CS0618: 'System.Reflection.AssemblyFlagsAttribute.AssemblyFlagsAttribute(int)' is obsolete: 'This constructor has been deprecated. Please use AssemblyFlagsAttribute(AssemblyNameFlags) instead. http://go.microsoft.com/fwlink/?linkid=14202'
            // [assembly: System.Reflection.AssemblyFlags(12345)] public class C {} 
            Diagnostic(ErrorCode.WRN_DeprecatedSymbolStr, "System.Reflection.AssemblyFlags(12345U)").WithArguments("System.Reflection.AssemblyFlagsAttribute.AssemblyFlagsAttribute(uint)", "This constructor has been deprecated. Please use AssemblyFlagsAttribute(AssemblyNameFlags) instead. http://go.microsoft.com/fwlink/?linkid=14202"));
        }

        #region "Metadata Verifier (TODO: consolidate with others)"

        internal void VerifyAssemblyTable(
            CSharpCompilation compilation,
            Action<AssemblyDefinition> verifier,
            string strData = null,
            byte[] blobData = null,
            Guid guidData = default(Guid),
            string uddData = null)
        {
            var stream = new MemoryStream();
            Assert.True(compilation.Emit(stream).Success);
            stream.Position = 0;

            using (var metadata = ModuleMetadata.CreateFromStream(stream))
            {
                var peReader = metadata.MetadataReader;
                AssemblyDefinition row = peReader.GetAssemblyDefinition();
                verifier?.Invoke(row);

                // Locale
                // temp
                if (strData != null)
                {
                    Assert.Equal(strData, peReader.GetString(row.Culture));
                }

                // PublicKey
                //Assert.Equal((uint)0, row.PublicKey);
            }
        }

        #endregion

        #region NetModule Assembly attribute tests

        #region Helpers

        private static readonly string s_defaultNetModuleSourceHeader =
            @"using System;
                using System.Reflection;
                using System.Security.Permissions;

                [assembly: AssemblyTitle(""AssemblyTitle"")]
                [assembly: FileIOPermission(SecurityAction.RequestOptional)]
                [assembly: UserDefinedAssemblyAttrNoAllowMultiple(""UserDefinedAssemblyAttrNoAllowMultiple"")]
                [assembly: UserDefinedAssemblyAttrAllowMultiple(""UserDefinedAssemblyAttrAllowMultiple"")]
            ";

        private static readonly string s_defaultNetModuleSourceBody =
                @"
                public class NetModuleClass { }

                [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = false)]
                public class UserDefinedAssemblyAttrNoAllowMultipleAttribute : Attribute
                {
                    public string Text { get; set; }
                    public string Text2 { get; set; }
                    public UserDefinedAssemblyAttrNoAllowMultipleAttribute(string text) { Text = text; }
                    public UserDefinedAssemblyAttrNoAllowMultipleAttribute(int text) { Text = text.ToString(); }
                    public UserDefinedAssemblyAttrNoAllowMultipleAttribute(object text) { Text = text.ToString(); }
                }

                [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
                public class UserDefinedAssemblyAttrAllowMultipleAttribute : Attribute
                {
                    public string Text { get; set; }
                    public string Text2 { get; set; }
                    public UserDefinedAssemblyAttrAllowMultipleAttribute(string text) { Text = text; }
                    public UserDefinedAssemblyAttrAllowMultipleAttribute(int text) { Text = text.ToString(); }
                    public UserDefinedAssemblyAttrAllowMultipleAttribute(object text) { Text = text.ToString(); }
                }
                ";

        private MetadataReference GetNetModuleWithAssemblyAttributesRef(string source = null, IEnumerable<MetadataReference> references = null)
        {
            string assemblyName = GetUniqueName();
            return GetNetModuleWithAssemblyAttributes(source, references, assemblyName).GetReference(display: assemblyName + ".netmodule");
        }

        private ModuleMetadata GetNetModuleWithAssemblyAttributes(string source = null, IEnumerable<MetadataReference> references = null, string assemblyName = null)
        {
            source = source ?? s_defaultNetModuleSourceHeader + s_defaultNetModuleSourceBody;
            var netmoduleCompilation = CreateCompilationWithMscorlib(source, options: TestOptions.ReleaseModule, references: references, assemblyName: assemblyName);
            return ModuleMetadata.CreateFromImage(netmoduleCompilation.EmitToArray());
        }

        private static void TestDuplicateAssemblyAttributesNotEmitted(AssemblySymbol assembly, int expectedSrcAttrCount, int expectedDuplicateAttrCount, string attrTypeName)
        {
            // SOURCE ATTRIBUTES

            var allSrcAttrs = assembly.GetAttributes();
            var srcAttrs = allSrcAttrs.Where((a) => string.Equals(a.AttributeClass.Name, attrTypeName, StringComparison.Ordinal)).AsImmutable();

            Assert.Equal(expectedSrcAttrCount, srcAttrs.Length);


            // EMITTED ATTRIBUTES

            // We should get only unique netmodule/assembly attributes here, duplicate ones should not be emitted.
            int expectedEmittedAttrsCount = expectedSrcAttrCount - expectedDuplicateAttrCount;

            var allEmittedAttrs = assembly.GetCustomAttributesToEmit(new ModuleCompilationState(), emittingAssemblyAttributesInNetModule: false);
            var emittedAttrs = allEmittedAttrs.Where(a => string.Equals(a.AttributeClass.Name, attrTypeName, StringComparison.Ordinal)).AsImmutable();

            Assert.Equal(expectedEmittedAttrsCount, emittedAttrs.Length);
            var uniqueAttributes = new HashSet<CSharpAttributeData>(comparer: CommonAttributeDataComparer.Instance);
            foreach (var attr in emittedAttrs)
            {
                Assert.True(uniqueAttributes.Add(attr));
            }
        }

        #endregion

        [Fact]
        public void AssemblyAttributesFromNetModule()
        {
            string consoleappSource =
                @"
                class Program
                {
                    static void Main(string[] args) { }
                }
                ";

            var netModuleWithAssemblyAttributes = GetNetModuleWithAssemblyAttributes();

            PEModule peModule = netModuleWithAssemblyAttributes.Module;
            var metadataReader = peModule.GetMetadataReader();

            Assert.Equal(0, metadataReader.GetTableRowCount(TableIndex.ExportedType));
            Assert.Equal(18, metadataReader.CustomAttributes.Count);
            Assert.Equal(0, metadataReader.DeclarativeSecurityAttributes.Count);

            EntityHandle token = peModule.GetTypeRef(peModule.GetAssemblyRef("mscorlib"), "System.Runtime.CompilerServices", "AssemblyAttributesGoHereM");
            Assert.False(token.IsNil);   //could the type ref be located? If not then the attribute's not there.

            var consoleappCompilation = CreateCompilationWithMscorlib(
                consoleappSource,
                references: new[] { netModuleWithAssemblyAttributes.GetReference() },
                options: TestOptions.ReleaseExe);

            var diagnostics = consoleappCompilation.GetDiagnostics();

            var attrs = consoleappCompilation.Assembly.GetAttributes();
            Assert.Equal(4, attrs.Length);
            foreach (var a in attrs)
            {
                switch (a.AttributeClass.Name)
                {
                    case "AssemblyTitleAttribute":
                        Assert.Equal(@"System.Reflection.AssemblyTitleAttribute(""AssemblyTitle"")", a.ToString());
                        break;
                    case "FileIOPermissionAttribute":
                        Assert.Equal(@"System.Security.Permissions.FileIOPermissionAttribute(System.Security.Permissions.SecurityAction.RequestOptional)", a.ToString());
                        break;
                    case "UserDefinedAssemblyAttrNoAllowMultipleAttribute":
                        Assert.Equal(@"UserDefinedAssemblyAttrNoAllowMultipleAttribute(""UserDefinedAssemblyAttrNoAllowMultiple"")", a.ToString());
                        break;
                    case "UserDefinedAssemblyAttrAllowMultipleAttribute":
                        Assert.Equal(@"UserDefinedAssemblyAttrAllowMultipleAttribute(""UserDefinedAssemblyAttrAllowMultiple"")", a.ToString());
                        break;
                    default:
                        Assert.Equal("Unexpected Attr", a.AttributeClass.Name);
                        break;
                }
            }

            var exeMetadata = AssemblyMetadata.CreateFromImage(consoleappCompilation.EmitToArray());

            peModule = exeMetadata.GetAssembly().ManifestModule;
            metadataReader = peModule.GetMetadataReader();

            Assert.Equal(1, metadataReader.GetTableRowCount(TableIndex.ModuleRef));
            Assert.Equal(3, metadataReader.GetTableRowCount(TableIndex.ExportedType));
            Assert.Equal(6, metadataReader.CustomAttributes.Count);
            Assert.Equal(1, metadataReader.DeclarativeSecurityAttributes.Count);

            token = peModule.GetTypeRef(peModule.GetAssemblyRef("mscorlib"), "System.Runtime.CompilerServices", "AssemblyAttributesGoHereM");
            Assert.True(token.IsNil);   //could the type ref be located? If not then the attribute's not there.

            consoleappCompilation = CreateCompilationWithMscorlib(
                consoleappSource,
                references: new[] { netModuleWithAssemblyAttributes.GetReference() },
                options: TestOptions.ReleaseModule);

            Assert.Equal(0, consoleappCompilation.Assembly.GetAttributes().Length);

            var modMetadata = ModuleMetadata.CreateFromImage(consoleappCompilation.EmitToArray());

            peModule = modMetadata.Module;
            metadataReader = peModule.GetMetadataReader();

            Assert.Equal(0, metadataReader.GetTableRowCount(TableIndex.ModuleRef));
            Assert.Equal(0, metadataReader.GetTableRowCount(TableIndex.ExportedType));
            Assert.Equal(0, metadataReader.CustomAttributes.Count);
            Assert.Equal(0, metadataReader.DeclarativeSecurityAttributes.Count);

            token = peModule.GetTypeRef(peModule.GetAssemblyRef("mscorlib"), "System.Runtime.CompilerServices", "AssemblyAttributesGoHereM");
            Assert.True(token.IsNil);   //could the type ref be located? If not then the attribute's not there.
        }

        [Fact]
        public void AssemblyAttributesFromNetModuleDropIdentical()
        {
            string consoleappSource =
                @"
                [assembly: UserDefinedAssemblyAttrNoAllowMultiple(""UserDefinedAssemblyAttrNoAllowMultiple"")]
                [assembly: UserDefinedAssemblyAttrAllowMultiple(""UserDefinedAssemblyAttrAllowMultiple"")]

                class Program
                {
                    static void Main(string[] args) { }
                }
                ";

            var consoleappCompilation = CreateCompilationWithMscorlib(consoleappSource, references: new[] { GetNetModuleWithAssemblyAttributesRef() }, options: TestOptions.ReleaseExe);
            var diagnostics = consoleappCompilation.GetDiagnostics();

            TestDuplicateAssemblyAttributesNotEmitted(consoleappCompilation.Assembly,
               expectedSrcAttrCount: 2,
               expectedDuplicateAttrCount: 1,
               attrTypeName: "UserDefinedAssemblyAttrAllowMultipleAttribute");

            TestDuplicateAssemblyAttributesNotEmitted(consoleappCompilation.Assembly,
               expectedSrcAttrCount: 2,
               expectedDuplicateAttrCount: 1,
               attrTypeName: "UserDefinedAssemblyAttrNoAllowMultipleAttribute");

            var attrs = consoleappCompilation.Assembly.GetAttributes();
            foreach (var a in attrs)
            {
                switch (a.AttributeClass.Name)
                {
                    case "AssemblyTitleAttribute":
                        Assert.Equal(@"System.Reflection.AssemblyTitleAttribute(""AssemblyTitle"")", a.ToString());
                        break;
                    case "FileIOPermissionAttribute":
                        Assert.Equal(@"System.Security.Permissions.FileIOPermissionAttribute(System.Security.Permissions.SecurityAction.RequestOptional)", a.ToString());
                        break;
                    case "UserDefinedAssemblyAttrNoAllowMultipleAttribute":
                        Assert.Equal(@"UserDefinedAssemblyAttrNoAllowMultipleAttribute(""UserDefinedAssemblyAttrNoAllowMultiple"")", a.ToString());
                        break;
                    case "UserDefinedAssemblyAttrAllowMultipleAttribute":
                        Assert.Equal(@"UserDefinedAssemblyAttrAllowMultipleAttribute(""UserDefinedAssemblyAttrAllowMultiple"")", a.ToString());
                        break;
                    default:
                        Assert.Equal("Unexpected Attr", a.AttributeClass.Name);
                        break;
                }
            }
        }

        [Fact]
        public void AssemblyAttributesFromNetModuleDropSpecial()
        {
            string consoleappSource =
                @"
                using System.Reflection;

                [assembly: AssemblyTitle(""AssemblyTitle (from source)"")]

                class Program
                {
                    static void Main(string[] args) { }
                }
                ";

            var consoleappCompilation = CreateCompilationWithMscorlib(consoleappSource, references: new[] { GetNetModuleWithAssemblyAttributesRef() }, options: TestOptions.ReleaseExe);
            var diagnostics = consoleappCompilation.GetDiagnostics();

            TestDuplicateAssemblyAttributesNotEmitted(consoleappCompilation.Assembly,
               expectedSrcAttrCount: 2,
               expectedDuplicateAttrCount: 1,
               attrTypeName: "AssemblyTitleAttribute");

            var attrs = consoleappCompilation.Assembly.GetCustomAttributesToEmit(new ModuleCompilationState(), emittingAssemblyAttributesInNetModule: false);
            foreach (var a in attrs)
            {
                switch (a.AttributeClass.Name)
                {
                    case "AssemblyTitleAttribute":
                        Assert.Equal(@"System.Reflection.AssemblyTitleAttribute(""AssemblyTitle (from source)"")", a.ToString());
                        break;
                    case "FileIOPermissionAttribute":
                        Assert.Equal(@"System.Security.Permissions.FileIOPermissionAttribute(System.Security.Permissions.SecurityAction.RequestOptional)", a.ToString());
                        break;
                    case "UserDefinedAssemblyAttrNoAllowMultipleAttribute":
                        Assert.Equal(@"UserDefinedAssemblyAttrNoAllowMultipleAttribute(""UserDefinedAssemblyAttrNoAllowMultiple"")", a.ToString());
                        break;
                    case "UserDefinedAssemblyAttrAllowMultipleAttribute":
                        Assert.Equal(@"UserDefinedAssemblyAttrAllowMultipleAttribute(""UserDefinedAssemblyAttrAllowMultiple"")", a.ToString());
                        break;
                    case "CompilationRelaxationsAttribute":
                    case "RuntimeCompatibilityAttribute":
                    case "DebuggableAttribute":
                        // synthesized attributes
                        break;
                    default:
                        Assert.Equal("Unexpected Attr", a.AttributeClass.Name);
                        break;
                }
            }
        }

        [Fact]
        public void AssemblyAttributesFromNetModuleAddMulti()
        {
            string consoleappSource =
                @"
                [assembly: UserDefinedAssemblyAttrAllowMultiple(""UserDefinedAssemblyAttrAllowMultiple (from source)"")]

                class Program
                {
                    static void Main(string[] args) { }
                }
                ";

            var consoleappCompilation = CreateCompilationWithMscorlib(consoleappSource, references: new[] { GetNetModuleWithAssemblyAttributesRef() }, options: TestOptions.ReleaseExe);
            var diagnostics = consoleappCompilation.GetDiagnostics();

            var attrs = consoleappCompilation.Assembly.GetAttributes();
            Assert.Equal(5, attrs.Length);
            foreach (var a in attrs)
            {
                switch (a.AttributeClass.Name)
                {
                    case "AssemblyTitleAttribute":
                        Assert.Equal(@"System.Reflection.AssemblyTitleAttribute(""AssemblyTitle"")", a.ToString());
                        break;
                    case "FileIOPermissionAttribute":
                        Assert.Equal(@"System.Security.Permissions.FileIOPermissionAttribute(System.Security.Permissions.SecurityAction.RequestOptional)", a.ToString());
                        break;
                    case "UserDefinedAssemblyAttrNoAllowMultipleAttribute":
                        Assert.Equal(@"UserDefinedAssemblyAttrNoAllowMultipleAttribute(""UserDefinedAssemblyAttrNoAllowMultiple"")", a.ToString());
                        break;
                    case "UserDefinedAssemblyAttrAllowMultipleAttribute":
                        Assert.True(
                            (@"UserDefinedAssemblyAttrAllowMultipleAttribute(""UserDefinedAssemblyAttrAllowMultiple"")" == a.ToString()) ||
                            (@"UserDefinedAssemblyAttrAllowMultipleAttribute(""UserDefinedAssemblyAttrAllowMultiple (from source)"")" == a.ToString()),
                            "Unexpected attribute construction");
                        break;
                    default:
                        Assert.Equal("Unexpected Attr", a.AttributeClass.Name);
                        break;
                }
            }
        }

        [Fact, WorkItem(546939, "DevDiv")]
        public void AssemblyAttributesFromNetModuleBadMulti()
        {
            string consoleappSource =
                @"
                [assembly: UserDefinedAssemblyAttrNoAllowMultiple(""UserDefinedAssemblyAttrNoAllowMultiple (from source)"")]

                class Program
                {
                    static void Main(string[] args) { }
                }
                ";

            var netmodule1Ref = GetNetModuleWithAssemblyAttributesRef();
            var compilation = CreateCompilationWithMscorlib(consoleappSource, references: new[] { netmodule1Ref }, options: TestOptions.ReleaseExe);
            var diagnostics = compilation.GetDiagnostics();
            compilation.VerifyDiagnostics(
                // error CS7061: Duplicate 'UserDefinedAssemblyAttrNoAllowMultipleAttribute' attribute in 'Test.netmodule'
                Diagnostic(ErrorCode.ERR_DuplicateAttributeInNetModule).WithArguments("UserDefinedAssemblyAttrNoAllowMultipleAttribute", netmodule1Ref.Display));

            var attrs = compilation.Assembly.GetAttributes();
            // even duplicates are preserved in source.
            Assert.Equal(5, attrs.Length);

            string netmodule2Source = @"
[assembly: UserDefinedAssemblyAttrNoAllowMultiple(""UserDefinedAssemblyAttrNoAllowMultiple (from source)"")]
";
            compilation = CreateCompilationWithMscorlib(netmodule2Source, options: TestOptions.ReleaseModule, references: new[] { netmodule1Ref });
            compilation.VerifyDiagnostics();
            var netmodule2Ref = compilation.EmitToImageReference();

            attrs = compilation.Assembly.GetAttributes();
            Assert.Equal(1, attrs.Length);

            compilation = CreateCompilationWithMscorlib("", options: TestOptions.ReleaseDll, references: new[] { netmodule1Ref, netmodule2Ref });
            compilation.VerifyDiagnostics(
                // error CS7061: Duplicate 'UserDefinedAssemblyAttrNoAllowMultipleAttribute' attribute in 'Test.netmodule'
                Diagnostic(ErrorCode.ERR_DuplicateAttributeInNetModule).WithArguments("UserDefinedAssemblyAttrNoAllowMultipleAttribute", netmodule1Ref.Display));

            attrs = compilation.Assembly.GetAttributes();
            // even duplicates are preserved in source.
            Assert.Equal(5, attrs.Length);
        }

        [Fact, WorkItem(546939, "DevDiv")]
        public void InternalsVisibleToAttributeDropIdentical()
        {
            var source = @"
using System.Runtime.CompilerServices;
[assembly:InternalsVisibleTo(""Assembly2"")]
[assembly:InternalsVisibleTo(""Assembly2"")]
";
            var compilation = CreateCompilationWithMscorlib(source);
            CompileAndVerify(compilation);

            TestDuplicateAssemblyAttributesNotEmitted(compilation.Assembly,
                expectedSrcAttrCount: 2,
                expectedDuplicateAttrCount: 1,
                attrTypeName: "InternalsVisibleToAttribute");
        }

        [Fact, WorkItem(546939, "DevDiv")]
        public void AssemblyAttributesFromSourceDropIdentical()
        {
            // Attribute with AllowMultiple = True
            string source = @"
[assembly: UserDefinedAssemblyAttrAllowMultipleAttribute(0)]  		            // unique
[assembly: UserDefinedAssemblyAttrAllowMultipleAttribute(1)]  		            // unique
[assembly: UserDefinedAssemblyAttrAllowMultipleAttribute(0)]  		            // duplicate
[assembly: UserDefinedAssemblyAttrAllowMultipleAttribute(""str1"")]  		    // unique
[assembly: UserDefinedAssemblyAttrAllowMultipleAttribute(""str2"")]		        // unique
[assembly: UserDefinedAssemblyAttrAllowMultipleAttribute(""str1"")]		        // duplicate
[assembly: UserDefinedAssemblyAttrAllowMultipleAttribute((object)0)]	        // unique
[assembly: UserDefinedAssemblyAttrAllowMultipleAttribute((object)""str1"")]	    // unique
[assembly: UserDefinedAssemblyAttrAllowMultipleAttribute((object)null)]	        // unique
[assembly: UserDefinedAssemblyAttrAllowMultipleAttribute(null)]		            // unique

[assembly: UserDefinedAssemblyAttrAllowMultipleAttribute(0, Text = ""str1"")]			            // unique
[assembly: UserDefinedAssemblyAttrAllowMultipleAttribute(0, Text = ""str1"")]  		                // duplicate
[assembly: UserDefinedAssemblyAttrAllowMultipleAttribute(0, Text = ""str2"")]			            // unique
[assembly: UserDefinedAssemblyAttrAllowMultipleAttribute(0, Text = ""str1"", Text2 = ""str2"")]	    // unique
[assembly: UserDefinedAssemblyAttrAllowMultipleAttribute(0, Text = ""str1"", Text2 = ""str2"")]	    // duplicate
[assembly: UserDefinedAssemblyAttrAllowMultipleAttribute(0, Text2 = ""str2"", Text = ""str1"")]	    // unique
[assembly: UserDefinedAssemblyAttrAllowMultipleAttribute(0, Text = ""str1"", Text2 = ""str1"")]	    // unique
[assembly: UserDefinedAssemblyAttrAllowMultipleAttribute(0, Text = ""str1"", Text2 = ""str1"")]	    // duplicate
[assembly: UserDefinedAssemblyAttrAllowMultipleAttribute(0, Text2 = ""str1"", Text = ""str1"")]	    // unique

class Program
{
    static void Main(string[] args) { }
}";

            var compilation = CreateCompilationWithMscorlib(source, references: new[] { GetNetModuleWithAssemblyAttributesRef() }, options: TestOptions.ReleaseDll);

            TestDuplicateAssemblyAttributesNotEmitted(compilation.Assembly,
                expectedSrcAttrCount: 20,
                expectedDuplicateAttrCount: 5,
                attrTypeName: "UserDefinedAssemblyAttrAllowMultipleAttribute");
        }

        [Fact, WorkItem(546939, "DevDiv")]
        public void AssemblyAttributesFromSourceDropIdentical_02()
        {
            // Attribute with AllowMultiple = False

            string source1 = @"
[assembly: UserDefinedAssemblyAttrNoAllowMultipleAttribute(0)]  		            // unique
";

            string source2 = @"
[assembly: UserDefinedAssemblyAttrNoAllowMultipleAttribute(0)]  		            // duplicate ignored, no error because identical
";
            string defaultHeaderString = @"
using System;
";
            var defsRef = CreateCompilationWithMscorlib(defaultHeaderString + s_defaultNetModuleSourceBody).ToMetadataReference();
            MetadataReference netmodule1Ref = GetNetModuleWithAssemblyAttributesRef(source2, references: new[] { defsRef });

            var compilation = CreateCompilationWithMscorlib(source1, references: new[] { defsRef, netmodule1Ref }, options: TestOptions.ReleaseDll);
            // duplicate ignored, no error because identical
            compilation.VerifyDiagnostics();

            TestDuplicateAssemblyAttributesNotEmitted(compilation.Assembly,
               expectedSrcAttrCount: 2,
               expectedDuplicateAttrCount: 1,
               attrTypeName: "UserDefinedAssemblyAttrNoAllowMultipleAttribute");

            MetadataReference netmodule2Ref = GetNetModuleWithAssemblyAttributesRef(source1, references: new[] { defsRef });
            compilation = CreateCompilationWithMscorlib("", references: new[] { defsRef, netmodule1Ref, netmodule2Ref }, options: TestOptions.ReleaseDll);
            // duplicate ignored, no error because identical
            compilation.VerifyDiagnostics();

            TestDuplicateAssemblyAttributesNotEmitted(compilation.Assembly,
               expectedSrcAttrCount: 2,
               expectedDuplicateAttrCount: 1,
               attrTypeName: "UserDefinedAssemblyAttrNoAllowMultipleAttribute");
        }

        [Fact, WorkItem(546939, "DevDiv")]
        public void AssemblyAttributesFromNetModuleDropIdentical_01()
        {
            // Duplicate ignored attributes in netmodule
            string netmoduleAttributes = @"
[assembly: UserDefinedAssemblyAttrAllowMultipleAttribute(0)]  		            // unique
[assembly: UserDefinedAssemblyAttrAllowMultipleAttribute(1)]  		            // unique
[assembly: UserDefinedAssemblyAttrAllowMultipleAttribute(0)]  		            // duplicate
[assembly: UserDefinedAssemblyAttrAllowMultipleAttribute(""str1"")]  		    // unique
[assembly: UserDefinedAssemblyAttrAllowMultipleAttribute(""str2"")]		        // unique
[assembly: UserDefinedAssemblyAttrAllowMultipleAttribute(""str1"")]		        // duplicate
[assembly: UserDefinedAssemblyAttrAllowMultipleAttribute((object)0)]	        // unique
[assembly: UserDefinedAssemblyAttrAllowMultipleAttribute((object)""str1"")]	    // unique
[assembly: UserDefinedAssemblyAttrAllowMultipleAttribute((object)null)]	        // unique
[assembly: UserDefinedAssemblyAttrAllowMultipleAttribute(null)]		            // unique

[assembly: UserDefinedAssemblyAttrAllowMultipleAttribute(0, Text = ""str1"")]			            // unique
[assembly: UserDefinedAssemblyAttrAllowMultipleAttribute(0, Text = ""str1"")]  		                // duplicate
[assembly: UserDefinedAssemblyAttrAllowMultipleAttribute(0, Text = ""str2"")]			            // unique
[assembly: UserDefinedAssemblyAttrAllowMultipleAttribute(0, Text = ""str1"", Text2 = ""str2"")]	    // unique
[assembly: UserDefinedAssemblyAttrAllowMultipleAttribute(0, Text = ""str1"", Text2 = ""str2"")]	    // duplicate
[assembly: UserDefinedAssemblyAttrAllowMultipleAttribute(0, Text2 = ""str2"", Text = ""str1"")]	    // unique
[assembly: UserDefinedAssemblyAttrAllowMultipleAttribute(0, Text = ""str1"", Text2 = ""str1"")]	    // unique
[assembly: UserDefinedAssemblyAttrAllowMultipleAttribute(0, Text = ""str1"", Text2 = ""str1"")]	    // duplicate
[assembly: UserDefinedAssemblyAttrAllowMultipleAttribute(0, Text2 = ""str1"", Text = ""str1"")]	    // unique
";
            MetadataReference netmoduleRef = GetNetModuleWithAssemblyAttributesRef(s_defaultNetModuleSourceHeader + netmoduleAttributes + s_defaultNetModuleSourceBody);

            string source = @"
class Program
{
    static void Main(string[] args) { }
}";
            var compilation = CreateCompilationWithMscorlib(source, references: new[] { netmoduleRef }, options: TestOptions.ReleaseDll);

            TestDuplicateAssemblyAttributesNotEmitted(compilation.Assembly,
               expectedSrcAttrCount: 20,
               expectedDuplicateAttrCount: 5,
               attrTypeName: "UserDefinedAssemblyAttrAllowMultipleAttribute");
        }

        [Fact, WorkItem(546939, "DevDiv")]
        public void AssemblyAttributesFromNetModuleDropIdentical_02()
        {
            string netmodule1Attributes = @"
[assembly: UserDefinedAssemblyAttrAllowMultipleAttribute(0)]  		            // unique
[assembly: UserDefinedAssemblyAttrAllowMultipleAttribute(""str2"")]		        // unique
[assembly: UserDefinedAssemblyAttrAllowMultipleAttribute(""str1"")]  		    // unique
[assembly: UserDefinedAssemblyAttrAllowMultipleAttribute(""str1"")]		        // duplicate
[assembly: UserDefinedAssemblyAttrAllowMultipleAttribute((object)0)]	        // unique
[assembly: UserDefinedAssemblyAttrAllowMultipleAttribute((object)""str1"")]	    // unique
[assembly: UserDefinedAssemblyAttrAllowMultipleAttribute((object)null)]	        // unique
[assembly: UserDefinedAssemblyAttrAllowMultipleAttribute(null)]		            // unique

[assembly: UserDefinedAssemblyAttrAllowMultipleAttribute(0, Text = ""str2"")]			            // unique
[assembly: UserDefinedAssemblyAttrAllowMultipleAttribute(0, Text2 = ""str2"", Text = ""str1"")]	    // unique
[assembly: UserDefinedAssemblyAttrAllowMultipleAttribute(0, Text2 = ""str1"", Text = ""str1"")]	    // unique
[assembly: UserDefinedAssemblyAttrAllowMultipleAttribute(0, Text = ""str1"", Text2 = ""str1"")]	    // unique
";

            string netmodule2Attributes = @"
[assembly: UserDefinedAssemblyAttrAllowMultipleAttribute(1)]  		            // unique
[assembly: UserDefinedAssemblyAttrAllowMultipleAttribute(0)]  		            // duplicate
[assembly: UserDefinedAssemblyAttrAllowMultipleAttribute(0, Text = ""str1"")]			            // unique
[assembly: UserDefinedAssemblyAttrAllowMultipleAttribute(0, Text = ""str1"", Text2 = ""str2"")]	    // unique
[assembly: UserDefinedAssemblyAttrAllowMultipleAttribute(0, Text = ""str1"", Text2 = ""str2"")]	    // duplicate
[assembly: UserDefinedAssemblyAttrAllowMultipleAttribute(0, Text = ""str1"", Text2 = ""str1"")]	    // duplicate
";
            string netmodule3Attributes = @"
[assembly: UserDefinedAssemblyAttrAllowMultipleAttribute(0, Text = ""str1"")]  		                // duplicate
[assembly: UserDefinedAssemblyAttrAllowMultipleAttribute(0, Text = ""str1"", Text2 = ""str1"")]	    // duplicate
";
            string defaultBodyString = @"
using System;
";
            MetadataReference netmoduleDefsRef = GetNetModuleWithAssemblyAttributesRef(defaultBodyString + s_defaultNetModuleSourceBody);
            MetadataReference netmodule0Ref = GetNetModuleWithAssemblyAttributesRef(s_defaultNetModuleSourceHeader, references: new[] { netmoduleDefsRef });
            MetadataReference netmodule1Ref = GetNetModuleWithAssemblyAttributesRef(netmodule1Attributes, references: new[] { netmoduleDefsRef });
            MetadataReference netmodule2Ref = GetNetModuleWithAssemblyAttributesRef(netmodule2Attributes, references: new[] { netmoduleDefsRef });
            MetadataReference netmodule3Ref = GetNetModuleWithAssemblyAttributesRef(netmodule3Attributes, references: new[] { netmoduleDefsRef });

            string source = @"
class Program
{
    static void Main(string[] args) { }
}";
            var compilation = CreateCompilationWithMscorlib(source, references: new[] { netmoduleDefsRef, netmodule0Ref, netmodule1Ref, netmodule2Ref, netmodule3Ref }, options: TestOptions.ReleaseDll);

            TestDuplicateAssemblyAttributesNotEmitted(compilation.Assembly,
               expectedSrcAttrCount: 21,
               expectedDuplicateAttrCount: 6,
               attrTypeName: "UserDefinedAssemblyAttrAllowMultipleAttribute");
        }

        [Fact, WorkItem(546939, "DevDiv")]
        public void AssemblyAttributesFromSourceAndNetModuleDropIdentical_01()
        {
            // All duplicate ignored attributes in netmodule
            string netmoduleAttributes = @"
[assembly: UserDefinedAssemblyAttrAllowMultipleAttribute(0)]  		            // duplicate
[assembly: UserDefinedAssemblyAttrAllowMultipleAttribute(""str1"")]		        // duplicate
[assembly: UserDefinedAssemblyAttrAllowMultipleAttribute(0, Text = ""str1"")]  		                // duplicate
[assembly: UserDefinedAssemblyAttrAllowMultipleAttribute(0, Text = ""str1"", Text2 = ""str2"")]	    // duplicate
[assembly: UserDefinedAssemblyAttrAllowMultipleAttribute(0, Text = ""str1"", Text2 = ""str1"")]	    // duplicate
";
            MetadataReference netmoduleRef = GetNetModuleWithAssemblyAttributesRef(s_defaultNetModuleSourceHeader + netmoduleAttributes + s_defaultNetModuleSourceBody);

            string source = @"
[assembly: UserDefinedAssemblyAttrAllowMultipleAttribute(0)]  		            // unique
[assembly: UserDefinedAssemblyAttrAllowMultipleAttribute(1)]  		            // unique
[assembly: UserDefinedAssemblyAttrAllowMultipleAttribute(""str1"")]  		    // unique
[assembly: UserDefinedAssemblyAttrAllowMultipleAttribute(""str2"")]		        // unique
[assembly: UserDefinedAssemblyAttrAllowMultipleAttribute((object)0)]	        // unique
[assembly: UserDefinedAssemblyAttrAllowMultipleAttribute((object)""str1"")]	    // unique
[assembly: UserDefinedAssemblyAttrAllowMultipleAttribute((object)null)]	        // unique
[assembly: UserDefinedAssemblyAttrAllowMultipleAttribute(null)]		            // unique

[assembly: UserDefinedAssemblyAttrAllowMultipleAttribute(0, Text = ""str1"")]			            // unique
[assembly: UserDefinedAssemblyAttrAllowMultipleAttribute(0, Text = ""str2"")]			            // unique
[assembly: UserDefinedAssemblyAttrAllowMultipleAttribute(0, Text = ""str1"", Text2 = ""str2"")]	    // unique
[assembly: UserDefinedAssemblyAttrAllowMultipleAttribute(0, Text2 = ""str2"", Text = ""str1"")]	    // unique
[assembly: UserDefinedAssemblyAttrAllowMultipleAttribute(0, Text = ""str1"", Text2 = ""str1"")]	    // unique
[assembly: UserDefinedAssemblyAttrAllowMultipleAttribute(0, Text2 = ""str1"", Text = ""str1"")]	    // unique

class Program
{
    static void Main(string[] args) { }
}";
            var compilation = CreateCompilationWithMscorlib(source, references: new[] { netmoduleRef }, options: TestOptions.ReleaseDll);

            TestDuplicateAssemblyAttributesNotEmitted(compilation.Assembly,
               expectedSrcAttrCount: 20,
               expectedDuplicateAttrCount: 5,
               attrTypeName: "UserDefinedAssemblyAttrAllowMultipleAttribute");
        }

        [Fact, WorkItem(546939, "DevDiv")]
        public void AssemblyAttributesFromSourceAndNetModuleDropIdentical_02()
        {
            // Duplicate ignored attributes in netmodule & source
            string netmoduleAttributes = @"
[assembly: UserDefinedAssemblyAttrAllowMultipleAttribute(0)]  		            // duplicate
[assembly: UserDefinedAssemblyAttrAllowMultipleAttribute(""str1"")]		        // duplicate
[assembly: UserDefinedAssemblyAttrAllowMultipleAttribute(0, Text = ""str1"")]  		                // duplicate
[assembly: UserDefinedAssemblyAttrAllowMultipleAttribute(0, Text = ""str1"", Text2 = ""str2"")]	    // duplicate
[assembly: UserDefinedAssemblyAttrAllowMultipleAttribute(0, Text = ""str1"", Text2 = ""str1"")]	    // duplicate
";
            MetadataReference netmoduleRef = GetNetModuleWithAssemblyAttributesRef(s_defaultNetModuleSourceHeader + netmoduleAttributes + s_defaultNetModuleSourceBody);

            string source = @"
[assembly: UserDefinedAssemblyAttrAllowMultipleAttribute(0)]  		            // unique
[assembly: UserDefinedAssemblyAttrAllowMultipleAttribute(1)]  		            // unique
[assembly: UserDefinedAssemblyAttrAllowMultipleAttribute(0)]  		            // duplicate
[assembly: UserDefinedAssemblyAttrAllowMultipleAttribute(""str1"")]  		    // unique
[assembly: UserDefinedAssemblyAttrAllowMultipleAttribute(""str2"")]		        // unique
[assembly: UserDefinedAssemblyAttrAllowMultipleAttribute(""str1"")]		        // duplicate
[assembly: UserDefinedAssemblyAttrAllowMultipleAttribute((object)0)]	        // unique
[assembly: UserDefinedAssemblyAttrAllowMultipleAttribute((object)""str1"")]	    // unique
[assembly: UserDefinedAssemblyAttrAllowMultipleAttribute((object)null)]	        // unique
[assembly: UserDefinedAssemblyAttrAllowMultipleAttribute(null)]		            // unique

[assembly: UserDefinedAssemblyAttrAllowMultipleAttribute(0, Text = ""str1"")]			            // unique
[assembly: UserDefinedAssemblyAttrAllowMultipleAttribute(0, Text = ""str1"")]  		                // duplicate
[assembly: UserDefinedAssemblyAttrAllowMultipleAttribute(0, Text = ""str2"")]			            // unique
[assembly: UserDefinedAssemblyAttrAllowMultipleAttribute(0, Text = ""str1"", Text2 = ""str2"")]	    // unique
[assembly: UserDefinedAssemblyAttrAllowMultipleAttribute(0, Text = ""str1"", Text2 = ""str2"")]	    // duplicate
[assembly: UserDefinedAssemblyAttrAllowMultipleAttribute(0, Text2 = ""str2"", Text = ""str1"")]	    // unique
[assembly: UserDefinedAssemblyAttrAllowMultipleAttribute(0, Text = ""str1"", Text2 = ""str1"")]	    // unique
[assembly: UserDefinedAssemblyAttrAllowMultipleAttribute(0, Text = ""str1"", Text2 = ""str1"")]	    // duplicate
[assembly: UserDefinedAssemblyAttrAllowMultipleAttribute(0, Text2 = ""str1"", Text = ""str1"")]	    // unique

class Program
{
    static void Main(string[] args) { }
}";
            var compilation = CreateCompilationWithMscorlib(source, references: new[] { netmoduleRef }, options: TestOptions.ReleaseDll);

            TestDuplicateAssemblyAttributesNotEmitted(compilation.Assembly,
               expectedSrcAttrCount: 25,
               expectedDuplicateAttrCount: 10,
               attrTypeName: "UserDefinedAssemblyAttrAllowMultipleAttribute");
        }

        [Fact, WorkItem(546825, "DevDiv")]
        public void Bug16910()
        {
            string mod =
                @"
                public static class Extensions {
                public static bool EB(this bool b) { return b; }
                }
                ";

            string app =
                @"
                public class Test { }
                ";

            var netModuleRef = GetNetModuleWithAssemblyAttributesRef(mod, new[] { SystemCoreRef });
            var appCompilation = CreateCompilationWithMscorlib(app, references: new[] { SystemCoreRef, netModuleRef }, options: TestOptions.ReleaseDll);
            var diagnostics = appCompilation.GetDiagnostics();
            Assert.False(diagnostics.Any());
        }

        [Fact, WorkItem(530585, "DevDiv")]
        public void Bug16465()
        {
            string mod =
                @"
using System.Configuration.Assemblies;
using System.Reflection;
 
[assembly: AssemblyAlgorithmId(AssemblyHashAlgorithm.SHA1)]
[assembly: AssemblyCulture(""en-US"")]
[assembly: AssemblyDelaySign(true)]
[assembly: AssemblyFlags(AssemblyNameFlags.EnableJITcompileOptimizer | AssemblyNameFlags.Retargetable | AssemblyNameFlags.EnableJITcompileTracking)]
[assembly: AssemblyVersion(""1.2.3.4"")]
[assembly: AssemblyFileVersion(""4.3.2.1"")]
[assembly: AssemblyTitle(""HELLO"")]
[assembly: AssemblyDescription(""World"")]
[assembly: AssemblyCompany(""MS"")]
[assembly: AssemblyProduct(""Roslyn"")]
[assembly: AssemblyInformationalVersion(""Info"")]
[assembly: AssemblyCopyright(""Roslyn"")]
[assembly: AssemblyTrademark(""Roslyn"")]
class Program1 { static void Main(string[] args) {    } }
                ";

            string app =
                @"
                public class Test { }
                ";

            var netModuleRef = GetNetModuleWithAssemblyAttributesRef(mod, new[] { SystemCoreRef });
            var appCompilation = CreateCompilationWithMscorlib(app, references: new[] { netModuleRef }, options: TestOptions.ReleaseDll);

            var module = (PEModuleSymbol)appCompilation.Assembly.Modules[1];
            var metadata = module.Module;

            EntityHandle token = metadata.GetTypeRef(metadata.GetAssemblyRef("mscorlib"), "System.Runtime.CompilerServices", "AssemblyAttributesGoHere");
            Assert.False(token.IsNil);   //could the type ref be located? If not then the attribute's not there.

            var attributes = module.GetCustomAttributesForToken(token);
            var builder = new System.Text.StringBuilder();

            builder.AppendLine();
            foreach (var attr in attributes)
            {
                builder.AppendLine(attr.ToString());
            }
            builder.AppendLine();

            var expectedStr =
@"
System.Reflection.AssemblyAlgorithmIdAttribute(System.Configuration.Assemblies.AssemblyHashAlgorithm.SHA1)
System.Reflection.AssemblyCultureAttribute(""en-US"")
System.Reflection.AssemblyDelaySignAttribute(true)
System.Reflection.AssemblyFlagsAttribute(System.Reflection.AssemblyNameFlags.None | System.Reflection.AssemblyNameFlags.EnableJITcompileOptimizer | System.Reflection.AssemblyNameFlags.EnableJITcompileTracking | System.Reflection.AssemblyNameFlags.Retargetable)
System.Reflection.AssemblyVersionAttribute(""1.2.3.4"")
System.Reflection.AssemblyFileVersionAttribute(""4.3.2.1"")
System.Reflection.AssemblyTitleAttribute(""HELLO"")
System.Reflection.AssemblyDescriptionAttribute(""World"")
System.Reflection.AssemblyCompanyAttribute(""MS"")
System.Reflection.AssemblyProductAttribute(""Roslyn"")
System.Reflection.AssemblyInformationalVersionAttribute(""Info"")
System.Reflection.AssemblyCopyrightAttribute(""Roslyn"")
System.Reflection.AssemblyTrademarkAttribute(""Roslyn"")
".Trim();
            var actualStr = builder.ToString().Trim();

            Assert.True(expectedStr.Equals(actualStr), AssertEx.GetAssertMessage(expectedStr, actualStr));
        }

        #endregion

        #region CompilationRelaxationsAttribute, RuntimeCompatibilityAttribute

        [Fact, WorkItem(545527, "DevDiv")]
        public void CompilationRelaxationsAndRuntimeCompatibility_MultiModule()
        {
            string moduleSrc = @"
using System.Runtime.CompilerServices;
[assembly: CompilationRelaxationsAttribute(CompilationRelaxations.NoStringInterning)]
[assembly: RuntimeCompatibilityAttribute(WrapNonExceptionThrows = false)]
";
            var module = CreateCompilationWithMscorlib(moduleSrc, options: TestOptions.ReleaseModule, assemblyName: "M");

            string assemblySrc = @"
public class C { }
";

            var assembly = CreateCompilationWithMscorlib(assemblySrc, new[] { module.EmitToImageReference() }, assemblyName: "C");

            CompileAndVerify(assembly, symbolValidator: moduleSymbol =>
            {
                var attrs = moduleSymbol.ContainingAssembly.GetAttributes().Select(a => a.ToString()).ToArray();
                AssertEx.SetEqual(new[]
                    {
                        "System.Diagnostics.DebuggableAttribute(System.Diagnostics.DebuggableAttribute.DebuggingModes.IgnoreSymbolStoreSequencePoints)",
                        "System.Runtime.CompilerServices.RuntimeCompatibilityAttribute(WrapNonExceptionThrows = false)",
                        "System.Runtime.CompilerServices.CompilationRelaxationsAttribute(System.Runtime.CompilerServices.CompilationRelaxations.NoStringInterning)"
                    },
                    attrs);
            });
        }

        [Fact, WorkItem(546460, "DevDiv")]
        public void RuntimeCompatibilityAttribute_False()
        {
            // the attribute suppresses WRN_UnreachableGeneralCatch since catch {} can catch an object not derived from Exception

            string source = @"
using System.Runtime.CompilerServices;

[assembly: RuntimeCompatibilityAttribute(WrapNonExceptionThrows = false)]

class C
{
    public static void Main()
    {
        try { }
        catch (System.Exception) { }
        catch { }
    }
}
";
            CreateCompilationWithMscorlib(source).VerifyDiagnostics();
        }

        [Fact, WorkItem(546460, "DevDiv")]
        public void RuntimeCompatibilityAttribute_Default()
        {
            string source = @"
using System.Runtime.CompilerServices;

[assembly: RuntimeCompatibilityAttribute()]

class C
{
    public static void Main()
    {
        try { }
        catch (System.Exception) { }
        catch { }
    }
}
";
            CreateCompilationWithMscorlib(source).VerifyDiagnostics(
                // (12,9): warning CS1058: A previous catch clause already catches all exceptions. All non-exceptions thrown will be wrapped in a System.Runtime.CompilerServices.RuntimeWrappedException.
                Diagnostic(ErrorCode.WRN_UnreachableGeneralCatch, "catch"));
        }

        [Fact, WorkItem(546460, "DevDiv")]
        public void RuntimeCompatibilityAttribute_GeneralNotLast()
        {
            string source = @"
using System.Runtime.CompilerServices;

[assembly: RuntimeCompatibilityAttribute(WrapNonExceptionThrows = false)]

class C
{
    public static void Main()
    {
        try { }
        catch { }
        catch (System.Exception) { }
    }
}
";
            CreateCompilationWithMscorlib(source).VerifyDiagnostics(
                // (12,9): error CS1017: Catch clauses cannot follow the general catch clause of a try statement
                Diagnostic(ErrorCode.ERR_TooManyCatches, "catch"));
        }

        [Fact]
        public void RuntimeCompatibilityAttribute_False_MultiModule()
        {
            string moduleSrc = @"
using System.Runtime.CompilerServices;
[assembly: RuntimeCompatibilityAttribute(WrapNonExceptionThrows = false)]
";
            var module = CreateCompilationWithMscorlib(moduleSrc, options: TestOptions.ReleaseModule, assemblyName: "M");

            string assemblySrc = @"
class C
{
    public static void Main()
    {
        try { }
        catch (System.Exception) { }
        catch { }
    }
}
";

            var assembly = CreateCompilationWithMscorlib(assemblySrc, new[] { module.EmitToImageReference() }, assemblyName: "C");
            assembly.VerifyDiagnostics();
        }

        [Fact]
        public void RuntimeCompatibility_Duplicates_Error1()
        {
            string moduleSrc1 = @"
using System.Runtime.CompilerServices;
[assembly: CompilationRelaxationsAttribute(CompilationRelaxations.NoStringInterning)]
[assembly: RuntimeCompatibilityAttribute(WrapNonExceptionThrows = false)]
";
            var module1 = CreateCompilationWithMscorlib(moduleSrc1, options: TestOptions.ReleaseModule, assemblyName: "M1");

            string moduleSrc2 = @"
using System.Runtime.CompilerServices;
[assembly: CompilationRelaxationsAttribute(CompilationRelaxations.NoStringInterning)]
[assembly: RuntimeCompatibilityAttribute(WrapNonExceptionThrows = true)]
";
            var module2 = CreateCompilationWithMscorlib(moduleSrc2, options: TestOptions.ReleaseModule, assemblyName: "M2");

            string assemblySrc = @"
public class C { }
";

            var assembly = CreateCompilationWithMscorlib(assemblySrc, new[] { module1.EmitToImageReference(), module2.EmitToImageReference() }, assemblyName: "C");

            assembly.VerifyDiagnostics(
                // error CS7061: Duplicate 'RuntimeCompatibilityAttribute' attribute in 'M1.netmodule'
                Diagnostic(ErrorCode.ERR_DuplicateAttributeInNetModule).WithArguments("RuntimeCompatibilityAttribute", "M1.netmodule"));
        }

        [Fact]
        public void RuntimeCompatibility_Duplicates_Error2()
        {
            string moduleSrc1 = @"
using System.Runtime.CompilerServices;
[assembly: CompilationRelaxationsAttribute(CompilationRelaxations.NoStringInterning)]
[assembly: RuntimeCompatibilityAttribute(WrapNonExceptionThrows = false)]
";
            var module1 = CreateCompilationWithMscorlib(moduleSrc1, options: TestOptions.ReleaseModule, assemblyName: "M1");

            string assemblySrc = @"
using System.Runtime.CompilerServices;
[assembly: CompilationRelaxationsAttribute(CompilationRelaxations.NoStringInterning)]
[assembly: RuntimeCompatibilityAttribute(WrapNonExceptionThrows = true)]

public class C { }
";

            var assembly = CreateCompilationWithMscorlib(assemblySrc, new[] { module1.EmitToImageReference() }, assemblyName: "C");

            assembly.VerifyDiagnostics(
                // error CS7061: Duplicate 'RuntimeCompatibilityAttribute' attribute in 'M1.netmodule'
                Diagnostic(ErrorCode.ERR_DuplicateAttributeInNetModule).WithArguments("RuntimeCompatibilityAttribute", "M1.netmodule"));
        }

        [Fact]
        public void CompilationRelaxations_Duplicates_Error1()
        {
            string moduleSrc1 = @"
using System.Runtime.CompilerServices;
[assembly: CompilationRelaxationsAttribute(2)]
[assembly: RuntimeCompatibilityAttribute(WrapNonExceptionThrows = true)]
";
            var module1 = CreateCompilationWithMscorlib(moduleSrc1, options: TestOptions.ReleaseModule, assemblyName: "M1");

            string moduleSrc2 = @"
using System.Runtime.CompilerServices;
[assembly: CompilationRelaxationsAttribute(6)]
[assembly: RuntimeCompatibilityAttribute(WrapNonExceptionThrows = true)]
";
            var module2 = CreateCompilationWithMscorlib(moduleSrc2, options: TestOptions.ReleaseModule, assemblyName: "M2");

            string assemblySrc = @"
public class C { }
";

            var assembly = CreateCompilationWithMscorlib(assemblySrc, new[] { module1.EmitToImageReference(), module2.EmitToImageReference() }, assemblyName: "C");

            assembly.VerifyDiagnostics(
                // error CS7061: Duplicate 'CompilationRelaxationsAttribute' attribute in 'M1.netmodule'
                Diagnostic(ErrorCode.ERR_DuplicateAttributeInNetModule).WithArguments("CompilationRelaxationsAttribute", "M1.netmodule"));
        }

        [Fact]
        public void CompilationRelaxations_Duplicates_Error2()
        {
            string moduleSrc1 = @"
using System.Runtime.CompilerServices;
[assembly: CompilationRelaxationsAttribute(2)]
[assembly: RuntimeCompatibilityAttribute(WrapNonExceptionThrows = true)]
";
            var module1 = CreateCompilationWithMscorlib(moduleSrc1, options: TestOptions.ReleaseModule, assemblyName: "M1");

            string assemblySrc = @"
using System.Runtime.CompilerServices;
[assembly: CompilationRelaxationsAttribute(6)]
[assembly: RuntimeCompatibilityAttribute(WrapNonExceptionThrows = true)]

public class C { }
";

            var assembly = CreateCompilationWithMscorlib(assemblySrc, new[] { module1.EmitToImageReference() }, assemblyName: "C");

            assembly.VerifyDiagnostics(
                // error CS7061: Duplicate 'CompilationRelaxationsAttribute' attribute in 'M1.netmodule'
                Diagnostic(ErrorCode.ERR_DuplicateAttributeInNetModule).WithArguments("CompilationRelaxationsAttribute", "M1.netmodule"));
        }

        [Fact]
        public void CompilationRelaxations_Duplicates_SameValue1()
        {
            string moduleSrc1 = @"
using System.Runtime.CompilerServices;
[assembly: CompilationRelaxationsAttribute(2)]
[assembly: RuntimeCompatibilityAttribute(WrapNonExceptionThrows = true)]
";
            var module1 = CreateCompilationWithMscorlib(moduleSrc1, options: TestOptions.ReleaseModule, assemblyName: "M1");

            string moduleSrc2 = @"
using System.Runtime.CompilerServices;
[assembly: CompilationRelaxationsAttribute(2)]
[assembly: RuntimeCompatibilityAttribute(WrapNonExceptionThrows = true)]
";
            var module2 = CreateCompilationWithMscorlib(moduleSrc2, options: TestOptions.ReleaseModule, assemblyName: "M2");

            string assemblySrc = @"
public class C { }
";

            var assembly = CreateCompilationWithMscorlib(assemblySrc, new[] { module1.EmitToImageReference(), module2.EmitToImageReference() }, assemblyName: "C");

            assembly.VerifyDiagnostics();
        }

        [Fact]
        public void CompilationRelaxations_Duplicates_SameValue2()
        {
            string moduleSrc1 = @"
using System.Runtime.CompilerServices;
[assembly: CompilationRelaxationsAttribute(2)]
[assembly: RuntimeCompatibilityAttribute(WrapNonExceptionThrows = true)]
";
            var module1 = CreateCompilationWithMscorlib(moduleSrc1, options: TestOptions.ReleaseModule, assemblyName: "M1");

            string assemblySrc = @"
using System.Runtime.CompilerServices;
[assembly: CompilationRelaxationsAttribute(2)]
[assembly: RuntimeCompatibilityAttribute(WrapNonExceptionThrows = true)]

public class C { }
";

            var assembly = CreateCompilationWithMscorlib(assemblySrc, new[] { module1.EmitToImageReference() }, assemblyName: "C");

            assembly.VerifyDiagnostics();
        }

        #endregion

        [Fact, WorkItem(530579, "DevDiv")]
        public void Bug530579_1()
        {
            var mod1Source = "[assembly:System.Reflection.AssemblyDescriptionAttribute(\"Module1\")]";

            var mod2Source = "[assembly:System.Reflection.AssemblyDescriptionAttribute(\"Module1\")]";

            var source = "[assembly:System.Reflection.AssemblyDescriptionAttribute(\"Module1\")]";

            var compMod1 = CreateCompilationWithMscorlib(mod1Source, options: TestOptions.ReleaseModule, assemblyName: "M1");
            var compMod2 = CreateCompilationWithMscorlib(mod2Source, options: TestOptions.ReleaseModule, assemblyName: "M2");

            var appCompilation = CreateCompilationWithMscorlib(source,
                                                               references: new MetadataReference[] { compMod1.EmitToImageReference(), compMod2.EmitToImageReference() },
                                                               options: TestOptions.ReleaseDll);

            Assert.Equal(3, appCompilation.Assembly.Modules.Length);

            CompileAndVerify(appCompilation, symbolValidator: (ModuleSymbol m) =>
                                                              {
                                                                  var list = GetAssemblyDescriptionAttributes(m.ContainingAssembly).ToArray();

                                                                  Assert.Equal(1, list.Length);
                                                                  Assert.Equal("System.Reflection.AssemblyDescriptionAttribute(\"Module1\")", list[0].ToString());
                                                              }).VerifyDiagnostics();
        }

        private static IEnumerable<CSharpAttributeData> GetAssemblyDescriptionAttributes(AssemblySymbol assembly)
        {
            return assembly.GetAttributes().Where(data => data.IsTargetAttribute(assembly, AttributeDescription.AssemblyDescriptionAttribute));
        }

        [Fact, WorkItem(530579, "DevDiv")]
        public void Bug530579_2()
        {
            var mod1Source = "[assembly:System.Reflection.AssemblyDescriptionAttribute(\"Module1\")]";

            var mod2Source = "[assembly:System.Reflection.AssemblyDescriptionAttribute(\"Module2\")]";

            var source = "";

            var compMod1 = CreateCompilationWithMscorlib(mod1Source, options: TestOptions.ReleaseModule, assemblyName: "M1");
            var compMod2 = CreateCompilationWithMscorlib(mod2Source, options: TestOptions.ReleaseModule, assemblyName: "M2");

            var appCompilation = CreateCompilationWithMscorlib(source,
                                                               references: new MetadataReference[] { compMod1.EmitToImageReference(), compMod2.EmitToImageReference() },
                                                               options: TestOptions.ReleaseDll);

            Assert.Equal(3, appCompilation.Assembly.Modules.Length);

            CompileAndVerify(appCompilation, symbolValidator: (ModuleSymbol m) =>
            {
                var list = GetAssemblyDescriptionAttributes(m.ContainingAssembly).ToArray();

                Assert.Equal(1, list.Length);
                Assert.Equal("System.Reflection.AssemblyDescriptionAttribute(\"Module2\")", list[0].ToString());
            }).VerifyDiagnostics(
                // warning CS7090: Attribute 'System.Reflection.AssemblyDescriptionAttribute' from .NET module 'M1.netmodule' is overridden.
                Diagnostic(ErrorCode.WRN_AssemblyAttributeFromModuleIsOverridden).WithArguments("System.Reflection.AssemblyDescriptionAttribute", "M1.netmodule")
            );
        }

        [Fact, WorkItem(530579, "DevDiv")]
        public void Bug530579_3()
        {
            var mod1Source = "[assembly:System.Reflection.AssemblyDescriptionAttribute(\"Module1\")]";

            var mod2Source = "[assembly:System.Reflection.AssemblyDescriptionAttribute(\"Module2\")]";

            var source = "[assembly:System.Reflection.AssemblyDescriptionAttribute(\"Module3\")]";

            var compMod1 = CreateCompilationWithMscorlib(mod1Source, options: TestOptions.ReleaseModule, assemblyName: "M1");
            var compMod2 = CreateCompilationWithMscorlib(mod2Source, options: TestOptions.ReleaseModule, assemblyName: "M2");

            var appCompilation = CreateCompilationWithMscorlib(source,
                                                               references: new MetadataReference[] { compMod1.EmitToImageReference(), compMod2.EmitToImageReference() },
                                                               options: TestOptions.ReleaseDll);

            Assert.Equal(3, appCompilation.Assembly.Modules.Length);

            CompileAndVerify(appCompilation, symbolValidator: (ModuleSymbol m) =>
            {
                var list = GetAssemblyDescriptionAttributes(m.ContainingAssembly).ToArray();

                Assert.Equal(1, list.Length);
                Assert.Equal("System.Reflection.AssemblyDescriptionAttribute(\"Module3\")", list[0].ToString());
            }).VerifyDiagnostics(
                // warning CS7090: Attribute 'System.Reflection.AssemblyDescriptionAttribute' from .NET module 'M2.netmodule' is overridden.
                Diagnostic(ErrorCode.WRN_AssemblyAttributeFromModuleIsOverridden).WithArguments("System.Reflection.AssemblyDescriptionAttribute", "M2.netmodule"),
                // warning CS7090: Attribute 'System.Reflection.AssemblyDescriptionAttribute' from .NET module 'M1.netmodule' is overridden.
                Diagnostic(ErrorCode.WRN_AssemblyAttributeFromModuleIsOverridden).WithArguments("System.Reflection.AssemblyDescriptionAttribute", "M1.netmodule")
            );
        }

        [Fact, WorkItem(530579, "DevDiv")]
        public void Bug530579_4()
        {
            var mod1Source = "[assembly:System.Reflection.AssemblyDescriptionAttribute(\"Module1\")]";

            var mod2Source = "[assembly:System.Reflection.AssemblyDescriptionAttribute(\"Module2\")]";

            var source = "[assembly:System.Reflection.AssemblyDescriptionAttribute(\"Module1\")]";

            var compMod1 = CreateCompilationWithMscorlib(mod1Source, options: TestOptions.ReleaseModule, assemblyName: "M1");
            var compMod2 = CreateCompilationWithMscorlib(mod2Source, options: TestOptions.ReleaseModule, assemblyName: "M2");

            var appCompilation = CreateCompilationWithMscorlib(source,
                                                               references: new MetadataReference[] { compMod1.EmitToImageReference(), compMod2.EmitToImageReference() },
                                                               options: TestOptions.ReleaseDll);

            Assert.Equal(3, appCompilation.Assembly.Modules.Length);

            CompileAndVerify(appCompilation, symbolValidator: (ModuleSymbol m) =>
            {
                var list = GetAssemblyDescriptionAttributes(m.ContainingAssembly).ToArray();

                Assert.Equal(1, list.Length);
                Assert.Equal("System.Reflection.AssemblyDescriptionAttribute(\"Module1\")", list[0].ToString());
            }).VerifyDiagnostics(
                // warning CS7090: Attribute 'System.Reflection.AssemblyDescriptionAttribute' from .NET module 'M2.netmodule' is overridden.
                Diagnostic(ErrorCode.WRN_AssemblyAttributeFromModuleIsOverridden).WithArguments("System.Reflection.AssemblyDescriptionAttribute", "M2.netmodule")
            );
        }

        [Fact, WorkItem(649346, "DevDiv")]
        public void Bug649346()
        {
            var mod1Source = "[assembly:System.Reflection.AssemblyFileVersionAttribute(\"1.2.3.4\")]";
            var source = @"[assembly:System.Reflection.AssemblyFileVersionAttribute(""4.3.2.1"")] class C { static void Main() {} }";

            var compMod1 = CreateCompilationWithMscorlib(mod1Source, options: TestOptions.ReleaseModule, assemblyName: "M1");
            var appCompilation = CreateCompilationWithMscorlib(source,
                                                               references: new MetadataReference[] { compMod1.EmitToImageReference() },
                                                               options: TestOptions.ReleaseExe);

            Assert.Equal(2, appCompilation.Assembly.Modules.Length);

            CompileAndVerify(appCompilation, symbolValidator: (ModuleSymbol m) =>
            {
                // var list = new ArrayBuilder<AttributeData>();
                var asm = m.ContainingAssembly;
                var attrs = m.ContainingAssembly.GetAttributes();
                var attrlist = attrs.Where(a => a.IsTargetAttribute(asm, AttributeDescription.AssemblyFileVersionAttribute));

                Assert.Equal(1, attrlist.Count());
                Assert.Equal("System.Reflection.AssemblyFileVersionAttribute(\"4.3.2.1\")", attrlist.First().ToString());
            }).VerifyDiagnostics(
                // warning CS7090: Attribute 'System.Reflection.AssemblyFileVersionAttribute' from module 'M1.netmodule' will be ignored in favor of the instance appearing in source
                Diagnostic(ErrorCode.WRN_AssemblyAttributeFromModuleIsOverridden).WithArguments("System.Reflection.AssemblyFileVersionAttribute", "M1.netmodule")
            );
        }

        [Fact, WorkItem(1082421, "DevDiv")]
        public void Bug1082421()
        {
            const string s = @"
using static System.Math;
 
[assembly: A(Log)]
 
static class Logo
{
    public const int Height = 32;
    public const int Width = 32;
}
";

            var compilation = CreateCompilationWithMscorlib(s, options: TestOptions.ReleaseDll);
            compilation.GetDiagnostics();
        }
    }
}
