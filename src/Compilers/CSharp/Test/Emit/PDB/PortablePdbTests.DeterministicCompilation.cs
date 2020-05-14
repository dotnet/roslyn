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
            fixed (byte* metadataReferenceBytesPtr = &metadataReferenceBytes[0])
            {
                var blobReader = new BlobReader(metadataReferenceBytesPtr, metadataReferenceBytes.Length);

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
        }

        // Move this to a central location?
        static unsafe ImmutableDictionary<string, string> ParserCompilerFlags(byte[] compilerFlagBytes)
        {
            fixed (byte* compilerFlagBytesPtr = &compilerFlagBytes[0])
            {
                var blobReader = new BlobReader(compilerFlagBytesPtr, compilerFlagBytes.Length);

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

            using var referenceStream = referenceCompilation.EmitToStream(EmitOptions.Default);

            var metadata = AssemblyMetadata.CreateFromStream(referenceStream);
            using var referencePEReader = new PEReader(referenceStream);

            var originalCompilation = CreateCompilation(
                Parse(source, "goo.cs"),
                references: new[] { new TestMetadataReference(metadata, fullPath: "abcd.dll") },
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
                    Assert.Equal("abcd.dll", name);
                    Assert.Equal(referencePEReader.GetMvid(), mvid);
                }
            }
        }
    }
}
