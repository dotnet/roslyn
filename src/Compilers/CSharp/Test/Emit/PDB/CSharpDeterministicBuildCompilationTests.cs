// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Text;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Debugging;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Roslyn.Test.Utilities.PDB;
using Roslyn.Utilities;
using TestResources.NetFX;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.PDB
{
    public class CSharpDeterministicBuildCompilationTests : CSharpTestBase, IEnumerable<object[]>
    {
        private static void VerifyCompilationOptions(
            CSharpCompilationOptions originalOptions,
            Compilation compilation,
            EmitOptions emitOptions,
            BlobReader compilationOptionsBlobReader,
            string langVersion,
            int sourceFileCount)
        {
            var pdbOptions = DeterministicBuildCompilationTestHelpers.ParseCompilationOptions(compilationOptionsBlobReader);

            DeterministicBuildCompilationTestHelpers.AssertCommonOptions(emitOptions, originalOptions, compilation, pdbOptions);

            // See CSharpCompilation.SerializeForPdb to see options that are included
            pdbOptions.VerifyPdbOption("nullable", originalOptions.NullableContextOptions);
            pdbOptions.VerifyPdbOption("checked", originalOptions.CheckOverflow);
            pdbOptions.VerifyPdbOption("unsafe", originalOptions.AllowUnsafe);

            Assert.Equal(langVersion, pdbOptions["language-version"]);
            Assert.Equal(sourceFileCount.ToString(), pdbOptions["source-file-count"]);

            var firstSyntaxTree = (CSharpSyntaxTree)compilation.SyntaxTrees.FirstOrDefault();
            pdbOptions.VerifyPdbOption("define", firstSyntaxTree.Options.PreprocessorSymbolNames, isDefault: v => v.IsEmpty(), toString: v => string.Join(",", v));
        }

        private static void TestDeterministicCompilationCSharp(
            string langVersion,
            SyntaxTree[] syntaxTrees,
            CSharpCompilationOptions compilationOptions,
            EmitOptions emitOptions,
            TestMetadataReferenceInfo[] metadataReferences,
            int? debugDocumentsCount = null)
        {
            var targetFramework = TargetFramework.NetCoreApp;
            var originalCompilation = CreateCompilation(
                syntaxTrees,
                references: metadataReferences.SelectAsArray(r => r.MetadataReference),
                options: compilationOptions,
                targetFramework: targetFramework);

            var peBlob = originalCompilation.EmitToArray(options: emitOptions);

            using (var peReader = new PEReader(peBlob))
            {
                var entries = peReader.ReadDebugDirectory();

                AssertEx.Equal(new[] { DebugDirectoryEntryType.CodeView, DebugDirectoryEntryType.PdbChecksum, DebugDirectoryEntryType.Reproducible, DebugDirectoryEntryType.EmbeddedPortablePdb }, entries.Select(e => e.Type));

                var codeView = entries[0];
                var checksum = entries[1];
                var reproducible = entries[2];
                var embedded = entries[3];

                using (var embeddedPdb = peReader.ReadEmbeddedPortablePdbDebugDirectoryData(embedded))
                {
                    var pdbReader = embeddedPdb.GetMetadataReader();
                    var metadataReferenceReader = DeterministicBuildCompilationTestHelpers.GetSingleBlob(PortableCustomDebugInfoKinds.CompilationMetadataReferences, pdbReader);
                    var compilationOptionsReader = DeterministicBuildCompilationTestHelpers.GetSingleBlob(PortableCustomDebugInfoKinds.CompilationOptions, pdbReader);

                    Assert.Equal(debugDocumentsCount ?? syntaxTrees.Length, pdbReader.Documents.Count);

                    VerifyCompilationOptions(compilationOptions, originalCompilation, emitOptions, compilationOptionsReader, langVersion, syntaxTrees.Length);
                    DeterministicBuildCompilationTestHelpers.VerifyReferenceInfo(metadataReferences, targetFramework, metadataReferenceReader);
                }
            }
        }

        [Theory]
        [ClassData(typeof(CSharpDeterministicBuildCompilationTests))]
        public void PortablePdb_DeterministicCompilation(CSharpCompilationOptions compilationOptions, EmitOptions emitOptions, CSharpParseOptions parseOptions)
        {
            var sourceOne = Parse(@"
using System;

class MainType
{
    public static void Main()
    {
        Console.WriteLine();
    }
}
", filename: "a.cs", options: parseOptions, encoding: Encoding.UTF8);

            var sourceTwo = Parse(@"
class TypeTwo
{
}", filename: "b.cs", options: parseOptions, encoding: new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));

            var sourceThree = Parse(@"
class TypeThree
{
}", filename: "c.cs", options: parseOptions, encoding: Encoding.Unicode);

            var referenceOneCompilation = CreateCompilation(
@"public struct StructWithReference
{
    string PrivateData;
}
public struct StructWithValue
{
    int PrivateData;
}", options: TestOptions.DebugDll);

            var referenceTwoCompilation = CreateCompilation(
@"public class ReferenceTwo
{
}", options: TestOptions.DebugDll);

            using var referenceOne = TestMetadataReferenceInfo.Create(
                referenceOneCompilation,
                fullPath: "abcd.dll",
                emitOptions: emitOptions);

            using var referenceTwo = TestMetadataReferenceInfo.Create(
                referenceTwoCompilation,
                fullPath: "efgh.dll",
                emitOptions: emitOptions);

            var testSource = new[] { sourceOne, sourceTwo, sourceThree };
            TestDeterministicCompilationCSharp(
                parseOptions.LanguageVersion.MapSpecifiedToEffectiveVersion().ToDisplayString(),
                testSource,
                compilationOptions,
                emitOptions,
                new[] { referenceOne, referenceTwo });
        }

        [Theory]
        [ClassData(typeof(CSharpDeterministicBuildCompilationTests))]
        public void PortablePdb_DeterministicCompilation_DuplicateFilePaths(CSharpCompilationOptions compilationOptions, EmitOptions emitOptions, CSharpParseOptions parseOptions)
        {
            var sourceOne = Parse(@"
using System;

class MainType
{
    public static void Main()
    {
        Console.WriteLine();
    }
}
", filename: "a.cs", options: parseOptions, encoding: Encoding.UTF8);

            var sourceTwo = Parse(@"
class TypeTwo
{
}", filename: "b.cs", options: parseOptions, encoding: new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));

            var sourceThree = Parse(@"
class TypeThree
{
}", filename: "a.cs", options: parseOptions, encoding: Encoding.Unicode);

            var referenceOneCompilation = CreateCompilation(
@"public struct StructWithReference
{
    string PrivateData;
}
public struct StructWithValue
{
    int PrivateData;
}", options: TestOptions.DebugDll);

            var referenceTwoCompilation = CreateCompilation(
@"public class ReferenceTwo
{
}", options: TestOptions.DebugDll);

            using var referenceOne = TestMetadataReferenceInfo.Create(
                referenceOneCompilation,
                fullPath: "abcd.dll",
                emitOptions: emitOptions);

            using var referenceTwo = TestMetadataReferenceInfo.Create(
                referenceTwoCompilation,
                fullPath: "efgh.dll",
                emitOptions: emitOptions);

            var testSource = new[] { sourceOne, sourceTwo, sourceThree };

            // Note that only one debug document can be present for each distinct source path.
            // So if more than one syntax tree has the same file path, it won't be possible to do a rebuild from the DLL+PDB.
            TestDeterministicCompilationCSharp(
                parseOptions.LanguageVersion.MapSpecifiedToEffectiveVersion().ToDisplayString(),
                testSource,
                compilationOptions,
                emitOptions,
                new[] { referenceOne, referenceTwo },
                debugDocumentsCount: 2);
        }

        [ConditionalTheory(typeof(DesktopOnly))]
        [ClassData(typeof(CSharpDeterministicBuildCompilationTests))]
        public void PortablePdb_DeterministicCompilationWithSJIS(CSharpCompilationOptions compilationOptions, EmitOptions emitOptions, CSharpParseOptions parseOptions)
        {
            var sourceOne = Parse(@"
using System;

class MainType
{
    public static void Main()
    {
        Console.WriteLine();
    }
}
", filename: "a.cs", options: parseOptions, encoding: Encoding.UTF8);

            var sourceTwo = Parse(@"
class TypeTwo
{
}", filename: "b.cs", options: parseOptions, encoding: new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));

            var sourceThree = Parse(@"
class TypeThree
{
}", filename: "c.cs", options: parseOptions, encoding: Encoding.GetEncoding(932)); // SJIS encoding

            var referenceOneCompilation = CreateCompilation(
@"public struct StructWithReference
{
    string PrivateData;
}
public struct StructWithValue
{
    int PrivateData;
}", options: TestOptions.DebugDll);

            var referenceTwoCompilation = CreateCompilation(
@"public class ReferenceTwo
{
}", options: TestOptions.DebugDll);

            using var referenceOne = TestMetadataReferenceInfo.Create(
                referenceOneCompilation,
                fullPath: "abcd.dll",
                emitOptions: emitOptions);

            using var referenceTwo = TestMetadataReferenceInfo.Create(
                referenceTwoCompilation,
                fullPath: "efgh.dll",
                emitOptions: emitOptions);

            var testSource = new[] { sourceOne, sourceTwo, sourceThree };
            TestDeterministicCompilationCSharp(
                parseOptions.LanguageVersion.MapSpecifiedToEffectiveVersion().ToDisplayString(),
                testSource,
                compilationOptions,
                emitOptions,
                new[] { referenceOne, referenceTwo });
        }

        public IEnumerator<object[]> GetEnumerator() => GetTestParameters().GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        private static IEnumerable<object[]> GetTestParameters()
        {
            foreach (var compilationOptions in GetCompilationOptions())
            {
                foreach (var emitOptions in DeterministicBuildCompilationTestHelpers.GetEmitOptions())
                {
                    foreach (var parseOptions in GetCSharpParseOptions())
                    {
                        yield return new object[] { compilationOptions, emitOptions, parseOptions };
                    }
                }
            }
        }

        private static IEnumerable<CSharpCompilationOptions> GetCompilationOptions()
        {
            // Provide non default options for to test that they are being serialized
            // to the pdb correctly. It needs to produce a compilation to be emitted, but otherwise
            // everything should be non-default if possible. Diagnostic settings are ignored
            // because they won't be serialized.

            // Use constructor that requires all arguments. If new arguments are added, it's possible they need to be
            // included in the pdb serialization and added to tests here
            var defaultOptions = new CSharpCompilationOptions(
                OutputKind.ConsoleApplication,
                reportSuppressedDiagnostics: false,
                moduleName: "Module",
                mainTypeName: "MainType",
                scriptClassName: null,
                usings: new[] { "System", "System.Threading" },
                optimizationLevel: OptimizationLevel.Debug,
                checkOverflow: true,
                allowUnsafe: true,
                cryptoKeyContainer: null,
                cryptoKeyFile: null,
                cryptoPublicKey: default,
                delaySign: null,
                platform: Platform.AnyCpu,
                generalDiagnosticOption: ReportDiagnostic.Default,
                warningLevel: 4,
                specificDiagnosticOptions: null,
                concurrentBuild: true,
                deterministic: true,
                currentLocalTime: default,
                debugPlusMode: false,
                xmlReferenceResolver: null,
                sourceReferenceResolver: null,
                syntaxTreeOptionsProvider: null,
                metadataReferenceResolver: null,
                assemblyIdentityComparer: null,
                strongNameProvider: null,
                metadataImportOptions: MetadataImportOptions.Public,
                referencesSupersedeLowerVersions: false,
                publicSign: false,
                topLevelBinderFlags: BinderFlags.None,
                nullableContextOptions: NullableContextOptions.Enable);

            yield return defaultOptions;
            yield return defaultOptions.WithNullableContextOptions(NullableContextOptions.Disable);
            yield return defaultOptions.WithNullableContextOptions(NullableContextOptions.Warnings);
            yield return defaultOptions.WithOptimizationLevel(OptimizationLevel.Release);
            yield return defaultOptions.WithAssemblyIdentityComparer(new DesktopAssemblyIdentityComparer(new AssemblyPortabilityPolicy(suppressSilverlightLibraryAssembliesPortability: false, suppressSilverlightPlatformAssembliesPortability: false)));
            yield return defaultOptions.WithAssemblyIdentityComparer(new DesktopAssemblyIdentityComparer(new AssemblyPortabilityPolicy(suppressSilverlightLibraryAssembliesPortability: true, suppressSilverlightPlatformAssembliesPortability: false)));
            yield return defaultOptions.WithAssemblyIdentityComparer(new DesktopAssemblyIdentityComparer(new AssemblyPortabilityPolicy(suppressSilverlightLibraryAssembliesPortability: false, suppressSilverlightPlatformAssembliesPortability: true)));
            yield return defaultOptions.WithAssemblyIdentityComparer(new DesktopAssemblyIdentityComparer(new AssemblyPortabilityPolicy(suppressSilverlightLibraryAssembliesPortability: true, suppressSilverlightPlatformAssembliesPortability: true)));
        }

        private static IEnumerable<CSharpParseOptions> GetCSharpParseOptions()
        {
            var parseOptions = new CSharpParseOptions(
                languageVersion: LanguageVersion.CSharp8,
                kind: SourceCodeKind.Regular);

            yield return parseOptions;
            yield return parseOptions.WithLanguageVersion(LanguageVersion.CSharp9);
            yield return parseOptions.WithLanguageVersion(LanguageVersion.Latest);
            yield return parseOptions.WithLanguageVersion(LanguageVersion.Preview);
        }
    }
}
