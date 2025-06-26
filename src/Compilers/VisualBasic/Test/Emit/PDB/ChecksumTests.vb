' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.IO
Imports Microsoft.CodeAnalysis.Emit
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.CodeAnalysis.Text
Imports Roslyn.Test.Utilities

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
            Dim other As VisualBasicCompilation = CreateCompilationWithMscorlib40(
    <compilation>
        <file name="a.vb"><![CDATA[
Public Class C
 Friend Sub Goo()
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
            Dim other As VisualBasicCompilation = CreateCompilationWithMscorlib40(
    <compilation>
        <file name="a.vb"><![CDATA[
Public Class C
 Friend Sub Goo()
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
            Dim other As VisualBasicCompilation = CreateCompilationWithMscorlib40(
    <compilation>
        <file name="a.vb"><![CDATA[
Public Class C
 Friend Sub Goo()
 End Sub
End Class

' same
#ExternalChecksum("bogus1.vb", "{406EA660-64CF-4C82-B6F0-42D48172A799}", "ab007f1d23d9")

#ExternalChecksum("bogus1.vb", "{406EA660-64CF-4C82-B6F0-42D48172A799}", "ab007f1d23d9")

]]>
        </file>
        <file name="a.vb"><![CDATA[
Public Class D
 Friend Sub Goo()
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
            Dim other As VisualBasicCompilation = CreateCompilationWithMscorlib40(
    <compilation>
        <file name="a.vb"><![CDATA[
Public Class C
 Friend Sub Goo()
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
        <ConditionalFact(GetType(WindowsOnly), Reason:=ConditionalSkipReason.TestHasWindowsPaths)>
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
        <file id="1" name="b:\base\b.vb" language="VB" checksumAlgorithm="SHA1" checksum="90-B2-29-4D-05-C7-A7-47-73-00-EF-F4-75-92-E5-84-E4-4A-BB-E4"/>
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
        <WorkItem(50611, "https://github.com/dotnet/roslyn/issues/50611")>
        <ConditionalFact(GetType(WindowsOnly), Reason:=ConditionalSkipReason.TestHasWindowsPaths)>
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
        <file id="1" name="b:\base\b.vb" language="VB" checksumAlgorithm="SHA1" checksum="F9-90-00-9D-9E-45-97-F2-3D-67-1C-D8-47-A8-9B-DA-4A-91-AA-7F"/>
        <file id="2" name="b:\base\line.vb" language="VB"/>
        <file id="3" name="q:\absolute\line.vb" language="VB"/>
    </files>
    <methods>
        <method containingType="C" name="M">
            <sequencePoints>
                <entry offset="0x0" hidden="true" document="2"/>
                <entry offset="0x1" hidden="true" document="2"/>
                <entry offset="0x8" startLine="1" startColumn="9" endLine="1" endColumn="12" document="2"/>
                <entry offset="0xf" startLine="2" startColumn="9" endLine="2" endColumn="12" document="2"/>
                <entry offset="0x16" startLine="3" startColumn="9" endLine="3" endColumn="12" document="2"/>
                <entry offset="0x1d" startLine="4" startColumn="9" endLine="4" endColumn="12" document="2"/>
                <entry offset="0x24" startLine="5" startColumn="9" endLine="5" endColumn="12" document="3"/>
                <entry offset="0x2b" hidden="true" document="3"/>
            </sequencePoints>
        </method>
    </methods>
</symbols>, format:=DebugInformationFormat.PortablePdb)
        End Sub

        <WorkItem(729235, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/729235")>
        <ConditionalFact(GetType(WindowsOnly), Reason:=ConditionalSkipReason.TestHasWindowsPaths)>
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
        <file id="1" name="b:\base\file.vb" language="VB" checksumAlgorithm="SHA1" checksum="C2-46-C6-34-F6-20-D3-FE-28-B9-D8-62-0F-A9-FB-2F-89-E7-48-23"/>
        <file id="2" name="b:\base\a.vb" language="VB" checksumAlgorithm="406ea660-64cf-4c82-b6f0-42d48172a79a" checksum="AB-00-7F-1D-23-DA"/>
        <file id="3" name="b:\base\b.vb" language="VB" checksumAlgorithm="406ea660-64cf-4c82-b6f0-42d48172a79a" checksum="AB-00-7F-1D-23-DB"/>
        <file id="4" name="b:\base\c.vb" language="VB" checksumAlgorithm="406ea660-64cf-4c82-b6f0-42d48172a79a" checksum="AB-00-7F-1D-23-DC"/>
        <file id="5" name="b:\base\d.vb" language="VB" checksumAlgorithm="406ea660-64cf-4c82-b6f0-42d48172a79a" checksum="AB-00-7F-1D-23-DD"/>
        <file id="6" name="b:\base\e.vb" language="VB" checksumAlgorithm="406ea660-64cf-4c82-b6f0-42d48172a79a" checksum="AB-00-7F-1D-23-DE"/>
    </files>
    <methods>
        <method containingType="C" name="M">
            <sequencePoints>
                <entry offset="0x0" hidden="true" document="2"/>
                <entry offset="0x1" hidden="true" document="2"/>
                <entry offset="0x8" startLine="1" startColumn="9" endLine="1" endColumn="12" document="2"/>
                <entry offset="0xf" startLine="2" startColumn="9" endLine="2" endColumn="12" document="3"/>
                <entry offset="0x16" startLine="3" startColumn="9" endLine="3" endColumn="12" document="4"/>
                <entry offset="0x1d" startLine="4" startColumn="9" endLine="4" endColumn="12" document="5"/>
                <entry offset="0x24" startLine="5" startColumn="9" endLine="5" endColumn="12" document="6"/>
                <entry offset="0x2b" hidden="true" document="6"/>
            </sequencePoints>
        </method>
    </methods>
</symbols>, format:=DebugInformationFormat.PortablePdb)
        End Sub

        <ConditionalFact(GetType(WindowsOnly), Reason:=ConditionalSkipReason.TestHasWindowsPaths)>
        <WorkItem(50611, "https://github.com/dotnet/roslyn/issues/50611")>
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
        <file id="1" name="file.vb" language="VB" checksumAlgorithm="SHA1" checksum="23-C1-6B-94-B0-D4-06-26-C8-D2-82-21-63-07-53-11-4D-5A-02-BC"/>
        <file id="2" name="a.vb" language="VB" checksumAlgorithm="406ea660-64cf-4c82-b6f0-42d48172a79a" checksum="AB-00-7F-1D-23-DA"/>
        <file id="3" name="./a.vb" language="VB"/>
        <file id="4" name="b.vb" language="VB"/>
    </files>
    <methods>
        <method containingType="C" name="M">
            <sequencePoints>
                <entry offset="0x0" hidden="true" document="2"/>
                <entry offset="0x1" hidden="true" document="2"/>
                <entry offset="0x8" startLine="1" startColumn="9" endLine="1" endColumn="12" document="2"/>
                <entry offset="0xf" startLine="2" startColumn="9" endLine="2" endColumn="12" document="3"/>
                <entry offset="0x16" startLine="3" startColumn="9" endLine="3" endColumn="12" document="4"/>
                <entry offset="0x1d" hidden="true" document="4"/>
            </sequencePoints>
        </method>
    </methods>
</symbols>, format:=DebugInformationFormat.PortablePdb)
        End Sub

    End Class

End Namespace
