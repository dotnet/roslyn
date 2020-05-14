// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Security.Cryptography;
using System.Text;
using Castle.DynamicProxy.Generators.Emitters.SimpleAST;
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
    public class PortablePdbTests : CSharpPDBTestBase
    {
        static byte[] GetSingleBlob(Guid infoGuid, MetadataReader pdbReader)
        {
            return (from cdiHandle in pdbReader.GetCustomDebugInformation(EntityHandle.ModuleDefinition)
                    let cdi = pdbReader.GetCustomDebugInformation(cdiHandle)
                    where pdbReader.GetGuid(cdi.Kind) == infoGuid
                    select pdbReader.GetBlobBytes(cdi.Value)).Single();
        }

        // Move this to a central location?
        static unsafe (int timestamp, int imageSize, string name, Guid mvid) ParseMetadataReferenceInfo(byte[] metadataReferenceBytes)
        {
            var blobReader = new BlobReader((byte*)metadataReferenceBytes[0], metadataReferenceBytes.Length);

            // Name is first. UTF8 encoded null-terminated string
            var terminatorIndex = metadataReferenceBytes.IndexOf((byte)0);
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
        static unsafe ImmutableDictionary<string, string> ParserCompilerFlags(byte[] compilerFlagBytes)
        {
            var blobReader = new BlobReader((byte*)compilerFlagBytes[0], compilerFlagBytes.Length);

            // Compiler flag bytes are UTF-8 null-terminated key-value pairs
            string key = null;
            Dictionary<string, string> kvp = new Dictionary<string, string>();
            for (int i = 0, start = 0; i < compilerFlagBytes.Length; i++)
            {
                if (compilerFlagBytes[i] == 0)
                {
                    var value = blobReader.ReadUTF8(i - start);

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

                    start = i + 1;
                }
            }

            Assert.Null(key);
            Assert.Equal(0, blobReader.RemainingBytes);
            return kvp.ToImmutableDictionary();
        }

        [Fact]
        public void SequencePointBlob()
        {
            string source = @"
class C
{
    public static void Main()
    {
        if (F())
        {
            System.Console.WriteLine(1);
        }
    }

    public static bool F() => false;
}
";
            var c = CreateCompilation(source, options: TestOptions.DebugDll);

            var pdbStream = new MemoryStream();
            var peBlob = c.EmitToArray(EmitOptions.Default.WithDebugInformationFormat(DebugInformationFormat.PortablePdb), pdbStream: pdbStream);

            using (var peReader = new PEReader(peBlob))
            using (var pdbMetadata = new PinnedMetadata(pdbStream.ToImmutable()))
            {
                var mdReader = peReader.GetMetadataReader();
                var pdbReader = pdbMetadata.Reader;

                foreach (var methodHandle in mdReader.MethodDefinitions)
                {
                    var method = mdReader.GetMethodDefinition(methodHandle);
                    var methodDebugInfo = pdbReader.GetMethodDebugInformation(methodHandle);

                    var name = mdReader.GetString(method.Name);

                    TextWriter writer = new StringWriter();
                    foreach (var sp in methodDebugInfo.GetSequencePoints())
                    {
                        if (sp.IsHidden)
                        {
                            writer.WriteLine($"{sp.Offset}: <hidden>");
                        }
                        else
                        {
                            writer.WriteLine($"{sp.Offset}: ({sp.StartLine},{sp.StartColumn})-({sp.EndLine},{sp.EndColumn})");
                        }
                    }

                    var spString = writer.ToString();
                    var spBlob = pdbReader.GetBlobBytes(methodDebugInfo.SequencePointsBlob);

                    switch (name)
                    {
                        case "Main":
                            AssertEx.AssertEqualToleratingWhitespaceDifferences(@"
0: (5,5)-(5,6)
1: (6,9)-(6,17)
7: <hidden>
10: (7,9)-(7,10)
11: (8,13)-(8,41)
18: (9,9)-(9,10)
19: (10,5)-(10,6)
", spString);
                            AssertEx.Equal(new byte[]
                            {
                                0x01, // local signature

                                0x00, // IL offset
                                0x00, // Delta Lines
                                0x01, // Delta Columns
                                0x05, // Start Line
                                0x05, // Start Column

                                0x01, // delta IL offset
                                0x00, // Delta Lines
                                0x08, // Delta Columns
                                0x02, // delta Start Line (signed compressed)
                                0x08, // delta Start Column (signed compressed)

                                0x06, // delta IL offset
                                0x00, // hidden
                                0x00, // hidden

                                0x03, // delta IL offset
                                0x00, // Delta Lines
                                0x01, // Delta Columns
                                0x02, // delta Start Line (signed compressed)
                                0x00, // delta Start Column (signed compressed)

                                0x01, // delta IL offset
                                0x00, // Delta Lines
                                0x1C, // Delta Columns
                                0x02, // delta Start Line (signed compressed)
                                0x08, // delta Start Column (signed compressed)

                                0x07, // delta IL offset
                                0x00, // Delta Lines
                                0x01, // Delta Columns
                                0x02, // delta Start Line (signed compressed)
                                0x79, // delta Start Column (signed compressed)

                                0x01, // delta IL offset
                                0x00, // Delta Lines
                                0x01, // Delta Columns
                                0x02, // delta Start Line (signed compressed)
                                0x79, // delta Start Column (signed compressed)
                            }, spBlob);
                            break;

                        case "F":
                            AssertEx.AssertEqualToleratingWhitespaceDifferences("0: (12,31)-(12,36)", spString);
                            AssertEx.Equal(new byte[]
                            {
                                0x00, // local signature

                                0x00, // delta IL offset
                                0x00, // Delta Lines
                                0x05, // Delta Columns
                                0x0C, // Start Line
                                0x1F  // Start Column
                            }, spBlob);
                            break;
                    }
                }
            }
        }

        [Fact]
        public void EmbeddedPortablePdb()
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
            var c = CreateCompilation(Parse(source, "goo.cs"), options: TestOptions.DebugDll);

            var peBlob = c.EmitToArray(EmitOptions.Default.
                WithDebugInformationFormat(DebugInformationFormat.Embedded).
                WithPdbFilePath(@"a/b/c/d.pdb").
                WithPdbChecksumAlgorithm(HashAlgorithmName.SHA512));

            using (var peReader = new PEReader(peBlob))
            {
                var entries = peReader.ReadDebugDirectory();

                AssertEx.Equal(new[] { DebugDirectoryEntryType.CodeView, DebugDirectoryEntryType.PdbChecksum, DebugDirectoryEntryType.EmbeddedPortablePdb }, entries.Select(e => e.Type));

                var codeView = entries[0];
                var checksum = entries[1];
                var embedded = entries[2];

                // EmbeddedPortablePdb entry:
                Assert.Equal(0x0100, embedded.MajorVersion);
                Assert.Equal(0x0100, embedded.MinorVersion);
                Assert.Equal(0u, embedded.Stamp);

                BlobContentId pdbId;
                using (var embeddedMetadataProvider = peReader.ReadEmbeddedPortablePdbDebugDirectoryData(embedded))
                {
                    var mdReader = embeddedMetadataProvider.GetMetadataReader();
                    AssertEx.Equal(new[] { "goo.cs" }, mdReader.Documents.Select(doc => mdReader.GetString(mdReader.GetDocument(doc).Name)));

                    pdbId = new BlobContentId(mdReader.DebugMetadataHeader.Id);
                }

                // CodeView entry:
                var codeViewData = peReader.ReadCodeViewDebugDirectoryData(codeView);
                Assert.Equal(0x0100, codeView.MajorVersion);
                Assert.Equal(0x504D, codeView.MinorVersion);
                Assert.Equal(pdbId.Stamp, codeView.Stamp);
                Assert.Equal(pdbId.Guid, codeViewData.Guid);
                Assert.Equal("d.pdb", codeViewData.Path);

                // Checksum entry:
                var checksumData = peReader.ReadPdbChecksumDebugDirectoryData(checksum);
                Assert.Equal("SHA512", checksumData.AlgorithmName);
                Assert.Equal(64, checksumData.Checksum.Length);
            }
        }

        [Fact]
        public void PortablePdb_DeterministicCompilation1()
        {
            var referenceCompilation = CreateCompilation(
@"public struct StructWithReference
{
    string PrivateData;
}
public struct StructWithValue
{
    int PrivateData;
}", options: TestOptions.DebugDll);

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

            var emitResult = referenceCompilation.Emit("reference.dll");
            Assert.True(emitResult.Success);

            using var referenceStream = File.OpenRead("reference.dll");
            using var referencePEReader = new PEReader(referenceStream);

            var originalCompilation = CreateCompilation(
                Parse(source, "goo.cs"),
                references: new[] { new TestMetadataReference(fullPath: "reference.dll") },
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

                    var metadataReferenceBytes = GetSingleBlob(PortableCustomDebugInfoKinds.MetadataReferenceInfo, pdbReader);
                    var compilerFlagBytes = GetSingleBlob(PortableCustomDebugInfoKinds.CompilationOptions, pdbReader);

                    Assert.NotEmpty(metadataReferenceBytes);
                    Assert.NotEmpty(compilerFlagBytes);

                    var (timestamp, imageSize, name, mvid) = ParseMetadataReferenceInfo(metadataReferenceBytes);
                    var compilerFlags = ParserCompilerFlags(compilerFlagBytes);

                    // Check version
                    var compilerVersion = typeof(Compilation).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
                    Assert.Equal(compilerVersion.ToString(), compilerFlags["compilerversion"]);

                    // Check source encoding
                    //Assert.Equal("", compilerFlags["sourceencoding"]);

                    // Check the metadata references
                    Assert.Equal(referencePEReader.GetTimestamp(), timestamp);
                    Assert.Equal(referencePEReader.GetSizeOfImage(), imageSize);
                    Assert.Equal(referenceCompilation.AssemblyName, name);
                    Assert.Equal(referencePEReader.GetMvid(), mvid);
                }
            }
        }

        [Fact]
        public void EmbeddedPortablePdb_Deterministic()
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
            var c = CreateCompilation(Parse(source, "goo.cs"), options: TestOptions.DebugDll.WithDeterministic(true));

            var peBlob = c.EmitToArray(EmitOptions.Default.
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

                // EmbeddedPortablePdb entry:
                Assert.Equal(0x0100, embedded.MajorVersion);
                Assert.Equal(0x0100, embedded.MinorVersion);
                Assert.Equal(0u, embedded.Stamp);

                BlobContentId pdbId;
                using (var embeddedMetadataProvider = peReader.ReadEmbeddedPortablePdbDebugDirectoryData(embedded))
                {
                    var mdReader = embeddedMetadataProvider.GetMetadataReader();
                    AssertEx.Equal(new[] { "goo.cs" }, mdReader.Documents.Select(doc => mdReader.GetString(mdReader.GetDocument(doc).Name)));

                    pdbId = new BlobContentId(mdReader.DebugMetadataHeader.Id);
                }

                // CodeView entry:
                var codeViewData = peReader.ReadCodeViewDebugDirectoryData(codeView);
                Assert.Equal(0x0100, codeView.MajorVersion);
                Assert.Equal(0x504D, codeView.MinorVersion);
                Assert.Equal(pdbId.Stamp, codeView.Stamp);
                Assert.Equal(pdbId.Guid, codeViewData.Guid);
                Assert.Equal("d.pdb", codeViewData.Path);

                // Checksum entry:
                var checksumData = peReader.ReadPdbChecksumDebugDirectoryData(checksum);
                Assert.Equal("SHA384", checksumData.AlgorithmName);
                Assert.Equal(48, checksumData.Checksum.Length);

                // Reproducible entry:
                Assert.Equal(0, reproducible.MajorVersion);
                Assert.Equal(0, reproducible.MinorVersion);
                Assert.Equal(0U, reproducible.Stamp);
                Assert.Equal(0, reproducible.DataSize);
            }
        }

        [Fact]
        public void SourceLink()
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
            var sourceLinkBlob = Encoding.UTF8.GetBytes(@"
{
  ""documents"": {
     ""f:/build/*"" : ""https://raw.githubusercontent.com/my-org/my-project/1111111111111111111111111111111111111111/*""
  }
}
");

            var c = CreateCompilation(Parse(source, "f:/build/goo.cs"), options: TestOptions.DebugDll);

            var pdbStream = new MemoryStream();
            c.EmitToArray(EmitOptions.Default.WithDebugInformationFormat(DebugInformationFormat.PortablePdb), pdbStream: pdbStream, sourceLinkStream: new MemoryStream(sourceLinkBlob));
            pdbStream.Position = 0;

            using (var provider = MetadataReaderProvider.FromPortablePdbStream(pdbStream))
            {
                var pdbReader = provider.GetMetadataReader();

                var actualBlob =
                    (from cdiHandle in pdbReader.GetCustomDebugInformation(EntityHandle.ModuleDefinition)
                     let cdi = pdbReader.GetCustomDebugInformation(cdiHandle)
                     where pdbReader.GetGuid(cdi.Kind) == PortableCustomDebugInfoKinds.SourceLink
                     select pdbReader.GetBlobBytes(cdi.Value)).Single();

                AssertEx.Equal(sourceLinkBlob, actualBlob);
            }
        }

        [Fact]
        public void SourceLink_Embedded()
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
            var sourceLinkBlob = Encoding.UTF8.GetBytes(@"
{
  ""documents"": {
     ""f:/build/*"" : ""https://raw.githubusercontent.com/my-org/my-project/1111111111111111111111111111111111111111/*""
  }
}
");
            var c = CreateCompilation(Parse(source, "f:/build/goo.cs"), options: TestOptions.DebugDll);

            var peBlob = c.EmitToArray(EmitOptions.Default.WithDebugInformationFormat(DebugInformationFormat.Embedded), sourceLinkStream: new MemoryStream(sourceLinkBlob));

            using (var peReader = new PEReader(peBlob))
            {
                var embeddedEntry = peReader.ReadDebugDirectory().Single(e => e.Type == DebugDirectoryEntryType.EmbeddedPortablePdb);

                using (var embeddedMetadataProvider = peReader.ReadEmbeddedPortablePdbDebugDirectoryData(embeddedEntry))
                {
                    var pdbReader = embeddedMetadataProvider.GetMetadataReader();

                    var actualBlob =
                        (from cdiHandle in pdbReader.GetCustomDebugInformation(EntityHandle.ModuleDefinition)
                         let cdi = pdbReader.GetCustomDebugInformation(cdiHandle)
                         where pdbReader.GetGuid(cdi.Kind) == PortableCustomDebugInfoKinds.SourceLink
                         select pdbReader.GetBlobBytes(cdi.Value)).Single();

                    AssertEx.Equal(sourceLinkBlob, actualBlob);
                }
            }
        }

        [Fact]
        public void SourceLink_Errors()
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
            var sourceLinkStream = new TestStream(canRead: true, readFunc: (_, __, ___) => { throw new Exception("Error!"); });

            var c = CreateCompilation(Parse(source, "f:/build/goo.cs"), options: TestOptions.DebugDll);
            var result = c.Emit(new MemoryStream(), new MemoryStream(), options: EmitOptions.Default.WithDebugInformationFormat(DebugInformationFormat.PortablePdb), sourceLinkStream: sourceLinkStream);
            result.Diagnostics.Verify(
                // error CS0041: Unexpected error writing debug information -- 'Error!'
                Diagnostic(ErrorCode.FTL_DebugEmitFailure).WithArguments("Error!").WithLocation(1, 1));
        }
    }
}
