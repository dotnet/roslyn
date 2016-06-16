' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Text
Imports Roslyn.Test.Utilities
Imports System.Collections.Immutable
Imports System.IO
Imports System.Xml.Linq
Imports Microsoft.CodeAnalysis.Test.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests.PDB

    Public Class ChecksumTests
        Inherits BasicTestBase

        Private Shared Function CreateCompilationWithChecksums(source As XCData, filePath As String, baseDirectory As String) As VisualBasicCompilation

            Dim tree As SyntaxTree
            Using stream = New MemoryStream
                Using writer = New StreamWriter(stream)
                    writer.Write(source.Value)
                    writer.Flush()
                    stream.Position = 0
                    Dim text = EncodedStringText.Create(stream, defaultEncoding:=Nothing)
                    tree = VisualBasicSyntaxTree.ParseText(text, path:=filePath)
                End Using
            End Using

            Dim resolver As New SourceFileResolver(ImmutableArray(Of String).Empty, baseDirectory)
            Return VisualBasicCompilation.Create(GetUniqueName(), {tree}, {MscorlibRef}, TestOptions.DebugDll.WithSourceReferenceResolver(resolver))
        End Function


        <Fact>
        Public Sub CheckSumDirectiveClashesSameTree()
            Dim other As VisualBasicCompilation = CreateCompilationWithMscorlib(
    <compilation>
        <file name="a.vb"><![CDATA[
Public Class C
 Friend Sub Foo()
 End Sub
End Class

#ExternalChecksum("bogus.vb", "{406EA660-64CF-4C82-B6F0-42D48172A799}", "ab007f1d23d9")

' same
#ExternalChecksum("bogus.vb", "{406EA660-64CF-4C82-B6F0-42D48172A799}", "ab007f1d23d9")

' different case in Hex numerics, but otherwise same
#ExternalChecksum("bogus.vb", "{406ea660-64cf-4C82-B6F0-42D48172A799}", "AB007f1d23d9")

' different case in path, but is a clash since VB compares paths in a case insensitive way
#ExternalChecksum("bogUs.vb", "{406EA660-64CF-4C82-B6F0-42D48172A788}", "ab007f1d23d9")

' whitespace in path, so not a clash
#ExternalChecksum("bogUs.cs ", "{406EA660-64CF-4C82-B6F0-42D48172A788}", "ab007f1d23d9")

#ExternalChecksum("bogus1.cs", "{406EA660-64CF-4C82-B6F0-42D48172A799}", "ab007f1d23d9")

' and now a clash in Guid
#ExternalChecksum("bogus1.cs", "{406EA660-64CF-4C82-B6F0-42D48172A798}", "ab007f1d23d9")

' and now a clash in CheckSum
#ExternalChecksum("bogus1.cs", "{406EA660-64CF-4C82-B6F0-42D48172A799}", "ab007f1d23d8")

]]>
        </file>
    </compilation>, options:=TestOptions.DebugDll)

            CompileAndVerify(other).VerifyDiagnostics(
                    Diagnostic(ERRID.WRN_MultipleDeclFileExtChecksum, "#ExternalChecksum(""bogUs.vb"", ""{406EA660-64CF-4C82-B6F0-42D48172A788}"", ""ab007f1d23d9"")").WithArguments("bogUs.vb"),
                    Diagnostic(ERRID.WRN_MultipleDeclFileExtChecksum, "#ExternalChecksum(""bogus1.cs"", ""{406EA660-64CF-4C82-B6F0-42D48172A798}"", ""ab007f1d23d9"")").WithArguments("bogus1.cs"),
                    Diagnostic(ERRID.WRN_MultipleDeclFileExtChecksum, "#ExternalChecksum(""bogus1.cs"", ""{406EA660-64CF-4C82-B6F0-42D48172A799}"", ""ab007f1d23d8"")").WithArguments("bogus1.cs")
            )

        End Sub

        <Fact>
        Public Sub CheckSumDirectiveClashesDifferentLength()
            Dim other As VisualBasicCompilation = CreateCompilationWithMscorlib(
    <compilation>
        <file name="a.vb"><![CDATA[
Public Class C
 Friend Sub Foo()
 End Sub
End Class

#ExternalChecksum("bogus1.vb", "{406EA660-64CF-4C82-B6F0-42D48172A799}", "ab007f1d23d9")
#ExternalChecksum("bogus1.vb", "{406EA660-64CF-4C82-B6F0-42D48172A799}", "ab007f1d23")
#ExternalChecksum("bogus1.vb", "{406EA660-64CF-4C82-B6F0-42D48172A799}", "")
#ExternalChecksum("bogus1.vb", "{406EA660-64CF-4C82-B6F0-42D48172A799}", "ab007f1d23d9")

' odd length, parse warning, ignored by emit
#ExternalChecksum("bogus1.vb", "{406EA660-64CF-4C82-B6F0-42D48172A799}", "ab007f1d23d")

' bad Guid, parse warning, ignored by emit
#ExternalChecksum("bogus1.vb", "{406EA660-64CF-4C82-B6F0-42D48172A79}", "ab007f1d23d9")


]]>
        </file>
    </compilation>, options:=TestOptions.DebugDll)


            CompileAndVerify(other).VerifyDiagnostics(
                Diagnostic(ERRID.WRN_BadChecksumValExtChecksum, """ab007f1d23d"""),
                Diagnostic(ERRID.WRN_BadGUIDFormatExtChecksum, """{406EA660-64CF-4C82-B6F0-42D48172A79}"""),
                Diagnostic(ERRID.WRN_MultipleDeclFileExtChecksum, "#ExternalChecksum(""bogus1.vb"", ""{406EA660-64CF-4C82-B6F0-42D48172A799}"", ""ab007f1d23"")").WithArguments("bogus1.vb"),
                Diagnostic(ERRID.WRN_MultipleDeclFileExtChecksum, "#ExternalChecksum(""bogus1.vb"", ""{406EA660-64CF-4C82-B6F0-42D48172A799}"", """")").WithArguments("bogus1.vb")
            )

        End Sub

        <Fact>
        Public Sub CheckSumDirectiveClashesDifferentTrees()
            Dim other As VisualBasicCompilation = CreateCompilationWithMscorlib(
    <compilation>
        <file name="a.vb"><![CDATA[
Public Class C
 Friend Sub Foo()
 End Sub
End Class

' same
#ExternalChecksum("bogus1.vb", "{406EA660-64CF-4C82-B6F0-42D48172A799}", "ab007f1d23d9")

#ExternalChecksum("bogus1.vb", "{406EA660-64CF-4C82-B6F0-42D48172A799}", "ab007f1d23d9")

]]>
        </file>
        <file name="a.vb"><![CDATA[
Public Class D
 Friend Sub Foo()
 End Sub
End Class

#ExternalChecksum("bogus1.vb", "{406EA660-64CF-4C82-B6F0-42D48172A799}", "ab007f1d23d9")

' same
#ExternalChecksum("bogus1.vb", "{406EA660-64CF-4C82-B6F0-42D48172A799}", "ab007f1d23d9")

' different
#ExternalChecksum("bogus1.vb", "{406EA660-64CF-4C82-B6F0-42D48172A799}", "ab007f1d23")

]]>
        </file>
    </compilation>, options:=TestOptions.DebugDll)


            CompileAndVerify(other).VerifyDiagnostics(
                Diagnostic(ERRID.WRN_MultipleDeclFileExtChecksum, "#ExternalChecksum(""bogus1.vb"", ""{406EA660-64CF-4C82-B6F0-42D48172A799}"", ""ab007f1d23"")").WithArguments("bogus1.vb")
            )

        End Sub

        <Fact>
        Public Sub CheckSumDirectiveFullWidth()
            Dim other As VisualBasicCompilation = CreateCompilationWithMscorlib(
    <compilation>
        <file name="a.vb"><![CDATA[
Public Class C
 Friend Sub Foo()
 End Sub
End Class

#ExternalChecksum("bogus1.vb", "{406EA660-64CF-4C82-B6F0-42D48172A79A}", "Ａb007f1d23dd")
' fullwidth digit in checksum - Ok and matches
#ExternalChecksum("bogus1.vb", "{406EA660-64CF-4C82-B6F0-42D48172A79A}", "Ab007f1d23dd")

' fullwidth in guid - invalid guid format and ignored (same behavior as in Dev12)
#ExternalChecksum("bogus1.vb", "{406EA660-64CF-4C82-B6F0-42D48172A79Ａ}", "Ab007f1d23dd")

]]>
        </file>
    </compilation>, OutputKind.DynamicallyLinkedLibrary)

            CompileAndVerify(other).VerifyDiagnostics(
                Diagnostic(ERRID.WRN_BadGUIDFormatExtChecksum, """{406EA660-64CF-4C82-B6F0-42D48172A79Ａ}""")
            )

        End Sub

        <WorkItem(729235, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/729235")>
        <Fact>
        Public Sub NormalizedPath_Tree()
            Dim source = <![CDATA[
Class C
    Sub M
    End Sub
End Class
]]>

            Dim comp = CreateCompilationWithChecksums(source, "b.vb", "b:\base")
            comp.VerifyDiagnostics()

            ' Only actually care about value of name attribute in file element.
            comp.VerifyPdb("C.M",
<symbols>
    <files>
        <file id="1" name="b:\base\b.vb" language="3a12d0b8-c26c-11d0-b442-00a0244a1dd2" languageVendor="994b45c4-e6e9-11d2-903f-00c04fa302a1" documentType="5a869d0b-6611-11d3-bd2a-0000f80849bd" checkSumAlgorithmId="ff1816ec-aa5e-4d10-87f7-6f4963833460" checkSum="90, B2, 29, 4D,  5, C7, A7, 47, 73,  0, EF, F4, 75, 92, E5, 84, E4, 4A, BB, E4, "/>
    </files>
    <methods>
        <method containingType="C" name="M">
            <sequencePoints>
                <entry offset="0x0" startLine="3" startColumn="5" endLine="3" endColumn="10" document="1"/>
                <entry offset="0x1" startLine="4" startColumn="5" endLine="4" endColumn="12" document="1"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x2">
                <currentnamespace name=""/>
            </scope>
        </method>
    </methods>
</symbols>)
        End Sub

        <WorkItem(729235, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/729235")>
        <Fact>
        Public Sub NormalizedPath_ExternalSource()
            Dim source = <![CDATA[
Class C
    Sub M
        M()
#ExternalSource("line.vb", 1)
        M()
#End ExternalSource
#ExternalSource("./line.vb", 2)
        M()
#End ExternalSource
#ExternalSource(".\line.vb", 3)
        M()
#End ExternalSource
#ExternalSource("q\..\line.vb", 4)
        M()
#End ExternalSource
#ExternalSource("q:\absolute\line.vb", 5)
        M()
#End ExternalSource
    End Sub
End Class
]]>

            Dim comp = CreateCompilationWithChecksums(source, "b.vb", "b:\base")

            ' Care about the fact that there's a single file element for "line.vb" and it has an absolute path.
            ' Care about the fact that the path that was already absolute wasn't affected by the base directory.
            ' Care about the fact that there is no document for b.vb
            comp.VerifyPdb("C.M",
<symbols>
    <files>
        <file id="1" name="b:\base\line.vb" language="3a12d0b8-c26c-11d0-b442-00a0244a1dd2" languageVendor="994b45c4-e6e9-11d2-903f-00c04fa302a1" documentType="5a869d0b-6611-11d3-bd2a-0000f80849bd"/>
        <file id="2" name="q:\absolute\line.vb" language="3a12d0b8-c26c-11d0-b442-00a0244a1dd2" languageVendor="994b45c4-e6e9-11d2-903f-00c04fa302a1" documentType="5a869d0b-6611-11d3-bd2a-0000f80849bd"/>
    </files>
    <methods>
        <method containingType="C" name="M">
            <sequencePoints>
                <entry offset="0x0" hidden="true" document="1"/>
                <entry offset="0x1" hidden="true" document="1"/>
                <entry offset="0x8" startLine="1" startColumn="9" endLine="1" endColumn="12" document="1"/>
                <entry offset="0xf" startLine="2" startColumn="9" endLine="2" endColumn="12" document="1"/>
                <entry offset="0x16" startLine="3" startColumn="9" endLine="3" endColumn="12" document="1"/>
                <entry offset="0x1d" startLine="4" startColumn="9" endLine="4" endColumn="12" document="1"/>
                <entry offset="0x24" startLine="5" startColumn="9" endLine="5" endColumn="12" document="2"/>
                <entry offset="0x2b" hidden="true" document="2"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x2c">
                <currentnamespace name=""/>
            </scope>
        </method>
    </methods>
</symbols>)
        End Sub

        <WorkItem(729235, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/729235")>
        <Fact>
        Public Sub NormalizedPath_ExternalChecksum()
            Dim source = <![CDATA[
Class C
#ExternalChecksum("a.vb", "{406EA660-64CF-4C82-B6F0-42D48172A79A}", "Ａb007f1d23da")
#ExternalChecksum("./b.vb", "{406EA660-64CF-4C82-B6F0-42D48172A79A}", "Ａb007f1d23db")
#ExternalChecksum(".\c.vb", "{406EA660-64CF-4C82-B6F0-42D48172A79A}", "Ａb007f1d23dc")
#ExternalChecksum("q\..\d.vb", "{406EA660-64CF-4C82-B6F0-42D48172A79A}", "Ａb007f1d23dd")
#ExternalChecksum("b:\base\e.vb", "{406EA660-64CF-4C82-B6F0-42D48172A79A}", "Ａb007f1d23de")
    Sub M
        M()
#ExternalSource("a.vb", 1)
        M()
#End ExternalSource
#ExternalSource("b.vb", 2)
        M()
#End ExternalSource
#ExternalSource("c.vb", 3)
        M()
#End ExternalSource
#ExternalSource("d.vb", 4)
        M()
#End ExternalSource
#ExternalSource("e.vb", 5)
        M()
#End ExternalSource
    End Sub
End Class
]]>

            Dim comp = CreateCompilationWithChecksums(source, "file.vb", "b:\base")

            ' Care about the fact that all pragmas are referenced, even though the paths differ before normalization.
            ' Care about the fact that there is no document reference to b:\base\file.vb
            comp.VerifyPdb("C.M",
<symbols>
    <files>
        <file id="1" name="b:\base\a.vb" language="3a12d0b8-c26c-11d0-b442-00a0244a1dd2" languageVendor="994b45c4-e6e9-11d2-903f-00c04fa302a1" documentType="5a869d0b-6611-11d3-bd2a-0000f80849bd" checkSumAlgorithmId="406ea660-64cf-4c82-b6f0-42d48172a79a" checkSum="AB,  0, 7F, 1D, 23, DA, "/>
        <file id="2" name="b:\base\b.vb" language="3a12d0b8-c26c-11d0-b442-00a0244a1dd2" languageVendor="994b45c4-e6e9-11d2-903f-00c04fa302a1" documentType="5a869d0b-6611-11d3-bd2a-0000f80849bd" checkSumAlgorithmId="406ea660-64cf-4c82-b6f0-42d48172a79a" checkSum="AB,  0, 7F, 1D, 23, DB, "/>
        <file id="3" name="b:\base\c.vb" language="3a12d0b8-c26c-11d0-b442-00a0244a1dd2" languageVendor="994b45c4-e6e9-11d2-903f-00c04fa302a1" documentType="5a869d0b-6611-11d3-bd2a-0000f80849bd" checkSumAlgorithmId="406ea660-64cf-4c82-b6f0-42d48172a79a" checkSum="AB,  0, 7F, 1D, 23, DC, "/>
        <file id="4" name="b:\base\d.vb" language="3a12d0b8-c26c-11d0-b442-00a0244a1dd2" languageVendor="994b45c4-e6e9-11d2-903f-00c04fa302a1" documentType="5a869d0b-6611-11d3-bd2a-0000f80849bd" checkSumAlgorithmId="406ea660-64cf-4c82-b6f0-42d48172a79a" checkSum="AB,  0, 7F, 1D, 23, DD, "/>
        <file id="5" name="b:\base\e.vb" language="3a12d0b8-c26c-11d0-b442-00a0244a1dd2" languageVendor="994b45c4-e6e9-11d2-903f-00c04fa302a1" documentType="5a869d0b-6611-11d3-bd2a-0000f80849bd" checkSumAlgorithmId="406ea660-64cf-4c82-b6f0-42d48172a79a" checkSum="AB,  0, 7F, 1D, 23, DE, "/>
    </files>
    <methods>
        <method containingType="C" name="M">
            <sequencePoints>
                <entry offset="0x0" hidden="true" document="1"/>
                <entry offset="0x1" hidden="true" document="1"/>
                <entry offset="0x8" startLine="1" startColumn="9" endLine="1" endColumn="12" document="1"/>
                <entry offset="0xf" startLine="2" startColumn="9" endLine="2" endColumn="12" document="2"/>
                <entry offset="0x16" startLine="3" startColumn="9" endLine="3" endColumn="12" document="3"/>
                <entry offset="0x1d" startLine="4" startColumn="9" endLine="4" endColumn="12" document="4"/>
                <entry offset="0x24" startLine="5" startColumn="9" endLine="5" endColumn="12" document="5"/>
                <entry offset="0x2b" hidden="true" document="5"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x2c">
                <currentnamespace name=""/>
            </scope>
        </method>
    </methods>
</symbols>)
        End Sub

        <WorkItem(729235, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/729235")>
        <Fact>
        Public Sub NormalizedPath_NoBaseDirectory()
            Dim source = <![CDATA[
Class C
#ExternalChecksum("a.vb", "{406EA660-64CF-4C82-B6F0-42D48172A79A}", "Ａb007f1d23da")
    Sub M
        M()
#ExternalSource("a.vb", 1)
        M()
#End ExternalSource
#ExternalSource("./a.vb", 2)
        M()
#End ExternalSource
#ExternalSource("b.vb", 3)
        M()
#End ExternalSource
    End Sub
End Class
]]>

            Dim comp = CreateCompilationWithChecksums(source, "file.vb", Nothing)

            ' Verify that nothing blew up.
            ' Care about the fact that there is no document reference to file.vb
            comp.VerifyPdb("C.M",
<symbols>
    <files>
        <file id="1" name="a.vb" language="3a12d0b8-c26c-11d0-b442-00a0244a1dd2" languageVendor="994b45c4-e6e9-11d2-903f-00c04fa302a1" documentType="5a869d0b-6611-11d3-bd2a-0000f80849bd" checkSumAlgorithmId="406ea660-64cf-4c82-b6f0-42d48172a79a" checkSum="AB,  0, 7F, 1D, 23, DA, "/>
        <file id="2" name="./a.vb" language="3a12d0b8-c26c-11d0-b442-00a0244a1dd2" languageVendor="994b45c4-e6e9-11d2-903f-00c04fa302a1" documentType="5a869d0b-6611-11d3-bd2a-0000f80849bd"/>
        <file id="3" name="b.vb" language="3a12d0b8-c26c-11d0-b442-00a0244a1dd2" languageVendor="994b45c4-e6e9-11d2-903f-00c04fa302a1" documentType="5a869d0b-6611-11d3-bd2a-0000f80849bd"/>
    </files>
    <methods>
        <method containingType="C" name="M">
            <sequencePoints>
                <entry offset="0x0" hidden="true" document="1"/>
                <entry offset="0x1" hidden="true" document="1"/>
                <entry offset="0x8" startLine="1" startColumn="9" endLine="1" endColumn="12" document="1"/>
                <entry offset="0xf" startLine="2" startColumn="9" endLine="2" endColumn="12" document="2"/>
                <entry offset="0x16" startLine="3" startColumn="9" endLine="3" endColumn="12" document="3"/>
                <entry offset="0x1d" hidden="true" document="3"/>
            </sequencePoints>
            <scope startOffset="0x0" endOffset="0x1e">
                <currentnamespace name=""/>
            </scope>
        </method>
    </methods>
</symbols>)
        End Sub

    End Class

End Namespace
