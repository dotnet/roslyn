// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;
using System.Security.Cryptography;
using System.Text;
using Microsoft.CodeAnalysis.CSharp.Symbols.Metadata.PE;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Debugging;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.PDB
{
    public class PortablePdbTests : CSharpPDBTestBase
    {
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

        public MetadataReader GetMetadataReaderfromSource(string source)
        {
            var c = CreateCompilation(new[] { source }, options: TestOptions.DebugDll);
            var pdbStream = new MemoryStream();
            var peImage = c.EmitToArray(EmitOptions.Default.WithDebugInformationFormat(DebugInformationFormat.PortablePdb), pdbStream: pdbStream);
            pdbStream.Position = 0;
            var provider = MetadataReaderProvider.FromPortablePdbStream(pdbStream);
            return provider.GetMetadataReader();
        }

        public MetadataReader GetMetadataReaderfromTwoSource(string source, string sourceB)
        {
            var sourceTreeA = SyntaxFactory.ParseSyntaxTree(source, path: "a.cs", encoding: Encoding.UTF8);
            var sourceTreeB = SyntaxFactory.ParseSyntaxTree(sourceB, path: "b.cs", encoding: Encoding.UTF8);
            var c = CreateCompilation(new[] { sourceTreeA, sourceTreeB }, options: TestOptions.DebugDll);
            var pdbStream = new MemoryStream();
            var peImage = c.EmitToArray(EmitOptions.Default.WithDebugInformationFormat(DebugInformationFormat.PortablePdb), pdbStream: pdbStream);
            pdbStream.Position = 0;
            var provider = MetadataReaderProvider.FromPortablePdbStream(pdbStream);
            return provider.GetMetadataReader();
        }

        [Fact]
        public void TestTypeDocumentInCDITotalDocs()
        {
            string source = @"
using System;

class M
{
    public static void A()
    {
        System.Console.WriteLine();
    }
}
class N
{
    public static void B()
    {
        System.Console.WriteLine();
    }

}

class O
{


}



";
            var pdbReader = GetMetadataReaderfromSource(source);
            Guid typeDocuments = new("932E74BC-DBA9-4478-8D46-0F32A7BAB3D3");
            var list = new List<CustomDebugInformationHandle>();
            foreach (var handle in pdbReader.CustomDebugInformation)
            {
                var entry = pdbReader.GetCustomDebugInformation(handle);
                var kind = entry.Kind;
                if (typeDocuments.Equals(pdbReader.GetGuid(kind)))
                {
                    list.Add(handle);
                }

            }
            Assert.Equal(1, list.Count);
        }

        [Fact]
        public void TestTypeDocumentInCDIDocNumber()
        {
            string source = @"
using System;

class M
{
    public static void A()
    {
        System.Console.WriteLine();
    }
}
class N
{


}

class O
{


}



";
            var pdbReader = GetMetadataReaderfromSource(source);
            Guid typeDocuments = new("932E74BC-DBA9-4478-8D46-0F32A7BAB3D3");
            var list = new System.Collections.Generic.List<int>();
            foreach (var handle in pdbReader.CustomDebugInformation)
            {
                var entry = pdbReader.GetCustomDebugInformation(handle);
                var kind = entry.Kind;
                if (typeDocuments.Equals(pdbReader.GetGuid(kind)))
                {
                    var temp = entry.Value;
                    list.Add(pdbReader.GetBlobReader(temp).ReadCompressedInteger());
                }
            }
            foreach (var doc in list)
            {
                Assert.Equal(1, doc);
            }
        }

        [Fact]
        public void TestTypeDocumentInCDIPartialClass()
        {
            string source = @"
using System;

partial class C
{
    

}
class N
{

        public static void A()
    {
        System.Console.WriteLine();
    }
}

class O
{
    public static void B()
    {
        System.Console.WriteLine();
    }

}



";
            string sourceB = @"
using System;

partial class C
{

}
";
            var pdbReader = GetMetadataReaderfromTwoSource(source, sourceB);
            Guid typeDocuments = new("932E74BC-DBA9-4478-8D46-0F32A7BAB3D3");
            BlobReader reader = new BlobReader();
            int i = 0;
            foreach (var handle in pdbReader.CustomDebugInformation)
            {
                var entry = pdbReader.GetCustomDebugInformation(handle);
                var kind = entry.Kind;
                if (typeDocuments.Equals(pdbReader.GetGuid(kind)))
                {
                    var value = entry.Value;
                    reader = pdbReader.GetBlobReader(value);
                    i++;
                }

            }
            i = 0;
            int[] testVals = new[] { 1, 2, 0, 0, 0, 0, 0, 0, 0, 0 };
            while (reader.RemainingBytes > 0)
            {

                Assert.Equal(testVals[i], reader.ReadCompressedInteger());
                i++;

            }
        }

        [Fact]
        public void TestTypeDocumentInCDIPartialClassandClasses()
        {
            string source = @"
using System;

partial class C
{
    

}
class N
{

        public static void A()
    {
        System.Console.WriteLine();
    }
}

class O
{
    public static void B()
    {
        System.Console.WriteLine();
    }

}



";
            string sourceB = @"
using System;

partial class C
{

}
";
            var pdbReader = GetMetadataReaderfromTwoSource(source, sourceB);

            Guid typeDocuments = new("932E74BC-DBA9-4478-8D46-0F32A7BAB3D3");
            EntityHandle[] hand = new[] { MetadataTokens.EntityHandle(2) };
            BlobReader[] reader = new BlobReader[10];
            int i = 0;
            foreach (var handle in pdbReader.CustomDebugInformation)
            {
                var entry = pdbReader.GetCustomDebugInformation(handle);
                var kind = entry.Kind;
                if (typeDocuments.Equals(pdbReader.GetGuid(kind)))
                {
                    var temp = entry.Value;
                    reader[i] = pdbReader.GetBlobReader(temp);
                    i++;
                }

            }
            i = 0;
            int[] testVals = new[] { 1, 2, 0, 0, 0, 0, 0, 0, 0, 0 };
            while (reader[0].RemainingBytes > 0)
            {
                var test = reader[0].ReadCompressedInteger();
                Assert.Equal(testVals[i], test);
                i++;
            }
        }

        [Fact]
        public void TestTypeDocumentInCDIFields()
        {
            string source = @"
using System;


class N
{

        public static void A()
    {
        System.Console.WriteLine();
    }
}

class O
{
    public static void B()
    {
        System.Console.WriteLine();
    }

}



";
            string sourceB = @"
using System;

class seven
{
    int x;
    const int z = 2;
    int y = 1;
}

";
            var pdbReader = GetMetadataReaderfromTwoSource(source, sourceB);
            Guid typeDocuments = new("932E74BC-DBA9-4478-8D46-0F32A7BAB3D3");
            BlobReader[] reader = new BlobReader[10];
            int i = 0;

            foreach (var handle in pdbReader.CustomDebugInformation)
            {
                var entry = pdbReader.GetCustomDebugInformation(handle);
                var kind = entry.Kind;
                if (typeDocuments.Equals(pdbReader.GetGuid(kind)))
                {
                    var temp = entry.Value;
                    reader[i] = pdbReader.GetBlobReader(temp);
                    i++;
                }

            }
            i = 0;
            int[] testVals = new[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
            foreach (var val in reader)
            {

                Assert.Equal(testVals[0], val.Length);
                i++;


            }
        }

        [Fact]
        public void TestTypeDocumentInCDIInterfaces()
        {
            string source = @"
using System;


class one
{
    int x = 5;
}

partial interface two
{

}

partial class three
{

}



class five
{

}

class six
{
    int x;
    const int z = 2;
    int y;

}



";
            string sourceB = @"
using System;

partial interface two
{
    public void F();
}

partial class three
{
    
}



";
            var pdbReader = GetMetadataReaderfromTwoSource(source, sourceB);
            Guid TypeDocuments = new("932E74BC-DBA9-4478-8D46-0F32A7BAB3D3");
            EntityHandle[] hand = new[] { MetadataTokens.EntityHandle(2) };
            // here - use pdbReader to read CDI for the type definitions
            var list = new System.Collections.Generic.List<int>();
            BlobReader reader = new BlobReader();
            int i = 0;

            foreach (var handle in pdbReader.CustomDebugInformation)
            {
                var entry = pdbReader.GetCustomDebugInformation(handle);
                var kind = entry.Kind;
                if (TypeDocuments.Equals(pdbReader.GetGuid(kind)))
                {
                    var value = entry.Value;
                    reader = pdbReader.GetBlobReader(value);
                    i++;
                }

            }
            i = 0;
            int[] testVals = new[] { 1, 1, 2, 1, 2, 1, 1, 0, 0, 0 };
            while (reader.RemainingBytes > 0)
            {

                Assert.Equal(testVals[i], reader.ReadCompressedInteger());
                i++;
            }
        }

        private bool AddDocumentsfromMethodDebugInformation(MethodDefinitionHandle methodHandle, MetadataReader pdbReader, List<DocumentHandle> docList)
        {

            var debugInfo = pdbReader.GetMethodDebugInformation(methodHandle);
            if (!debugInfo.Document.IsNil && docList.Count < 1)
            {
                // duplicates empty or not :: x
                docList.Add(debugInfo.Document);
                return true;
            }

            if (!debugInfo.SequencePointsBlob.IsNil)
            {

                // check duplicates :: x
                foreach (var point in debugInfo.GetSequencePoints())
                {
                    if (!point.Document.IsNil)
                    {
                        // Hash set instead of list for time. :: x
                        if (!docList.Contains(point.Document))
                        {
                            docList.Add(point.Document);
                        }
                    }

                }
                return true;
            }
            return false;
        }

        private bool AddTypeDocumentsInCustomDebugInformation(TypeDefinitionHandle typeHandle, MetadataReader pdbReader, List<DocumentHandle> docList)
        {
            foreach (var handle in pdbReader.GetCustomDebugInformation(typeHandle))
            {
                var typeId = pdbReader.GetCustomDebugInformation(handle).Parent;

                if (((TypeDefinitionHandle)typeId).Equals(typeHandle))
                {
                    var blob = pdbReader.GetCustomDebugInformation(handle).Value;
                    var reader = pdbReader.GetBlobReader(blob);
                    while (reader.RemainingBytes > 0)
                    {
                        docList.Add(MetadataTokens.DocumentHandle(reader.ReadCompressedInteger()));
                    }
                    return true;
                }
            }
            return false;
        }

        private void AddTypeDocuments(PENamedTypeSymbol typeSymbol, MetadataReader pdbReader, List<DocumentHandle> docList)
        {
            if (AddTypeDocumentsInCustomDebugInformation(typeSymbol.Handle, pdbReader, docList))
            {
                return;
            }
            foreach (var typeMethod in typeSymbol.GetMethodsToEmit())
            {
                // Test case where method does not have body, where can you find document?
                // duplicate 
                if (AddDocumentsfromMethodDebugInformation(((PEMethodSymbol)typeMethod).Handle, pdbReader, docList))
                {
                }
            }
        }

        private List<DocumentHandle> FindSourceDocuments(Symbol symbol, MetadataReader pdbReader)
        {
            var docList = new List<DocumentHandle>();
            switch (symbol.Kind)
            {
                case SymbolKind.Method:
                    var method = (PEMethodSymbol)symbol;
                    if (AddDocumentsfromMethodDebugInformation(method.Handle, pdbReader, docList))
                    {
                    }
                    else
                    {
                        var typeM = (PENamedTypeSymbol)(method.ContainingType);
                        AddTypeDocumentsInCustomDebugInformation(typeM.Handle, pdbReader, docList);
                    }
                    break;
                case SymbolKind.Field:
                    var field = (PEFieldSymbol)symbol;
                    var typeF = (PENamedTypeSymbol)field.ContainingType;
                    AddTypeDocuments(typeF, pdbReader, docList);
                    break;
                case SymbolKind.Property:
                    var propertyMethod = (PEMethodSymbol)((PEPropertySymbol)symbol).GetMethod;
                    if (propertyMethod.Equals(null))
                    {
                        propertyMethod = (PEMethodSymbol)((PEPropertySymbol)symbol).SetMethod;
                        if (propertyMethod.Equals(null))
                        {
                            // throw error
                        }
                    }
                    // check get method if null, use set vice versa. Also check for bad metadata. :: x
                    AddDocumentsfromMethodDebugInformation(propertyMethod.Handle, pdbReader, docList);
                    break;
                case SymbolKind.Event:
                    var eventMethod = (PEMethodSymbol)((PEEventSymbol)symbol).AddMethod;
                    if (AddDocumentsfromMethodDebugInformation(eventMethod.Handle, pdbReader, docList) && !eventMethod.Equals(null)) { }
                    else
                    {
                        eventMethod = (PEMethodSymbol)((PEEventSymbol)symbol).RemoveMethod;
                        if (AddDocumentsfromMethodDebugInformation(eventMethod.Handle, pdbReader, docList) && !eventMethod.Equals(null)) { }
                        else
                        {
                            var typeE = (PENamedTypeSymbol)(eventMethod.ContainingType);
                            AddTypeDocumentsInCustomDebugInformation(typeE.Handle, pdbReader, docList);
                        }
                    }
                    break;
                case SymbolKind.NamedType:
                    var typeT = (PENamedTypeSymbol)symbol;
                    AddTypeDocuments(typeT, pdbReader, docList);
                    break;
                case SymbolKind.Parameter:
                    var parameterSymbol = (PEParameterSymbol)symbol;
                    var parameterContainingSymbol = parameterSymbol.ContainingSymbol;
                    switch (parameterContainingSymbol.Kind)
                    {
                        case SymbolKind.Method:
                            var methodP = (PEMethodSymbol)parameterContainingSymbol;
                            if (!AddDocumentsfromMethodDebugInformation(methodP.Handle, pdbReader, docList))
                            {
                                var typeM = (PENamedTypeSymbol)(methodP.ContainingType);
                                AddTypeDocuments(typeM, pdbReader, docList);
                            }
                            break;
                        case SymbolKind.Property:
                            var property = (PEPropertySymbol)parameterContainingSymbol;
                            var propertyMethodP = (PEMethodSymbol)(property.GetMethod ?? property.SetMethod);
                            if (propertyMethodP != null)
                            {
                                AddDocumentsfromMethodDebugInformation(propertyMethodP.Handle, pdbReader, docList);
                            }
                            break;
                        default:
                            // error message

                            break;

                    }
                    // check kind of symbol, property or methods :: x
                    break;
                // Parameter :: x
                default:
                    // error message for incorrect type
                    break;
            }

            return docList;
        }
        [Fact]
        public void TestTypetoDocumentNavigation()
        {
            string sourceA = @"
using System;

public partial class C
{
    int x = 1;
}
";

            string sourceB = @"
using System;

public partial class C
{
    int y = 1;

    public void F2()
    {
#line 1 ""a.txt""
        int z = 1;
#line default
    } 
}
";
            var c1 = CreateCompilation(new[]
            {
                SyntaxFactory.ParseSyntaxTree(sourceA, path: "X.cs", encoding: Encoding.UTF8),
                SyntaxFactory.ParseSyntaxTree(sourceB, path: "Z.cs", encoding: Encoding.UTF8)
            }, options: TestOptions.DebugDll);

            var pdbStream = new MemoryStream();
            var peImage = c1.EmitToArray(EmitOptions.Default.WithDebugInformationFormat(DebugInformationFormat.PortablePdb), pdbStream: pdbStream);
            pdbStream.Position = 0;

            string source2 = @"
using System;

public class Program
{
    public static void Main() { }   
}
";
            var assemblyMetadata = AssemblyMetadata.CreateFromImage(peImage);
            var metadataReference = assemblyMetadata.GetReference();
            var c2 = CreateCompilation(new[] { source2 }, new[] { metadataReference }, options: TestOptions.DebugDll);

            var typeC = c2.GetTypeByMetadataName("C");
            Symbol symbol = typeC.GetMethod("F2");
            //symbol = (PEMethodSymbol)symbol;
            using var provider = MetadataReaderProvider.FromPortablePdbStream(pdbStream);
            var pdbReader = provider.GetMetadataReader();
            var docList = FindSourceDocuments(symbol, pdbReader);
            Assert.Equal(0x30000002, MetadataTokens.GetToken(docList[0]));

            // go through all methods and collect documents.

        }

        [Fact]
        public void TestTypetoDocumentNavigation2()
        {
            string sourceA = @"
using System;

public class D
{
}
";

            string sourceB = @"
using System;

public partial class C
{
    public void F2() { }
       
}
";

            var c1 = CreateCompilation(new[]
            {
                SyntaxFactory.ParseSyntaxTree(sourceA, path: "X.cs", encoding: Encoding.UTF8),
                SyntaxFactory.ParseSyntaxTree(sourceB, path: "Z.cs", encoding: Encoding.UTF8)
            }, options: TestOptions.DebugDll);

            var pdbStream = new MemoryStream();
            var peImage = c1.EmitToArray(EmitOptions.Default.WithDebugInformationFormat(DebugInformationFormat.PortablePdb), pdbStream: pdbStream);
            pdbStream.Position = 0;


            string source2 = @"
using System;

public class Program
{
    public static void Main() { }   
}
";
            var assemblyMetadata = AssemblyMetadata.CreateFromImage(peImage);
            var metadataReference = assemblyMetadata.GetReference();
            var c2 = CreateCompilation(new[] { source2 }, new[] { metadataReference }, options: TestOptions.DebugDll);

            var typeC = c2.GetTypeByMetadataName("C");
            Symbol symbol = typeC.GetMethod("F2");
            //symbol = (PEMethodSymbol)symbol;
            using var provider = MetadataReaderProvider.FromPortablePdbStream(pdbStream);
            var pdbReader = provider.GetMetadataReader();
            var docList = FindSourceDocuments(symbol, pdbReader);
            Assert.Equal(0x30000002, MetadataTokens.GetToken(docList[0]));

            // go through all methods and collect documents.

        }

        [Fact]
        public void TestTypetoDocumentNavigationMethod()
        {
            string sourceA = @"
using System;

public class C
{
    public void F2() { }
}
";

            string sourceB = @"
using System;

public class D
{
   
       
}
";

            var c1 = CreateCompilation(new[]
            {
                SyntaxFactory.ParseSyntaxTree(sourceA, path: "X.cs", encoding: Encoding.UTF8),
                SyntaxFactory.ParseSyntaxTree(sourceB, path: "Z.cs", encoding: Encoding.UTF8)
            }, options: TestOptions.DebugDll);

            var pdbStream = new MemoryStream();
            var peImage = c1.EmitToArray(EmitOptions.Default.WithDebugInformationFormat(DebugInformationFormat.PortablePdb), pdbStream: pdbStream);
            pdbStream.Position = 0;


            string source2 = @"
using System;

public class Program
{
    public static void Main() { }   
}
";
            var assemblyMetadata = AssemblyMetadata.CreateFromImage(peImage);
            var metadataReference = assemblyMetadata.GetReference();
            var c2 = CreateCompilation(new[] { source2 }, new[] { metadataReference }, options: TestOptions.DebugDll);

            var typeC = c2.GetTypeByMetadataName("C");
            Symbol symbol = typeC.GetMethod("F2");
            //symbol = (PEMethodSymbol)symbol;
            using var provider = MetadataReaderProvider.FromPortablePdbStream(pdbStream);
            var pdbReader = provider.GetMetadataReader();
            var docList = FindSourceDocuments(symbol, pdbReader);
            Assert.Equal(0x30000001, MetadataTokens.GetToken(docList[0]));
        }


        [Fact]
        public void TestTypetoDocumentNavigationField()
        {
            string sourceA = @"
using System;

public class C
{
    public int g;
    private int x = 5;
    public string str = ""sam"";
    public void F2() { }
    
}
";

            string sourceB = @"
using System;

public class D
{

    public void R2() { }
}
";

            var c1 = CreateCompilation(new[]
            {
                SyntaxFactory.ParseSyntaxTree(sourceA, path: "X.cs", encoding: Encoding.UTF8),
                SyntaxFactory.ParseSyntaxTree(sourceB, path: "Z.cs", encoding: Encoding.UTF8)
            }, options: TestOptions.DebugDll);

            var pdbStream = new MemoryStream();
            var peImage = c1.EmitToArray(EmitOptions.Default.WithDebugInformationFormat(DebugInformationFormat.PortablePdb), pdbStream: pdbStream);
            pdbStream.Position = 0;


            string source2 = @"
using System;

public class Program
{
    public static void Main() { }   
}
";
            var assemblyMetadata = AssemblyMetadata.CreateFromImage(peImage);
            var metadataReference = assemblyMetadata.GetReference();
            var c2 = CreateCompilation(new[] { source2 }, new[] { metadataReference }, options: TestOptions.DebugDll);

            var typeC = c2.GetTypeByMetadataName("C");
            Symbol symbol = typeC.GetField("str");
            using var provider = MetadataReaderProvider.FromPortablePdbStream(pdbStream);
            var pdbReader = provider.GetMetadataReader();
            var docList = FindSourceDocuments(symbol, pdbReader);
            Assert.Equal(0x30000001, MetadataTokens.GetToken(docList[0]));
        }

        [Fact]
        public void TestTypetoDocumentNavigationProperty()
        {
            string sourceA = @"
using System;

public class C
{
    public string str { get; set; }
    public void F2() { }
    
}
";

            string sourceB = @"
using System;

public class D
{

    public void R2() { }
}
";

            var c1 = CreateCompilation(new[]
            {
                SyntaxFactory.ParseSyntaxTree(sourceA, path: "X.cs", encoding: Encoding.UTF8),
                SyntaxFactory.ParseSyntaxTree(sourceB, path: "Z.cs", encoding: Encoding.UTF8)
            }, options: TestOptions.DebugDll);

            var pdbStream = new MemoryStream();
            var peImage = c1.EmitToArray(EmitOptions.Default.WithDebugInformationFormat(DebugInformationFormat.PortablePdb), pdbStream: pdbStream);
            pdbStream.Position = 0;


            string source2 = @"
using System;

public class Program
{
    public static void Main() { }   
}
";
            var assemblyMetadata = AssemblyMetadata.CreateFromImage(peImage);
            var metadataReference = assemblyMetadata.GetReference();
            var c2 = CreateCompilation(new[] { source2 }, new[] { metadataReference }, options: TestOptions.DebugDll);

            var typeC = c2.GetTypeByMetadataName("C");
            Symbol symbol = typeC.GetProperty("str");
            using var provider = MetadataReaderProvider.FromPortablePdbStream(pdbStream);
            var pdbReader = provider.GetMetadataReader();
            var docList = FindSourceDocuments(symbol, pdbReader);
            Assert.Equal(0x30000001, MetadataTokens.GetToken(docList[0]));
        }

        [Fact]
        public void TestTypetoDocumentNavigationEvent()
        {
            string sourceA = @"
using System;

public class C
{
    public event EventHandler ev { add{} remove{} } 
    public void F2() { }
    
}
";

            string sourceB = @"
using System;

public class D
{

    public void R2() { }
}
";

            var c1 = CreateCompilation(new[]
            {
                SyntaxFactory.ParseSyntaxTree(sourceA, path: "X.cs", encoding: Encoding.UTF8),
                SyntaxFactory.ParseSyntaxTree(sourceB, path: "Z.cs", encoding: Encoding.UTF8)
            }, options: TestOptions.DebugDll);

            var pdbStream = new MemoryStream();
            var peImage = c1.EmitToArray(EmitOptions.Default.WithDebugInformationFormat(DebugInformationFormat.PortablePdb), pdbStream: pdbStream);
            pdbStream.Position = 0;


            string source2 = @"
using System;

public class Program
{
    public static void Main() { }   
}
";
            var assemblyMetadata = AssemblyMetadata.CreateFromImage(peImage);
            var metadataReference = assemblyMetadata.GetReference();
            var c2 = CreateCompilation(new[] { source2 }, new[] { metadataReference }, options: TestOptions.DebugDll);

            var typeC = c2.GetTypeByMetadataName("C");
            Symbol symbol = typeC.GetEvent("ev");
            using var provider = MetadataReaderProvider.FromPortablePdbStream(pdbStream);
            var pdbReader = provider.GetMetadataReader();
            var docList = FindSourceDocuments(symbol, pdbReader);
            Assert.Equal(0x30000001, MetadataTokens.GetToken(docList[0]));
        }
        // two more test cases one for default and implicilty events
        [Fact]
        public void TestTypetoDocumentNavigationType()
        {
            string sourceA = @"
using System;

public class C
{
    public int x = 5;
    public event EventHandler ev; 
    public string str { get; set; }
    public void R2() { }
    

}
";

            string sourceB = @"
using System;

public class D
{

    public void R2() { }
}
";

            var c1 = CreateCompilation(new[]
            {
                SyntaxFactory.ParseSyntaxTree(sourceA, path: "X.cs", encoding: Encoding.UTF8),
                SyntaxFactory.ParseSyntaxTree(sourceB, path: "Z.cs", encoding: Encoding.UTF8)
            }, options: TestOptions.DebugDll);

            var pdbStream = new MemoryStream();
            var peImage = c1.EmitToArray(EmitOptions.Default.WithDebugInformationFormat(DebugInformationFormat.PortablePdb), pdbStream: pdbStream);
            pdbStream.Position = 0;


            string source2 = @"
using System;

public class Program
{
    public static void Main() { }   
}
";
            var assemblyMetadata = AssemblyMetadata.CreateFromImage(peImage);
            var metadataReference = assemblyMetadata.GetReference();
            var c2 = CreateCompilation(new[] { source2 }, new[] { metadataReference }, options: TestOptions.DebugDll);

            Symbol symbol = c2.GetTypeByMetadataName("C");
            using var provider = MetadataReaderProvider.FromPortablePdbStream(pdbStream);
            var pdbReader = provider.GetMetadataReader();
            var docList = FindSourceDocuments(symbol, pdbReader);
            Assert.Equal(0x30000001, MetadataTokens.GetToken(docList[0]));
        }

        [Fact]
        public void TestTypetoDocumentNavigationMultipleDocuments()
        {
            string sourceA = @"
using System;

public partial class C
{
    public void F2() { }
}
";

            string sourceB = @"
using System;

public partial class C
{
   
    public void F3() { }   
}
";

            var c1 = CreateCompilation(new[]
            {
                SyntaxFactory.ParseSyntaxTree(sourceA, path: "X.cs", encoding: Encoding.UTF8),
                SyntaxFactory.ParseSyntaxTree(sourceB, path: "Z.cs", encoding: Encoding.UTF8)
            }, options: TestOptions.DebugDll);

            var pdbStream = new MemoryStream();
            var peImage = c1.EmitToArray(EmitOptions.Default.WithDebugInformationFormat(DebugInformationFormat.PortablePdb), pdbStream: pdbStream);
            pdbStream.Position = 0;


            string source2 = @"
using System;

public partial class D
{
    public static void Main() { }   
}
";
            var assemblyMetadata = AssemblyMetadata.CreateFromImage(peImage);
            var metadataReference = assemblyMetadata.GetReference();
            var c2 = CreateCompilation(new[] { source2 }, new[] { metadataReference }, options: TestOptions.DebugDll);

            Symbol symbol = c2.GetTypeByMetadataName("C");
            using var provider = MetadataReaderProvider.FromPortablePdbStream(pdbStream);
            var pdbReader = provider.GetMetadataReader();
            var docList = FindSourceDocuments(symbol, pdbReader);
            Assert.Equal(0x30000001, MetadataTokens.GetToken(docList[0]));
            Assert.Equal(0x30000002, MetadataTokens.GetToken(docList[1]));
        }
        [Fact]
        public void TestTypetoDocumentNavigationMethodWithNoBody()
        {
            string sourceA = @"
using System;

public partial  class C
{
#line hidden
    public void F1() {}  
#line default

    public void F2() {}
    public void F3() {}  

}
";

            string sourceB = @"
using System;

public partial class C
{
   
    
}
";

            var c1 = CreateCompilation(new[]
            {
                SyntaxFactory.ParseSyntaxTree(sourceA, path: "X.cs", encoding: Encoding.UTF8),
                SyntaxFactory.ParseSyntaxTree(sourceB, path: "Z.cs", encoding: Encoding.UTF8)
            }, options: TestOptions.DebugDll);

            var pdbStream = new MemoryStream();
            var peImage = c1.EmitToArray(EmitOptions.Default.WithDebugInformationFormat(DebugInformationFormat.PortablePdb), pdbStream: pdbStream);
            pdbStream.Position = 0;


            string source2 = @"
using System;

public class Program
{
    public static void Main() { }   
}
";
            var assemblyMetadata = AssemblyMetadata.CreateFromImage(peImage);
            var metadataReference = assemblyMetadata.GetReference();
            var c2 = CreateCompilation(new[] { source2 }, new[] { metadataReference }, options: TestOptions.DebugDll);
            var typeC = c2.GetTypeByMetadataName("C");
            Symbol symbol = typeC.GetMethod("F1");
            using var provider = MetadataReaderProvider.FromPortablePdbStream(pdbStream);
            var pdbReader = provider.GetMetadataReader();
            var docList = FindSourceDocuments(symbol, pdbReader);
            Assert.Equal(0x30000001, MetadataTokens.GetToken(docList[0]));
            Assert.Equal(0x30000002, MetadataTokens.GetToken(docList[1]));
        }




    }
}


