' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Roslyn.Test.Utilities
Imports System.IO
Imports System.Reflection.Metadata
Imports Microsoft.CodeAnalysis.Emit
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports System.Reflection.PortableExecutable
Imports System.Text
Imports Microsoft.CodeAnalysis.Debugging

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests.PDB
    Public Class PortablePdbTests
        Inherits BasicTestBase

        <Fact()>
        Public Sub DateTimeConstant()
            Dim source = <compilation>
                             <file>
Imports System

Public Class C
    Public Sub M()
        const dt1 as datetime = #3/01/2016#
        const dt2 as datetime = #10:53:37 AM#
        const dt3 as datetime = #3/01/2016 10:53:37 AM#
    End Sub
End Class
</file>
                         </compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(
                source,
                TestOptions.DebugDll)

            Dim pdbStream = New MemoryStream()
            compilation.EmitToArray(EmitOptions.Default.WithDebugInformationFormat(DebugInformationFormat.PortablePdb), pdbStream:=pdbStream)

            Using pdbMetadata As New PinnedMetadata(pdbStream.ToImmutable())
                Dim mdReader = pdbMetadata.Reader

                Assert.Equal(3, mdReader.LocalConstants.Count)

                For Each constantHandle In mdReader.LocalConstants
                    Dim constant = mdReader.GetLocalConstant(constantHandle)
                    Dim sigReader = mdReader.GetBlobReader(constant.Signature)

                    ' DateTime constants are always SignatureTypeCode.ValueType {17}
                    Dim rawTypeCode = sigReader.ReadCompressedInteger()
                    Assert.Equal(17, rawTypeCode)

                    ' DateTime constants are always HandleKind.TypeReference {1}
                    Dim typeHandle = sigReader.ReadTypeHandle()
                    Assert.Equal(HandleKind.TypeReference, typeHandle.Kind)

                    ' DateTime constants are always stored and retrieved with no time zone specification
                    Dim value = sigReader.ReadDateTime()
                    Assert.Equal(DateTimeKind.Unspecified, value.Kind)
                Next
            End Using
        End Sub

        <Fact>
        Public Sub EmbeddedPortablePdb()
            Dim source = "
Imports System

Class C
    Public Shared Sub Main()
        Console.WriteLine()
    End Sub
End Class
"
            Dim c = CreateCompilationWithMscorlib(Parse(source, "foo.vb"), options:=TestOptions.DebugDll)
            Dim peBlob = c.EmitToArray(EmitOptions.Default.WithDebugInformationFormat(DebugInformationFormat.Embedded).WithPdbFilePath("a/b/c/d.pdb"))

            Using peReader = New PEReader(peBlob)
                Dim entries = peReader.ReadDebugDirectory()

                AssertEx.Equal({DebugDirectoryEntryType.CodeView, DebugDirectoryEntryType.EmbeddedPortablePdb}, entries.Select(Function(e) e.Type))

                Dim codeView = entries(0)
                Dim embedded = entries(1)

                ' EmbeddedPortablePdb entry
                Assert.Equal(&H100, embedded.MajorVersion)
                Assert.Equal(&H100, embedded.MinorVersion)
                Assert.Equal(CUInt(0), embedded.Stamp)

                Dim pdbId As BlobContentId
                Using embeddedMetadataProvider = peReader.ReadEmbeddedPortablePdbDebugDirectoryData(embedded)
                    Dim mdReader = embeddedMetadataProvider.GetMetadataReader()
                    AssertEx.Equal({"foo.vb"}, mdReader.Documents.Select(Function(doc) mdReader.GetString(mdReader.GetDocument(doc).Name)))
                    pdbId = New BlobContentId(mdReader.DebugMetadataHeader.Id)
                End Using

                ' CodeView entry:
                Dim codeViewData = peReader.ReadCodeViewDebugDirectoryData(codeView)
                Assert.Equal(&H100, codeView.MajorVersion)
                Assert.Equal(&H504D, codeView.MinorVersion)
                Assert.Equal(pdbId.Stamp, codeView.Stamp)
                Assert.Equal(pdbId.Guid, codeViewData.Guid)
                Assert.Equal("d.pdb", codeViewData.Path)
            End Using
        End Sub

        <Fact>
        Public Sub EmbeddedPortablePdb_Deterministic()
            Dim source = "
Imports System

Class C
    Public Shared Sub Main()
        Console.WriteLine()
    End Sub
End Class
"
            Dim c = CreateCompilationWithMscorlib(Parse(source, "foo.vb"), options:=TestOptions.DebugDll.WithDeterministic(True))
            Dim peBlob = c.EmitToArray(EmitOptions.Default.WithDebugInformationFormat(DebugInformationFormat.Embedded).WithPdbFilePath("a/b/c/d.pdb"))

            Using peReader = New PEReader(peBlob)
                Dim entries = peReader.ReadDebugDirectory()

                AssertEx.Equal({DebugDirectoryEntryType.CodeView, DebugDirectoryEntryType.Reproducible, DebugDirectoryEntryType.EmbeddedPortablePdb}, entries.Select(Function(e) e.Type))

                Dim codeView = entries(0)
                Dim reproducible = entries(1)
                Dim embedded = entries(2)

                ' EmbeddedPortablePdb entry
                Assert.Equal(&H100, embedded.MajorVersion)
                Assert.Equal(&H100, embedded.MinorVersion)
                Assert.Equal(CUInt(0), embedded.Stamp)

                Dim pdbId As BlobContentId
                Using embeddedMetadataProvider = peReader.ReadEmbeddedPortablePdbDebugDirectoryData(embedded)
                    Dim mdReader = embeddedMetadataProvider.GetMetadataReader()
                    AssertEx.Equal({"foo.vb"}, mdReader.Documents.Select(Function(doc) mdReader.GetString(mdReader.GetDocument(doc).Name)))
                    pdbId = New BlobContentId(mdReader.DebugMetadataHeader.Id)
                End Using

                ' CodeView entry:
                Dim codeViewData = peReader.ReadCodeViewDebugDirectoryData(codeView)
                Assert.Equal(&H100, codeView.MajorVersion)
                Assert.Equal(&H504D, codeView.MinorVersion)
                Assert.Equal(pdbId.Stamp, codeView.Stamp)
                Assert.Equal(pdbId.Guid, codeViewData.Guid)
                Assert.Equal("d.pdb", codeViewData.Path)

                ' Reproducible entry
                Assert.Equal(0, reproducible.MajorVersion)
                Assert.Equal(0, reproducible.MinorVersion)
                Assert.Equal(CUInt(0), reproducible.Stamp)
                Assert.Equal(0, reproducible.DataSize)
            End Using
        End Sub

        <Fact>
        Public Sub SourceLink()
            Dim source = "
Imports System

Class C
    Public Shared Sub Main()
        Console.WriteLine()
    End Sub
End Class
"
            Dim sourceLinkBlob = Encoding.UTF8.GetBytes("
{
  ""documents"": {
     ""f:/build/*"" : ""https://raw.githubusercontent.com/my-org/my-project/1111111111111111111111111111111111111111/*"";
  }
}
")
            Dim c = CreateCompilationWithMscorlib(Parse(source, "f:/build/foo.vb"), options:=TestOptions.DebugDll)

            Dim pdbStream = New MemoryStream()
            c.EmitToArray(EmitOptions.Default.WithDebugInformationFormat(DebugInformationFormat.PortablePdb), pdbStream:=pdbStream, sourceLinkStream:=New MemoryStream(sourceLinkBlob))
            pdbStream.Position = 0

            Using provider = MetadataReaderProvider.FromPortablePdbStream(pdbStream)
                Dim pdbReader = provider.GetMetadataReader()
                Dim actualBlob =
                    (From cdiHandle In pdbReader.GetCustomDebugInformation(EntityHandle.ModuleDefinition)
                     Let cdi = pdbReader.GetCustomDebugInformation(cdiHandle)
                     Where pdbReader.GetGuid(cdi.Kind) = PortableCustomDebugInfoKinds.SourceLink
                     Select pdbReader.GetBlobBytes(cdi.Value)).Single()

                AssertEx.Equal(sourceLinkBlob, actualBlob)
            End Using
        End Sub

        <Fact>
        Public Sub SourceLink_Embedded()
            Dim source = "
Imports System

Class C
    Public Shared Sub Main()
        Console.WriteLine()
    End Sub
End Class
"
            Dim sourceLinkBlob = Encoding.UTF8.GetBytes("
{
  ""documents"": {
     ""f:/build/*"" : ""https://raw.githubusercontent.com/my-org/my-project/1111111111111111111111111111111111111111/*"";
  }
}
")
            Dim c = CreateCompilationWithMscorlib(Parse(source, "f:/build/foo.vb"), options:=TestOptions.DebugDll)
            Dim peBlob = c.EmitToArray(EmitOptions.Default.WithDebugInformationFormat(DebugInformationFormat.Embedded), sourceLinkStream:=New MemoryStream(sourceLinkBlob))

            Using peReader = New PEReader(peBlob)
                Dim embeddedEntry = peReader.ReadDebugDirectory().Single(Function(e) e.Type = DebugDirectoryEntryType.EmbeddedPortablePdb)

                Using embeddedMetadataProvider = peReader.ReadEmbeddedPortablePdbDebugDirectoryData(embeddedEntry)
                    Dim pdbReader = embeddedMetadataProvider.GetMetadataReader()

                    Dim actualBlob =
                        (From cdiHandle In pdbReader.GetCustomDebugInformation(EntityHandle.ModuleDefinition)
                         Let cdi = pdbReader.GetCustomDebugInformation(cdiHandle)
                         Where pdbReader.GetGuid(cdi.Kind) = PortableCustomDebugInfoKinds.SourceLink
                         Select pdbReader.GetBlobBytes(cdi.Value)).Single()

                    AssertEx.Equal(sourceLinkBlob, actualBlob)
                End Using
            End Using
        End Sub

        <Fact>
        Public Sub SourceLink_Errors()
            Dim source = "
Imports System

Class C
    Public Shared Sub Main()
        Console.WriteLine()
    End Sub
End Class
"
            Dim sourceLinkStream = New TestStream(canRead:=True, readFunc:=Function(a1, a2, a3)
                                                                               Throw New Exception("Error!")
                                                                           End Function)

            Dim c = CreateCompilationWithMscorlib(Parse(source, "f:/build/foo.vb"), options:=TestOptions.DebugDll)
            Dim result = c.Emit(New MemoryStream(), New MemoryStream(), options:=EmitOptions.Default.WithDebugInformationFormat(DebugInformationFormat.PortablePdb), sourceLinkStream:=sourceLinkStream)

            result.Diagnostics.Verify(
                Diagnostic(ERRID.ERR_PDBWritingFailed).WithArguments("Error!").WithLocation(1, 1))
        End Sub

        <Fact>
        Public Sub EmbeddedSource()
            Const source = "
Imports System

Class C
    Public Shared Sub Main()
        Console.WriteLine()
    End Sub
End Class
"
            Dim tree = Parse(source, "f:/build/foo.cs")
            Dim c = CreateCompilationWithMscorlib(tree, options:=TestOptions.DebugDll)

            Dim pdbStream = New MemoryStream()
            c.EmitToArray(
                EmitOptions.Default.WithDebugInformationFormat(DebugInformationFormat.PortablePdb),
                pdbStream:=pdbStream,
                embeddedTexts:={EmbeddedText.FromSource(tree.FilePath, tree.GetText())})
            pdbStream.Position = 0

            Using provider As MetadataReaderProvider = MetadataReaderProvider.FromPortablePdbStream(pdbStream)
                Dim pdbReader = provider.GetMetadataReader()

                Dim embeddedSource =
                    (From documentHandle In pdbReader.Documents
                     Let document = pdbReader.GetDocument(documentHandle)
                     Select New With
                     {
                         .FilePath = pdbReader.GetString(document.Name),
                         .Text = pdbReader.GetEmbeddedSource(documentHandle)
                     }).Single()

                Assert.Equal(embeddedSource.FilePath, "f:/build/foo.cs")
                Assert.Equal(source, embeddedSource.Text.ToString())
            End Using
        End Sub

        <Fact>
        Public Sub EmbeddedSource_InEmbeddedPdb()
            Const source = "
Imports System

Class C
    Public Shared Sub Main()
        Console.WriteLine()
    End Sub
End Class
"
            Dim tree = Parse(source, "f:/build/foo.cs")
            Dim c = CreateCompilationWithMscorlib(tree, options:=TestOptions.DebugDll)

            Dim pdbStream = New MemoryStream()
            Dim peBlob = c.EmitToArray(
                EmitOptions.Default.WithDebugInformationFormat(DebugInformationFormat.Embedded),
                embeddedTexts:={EmbeddedText.FromSource(tree.FilePath, tree.GetText())})
            pdbStream.Position = 0

            Using peReader As New PEReader(peBlob)
                Dim embeddedEntry = peReader.ReadDebugDirectory().Single(Function(e) e.Type = DebugDirectoryEntryType.EmbeddedPortablePdb)

                Using embeddedMetadataProvider As MetadataReaderProvider = peReader.ReadEmbeddedPortablePdbDebugDirectoryData(embeddedEntry)
                    Dim pdbReader = embeddedMetadataProvider.GetMetadataReader()

                    Dim embeddedSource =
                        (From documentHandle In pdbReader.Documents
                         Let document = pdbReader.GetDocument(documentHandle)
                         Select New With
                         {
                             .FilePath = pdbReader.GetString(document.Name),
                             .Text = pdbReader.GetEmbeddedSource(documentHandle)
                         }).Single()

                    Assert.Equal(embeddedSource.FilePath, "f:/build/foo.cs")
                    Assert.Equal(source, embeddedSource.Text.ToString())
                End Using
            End Using
        End Sub
    End Class
End Namespace
