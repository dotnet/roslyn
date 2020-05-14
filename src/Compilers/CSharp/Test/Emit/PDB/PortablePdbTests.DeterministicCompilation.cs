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
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Debugging;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.InternalUtilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.PDB
{
    public partial class PortablePdbTests
    {
        private class TestMetadataReferenceInfo
        {
            public readonly Compilation Compilation;
            public readonly TestMetadataReference MetadataReference;
            public int Timestamp;
            public int SizeOfImage;
            public Guid Mvid;
            public string Name;

            public TestMetadataReferenceInfo(string code, string fullPath)
            {
                Compilation = CreateCompilation(code, options: TestOptions.DebugDll);
                using var referenceStream = Compilation.EmitToStream(EmitOptions.Default);

                var metadata = AssemblyMetadata.CreateFromStream(referenceStream);
                MetadataReference = new TestMetadataReference(metadata, fullPath: fullPath);

                using var peReader = new PEReader(referenceStream);
                Timestamp = peReader.GetTimestamp();
                SizeOfImage = peReader.GetSizeOfImage();
                Mvid = peReader.GetMvid();
                Name = PathUtilities.GetFileName(fullPath);
            }
        }
        static BlobReader GetSingleBlob(Guid infoGuid, MetadataReader pdbReader)
        {
            return (from cdiHandle in pdbReader.GetCustomDebugInformation(EntityHandle.ModuleDefinition)
                    let cdi = pdbReader.GetCustomDebugInformation(cdiHandle)
                    where pdbReader.GetGuid(cdi.Kind) == infoGuid
                    select pdbReader.GetBlobReader(cdi.Value)).Single();
        }

        // Move this to a central location?
        static (int timestamp, int imageSize, string name, Guid mvid) ParseMetadataReferenceInfo(BlobReader blobReader)
        {
            // Name is first. UTF8 encoded null-terminated string
            var terminatorIndex = blobReader.IndexOf(0);
            Assert.NotEqual(-1, terminatorIndex);

            var name = blobReader.ReadUTF8(terminatorIndex);

            // Skip the null terminator
            blobReader.ReadByte();

            var timestamp = blobReader.ReadInt32();
            var imageSize = blobReader.ReadInt32();
            var mvid = blobReader.ReadGuid();

            Assert.Equal(0, blobReader.RemainingBytes);

            return (timestamp, imageSize, name, mvid);
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
                var (timestamp, imageSize, name, mvid) = ParseMetadataReferenceInfo(metadataReferenceReader);
                Assert.Equal(reference.Timestamp, timestamp);
                Assert.Equal(reference.SizeOfImage, imageSize);
                Assert.Equal(reference.Name, name);
                Assert.Equal(reference.Mvid, mvid);
            }
        }

        private static void TestDeterministicCompilation(SyntaxTree syntaxTree, params TestMetadataReferenceInfo[] metadataReferences)
        {
            var originalCompilation = CreateCompilation(
                syntaxTree,
                references: metadataReferences.SelectAsArray(r => r.MetadataReference),
                options: TestOptions.DebugDll.WithDeterministic(true));

            var originalCompilationOptions = originalCompilation.Options;
            var originalEmitOptions = EmitOptions.Default;

            var peBlob = originalCompilation.EmitToArray(
                originalEmitOptions.
                WithDebugInformationFormat(DebugInformationFormat.Embedded).
                WithPdbChecksumAlgorithm(HashAlgorithmName.SHA384).
                WithPdbFilePath(@"a/b/c/d.pdb"));

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

                    VerifyCompilationOptions(originalCompilationOptions, compilationOptionsReader);
                    VerifyReferenceInfo(metadataReferences, metadataReferenceReader);
                }
            }
        }

        [Fact]
        public void PortablePdb_DeterministicCompilation1()
        {

            string source = @"
using System;

class C
{
    public static void Main()
    {
        Console.WriteLine();
    }
}
";
            var reference = new TestMetadataReferenceInfo(
@"public struct StructWithReference
{
    string PrivateData;
}
public struct StructWithValue
{
    int PrivateData;
}", fullPath: "abcd.dll");

            TestDeterministicCompilation(Parse(source, "goo.cs"), reference);
        }
    }
}
