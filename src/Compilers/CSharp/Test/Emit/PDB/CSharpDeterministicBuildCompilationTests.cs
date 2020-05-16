// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
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
    public class CSharpDeterministicBuildCompilationTests : CSharpTestBase
    {
        // Provide non default options for to test that they are being serialized
        // to the pdb correctly. It needs to produce a compilation to be emitted, but otherwise
        // everything should be non-default if possible. Diagnostic settings are ignored
        // because they won't be serialized. 

        private static readonly Encoding DefaultEncoding = Encoding.UTF7;
        private static readonly string[] PreprocessorSymbols = new[] { "PreOne", "PreTwo" };

        private static readonly CSharpParseOptions CSharpParseOptions = new CSharpParseOptions(
            languageVersion: LanguageVersion.CSharp8,
            kind: SourceCodeKind.Regular,
            preprocessorSymbols: PreprocessorSymbols);

        // Use constructor that requires all arguments. If new arguments are added, it's possible they need to be
        // included in the pdb serialization and added to tests here
        private static readonly CSharpCompilationOptions CSharpCompilationOptions = new CSharpCompilationOptions(
            OutputKind.ConsoleApplication,
            reportSuppressedDiagnostics: true,
            moduleName: "Module",
            mainTypeName: "MainType",
            scriptClassName: null,
            usings: new[] { "System", "System.Threading" },
            optimizationLevel: OptimizationLevel.Debug,
            checkOverflow: true,
            allowUnsafe: true,
            cryptoKeyContainer: null,
            cryptoKeyFile: null,
            cryptoPublicKey: ImmutableArray<byte>.Empty,
            delaySign: null,
            platform: Platform.AnyCpu,
            generalDiagnosticOption: ReportDiagnostic.Default,
            warningLevel: 4,
            specificDiagnosticOptions: null,
            concurrentBuild: false,
            deterministic: true,
            currentLocalTime: DateTime.Now,
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
            codePage: DefaultEncoding,
            preprocessorSymbols: PreprocessorSymbols);

        private static readonly EmitOptions EmitOptions = new EmitOptions(
            debugInformationFormat: DebugInformationFormat.Embedded,
            pdbChecksumAlgorithm: HashAlgorithmName.SHA256);

        private static void VerifyCompilationOptions(CSharpCompilationOptions originalOptions, BlobReader compilationOptionsBlobReader, string compilerVersion = null)
        {
            var pdbOptions = DeterministicBuildCompilationTestHelpers.ParseCompilationOptions(compilationOptionsBlobReader);
            compilerVersion ??= DeterministicBuildCompilationTestHelpers.GetCurrentCompilerVersion();

            // See CSharpCompilation.SerializeForPdb to see options that are included
            Assert.Equal(compilerVersion.ToString(), pdbOptions["compilerversion"]);
            Assert.Equal(originalOptions.NullableContextOptions.ToString(), pdbOptions["nullable"]);
            Assert.Equal(originalOptions.CheckOverflow.ToString(), pdbOptions["checked"]);
            Assert.Equal(originalOptions.CodePage.CodePage.ToString(), pdbOptions["codepage"]);
            Assert.Equal(originalOptions.AllowUnsafe.ToString(), pdbOptions["unsafe"]);
            Assert.Equal(string.Join(",", originalOptions.PreprocessorSymbols), pdbOptions["define"]);
        }

        private static void TestDeterministicCompilationCSharp(string code, Encoding encoding, params TestMetadataReferenceInfo[] metadataReferences)
        {
            var syntaxTree = Parse(code, "goo.cs", CSharpParseOptions, encoding);

            var originalCompilation = CreateCompilation(
                syntaxTree,
                references: metadataReferences.SelectAsArray(r => r.MetadataReference),
                options: CSharpCompilationOptions);

            var peBlob = originalCompilation.EmitToArray(EmitOptions);

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

                    VerifyCompilationOptions(CSharpCompilationOptions, compilationOptionsReader);
                    DeterministicBuildCompilationTestHelpers.VerifyReferenceInfo(metadataReferences, metadataReferenceReader);
                }
            }
        }

        [Fact]
        public void PortablePdb_DeterministicCompilation1()
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
            var referenceCompilation = CreateCompilation(
@"public struct StructWithReference
{
    string PrivateData;
}
public struct StructWithValue
{
    int PrivateData;
}",
            options: TestOptions.DebugDll);

            using var reference = TestMetadataReferenceInfo.Create(
                referenceCompilation,
                fullPath: "abcd.dll",
                emitOptions: EmitOptions.Default);

            TestDeterministicCompilationCSharp(source, DefaultEncoding, reference);
        }
    }
}
