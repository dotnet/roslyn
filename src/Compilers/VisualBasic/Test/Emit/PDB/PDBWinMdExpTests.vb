' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.IO
Imports System.Text
Imports System.Xml
Imports Roslyn.Test.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests.PDB
    Public Class PDBWinMdExpTests
        Inherits BasicTestBase

        <ConditionalFact(GetType(WindowsOnly), Reason:=ConditionalSkipReason.NativePdbRequiresDesktop)>
        Public Sub TestWinMdExpData_Empty()
            Dim source =
<compilation>
    <file name="a.vb"><![CDATA[
Imports System
]]></file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(source, TestOptions.ReleaseDll.WithOutputKind(OutputKind.WindowsRuntimeMetadata))

            Dim actual = PdbTestUtilities.GetTokenToLocationMap(compilation, True)

            Dim expected =
<?xml version="1.0" encoding="utf-16"?>
<token-map/>

            AssertEqual(expected, actual)
        End Sub

        <ConditionalFact(GetType(WindowsOnly), Reason:=ConditionalSkipReason.NativePdbRequiresDesktop)>
        Public Sub TestWinMdExpData_Basic()
            Dim source =
<compilation>
    <file name="a.vb"><![CDATA[
Imports System

Module Module1
    Sub Main(args As String())

        Dim x As Integer = 0
        Do While x < 5
            If x < 1 Then
                Console.WriteLine("<1")
            ElseIf x < 2 Then
                Dim s2 As String = "<2"
                Console.WriteLine(s2)
            ElseIf x < 3 Then
                Dim s3 As String = "<3"
                Console.WriteLine(s3)
            Else
                Dim e1 As String = "Else"
                Console.WriteLine(e1)
            End If

            Dim newX As Integer = x + 1
            x = newX
        Loop

    End Sub
End Module
]]></file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(source, TestOptions.ReleaseDll.WithOutputKind(OutputKind.WindowsRuntimeMetadata))

            Dim actual = PdbTestUtilities.GetTokenToLocationMap(compilation, True)

            Dim expected =
<?xml version="1.0" encoding="utf-16"?>
<token-map>
    <token-location token="0x02xxxxxx" file="a.vb" start-line="3" start-column="8" end-line="3" end-column="15"/>
    <token-location token="0x06xxxxxx" file="a.vb" start-line="4" start-column="9" end-line="4" end-column="13"/>
</token-map>

            AssertEqual(expected, actual)
        End Sub

        <WorkItem(693206, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/693206")>
        <ConditionalFact(GetType(WindowsOnly), Reason:=ConditionalSkipReason.NativePdbRequiresDesktop)>
        Public Sub Bug693206()
            Dim source =
<compilation>
    <file name="a.vb"><![CDATA[
Imports System

Namespace X
    Module Module1
        Enum E
            One
        End Enum 
    End Module
End Namespace
]]></file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(source, TestOptions.ReleaseDll.WithOutputKind(OutputKind.WindowsRuntimeMetadata))

            Dim actual = PdbTestUtilities.GetTokenToLocationMap(compilation, True)

            Dim expected =
<?xml version="1.0" encoding="utf-16"?>
<token-map>
    <token-location token="0x02xxxxxx" file="a.vb" start-line="4" start-column="12" end-line="4" end-column="19"/>
    <token-location token="0x02xxxxxx" file="a.vb" start-line="5" start-column="14" end-line="5" end-column="15"/>
    <token-location token="0x04xxxxxx" file="a.vb" start-line="5" start-column="14" end-line="5" end-column="15"/>
    <token-location token="0x04xxxxxx" file="a.vb" start-line="6" start-column="13" end-line="6" end-column="16"/>
</token-map>

            AssertEqual(expected, actual)
        End Sub

        <ConditionalFact(GetType(WindowsOnly), Reason:=ConditionalSkipReason.NativePdbRequiresDesktop)>
        Public Sub TestWinMdExpData_Property_Event()
            Dim source =
<compilation>
    <file name="a.vb"><![CDATA[
Imports System

Namespace X
    Public Delegate Sub D(i As Integer)

    Public Class Bar
        Shared Sub New()
        End Sub

        Sub New(i As Integer)
        End Sub

        Public Event E As D
        Public Event E2 As action

        Public Property P As Integer

        Public Property P2 As Integer
            Get
                Return Nothing
            End Get
            Set(value As Integer)
            End Set
        End Property

        Default Public Property P3(i As Integer) As Integer
            Get
                Return Nothing
            End Get
            Set(value As Integer)
            End Set
        End Property
    End Class
End Namespace
]]></file>
</compilation>

            Dim compilation =
                CompilationUtils.CreateEmptyCompilationWithReferences(
                    source,
                    LatestVbReferences,
                    options:=TestOptions.ReleaseDll.WithOutputKind(OutputKind.WindowsRuntimeMetadata))
            CompilationUtils.AssertNoDiagnostics(compilation)

            Dim actual = PdbTestUtilities.GetTokenToLocationMap(compilation, True)

            Dim expected =
<?xml version="1.0" encoding="utf-16"?>
<token-map>
    <token-location token="0x02xxxxxx" file="a.vb" start-line="4" start-column="25" end-line="4" end-column="26"/>
    <token-location token="0x06xxxxxx" file="a.vb" start-line="4" start-column="25" end-line="4" end-column="26"/>
    <token-location token="0x06xxxxxx" file="a.vb" start-line="4" start-column="25" end-line="4" end-column="26"/>
    <token-location token="0x02xxxxxx" file="a.vb" start-line="6" start-column="18" end-line="6" end-column="21"/>
    <token-location token="0x06xxxxxx" file="a.vb" start-line="7" start-column="20" end-line="7" end-column="23"/>
    <token-location token="0x06xxxxxx" file="a.vb" start-line="10" start-column="13" end-line="10" end-column="16"/>
    <token-location token="0x04xxxxxx" file="a.vb" start-line="13" start-column="22" end-line="13" end-column="23"/>
    <token-location token="0x06xxxxxx" file="a.vb" start-line="13" start-column="22" end-line="13" end-column="23"/>
    <token-location token="0x06xxxxxx" file="a.vb" start-line="13" start-column="22" end-line="13" end-column="23"/>
    <token-location token="0x14xxxxxx" file="a.vb" start-line="13" start-column="22" end-line="13" end-column="23"/>
    <token-location token="0x04xxxxxx" file="a.vb" start-line="14" start-column="22" end-line="14" end-column="24"/>
    <token-location token="0x06xxxxxx" file="a.vb" start-line="14" start-column="22" end-line="14" end-column="24"/>
    <token-location token="0x06xxxxxx" file="a.vb" start-line="14" start-column="22" end-line="14" end-column="24"/>
    <token-location token="0x14xxxxxx" file="a.vb" start-line="14" start-column="22" end-line="14" end-column="24"/>
    <token-location token="0x04xxxxxx" file="a.vb" start-line="16" start-column="25" end-line="16" end-column="26"/>
    <token-location token="0x06xxxxxx" file="a.vb" start-line="16" start-column="25" end-line="16" end-column="26"/>
    <token-location token="0x06xxxxxx" file="a.vb" start-line="16" start-column="25" end-line="16" end-column="26"/>
    <token-location token="0x17xxxxxx" file="a.vb" start-line="16" start-column="25" end-line="16" end-column="26"/>
    <token-location token="0x17xxxxxx" file="a.vb" start-line="18" start-column="25" end-line="18" end-column="27"/>
    <token-location token="0x06xxxxxx" file="a.vb" start-line="19" start-column="13" end-line="19" end-column="16"/>
    <token-location token="0x06xxxxxx" file="a.vb" start-line="22" start-column="13" end-line="22" end-column="16"/>
    <token-location token="0x17xxxxxx" file="a.vb" start-line="26" start-column="33" end-line="26" end-column="35"/>
    <token-location token="0x06xxxxxx" file="a.vb" start-line="27" start-column="13" end-line="27" end-column="16"/>
    <token-location token="0x06xxxxxx" file="a.vb" start-line="30" start-column="13" end-line="30" end-column="16"/>
</token-map>

            AssertEqual(expected, actual)
        End Sub

        Private Shared Sub AssertEqual(expected As System.Xml.Linq.XDocument, actual As String)
            Dim builder As New StringBuilder
            Dim writer As New System.Xml.XmlTextWriter(New StringWriter(builder))
            writer.Formatting = Formatting.Indented
            expected.WriteTo(writer)
            Assert.Equal(builder.ToString(), actual)
        End Sub

        <ConditionalFact(GetType(WindowsOnly), Reason:=ConditionalSkipReason.NativePdbRequiresDesktop)>
        Public Sub TestWinMdExpData_AnonymousTypes()
            Dim source =
<compilation>
    <file name="a.vb"><![CDATA[
Imports System

Namespace X
    Public Class Bar
        Public Shared Sub S()
			Dim a = New With { .x = 1, .y = New With { .a = 1 } }
			Dim b = New With { .t = New With { .t = New With { .t = New With { .t = New With { .a = 1 } } } } }
		End Sub
    End Class
End Namespace
]]></file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(source, TestOptions.ReleaseDll.WithOutputKind(OutputKind.WindowsRuntimeMetadata))

            Dim actual = PdbTestUtilities.GetTokenToLocationMap(compilation, True)

            Dim expected =
<?xml version="1.0" encoding="utf-16"?>
<token-map>
    <token-location token="0x02xxxxxx" file="a.vb" start-line="4" start-column="18" end-line="4" end-column="21"/>
    <token-location token="0x06xxxxxx" file="a.vb" start-line="4" start-column="18" end-line="4" end-column="21"/>
    <token-location token="0x06xxxxxx" file="a.vb" start-line="5" start-column="27" end-line="5" end-column="28"/>
</token-map>

            AssertEqual(expected, actual)
        End Sub

    End Class
End Namespace
