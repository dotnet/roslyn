// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Security.Cryptography;
using System.Text;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Debugging;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.InternalUtilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.VisualBasic;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.PDB
{
    public partial class DeterministicBuildCompilationTests : CSharpPDBTestBase
    {
        #region Options
        // Provide non default options for both CSharp and VB to test that they are being serialized
        // to the pdb correctly. It needs to produce a compilation to be emitted, but otherwise
        // everything should be non-default if possible. Diagnostic settings are ignored
        // because they won't be serialized. 
        private static readonly CSharpParseOptions CSharpParseOptions = new CSharpParseOptions(
            languageVersion: LanguageVersion.CSharp8,
            kind: SourceCodeKind.Regular,
            preprocessorSymbols: new[] { "PreOne", "PreTwo" });

        private static readonly CSharpCompilationOptions CSharpCompilationOptions = new CSharpCompilationOptions(
            OutputKind.ConsoleApplication,
            moduleName: "Module",
            mainTypeName: "MainType",
            usings: new[] { "System", "System.Threading" },
            optimizationLevel: OptimizationLevel.Debug,
            checkOverflow: true,
            allowUnsafe: true,
            deterministic: true,
            nullableContextOptions: NullableContextOptions.Enable);

        private static readonly VisualBasicParseOptions VisualBasicParseOptions = new VisualBasicParseOptions(
            VisualBasic.LanguageVersion.VisualBasic16,
            documentationMode: DocumentationMode.Diagnose,
            preprocessorSymbols: new[] { new KeyValuePair<string, object>("PreOne", "True"), new KeyValuePair<string, object>("PreTwo", "Test") });

        private static readonly VisualBasicCompilationOptions VisualBasicCompilationOptions = new VisualBasicCompilationOptions(
            OutputKind.ConsoleApplication,
            moduleName: "Module",
            mainTypeName: "MainType",
            globalImports: GlobalImport.Parse("System", "System.Threading"),
            rootNamespace: "RootNamespace",
            optionStrict: OptionStrict.On,
            optionInfer: false,
            optionExplicit: false,
            optionCompareText: true,
            parseOptions: VisualBasicParseOptions,
            embedVbCoreRuntime: true,
            optimizationLevel: OptimizationLevel.Release,
            checkOverflow: false,
            deterministic: true);

        private static readonly EmitOptions EmitOptions = new EmitOptions(
            debugInformationFormat: DebugInformationFormat.Embedded,
            pdbChecksumAlgorithm: HashAlgorithmName.SHA256);

        #endregion
        static BlobReader GetSingleBlob(Guid infoGuid, MetadataReader pdbReader)
        {
            return (from cdiHandle in pdbReader.GetCustomDebugInformation(EntityHandle.ModuleDefinition)
                    let cdi = pdbReader.GetCustomDebugInformation(cdiHandle)
                    where pdbReader.GetGuid(cdi.Kind) == infoGuid
                    select pdbReader.GetBlobReader(cdi.Value)).Single();
        }

        // Move this to a central location?
        static MetadataReferenceInfo ParseMetadataReferenceInfo(BlobReader blobReader)
        {
            // Name is first. UTF8 encoded null-terminated string
            var terminatorIndex = blobReader.IndexOf(0);
            Assert.NotEqual(-1, terminatorIndex);

            var name = blobReader.ReadUTF8(terminatorIndex);

            // Skip the null terminator
            blobReader.ReadByte();

            // Extern aliases are second
            terminatorIndex = blobReader.IndexOf(0);
            Assert.NotEqual(-1, terminatorIndex);

            var externAliases = blobReader.ReadUTF8(terminatorIndex);

            // Skip the null terminator
            blobReader.ReadByte();

            var kind = (MetadataImageKind)blobReader.ReadByte();
            var embedInteropTypes = blobReader.ReadBoolean();
            var timestamp = blobReader.ReadInt32();
            var imageSize = blobReader.ReadInt32();
            var mvid = blobReader.ReadGuid();

            Assert.Equal(0, blobReader.RemainingBytes);

            return new MetadataReferenceInfo(
                timestamp,
                imageSize,
                name,
                mvid,
                string.IsNullOrEmpty(externAliases)
                    ? ImmutableArray<string>.Empty
                    : externAliases.Split(',').ToImmutableArray(),
                kind,
                embedInteropTypes);
        }

        // Move this to a central location?
        static ImmutableDictionary<string, string> ParseCompilationOptions(BlobReader blobReader)
        {

            // Compiler flag bytes are UTF-8 null-terminated key-value pairs
            string key = null;
            Dictionary<string, string> kvp = new Dictionary<string, string>();
            for (; ; )
            {
                var nullIndex = blobReader.IndexOf(0);
                if (nullIndex == -1)
                {
                    break;
                }

                var value = blobReader.ReadUTF8(nullIndex);

                // Skip the null terminator
                blobReader.ReadByte();

                if (key is null)
                {
                    key = value;
                }
                else
                {
                    kvp[key] = value;
                    key = null;
                }
            }

            Assert.Null(key);
            Assert.Equal(0, blobReader.RemainingBytes);
            return kvp.ToImmutableDictionary();
        }

        private static void VerifyCompilationOptions(CompilationOptions originalOptions, BlobReader compilationOptionsBlobReader, string compilerVersion = null)
        {
            var pdbOptions = ParseCompilationOptions(compilationOptionsBlobReader);
            compilerVersion ??= typeof(Compilation).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;

            Assert.Equal(compilerVersion.ToString(), pdbOptions["compilerversion"]);
            Assert.Equal(originalOptions.NullableContextOptions.ToString(), pdbOptions["nullable"]);
            Assert.Equal(originalOptions.CheckOverflow.ToString(), pdbOptions["checked"]);
        }

        private static void VerifyReferenceInfo(TestMetadataReferenceInfo[] references, BlobReader metadataReferenceReader)
        {
            foreach (var reference in references)
            {
                var info = ParseMetadataReferenceInfo(metadataReferenceReader);
                var originalInfo = reference.MetadataReferenceInfo;

                originalInfo.AssertEqual(info);
            }
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

                    var metadataReferenceReader = GetSingleBlob(PortableCustomDebugInfoKinds.MetadataReferenceInfo, pdbReader);
                    var compilationOptionsReader = GetSingleBlob(PortableCustomDebugInfoKinds.CompilationOptions, pdbReader);

                    VerifyCompilationOptions(CSharpCompilationOptions, compilationOptionsReader);
                    VerifyReferenceInfo(metadataReferences, metadataReferenceReader);
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
            using var reference = TestMetadataReferenceInfo.Create(
@"public struct StructWithReference
{
    string PrivateData;
}
public struct StructWithValue
{
    int PrivateData;
}",
    fullPath: "abcd.dll",
    emitOptions: EmitOptions.Default);

            TestDeterministicCompilationCSharp(source, Encoding.UTF7, reference);
        }
    }
}
