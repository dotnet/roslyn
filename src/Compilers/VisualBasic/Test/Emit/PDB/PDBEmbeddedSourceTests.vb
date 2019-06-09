' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.IO
Imports System.Reflection.Metadata
Imports System.Reflection.PortableExecutable
Imports Microsoft.CodeAnalysis.Emit
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Roslyn.Test.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests.PDB
    Public Class PDBEmbeddedSourceTests
        Inherits BasicTestBase

        <ConditionalFact(GetType(WindowsDesktopOnly), Reason:="https://github.com/dotnet/roslyn/issues/28045")>
        Public Sub StandalonePdb()
            Dim source1 = "
Imports System

Class C
    Public Shared Sub Main()
        Console.WriteLine()
    End Sub
End Class
"
            Dim source2 = "
' no code
"

            Dim tree1 = Parse(source1, "f:/build/goo.vb")
            Dim tree2 = Parse(source2, "f:/build/nocode.vb")
            Dim c = CreateCompilationWithMscorlib40({tree1, tree2}, options:=TestOptions.DebugDll)
            Dim embeddedTexts = {
                                   EmbeddedText.FromSource(tree1.FilePath, tree1.GetText()),
                                   EmbeddedText.FromSource(tree2.FilePath, tree2.GetText())
                                }

            c.VerifyPdb(
<symbols>
    <files>
        <file id="1" name="f:/build/goo.vb" language="VB" checksumAlgorithm="SHA1" checksum="03-28-AD-AE-03-81-AD-8B-6E-C4-60-7B-13-4E-9C-4F-8E-D6-D5-65" embeddedSourceLength="99"><![CDATA[﻿
Imports System
Class C
    Public Shared Sub Main()
        Console.WriteLine()
    End Sub
End Class
]]></file>
        <file id="2" name="f:/build/nocode.vb" language="VB" checksumAlgorithm="SHA1" checksum="40-43-2C-44-BA-1C-C7-1A-B3-F3-68-E5-96-7C-65-9D-61-85-D5-44" embeddedSourceLength="20"><![CDATA[﻿
' no code
]]></file>
    </files>
    <methods>
        <method containingType="C" name="Main">
            <sequencePoints>
                <entry offset="0x0" startLine="5" startColumn="5" endLine="5" endColumn="29" document="1"/>
                <entry offset="0x1" startLine="6" startColumn="9" endLine="6" endColumn="28" document="1"/>
                <entry offset="0x7" startLine="7" startColumn="5" endLine="7" endColumn="12" document="1"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x8">
                <namespace name="System" importlevel="file"/>
                <currentnamespace name=""/>
            </scope>
        </method>
    </methods>
</symbols>,
            embeddedTexts)
        End Sub

        <Fact>
        Public Sub EmbeddedPdb()
            Const source = "
Imports System

Class C
    Public Shared Sub Main()
        Console.WriteLine()
    End Sub
End Class
"
            Dim tree = Parse(source, "f:/build/goo.cs")
            Dim c = CreateCompilationWithMscorlib40(tree, options:=TestOptions.DebugDll)

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

                    Assert.Equal(embeddedSource.FilePath, "f:/build/goo.cs")
                    Assert.Equal(source, embeddedSource.Text.ToString())
                End Using
            End Using
        End Sub
    End Class
End Namespace
