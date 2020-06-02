// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Collections.Generic;
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
            string compilerVersion = null)
        {
            var pdbOptions = DeterministicBuildCompilationTestHelpers.ParseCompilationOptions(compilationOptionsBlobReader);
            compilerVersion ??= DeterministicBuildCompilationTestHelpers.GetCurrentCompilerVersion();


            DeterministicBuildCompilationTestHelpers.AssertEncoding(emitOptions, compilation, pdbOptions);

            Assert.Equal(DeterministicBuildCompilationTestHelpers.GetPortabilityPolicy(originalOptions), pdbOptions["portability-policy"]);

            // See CSharpCompilation.SerializeForPdb to see options that are included
            Assert.Equal(compilerVersion.ToString(), pdbOptions["compilerversion"]);
            Assert.Equal(originalOptions.NullableContextOptions.ToString(), pdbOptions["nullable"]);
            Assert.Equal(originalOptions.CheckOverflow.ToString(), pdbOptions["checked"]);
            Assert.Equal(originalOptions.AllowUnsafe.ToString(), pdbOptions["unsafe"]);

            var isOptimized = originalOptions.OptimizationLevel == OptimizationLevel.Release ? true : false;
            Assert.Equal(isOptimized.ToString(), pdbOptions["optimize"]);

            var firstSyntaxTree = compilation.SyntaxTrees.FirstOrDefault() as CSharpSyntaxTree;
            if (firstSyntaxTree is null || firstSyntaxTree.Options.PreprocessorSymbols.IsEmpty)
            {
                Assert.False(pdbOptions.ContainsKey("define"));
            }
            else
            {
                Assert.Equal(string.Join(",", firstSyntaxTree.Options.PreprocessorSymbolNames), pdbOptions["define"]);
            }
        }

        private static void TestDeterministicCompilationCSharp(SyntaxTree[] syntaxTrees, CSharpCompilationOptions compilationOptions, EmitOptions emitOptions, params TestMetadataReferenceInfo[] metadataReferences)
        {
            var originalCompilation = CreateCompilation(
                syntaxTrees,
                references: metadataReferences.SelectAsArray(r => r.MetadataReference),
                targetFramework: TargetFramework.NetStandard20,
                options: compilationOptions);

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

                    var metadataReferenceReader = DeterministicBuildCompilationTestHelpers.GetSingleBlob(PortableCustomDebugInfoKinds.MetadataReferenceInfo, pdbReader);
                    var compilationOptionsReader = DeterministicBuildCompilationTestHelpers.GetSingleBlob(PortableCustomDebugInfoKinds.CompilationOptions, pdbReader);

                    VerifyCompilationOptions(compilationOptions, originalCompilation, emitOptions, compilationOptionsReader);
                    DeterministicBuildCompilationTestHelpers.VerifyReferenceInfo(metadataReferences, metadataReferenceReader);
                }
            }
        }

        [Theory]
        [ClassData(typeof(CSharpDeterministicBuildCompilationTests))]
        public void PortablePdb_DeterministicCompilation(CSharpCompilationOptions compilationOptions, EmitOptions emitOptions)
        {
            var parseOptions = new CSharpParseOptions(
                languageVersion: LanguageVersion.CSharp8,
                kind: SourceCodeKind.Regular);

            var sourceOne = Parse(@"
using System;

class MainType
{
    public static void Main()
    {
        Console.WriteLine();
    }
}
", options: parseOptions, encoding: Encoding.UTF8);

            var sourceTwo = Parse(@"
class TypeTwo
{
}", options: parseOptions, encoding: new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));

            var sourceThree = Parse(@"
class TypeThree
{
}", options: parseOptions, encoding: Encoding.UTF7);

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
            TestDeterministicCompilationCSharp(testSource, compilationOptions, emitOptions, referenceOne, referenceTwo);
        }

        [ConditionalTheory(typeof(DesktopOnly))]
        [ClassData(typeof(CSharpDeterministicBuildCompilationTests))]
        public void PortablePdb_DeterministicCompilationWithSJIS(CSharpCompilationOptions compilationOptions, EmitOptions emitOptions)
        {
            var parseOptions = new CSharpParseOptions(
                languageVersion: LanguageVersion.CSharp8,
                kind: SourceCodeKind.Regular);

            var sourceOne = Parse(@"
using System;

class MainType
{
    public static void Main()
    {
        Console.WriteLine();
    }
}
", options: parseOptions, encoding: Encoding.UTF8);

            var sourceTwo = Parse(@"
class TypeTwo
{
}", options: parseOptions, encoding: new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));

            var sourceThree = Parse(@"
class TypeThree
{
}", options: parseOptions, encoding: Encoding.GetEncoding(932)); // SJIS encoding

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
            TestDeterministicCompilationCSharp(testSource, compilationOptions, emitOptions, referenceOne, referenceTwo);
        }

        public IEnumerator<object[]> GetEnumerator()
        => GetTestParameters().GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator()
        => GetEnumerator();

        private static IEnumerable<object[]> GetTestParameters()
        {
            var compilationOptionsSet = GetCompilationOptions();
            var emitOptionsSet = DeterministicBuildCompilationTestHelpers.GetEmitOptions();

            foreach (var compilationOptions in compilationOptionsSet)
            {
                foreach (var emitOptions in emitOptionsSet)
                {
                    yield return new object[] { compilationOptions, emitOptions };
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
    }
}
