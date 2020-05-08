// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Security.Cryptography;
using System.Text;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Debugging;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualBasic;
using Roslyn.Test.Utilities;
using Roslyn.Test.Utilities.PDB;
using Roslyn.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.PDB
{
    public class CSharpDeterministicBuildCompilationTests : CSharpTestBase, IEnumerable<object[]>
    {
        private static void VerifyCompilationOptions(CSharpCompilationOptions originalOptions, BlobReader compilationOptionsBlobReader, string compilerVersion = null)
        {
            var pdbOptions = DeterministicBuildCompilationTestHelpers.ParseCompilationOptions(compilationOptionsBlobReader);
            compilerVersion ??= DeterministicBuildCompilationTestHelpers.GetCurrentCompilerVersion();

            // See CSharpCompilation.SerializeForPdb to see options that are included
            Assert.Equal(compilerVersion.ToString(), pdbOptions["compilerversion"]);
            Assert.Equal(originalOptions.NullableContextOptions.ToString(), pdbOptions["nullable"]);
            Assert.Equal(originalOptions.CheckOverflow.ToString(), pdbOptions["checked"]);
            Assert.Equal(originalOptions.AllowUnsafe.ToString(), pdbOptions["unsafe"]);

            if (originalOptions.CodePage is null)
            {
                Assert.False(pdbOptions.ContainsKey("codepage"));
            }
            else
            {
                Assert.Equal(originalOptions.CodePage.CodePage.ToString(), pdbOptions["codepage"]);
            }

            if (originalOptions.PreprocessorSymbols.Any())
            {
                Assert.Equal(string.Join(",", originalOptions.PreprocessorSymbols), pdbOptions["define"]);
            }
            else
            {
                Assert.False(pdbOptions.ContainsKey("define"));
            }
        }

        private static void TestDeterministicCompilationCSharp(string code, CSharpParseOptions parseOptions, CSharpCompilationOptions compilationOptions, EmitOptions emitOptions, params TestMetadataReferenceInfo[] metadataReferences)
        {
            var syntaxTree = Parse(code, "goo.cs", parseOptions, compilationOptions.CodePage ?? Encoding.UTF8);

            var originalCompilation = CreateCompilation(
                syntaxTree,
                references: metadataReferences.SelectAsArray(r => r.MetadataReference),
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

                    VerifyCompilationOptions(compilationOptions, compilationOptionsReader);
                    DeterministicBuildCompilationTestHelpers.VerifyReferenceInfo(metadataReferences, metadataReferenceReader);
                }
            }
        }

        [Theory]
        [ClassData(typeof(CSharpDeterministicBuildCompilationTests))]
        public void PortablePdb_DeterministicCompilation1(CSharpCompilationOptions compilationOptions)
        {
            string source = @"
using System;

class MainType
{
    public static void Main()
    {
        Console.WriteLine();
    }
}
";
            var referenceOneCompilation = CreateCompilation(
@"public struct StructWithReference
{
    string PrivateData;
}
public struct StructWithValue
{
    int PrivateData;
}",
            options: TestOptions.DebugDll);

            var referenceTwoCompilation = CreateCompilation(
@"public class ReferenceTwo
{
}",
            options: TestOptions.DebugDll);

            CSharpParseOptions parseOptions = new CSharpParseOptions(
                languageVersion: LanguageVersion.CSharp8,
                kind: SourceCodeKind.Regular,
                preprocessorSymbols: compilationOptions.PreprocessorSymbols);

            EmitOptions emitOptions = new EmitOptions(
                debugInformationFormat: DebugInformationFormat.Embedded,
                pdbChecksumAlgorithm: HashAlgorithmName.SHA256);


            using var referenceOne = TestMetadataReferenceInfo.Create(
                referenceOneCompilation,
                fullPath: "abcd.dll",
                emitOptions: emitOptions);

            using var referenceTwo = TestMetadataReferenceInfo.Create(
                referenceTwoCompilation,
                fullPath: "efgh.dll",
                emitOptions: emitOptions);

            TestDeterministicCompilationCSharp(source, parseOptions, compilationOptions, emitOptions, referenceOne, referenceTwo);
        }

        public IEnumerator<object[]> GetEnumerator()
        => GetData().Select(o => new object[] { o }).GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator()
        => GetEnumerator();

        private static IEnumerable<CSharpCompilationOptions> GetData()
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
                nullableContextOptions: NullableContextOptions.Enable,
                codePage: Encoding.UTF7,
                preprocessorSymbols: new[] { "PreOne", "PreTwo" });

            yield return defaultOptions;
            yield return defaultOptions.WithCodePage(Encoding.Default);
            yield return defaultOptions.WithCodePage(Encoding.UTF32);
            yield return defaultOptions.WithCodePage(null);
            yield return defaultOptions.WithPreprocessorSymbols(new[] { "PreOne", "PreTwo", "PreThree" });
            yield return defaultOptions.WithPreprocessorSymbols(null);
            yield return defaultOptions.WithPreprocessorSymbols(new string[0]);
            yield return defaultOptions.WithNullableContextOptions(NullableContextOptions.Disable);
            yield return defaultOptions.WithNullableContextOptions(NullableContextOptions.Warnings);
        }
    }
}
